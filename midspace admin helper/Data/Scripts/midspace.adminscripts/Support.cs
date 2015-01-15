namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Common.ObjectBuilders.Voxels;
    using Sandbox.ModAPI;
    using Sandbox.ModAPI.Interfaces;
    using VRageMath;
    using VRage.Common.Voxels;

    public static class Support
    {
        public static IMyEntity FindLookAtEntity(IMyControllableEntity controlledEntity, bool findShips = true, bool findPlayers = true, bool findAsteroids = true)
        {
            Matrix worldMatrix;
            Vector3D startPosition;
            Vector3D endPosition;
            if (MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.Parent == null)
            {
                worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.GetHeadMatrix(true, true, true); // most accurate for player view.
                startPosition = worldMatrix.Translation + worldMatrix.Forward * 0.5f;
                endPosition = worldMatrix.Translation + worldMatrix.Forward * 1000.5f;
            }
            else
            {
                worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.WorldMatrix;
                // TODO: need to adjust for position of cockpit within ship.
                startPosition = worldMatrix.Translation + worldMatrix.Forward * 1.5f;
                endPosition = worldMatrix.Translation + worldMatrix.Forward * 1001.5f;
            }

            //var worldMatrix = MyAPIGateway.Session.Player.PlayerCharacter.Entity.WorldMatrix;
            //var position = MyAPIGateway.Session.Player.PlayerCharacter.Entity.GetPosition();
            //var position = worldMatrix.Translation + worldMatrix.Forward * 0.5f + worldMatrix.Up * 1.0f;
            //MyAPIGateway.Utilities.ShowMessage("Pos", string.Format("x={0:N},y={1:N},z={2:N}  x={3:N},y={4:N},z={5:N}", playerPos.X, playerPos.Y, playerPos.Z, playerMatrix.Forward.X, playerMatrix.Forward.Y, playerMatrix.Forward.Z));

            // The CameraController.GetViewMatrix appears warped at the moment.
            //var position = ((IMyEntity)MyAPIGateway.Session.CameraController).GetPosition();
            //var worldMatrix = MyAPIGateway.Session.CameraController.GetViewMatrix();
            //var position = worldMatrix.Translation;
            //MyAPIGateway.Utilities.ShowMessage("Cam", string.Format("x={0:N},y={1:N},z={2:N}  x={3:N},y={4:N},z={5:N}", position.X, position.Y, position.Z, worldMatrix.Forward.X, worldMatrix.Forward.Y, worldMatrix.Forward.Z));

            var entites = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entites, e => e != null);

            var list = new Dictionary<IMyEntity, double>();
            var ray = new RayD(startPosition, worldMatrix.Forward);

            foreach (var entity in entites)
            {
                if (findShips)
                {
                    var cubeGrid = entity as Sandbox.ModAPI.IMyCubeGrid;

                    // check if the ray comes anywhere near the Grid before continuing.
                    if (cubeGrid != null && ray.Intersects(entity.WorldAABB).HasValue)
                    {
                        var hit = cubeGrid.RayCastBlocks(startPosition, endPosition);
                        if (hit.HasValue)
                        {
                            var distance = (startPosition - cubeGrid.GridIntegerToWorld(hit.Value)).Length();
                            list.Add(entity, distance);
                        }
                    }
                }

                if (findPlayers)
                {
                    var controller = entity as IMyControllableEntity;
                    if (MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.EntityId != entity.EntityId && controller != null && ray.Intersects(entity.WorldAABB).HasValue)
                    {
                        var distance = (startPosition - entity.GetPosition()).Length();
                        list.Add(entity, distance);
                    }
                }

                if (findAsteroids)
                {
                    var voxelMap = entity as IMyVoxelMap;
                    if (voxelMap != null)
                    {
                        var aabb = new BoundingBoxD(voxelMap.PositionLeftBottomCorner, voxelMap.PositionLeftBottomCorner + voxelMap.Storage.Size);
                        var hit = ray.Intersects(aabb);
                        if (hit.HasValue)
                        {
                            var distance = (startPosition - hit.Value).Length();
                            list.Add(entity, distance);
                        }
                    }
                }
            }

            if (list.Count == 0)
                return null;

            // find the closest Entity.
            return list.OrderBy(f => f.Value).First().Key;
        }

        public static IMyCubeBlock FindRotorBase(long entityId, IMyCubeGrid parent = null)
        {
            var entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities, e => e is IMyCubeGrid);

            foreach (var entity in entities)
            {
                var cubeGrid = (IMyCubeGrid)entity;

                if (cubeGrid == null)
                    continue;

                var blocks = new List<IMySlimBlock>();
                cubeGrid.GetBlocks(blocks, block => block != null && block.FatBlock != null && 
                    (block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorAdvancedStator) ||
                    block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorStator) ||
                    block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorSuspension) ||
                    block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorBase)));

                foreach (var block in blocks)
                {
                    var motorBase = block.GetObjectBuilder() as MyObjectBuilder_MotorBase;

                    if (motorBase == null || motorBase.RotorEntityId == 0 || !MyAPIGateway.Entities.ExistsById(motorBase.RotorEntityId))
                        continue;

                    if (motorBase.RotorEntityId == entityId)
                        return block.FatBlock;
                }
            }

            return null;
        }

        public static HashSet<IMyEntity> FindShipsByName(string findShipName, bool searchTransmittingBlockNames = true)
        {
            var allShips = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(allShips, e => e is IMyCubeGrid);

            // no search name was defined, so add all ships.
            if (string.IsNullOrEmpty(findShipName))
                return allShips;

            var shipList = new HashSet<IMyEntity>();
            foreach (var ship in allShips)
            {
                if (ship.DisplayName.IndexOf(findShipName, StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    shipList.Add(ship);
                }
                else if (searchTransmittingBlockNames)
                {
                    // look for a ship with an antenna or beacon with partially matching name.
                    var blocks = new List<Sandbox.ModAPI.IMySlimBlock>();
                    ((IMyCubeGrid)ship).GetBlocks(blocks, f => f.FatBlock != null && (f.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_RadioAntenna) || f.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Beacon)));
                    if (blocks.Any(b => ((Sandbox.ModAPI.Ingame.IMyTerminalBlock)b.FatBlock).CustomName.IndexOf(findShipName, StringComparison.InvariantCultureIgnoreCase) >= 0))
                    {
                        shipList.Add(ship);
                    }
                }
            }

            return shipList;
        }

        public static string CreateUniqueStorageName(string baseName)
        {
            int index = 0;
            var uniqueName = string.Format("{0}{1}", baseName, index);

            var currentAsteroidList = new List<IMyVoxelMap>();
            MyAPIGateway.Session.VoxelMaps.GetInstances(currentAsteroidList, v => v != null);

            while (currentAsteroidList.Any(a => a.StorageName.Equals(uniqueName, StringComparison.InvariantCultureIgnoreCase)))
            {
                index++;
                uniqueName = string.Format("{0}{1}", baseName, index);
            }

            return uniqueName;
        }

        public static bool MovePlayerToPlayer(IMyPlayer player, IMyPlayer targetPlayer, bool safely = true)
        {
            var worldMatrix = targetPlayer.Controller.ControlledEntity.Entity.WorldMatrix;
            
            var position = worldMatrix.Translation + worldMatrix.Forward * -2.5d;

            if (safely)
            {
                // Find empty location, centering on the target Player.
                var freePos = MyAPIGateway.Entities.FindFreePlace(position, (float)player.Controller.ControlledEntity.Entity.WorldVolume.Radius, 500, 20, 1f);
                if (!freePos.HasValue)
                {
                    MyAPIGateway.Utilities.ShowMessage("Failed", "Could not find safe location to transport to.");
                    return false;
                }

                // Offset will center the player character in the middle of the location.
                var offset = player.Controller.ControlledEntity.Entity.WorldAABB.Center - player.Controller.ControlledEntity.Entity.GetPosition();
                position = freePos.Value - offset;
            }

            var matrix = MatrixD.CreateWorld(position, worldMatrix.Forward, worldMatrix.Up);
            var linearVelocity = targetPlayer.Controller.ControlledEntity.Entity.Physics.LinearVelocity;

            player.Controller.ControlledEntity.Entity.Physics.LinearVelocity = linearVelocity;
            player.Controller.ControlledEntity.Entity.SetWorldMatrix(matrix);
            player.Controller.ControlledEntity.Entity.SetPosition(position);

            return true;
        }

        public static bool MovePlayerToCockpit(IMyPlayer player, IMyEntity cockpit)
        {
            if (player == null || cockpit == null)
                return false;

            var worldMatrix = cockpit.WorldMatrix;
            var position = worldMatrix.Translation + worldMatrix.Forward * -2.5d + worldMatrix.Up * -0.9d;  // Suitable for Large 1x1x1 cockpit.

            var matrix = MatrixD.CreateWorld(position, worldMatrix.Forward, worldMatrix.Up);
            var linearVelocity = cockpit.Parent.Physics.LinearVelocity;

            // The Physics.LinearVelocity doesn't change the player speed quickly enough before SetPosition is called, as
            // the player will smack into the other obejct before it's correct velocity is actually registered.
            player.Controller.ControlledEntity.Entity.Physics.LinearVelocity = linearVelocity;

            player.Controller.ControlledEntity.Entity.SetWorldMatrix(matrix);

            // The SetWorldMatrix doesn't rotate the player quickly enough before SetPosition is called, as 
            // the player will bounce off objects before it's correct orentation is actually registered.
            player.Controller.ControlledEntity.Entity.SetPosition(position);

            return true;
        }

        public static bool MovePlayerToShipGrid(IMyPlayer player, IMyEntity ship, bool safely = true)
        {
            var destination = ship.WorldAABB.GetCorners()[0];

            if (safely)
            {
                // Find empty location, centering on the ship grid.
                var freePos = MyAPIGateway.Entities.FindFreePlace(ship.WorldAABB.Center, (float)player.Controller.ControlledEntity.Entity.WorldVolume.Radius, 500, 20, 1f);
                if (!freePos.HasValue)
                {
                    MyAPIGateway.Utilities.ShowMessage("Failed", "Could not find safe location to transport to.");
                    return false;
                }

                // Offset will center the player character in the middle of the location.
                var offset = player.Controller.ControlledEntity.Entity.WorldAABB.Center - player.Controller.ControlledEntity.Entity.GetPosition();
                destination = freePos.Value - offset;
            }

            player.Controller.ControlledEntity.Entity.Physics.LinearVelocity = ship.Physics.LinearVelocity;
            player.Controller.ControlledEntity.Entity.SetPosition(destination);
            return true;
        }

        public static bool MoveShipToPlayer(IMyEntity shipGrid, IMyPlayer targetPlayer)
        {
            // TODO: complete.

            MyAPIGateway.Utilities.ShowMessage("Incomplete", "This function not complete. Cannot transport piloted Ship to another player.");

            return false;
        }

        public static bool MoveShipToShip(IMyEntity shipGrid, IMyEntity targetshipGrid)
        {
            // TODO: determine good location for moving one ship to another, checking for OrientedBoundingBox.Intersects().

            //// Move the ship the player is piloting.
            //var cubeGrid = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetTopMostParent();
            var grids = shipGrid.GetAttachedGrids();
            //var worldOffset = position - MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetPosition();

            MyAPIGateway.Utilities.ShowMessage("Incomplete", "This function not complete. Cannot transport piloted Ship to another ship.");

            foreach (var grid in grids)
            {
            //    grid.SetPosition(grid.GetPosition() + worldOffset);
            }

            return false;
        }

        /// <summary>
        /// Create a new Asteroid, ready for some manipulation.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="size">Currently the size must be multiple of 64, eg. 128x64x256</param>
        /// <param name="position"></param>
        public static IMyStorage CreateNewAsteroid(string storageName, Vector3I size, Vector3D position)
        {
            var cache = new MyStorageDataCache();

            MyAPIGateway.Utilities.ShowMessage("Test a", "check");

            // new storage is created completely full
            // no geometry will be created because that requires full-empty transition
            var storage = MyAPIGateway.Session.VoxelMaps.CreateStorage(size);

            MyAPIGateway.Utilities.ShowMessage("Test b", "check");

            // always ensure cache is large enough for whatever you plan to load into it
            cache.Resize(size);

            // range is specified using inclusive min and max coordinates
            // Choose a reasonable size of range you plan to work with, to avoid high memory usage
            // memory size in bytes required by cache is computed as Size.X * Size.Y * Size.Z * 2, where Size is size of the range.
            // min and max coordinates are inclusive, so if you want to read 8^3 voxels starting at coordinate [8,8,8], you
            // should pass in min = [8,8,8], max = [15,15,15]
            // For LOD, you should only use LOD0 or LOD1
            // When you write data inside cache back to storage, you always write to LOD0 (the most detailed LOD), LOD1 can only be read from.
            storage.ReadRange(cache, MyStorageDataTypeFlags.All, (int)VRageRender.MyLodTypeEnum.LOD0, Vector3I.Zero, size - 1);

            MyAPIGateway.Utilities.ShowMessage("Test c", "check");

            // resets all loaded content to empty
            cache.ClearContent(0);

            // write new data back to the storage
            storage.WriteRange(cache, MyStorageDataTypeFlags.Content, Vector3I.Zero, size - 1);

            MyAPIGateway.Utilities.ShowMessage("Test d", "check");

            //var controlledEntity = MyAPIGateway.Session.ControlledObject.Entity;
            var builder = new MyObjectBuilder_VoxelMap
            {
                PositionAndOrientation = new MyPositionAndOrientation
                {
                    Position = position,
                    // voxel maps cannot be rotated as of right now, so use default orientation
                    Forward = Vector3.Forward,
                    Up = Vector3.Up,
                },
                PersistentFlags = MyPersistentEntityFlags2.InScene | MyPersistentEntityFlags2.Enabled | MyPersistentEntityFlags2.CastShadows,

                Name = storageName,
                Filename = storageName,

                // links an entity with data and geometry
                StorageName = storageName,
                

                // Copy-on-write is performed on storage which is not mutable (new mutable copy is create at the time of first write)
                MutableStorage = false,
            };

            MyAPIGateway.Utilities.ShowMessage("Test e", "check");


            //var tempList = new List<MyObjectBuilder_EntityBase> { builder };
            //MyAPIGateway.Entities.RemapObjectBuilderCollection(tempList);
            //tempList.ForEach(grid => MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(grid));
            //MyAPIGateway.Multiplayer.SendEntitiesCreated(tempList);


            //var currentAsteroidList = new List<IMyVoxelMap>();
            //IMyVoxelMap originalAsteroid = null;
            //MyAPIGateway.Session.VoxelMaps.GetInstances(currentAsteroidList, v => v.StorageName.Equals(storageName, StringComparison.InvariantCultureIgnoreCase));

            //MyAPIGateway.Utilities.ShowMessage("Test z", string.Format("check {0}", currentAsteroidList.Count));

            //return currentAsteroidList[0].Storage;

            MyAPIGateway.Entities.RemapObjectBuilder(builder);
            var voxelMapEntity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(builder);
            MyAPIGateway.Utilities.ShowMessage("Test f", string.Format("check {0}", voxelMapEntity == null));

            var voxelMap = (IMyVoxelMap)voxelMapEntity;

            MyAPIGateway.Utilities.ShowMessage("Test g", string.Format("check {0}", voxelMap == null));


            //MyAPIGateway.Utilities.ShowMessage("Test f", string.Format("check"));

            

            

            //MyAPIGateway.Utilities.ShowMessage("Test h", string.Format("check {0}", voxelMap.Storage == null));

            return voxelMap.Storage;
            return null;
        }
    }
}