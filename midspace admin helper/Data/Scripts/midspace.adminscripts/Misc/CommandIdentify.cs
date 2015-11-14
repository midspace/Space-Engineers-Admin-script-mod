namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Sandbox.Definitions;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using VRage.ModAPI;
    using VRageMath;

    public class CommandIdentify : ChatCommand
    {
        /// <summary>
        /// Temporary cache created when player id's an item in game.
        /// </summary>
        public static IMyEntity IdentifyCache = null;

        public CommandIdentify()
            : base(ChatCommandSecurity.Admin, "id", new[] { "/id" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/id", "Identifies the name of the object the player is looking at.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/id", StringComparison.InvariantCultureIgnoreCase))
            {
                IMyEntity entity;
                double distance;
                Vector3D hitPoint;
                Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, out entity, out distance, out hitPoint, true, true, true, true, true, true);
                if (entity != null)
                {
                    IdentifyCache = entity;
                    string displayType;
                    string displayName;
                    string description;
                    if (entity is IMyVoxelMap)
                    {
                        var voxelMap = (IMyVoxelMap)entity;
                        displayType = "asteroid";
                        displayName = voxelMap.StorageName;
                        var aabb = new BoundingBoxD(voxelMap.PositionLeftBottomCorner, voxelMap.PositionLeftBottomCorner + voxelMap.Storage.Size);
                        description = string.Format("Distance: {0:N} m\r\nSize: {1}\r\nBoundingBox Center: [X:{2:N} Y:{3:N} Z:{4:N}]\r\n\r\nUse /detail for more information on asteroid content.",
                            distance, voxelMap.Storage.Size,
                            aabb.Center.X, aabb.Center.Y, aabb.Center.Z);

                        MyAPIGateway.Utilities.ShowMissionScreen(string.Format("ID {0}:", displayType), string.Format("'{0}'", displayName), " ", description, null, "OK");
                    }
                    else if (entity is Sandbox.Game.Entities.MyPlanet)
                    {
                        var planet = (Sandbox.Game.Entities.MyPlanet)entity;
                        displayType = "planet";
                        displayName = planet.StorageName;
                        description = string.Format("Distance: {0:N} m\r\nCenter: [X:{1:N} Y:{2:N} Z:{3:N}]\r\nMinimum Radius: {4:N} m\r\nAverage Radius: {5:N} m\r\nAtmosphere Radius: {6:N} m\r\nHas Atmosphere: {7}",
                            distance,
                            planet.WorldMatrix.Translation.X, planet.WorldMatrix.Translation.Y, planet.WorldMatrix.Translation.Z,
                            planet.MinimumRadius,
                            planet.AverageRadius,
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
                    else if (entity is IMyCharacter)
                    {
                        displayType = "player";
                        displayName = entity.DisplayName;
                        description = string.Format("Distance: {0:N} m", distance);
                        MyAPIGateway.Utilities.ShowMissionScreen(string.Format("ID {0}:", displayType), string.Format("'{0}'", displayName), " ", description, null, "OK");
                    }
                    else if (entity is MyReplicableEntity)
                    {
                        displayType = "Unknown";

                        var replicable = (MyReplicableEntity)entity;
                        if (replicable.DefinitionId.HasValue)
                        {
                            MyDefinitionBase definition;
                            if (MyDefinitionManager.Static.TryGetDefinition(replicable.DefinitionId.Value, out definition))
                                displayType = definition.Id.SubtypeName;
                        }

                        displayName = entity.DisplayName;
                        description = string.Format("Distance: {0:N} m", distance);
                        MyAPIGateway.Utilities.ShowMissionScreen(string.Format("ID {0}:", displayType), string.Format("'{0}'", displayName), " ", description, null, "OK");
                    }
                    else
                    {
                        displayType = "unknown";
                        displayName = entity.DisplayName;
                        description = string.Format("Distance: {0:N} m", distance);
                        MyAPIGateway.Utilities.ShowMissionScreen(string.Format("ID {0}:", displayType), string.Format("'{0}'", displayName), " ", description, null, "OK");
                    }

                    return true;
                }

                MyAPIGateway.Utilities.ShowMessage("ID", "Could not find object.");
                return true;
            }

            return false;
        }
    }
}
