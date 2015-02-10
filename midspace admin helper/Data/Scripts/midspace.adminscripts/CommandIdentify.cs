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

        public override void Help()
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

                        // TODO: try using IMyParallelTask to process.

                        var oldCache = new MyStorageDataCache();
                        oldCache.Resize(voxelMap.Storage.Size);
                        voxelMap.Storage.ReadRange(oldCache, MyStorageDataTypeFlags.ContentAndMaterial, 1, Vector3I.Zero, voxelMap.Storage.Size - 1);

                        Vector3I min = Vector3I.MaxValue;
                        Vector3I max = Vector3I.MinValue;

                        Vector3I p;
                        for (p.Z = 0; p.Z < voxelMap.Storage.Size.Z; ++p.Z)
                            for (p.Y = 0; p.Y < voxelMap.Storage.Size.Y; ++p.Y)
                                for (p.X = 0; p.X < voxelMap.Storage.Size.X; ++p.X)
                                {
                                    var content = oldCache.Content(ref p);
                                    if (content > 0)
                                    {
                                        min = Vector3I.Min(min, p);
                                        max = Vector3I.Max(max, p + 1);
                                    }
                                }

                        var contentBox = new BoundingBoxD(voxelMap.PositionLeftBottomCorner + min, voxelMap.PositionLeftBottomCorner + max);
                        description = string.Format("Distance: {0:N}\r\nSize: {1}\r\nBoundingBox Center: {2}\r\nLOD1 Content Center: {3}", distance, voxelMap.Storage.Size, aabb.Center, contentBox.Center);

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
                            "disabled for release ", //gridCube.LocalAABB.Size(), // breaking change for 01.069 .Size() -> .Size 
                            blocks.Count,
                            attachedGrids.Count
                            );
                    }
                    else
                    {
                        displayType = "player";
                        displayName = entity.DisplayName;
                        description = string.Format("Distance: {0:N}", distance);
                    }

                    MyAPIGateway.Utilities.ShowMissionScreen(string.Format("ID {0}:", displayType), string.Format("'{0}'", displayName), " ", description, null, "OK");
                    return true;
                }

                MyAPIGateway.Utilities.ShowMessage("ID", "Could not find object.");
                return true;
            }

            return false;
        }
    }
}
