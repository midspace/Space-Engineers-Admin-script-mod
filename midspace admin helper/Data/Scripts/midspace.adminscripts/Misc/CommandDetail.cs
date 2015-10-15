namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using VRage.ModAPI;
    using VRage.Voxels;
    using VRageMath;

    public class CommandDetail : ChatCommand
    {
        public CommandDetail()
            : base(ChatCommandSecurity.Admin, "detail", new[] { "/detail" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/detail", "Provides detail on the object the player is looking at.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.Equals("/detail", StringComparison.InvariantCultureIgnoreCase))
            {
                IMyEntity entity;
                double distance;
                Vector3D hitPoint;
                Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, out entity, out distance, out hitPoint, true, true, true, true, true);
                if (entity != null)
                {
                    string displayType;
                    string displayName;
                    string description;
                    if (entity is IMyVoxelMap)
                    {
                        var voxelMap = (IMyVoxelMap)entity;
                        displayType = "asteroid";
                        displayName = voxelMap.StorageName;
                        var aabb = new BoundingBoxD(voxelMap.PositionLeftBottomCorner, voxelMap.PositionLeftBottomCorner + voxelMap.Storage.Size);

                        //float GetVoxelContentInBoundingBox(BoundingBoxD worldAabb, out float cellCount);  // Do we bother with this detail?

                        if (voxelMap.Storage.Size.RectangularLength() >= 1536) // 512x512x512 or bigger.
                        {
                            MyAPIGateway.Utilities.ShowMessage("Asteroid", "{0} is too big to load immediately, so processing is occurring in the background. Details will appear when finished.", displayName);

                            // use IMyParallelTask to process in the background.
                            MyAPIGateway.Parallel.StartBackground(
                                delegate() { ProcessAsteroid(displayType, displayName, voxelMap, distance, aabb); });
                        }
                        else
                        {
                            ProcessAsteroid(displayType, displayName, voxelMap, distance, aabb);
                        }
                    }
                    else if (entity is Sandbox.Game.Entities.MyPlanet)
                    {
                        var planet = (Sandbox.Game.Entities.MyPlanet)entity;
                        displayType = "planet";
                        displayName = planet.StorageName;
                        description = string.Format("Distance: {0:N}\r\nMinimum Surface Radius: {1:N}\r\nAtmosphere Radius: {2:N}\r\nHas Atmosphere: {3}",
                            distance,
                            planet.MinimumSurfaceRadius,
                            planet.AtmosphereRadius,
                            planet.HasAtmosphere);
                        MyAPIGateway.Utilities.ShowMissionScreen(string.Format("ID {0}:", displayType), string.Format("'{0}'", displayName), " ", description, null, "OK");
                    }
                    else if (entity is IMyCubeBlock || entity is IMyCubeGrid)
                    {
                        IMyCubeGrid gridCube;
                        IMyCubeBlock cubeBlock = null;

                        if (entity is IMyCubeGrid)
                            gridCube = (IMyCubeGrid)entity;
                        else
                        {
                            cubeBlock = (IMyCubeBlock)entity;
                            gridCube = (IMyCubeGrid)cubeBlock.GetTopMostParent();
                        }

                        var attachedGrids = gridCube.GetAttachedGrids();
                        var blocks = new List<IMySlimBlock>();
                        gridCube.GetBlocks(blocks);
                        //var cockpits = entity.FindWorkingCockpits(); // TODO: determine if any cockpits are occupied.


                        var identities = new List<IMyIdentity>();
                        MyAPIGateway.Players.GetAllIdentites(identities);
                        var ownerCounts = new Dictionary<long, long>();

                        foreach (var block in blocks.Where(f => f.FatBlock != null && f.FatBlock.OwnerId != 0))
                        {
                            if (ownerCounts.ContainsKey(block.FatBlock.OwnerId))
                                ownerCounts[block.FatBlock.OwnerId]++;
                            else
                                ownerCounts.Add(block.FatBlock.OwnerId, 1);
                        }

                        var ownerList = new List<string>();
                        foreach (var ownerKvp in ownerCounts)
                        {
                            var owner = identities.FirstOrDefault(p => p.PlayerId == ownerKvp.Key);
                            if (owner == null)
                                continue;
                            ownerList.Add(string.Format("{0} [{1}]", owner.DisplayName, ownerKvp.Value));
                        }

                        //var damage = new StringBuilder();
                        //var buildComplete = new StringBuilder();
                        var incompleteBlocks = 0;

                        foreach (var block in blocks)
                        {
                            //damage.    cube.IntegrityPercent <= cube.BuildPercent;
                            //complete.    cube.BuildPercent;

                            // This information does not appear to work.
                            // Unsure if the API is broken, incomplete , or a temporary bug under 01.070.
                            //damage.AppendFormat("D={0:N} ", block.DamageRatio);  
                            //damage.AppendFormat("A={0:N} ", block.AccumulatedDamage);

                            if (!block.IsFullIntegrity)
                            {
                                incompleteBlocks++;
                                //buildComplete.AppendFormat("B={0:N} ", block.BuildLevelRatio);
                                //buildComplete.AppendFormat("I={0:N} ", block.BuildIntegrity);
                                //buildComplete.AppendFormat("M={0:N} ", block.MaxIntegrity);
                            }
                        }

                        displayType = gridCube.IsStatic ? "Station" : gridCube.GridSizeEnum.ToString() + " Ship";
                        displayName = gridCube.DisplayName;

                        description = string.Format("Distance: {0:N} m\r\n",
                            distance);

                        if (gridCube.Physics == null)
                            description += string.Format("Projection has no physics characteristics.\r\n");
                        else
                            description += string.Format("Mass: {0:N} kg\r\nVector: {1}\r\nVelocity: {2:N} m/s\r\nMass Center: {3}\r\n",
                                gridCube.Physics.Mass,
                                gridCube.Physics.LinearVelocity,
                                gridCube.Physics.LinearVelocity.Length(),
                                gridCube.Physics.CenterOfMassWorld);

                        description += string.Format("Size : {0}\r\nNumber of Blocks : {1:#,##0}\r\nAttached Grids : {2:#,##0} (including this one).\r\nOwners : {3}\r\nBuild : {4} blocks incomplete.",
                            gridCube.LocalAABB.Size,
                            blocks.Count,
                            attachedGrids.Count,
                            String.Join(", ", ownerList),
                            incompleteBlocks);

                        if (cubeBlock != null)
                        {
                            string ownerName = "";
                            var owner = identities.FirstOrDefault(p => p.PlayerId == cubeBlock.OwnerId);
                            if (owner != null)
                                ownerName = owner.DisplayName;
                            description += string.Format("\r\n\r\nCube;\r\n  Type : {0}\r\n  Name : {1}\r\n  Owner : {2}", cubeBlock.DefinitionDisplayNameText, cubeBlock.DisplayNameText, ownerName);
                        }

                        MyAPIGateway.Utilities.ShowMissionScreen(string.Format("ID {0}:", displayType), string.Format("'{0}'", displayName), " ", description, null, "OK");
                    }
                    else
                    {
                        displayType = "player";
                        displayName = entity.DisplayName;
                        description = string.Format("Distance: {0:N}", distance);
                        MyAPIGateway.Utilities.ShowMissionScreen(string.Format("ID {0}:", displayType), string.Format("'{0}'", displayName), " ", description, null, "OK");
                    }

                    return true;
                }

                MyAPIGateway.Utilities.ShowMessage("ID", "Could not find object.");
                return true;
            }

            return false;
        }

        private void ProcessAsteroid(string displayType, string displayName, IMyVoxelMap voxelMap, double distance, BoundingBoxD aabb)
        {
            Vector3I min = Vector3I.MaxValue;
            Vector3I max = Vector3I.MinValue;
            Vector3I block;
            Dictionary<byte, long> assetCount = new Dictionary<byte, long>();

            // read the asteroid in chunks of 64 to avoid the Arithmetic overflow issue.
            for (block.Z = 0; block.Z < voxelMap.Storage.Size.Z; block.Z += 64)
                for (block.Y = 0; block.Y < voxelMap.Storage.Size.Y; block.Y += 64)
                    for (block.X = 0; block.X < voxelMap.Storage.Size.X; block.X += 64)
                    {
                        var cacheSize = new Vector3I(64);
                        var oldCache = new MyStorageData();
                        oldCache.Resize(cacheSize);
                        // LOD1 is not detailed enough for content information on asteroids.
                        voxelMap.Storage.ReadRange(oldCache, MyStorageDataTypeFlags.ContentAndMaterial, 0, block, block + cacheSize - 1);

                        Vector3I p;
                        for (p.Z = 0; p.Z < cacheSize.Z; ++p.Z)
                            for (p.Y = 0; p.Y < cacheSize.Y; ++p.Y)
                                for (p.X = 0; p.X < cacheSize.X; ++p.X)
                                {
                                    var content = oldCache.Content(ref p);
                                    if (content > 0)
                                    {
                                        min = Vector3I.Min(min, p + block);
                                        max = Vector3I.Max(max, p + block + 1);

                                        var material = oldCache.Material(ref p);
                                        if (assetCount.ContainsKey(material))
                                            assetCount[material] += content;
                                        else
                                            assetCount.Add(material, content);
                                    }
                                }
                    }

            var assetNameCount = new Dictionary<string, long>();
            foreach (var kvp in assetCount)
            {
                var name = MyDefinitionManager.Static.GetVoxelMaterialDefinition(kvp.Key).Id.SubtypeName;
                if (assetNameCount.ContainsKey(name))
                    assetNameCount[name] += kvp.Value;
                else
                    assetNameCount.Add(name, kvp.Value);
            }
            assetNameCount = assetNameCount.OrderByDescending(e => e.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var sum = assetNameCount.Values.Sum();
            var ores = new StringBuilder();

            foreach (var kvp in assetNameCount)
                ores.AppendFormat("{0}  {1:N}  {2:P}\r\n", kvp.Key, (double)kvp.Value / 255, (double)kvp.Value / (double)sum);

            var contentBox = new BoundingBoxD(voxelMap.PositionLeftBottomCorner + min, voxelMap.PositionLeftBottomCorner + max);
            var description = string.Format("Distance: {0:N}\r\nSize: {1}\r\nBoundingBox Center: [X:{2:N} Y:{3:N} Z:{4:N}]\r\n\r\nContent Size:{5}\r\nLOD0 Content Center: [X:{6:N} Y:{7:N} Z:{8:N}]\r\n\r\nMaterial  Mass  Percent\r\n{9}",
                distance, voxelMap.Storage.Size,
                aabb.Center.X, aabb.Center.Y, aabb.Center.Z,
                max - min,
                contentBox.Center.X, contentBox.Center.Y, contentBox.Center.Z,
                ores);

            MyAPIGateway.Utilities.ShowMissionScreen(string.Format("ID {0}:", displayType), string.Format("'{0}'", displayName), " ", description, null, "OK");
        }
    }
}
