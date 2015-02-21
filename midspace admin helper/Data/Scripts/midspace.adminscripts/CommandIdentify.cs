namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;

    using Sandbox.ModAPI;
    using VRage.Common.Voxels;
    using VRageMath;

    public class CommandIdentify : ChatCommand
    {
        public CommandIdentify()
            : base(ChatCommandSecurity.Admin, "id", new[] { "/id" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/id", "Identifies the name of the object the player is looking at.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.Equals("/id", StringComparison.InvariantCultureIgnoreCase))
            {
                IMyEntity entity;
                double distance;
                Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, out entity, out distance);
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
                    else if (entity is IMyCubeGrid)
                    {
                        var gridCube = (IMyCubeGrid)entity;
                        var attachedGrids = gridCube.GetAttachedGrids();
                        var blocks = new List<IMySlimBlock>();
                        gridCube.GetBlocks(blocks);
                        //var cockpits = entity.FindWorkingCockpits(); // TODO: determine if any cockpits are occupied.
                        //gridCube.BigOwners
                        //gridCube.SmallOwners

                        displayType = gridCube.IsStatic ? "Station" : gridCube.GridSizeEnum.ToString() + " Ship";
                        displayName = entity.DisplayName;
                        description = string.Format("Distance: {0:N} m\r\nMass: {1:N} kg\r\nVector: {2}\r\nVelocity: {3:N} m/s\r\nMass Center: {4}\r\nSize: {5}\r\nNumber of Blocks: {6:#,##0}\r\nAttached Grids: {7:#,##0}",
                            distance,
                            gridCube.Physics.Mass,
                            gridCube.Physics.LinearVelocity,
                            gridCube.Physics.LinearVelocity.Length(),
                            gridCube.Physics.CenterOfMassWorld,
                            gridCube.LocalAABB.Size,
                            blocks.Count,
                            attachedGrids.Count
                            );
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

            // read the asteroid in chunks of 64 to avoid the Arithmetic overflow issue.
            for (block.Z = 0; block.Z < voxelMap.Storage.Size.Z; block.Z += 64)
                for (block.Y = 0; block.Y < voxelMap.Storage.Size.Y; block.Y += 64)
                    for (block.X = 0; block.X < voxelMap.Storage.Size.X; block.X += 64)
                    {
                        var size = new Vector3I(64);
                        var oldCache = new MyStorageDataCache();
                        oldCache.Resize(size);
                        // LOD1 is not detailed enough for content information on asteroids.
                        voxelMap.Storage.ReadRange(oldCache, MyStorageDataTypeFlags.Content, 0, block, block + size - 1);

                        Vector3I p;
                        for (p.Z = 0; p.Z < size.Z; ++p.Z)
                            for (p.Y = 0; p.Y < size.Y; ++p.Y)
                                for (p.X = 0; p.X < size.X; ++p.X)
                                {
                                    var content = oldCache.Content(ref p);
                                    if (content > 0)
                                    {
                                        min = Vector3I.Min(min, p + block);
                                        max = Vector3I.Max(max, p + block + 1);
                                    }
                                }
                    }

            var contentBox = new BoundingBoxD(voxelMap.PositionLeftBottomCorner + min, voxelMap.PositionLeftBottomCorner + max);
            var description = string.Format("Distance: {0:N}\r\nSize: {1}\r\nBoundingBox Center: [X:{2:N} Y:{3:N} Z:{4:N}]\r\n\r\nContent Size:{5}\r\nLOD0 Content Center: [X:{6:N} Y:{7:N} Z:{8:N}]",
                distance, voxelMap.Storage.Size,
                aabb.Center.X, aabb.Center.Y, aabb.Center.Z,
                max - min,
                contentBox.Center.X, contentBox.Center.Y, contentBox.Center.Z);
            MyAPIGateway.Utilities.ShowMissionScreen(string.Format("ID {0}:", displayType), string.Format("'{0}'", displayName), " ", description, null, "OK");
        }
    }
}
