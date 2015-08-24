namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using Sandbox.ModAPI.Interfaces;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;
    using VRage.Voxels;
    using VRageMath;

    public static class Support
    {
        #region Find Assets

        public static IMyEntity FindLookAtEntity(IMyControllableEntity controlledEntity, bool findShips, bool findCubes, bool findPlayers, bool findAsteroids, bool findPlanets)
        {
            IMyEntity entity;
            double distance;
            FindLookAtEntity(controlledEntity, out entity, out distance, findShips, findCubes, findPlayers, findAsteroids, findPlanets);
            return entity;
        }

        public static void FindLookAtEntity(IMyControllableEntity controlledEntity, out IMyEntity lookEntity, out double lookDistance, bool findShips, bool findCubes, bool findPlayers, bool findAsteroids, bool findPlanets)
        {
            const float range = 5000000;
            Matrix worldMatrix;
            Vector3D startPosition;
            Vector3D endPosition;
            if (MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.Parent == null)
            {
                worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.GetHeadMatrix(true, true, false); // dead center of player cross hairs, or the direction the player is looking with ALT.
                startPosition = worldMatrix.Translation + worldMatrix.Forward * 0.5f;
                endPosition = worldMatrix.Translation + worldMatrix.Forward * (range + 0.5f);
            }
            else
            {
                worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.WorldMatrix;
                // TODO: need to adjust for position of cockpit within ship.
                startPosition = worldMatrix.Translation + worldMatrix.Forward * 1.5f;
                endPosition = worldMatrix.Translation + worldMatrix.Forward * (range + 1.5f);
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
                if (findShips || findCubes)
                {
                    var cubeGrid = entity as Sandbox.ModAPI.IMyCubeGrid;

                    // check if the ray comes anywhere near the Grid before continuing.
                    if (cubeGrid != null && ray.Intersects(entity.WorldAABB).HasValue)
                    {
                        var hit = cubeGrid.RayCastBlocks(startPosition, endPosition);
                        if (hit.HasValue)
                        {
                            var distance = (startPosition - cubeGrid.GridIntegerToWorld(hit.Value)).Length();
                            var block = cubeGrid.GetCubeBlock(hit.Value);

                            if (block.FatBlock != null && findCubes)
                                list.Add(block.FatBlock, distance);
                            else if (findShips)
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
                            var center = voxelMap.PositionLeftBottomCorner + (voxelMap.Storage.Size / 2);
                            var distance = (startPosition - center).Length();  // use distance to center of asteroid.
                            list.Add(entity, distance);
                        }
                    }
                }

                if (findPlanets)
                {
                    // Looks to be working against Git and public release.
                    var planet = entity as Sandbox.Game.Entities.MyPlanet;
                    if (planet != null)
                    {
                        var aabb = new BoundingBoxD(planet.PositionLeftBottomCorner, planet.PositionLeftBottomCorner + planet.Size);
                        var hit = ray.Intersects(aabb);
                        if (hit.HasValue)
                        {
                            var center = planet.PositionLeftBottomCorner + (planet.Size / 2);
                            var distance = (startPosition - center).Length(); // use distance to center of planet.
                            list.Add(entity, distance);
                        }
                    }
                }
            }

            if (list.Count == 0)
            {
                lookEntity = null;
                lookDistance = 0;
                return;
            }

            // find the closest Entity.
            var item = list.OrderBy(f => f.Value).First();
            lookEntity = item.Key;
            lookDistance = item.Value;
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

        /// <summary>
        /// Find the physical object of the specified name or partial name.
        /// </summary>
        /// <param name="itemName">The name of the physical object to find.</param>
        /// <param name="objectBuilder">The object builder of the physical object, ready for use.</param>
        /// <param name="options">Returns a list of potential matches if there was more than one of the same or partial name.</param>
        /// <returns>Returns true if a single exact match was found.</returns>
        public static bool FindPhysicalParts(string[] _oreNames, string[] _ingotNames, string[] _physicalItemNames, MyPhysicalItemDefinition[] _physicalItems, string itemName, out MyObjectBuilder_Base objectBuilder, out string[] options)
        {
            var itemNames = itemName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // prefix the search term with 'ore' to find this ore name.
            if (itemNames.Length > 1 && itemNames[0].Equals("ore", StringComparison.InvariantCultureIgnoreCase))
            {
                var findName = itemName.Substring(4).Trim();

                var exactMatchOres = _oreNames.Where(ore => ore.Equals(findName, StringComparison.InvariantCultureIgnoreCase)).ToArray();
                if (exactMatchOres.Length == 1)
                {
                    objectBuilder = new MyObjectBuilder_Ore() { SubtypeName = exactMatchOres[0] };
                    options = new string[0];
                    return true;
                }
                else if (exactMatchOres.Length > 1)
                {
                    objectBuilder = null;
                    options = exactMatchOres;
                    return false;
                }

                var partialMatchOres = _oreNames.Where(ore => ore.IndexOf(findName, StringComparison.InvariantCultureIgnoreCase) >= 0).ToArray();
                if (partialMatchOres.Length == 1)
                {
                    objectBuilder = new MyObjectBuilder_Ore() { SubtypeName = partialMatchOres[0] };
                    options = new string[0];
                    return true;
                }
                else if (partialMatchOres.Length > 1)
                {
                    objectBuilder = null;
                    options = partialMatchOres;
                    return false;
                }

                objectBuilder = null;
                options = new string[0];
                return false;
            }

            // prefix the search term with 'ingot' to find this ingot name.
            if (itemNames.Length > 0 && itemNames[0].Equals("ingot", StringComparison.InvariantCultureIgnoreCase))
            {
                var findName = itemName.Substring(6).Trim();

                var exactMatchIngots = _ingotNames.Where(ingot => ingot.Equals(findName, StringComparison.InvariantCultureIgnoreCase)).ToArray();
                if (exactMatchIngots.Length == 1)
                {
                    objectBuilder = new MyObjectBuilder_Ingot() { SubtypeName = exactMatchIngots[0] };
                    options = new string[0];
                    return true;
                }
                else if (exactMatchIngots.Length > 1)
                {
                    objectBuilder = null;
                    options = exactMatchIngots;
                    return false;
                }

                var partialMatchIngots = _ingotNames.Where(ingot => ingot.IndexOf(findName, StringComparison.InvariantCultureIgnoreCase) >= 0).ToArray();
                if (partialMatchIngots.Length == 1)
                {
                    objectBuilder = new MyObjectBuilder_Ingot() { SubtypeName = partialMatchIngots[0] };
                    options = new string[0];
                    return true;
                }
                else if (partialMatchIngots.Length > 1)
                {
                    objectBuilder = null;
                    options = partialMatchIngots;
                    return false;
                }

                objectBuilder = null;
                options = new string[0];
                return false;
            }

            // full name match.
            var res = _physicalItemNames.FirstOrDefault(s => s != null && s.Equals(itemName, StringComparison.InvariantCultureIgnoreCase));

            // need a good method for finding partial name matches.
            if (res == null)
            {
                var matches = _physicalItemNames.Where(s => s != null && s.StartsWith(itemName, StringComparison.InvariantCultureIgnoreCase)).Distinct().ToArray();

                if (matches.Length == 1)
                {
                    res = matches.FirstOrDefault();
                }
                else
                {
                    matches = _physicalItemNames.Where(s => s != null && s.IndexOf(itemName, StringComparison.InvariantCultureIgnoreCase) >= 0).Distinct().ToArray();
                    if (matches.Length == 1)
                    {
                        res = matches.FirstOrDefault();
                    }
                    else if (matches.Length > 1)
                    {
                        objectBuilder = null;
                        options = matches;
                        return false;
                    }
                }
            }

            if (res != null)
            {
                var item = _physicalItems[Array.IndexOf(_physicalItemNames, res)];
                if (item != null)
                {
                    objectBuilder = MyObjectBuilderSerializer.CreateNewObject(item.Id.TypeId, item.Id.SubtypeName);
                    options = new string[0];
                    return true;
                }
            }

            objectBuilder = null;
            options = new string[0];
            return false;
        }

        /// <summary>
        /// Find the physical asteroid either of the specified name or in the user hot list;
        /// </summary>
        /// <param name="searchAsteroidName"></param>
        /// <param name="originalAsteroid"></param>
        /// <returns></returns>
        public static bool FindAsteroid(string searchAsteroidName, out IMyVoxelBase originalAsteroid)
        {
            var currentAsteroidList = new List<IMyVoxelBase>();
            MyAPIGateway.Session.VoxelMaps.GetInstances(currentAsteroidList, v => v.StorageName.Equals(searchAsteroidName, StringComparison.InvariantCultureIgnoreCase));
            if (currentAsteroidList.Count == 1)
            {
                originalAsteroid = currentAsteroidList[0];
                return true;
            }
            else
            {
                MyAPIGateway.Session.VoxelMaps.GetInstances(currentAsteroidList, v => v.StorageName.IndexOf(searchAsteroidName, StringComparison.InvariantCultureIgnoreCase) >= 0);

                if (currentAsteroidList.Count == 1)
                {
                    originalAsteroid = currentAsteroidList[0];
                    return true;
                }
            }

            int index;
            if (searchAsteroidName.Substring(0, 1) == "#" && Int32.TryParse(searchAsteroidName.Substring(1), out index) && index > 0 && index <= CommandAsteroidsList.AsteroidCache.Count)
            {
                originalAsteroid = CommandAsteroidsList.AsteroidCache[index - 1];
                return true;
            }

            originalAsteroid = null;
            return false;
        }

        public static bool FindMaterial(string searchMaterialName, out MyVoxelMaterialDefinition material, ref string suggestedMaterials)
        {
            string[] validMaterials = MyDefinitionManager.Static.GetVoxelMaterialDefinitions().Where(k => k.Id.SubtypeName.Equals(searchMaterialName, StringComparison.InvariantCultureIgnoreCase)).Select(k => k.Id.SubtypeName).ToArray();
            if (validMaterials.Length == 0 || validMaterials.Length > 1)
            {
                validMaterials = MyDefinitionManager.Static.GetVoxelMaterialDefinitions().Where(k => k.Id.SubtypeName.IndexOf(searchMaterialName, StringComparison.InvariantCultureIgnoreCase) >= 0).Select(k => k.Id.SubtypeName).ToArray();
                if (validMaterials.Length == 0)
                {
                    validMaterials = MyDefinitionManager.Static.GetVoxelMaterialDefinitions().Select(k => k.Id.SubtypeName).ToArray();
                    material = null;
                    suggestedMaterials = String.Join(", ", validMaterials);
                    return false;
                }
                if (validMaterials.Length > 1)
                {
                    material = null;
                    suggestedMaterials = String.Join(", ", validMaterials);
                    return false;
                }
            }

            suggestedMaterials = "";
            material = MyDefinitionManager.Static.GetVoxelMaterialDefinition(validMaterials[0]);
            return true;
        }

        #endregion

        #region Find Cube in Grid

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

                    if (motorBase == null || motorBase.RotorEntityId == 0 || !MyAPIGateway.Entities.EntityExists(motorBase.RotorEntityId))
                        continue;

                    if (motorBase.RotorEntityId == entityId)
                        return block.FatBlock;
                }
            }

            return null;
        }

        #endregion

        #region Voxel

        public static string CreateUniqueStorageName(string baseName)
        {
            long index = 0;
            var match = Regex.Match(baseName, @"^(?<Key>.+?)(?<Value>(\d+?))$", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                baseName = match.Groups["Key"].Captures[0].Value;
                long.TryParse(match.Groups["Value"].Captures[0].Value, out index);
            }

            var uniqueName = string.Format("{0}{1}", baseName, index);
            var currentAsteroidList = new List<IMyVoxelBase>();
            MyAPIGateway.Session.VoxelMaps.GetInstances(currentAsteroidList, v => v != null);

            while (currentAsteroidList.Any(a => a.StorageName.Equals(uniqueName, StringComparison.InvariantCultureIgnoreCase)))
            {
                index++;
                uniqueName = string.Format("{0}{1}", baseName, index);
            }

            return uniqueName;
        }

        /// <summary>
        /// Create a new Asteroid, ready for some manipulation.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="size">Currently the size must be multiple of 64, eg. 128x64x256</param>
        /// <param name="position"></param>
        public static IMyVoxelMap CreateNewAsteroid(string storageName, Vector3I size, Vector3D position)
        {
            var cache = new MyStorageDataCache();

            // new storage is created completely full
            // no geometry will be created because that requires full-empty transition
            var storage = MyAPIGateway.Session.VoxelMaps.CreateStorage(size);

            // midspace's Note: The following steps appear redundant, as the storage space is created empty.
            /*
            // always ensure cache is large enough for whatever you plan to load into it
            cache.Resize(size);

            // range is specified using inclusive min and max coordinates
            // Choose a reasonable size of range you plan to work with, to avoid high memory usage
            // memory size in bytes required by cache is computed as Size.X * Size.Y * Size.Z * 2, where Size is size of the range.
            // min and max coordinates are inclusive, so if you want to read 8^3 voxels starting at coordinate [8,8,8], you
            // should pass in min = [8,8,8], max = [15,15,15]
            // For LOD, you should only use LOD0 or LOD1
            // When you write data inside cache back to storage, you always write to LOD0 (the most detailed LOD), LOD1 can only be read from.
            storage.ReadRange(cache, MyStorageDataTypeFlags.All, 0, Vector3I.Zero, size - 1);

            // resets all loaded content to empty
            cache.ClearContent(0);

            // write new data back to the storage
            storage.WriteRange(cache, MyStorageDataTypeFlags.Content, Vector3I.Zero, size - 1);
            */

            return MyAPIGateway.Session.VoxelMaps.CreateVoxelMap(storageName, storage, position, 0);
        }

        #endregion

        #region MoveTo

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <param name="targetPlayer"></param>
        /// <param name="safely">Attempts to find a safe location not inside of a wall.</param>
        /// <param name="agressivePosition">Places the player behind the targetPlayer, otherwise face to face.</param>
        /// <returns></returns>
        public static bool MovePlayerToPlayer(IMyPlayer player, IMyPlayer targetPlayer, bool safely = true, bool agressivePosition = true)
        {
            if (targetPlayer == null || targetPlayer.Controller == null || targetPlayer.Controller.ControlledEntity == null)
            {
                MyAPIGateway.Utilities.ShowMessage("Failed", "Player does not have body to teleport to.");
                return false;
            }

            var worldMatrix = targetPlayer.Controller.ControlledEntity.Entity.WorldMatrix;

            Vector3D position;
            MatrixD matrix;

            if (agressivePosition)
                position = worldMatrix.Translation + worldMatrix.Forward * -2.5d;
            else
                position = worldMatrix.Translation + worldMatrix.Forward * 2.5d;

            var currentPosition = player.Controller.ControlledEntity.Entity.GetPosition();

            if (safely)
            {
                if (!FindPlayerFreePosition(ref position, player))
                {
                    MyAPIGateway.Utilities.ShowMessage("Failed", "Could not find safe location to transport to.");
                    return false;
                }
            }

            if (agressivePosition)
                matrix = MatrixD.CreateWorld(position, worldMatrix.Forward, worldMatrix.Up);
            else
                matrix = MatrixD.CreateWorld(position, worldMatrix.Backward, worldMatrix.Up);

            var linearVelocity = targetPlayer.Controller.ControlledEntity.Entity.Physics.LinearVelocity;

            player.Controller.ControlledEntity.Entity.Physics.LinearVelocity = linearVelocity;
            player.Controller.ControlledEntity.Entity.SetWorldMatrix(matrix);
            player.Controller.ControlledEntity.Entity.SetPosition(position);

            //save teleport in history
            CommandTeleportBack.SaveTeleportInHistory(currentPosition);

            return true;
        }

        /// <summary>
        /// Move player to specific cube which may be a cockpit.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="cube"></param>
        /// <param name="safely"></param>
        /// <returns></returns>
        public static bool MovePlayerToCube(IMyPlayer player, IMyEntity cube, bool safely = true)
        {
            if (player == null || cube == null)
                return false;

            var worldMatrix = cube.WorldMatrix;
            // TODO: search local grid for empty location.
            var position = worldMatrix.Translation + worldMatrix.Forward * -2.5d + worldMatrix.Up * -0.9d;  // Suitable for Large 1x1x1 cockpit.

            if (safely)
            {
                if (!FindPlayerFreePosition(ref position, player))
                {
                    MyAPIGateway.Utilities.ShowMessage("Failed", "Could not find safe location to transport to.");
                    return false;
                }
            }

            var currentPosition = player.Controller.ControlledEntity.Entity.GetPosition();

            var matrix = MatrixD.CreateWorld(position, worldMatrix.Forward, worldMatrix.Up);
            var linearVelocity = cube.Parent.Physics.LinearVelocity;

            // The Physics.LinearVelocity doesn't change the player speed quickly enough before SetPosition is called, as
            // the player will smack into the other obejct before it's correct velocity is actually registered.
            player.Controller.ControlledEntity.Entity.Physics.LinearVelocity = linearVelocity;

            player.Controller.ControlledEntity.Entity.SetWorldMatrix(matrix);

            // The SetWorldMatrix doesn't rotate the player quickly enough before SetPosition is called, as 
            // the player will bounce off objects before it's correct orentation is actually registered.
            player.Controller.ControlledEntity.Entity.SetPosition(position);

            // save teleport in history
            CommandTeleportBack.SaveTeleportInHistory(currentPosition);

            return true;
        }

        public static bool MovePlayerToShipGrid(IMyPlayer player, IMyEntity ship, bool safely = true)
        {
            var destination = ship.WorldAABB.GetCorners()[0];

            var currentPosition = player.Controller.ControlledEntity.Entity.GetPosition();

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

            //save teleport in history
            CommandTeleportBack.SaveTeleportInHistory(currentPosition);

            return true;
        }

        public static bool MoveShipToPlayer(IMyEntity shipGrid, IMyPlayer targetPlayer)
        {
            // TODO: complete.

            MyAPIGateway.Utilities.ShowMessage("Incomplete", "This function not complete. Cannot transport piloted Ship to another player.");


            //save teleport in history
            //CommandBack.SaveTeleportInHistory(currentPosition);

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
                //grid.SetPosition(grid.GetPosition() + worldOffset);
            }

            //save teleport in history
            //CommandBack.SaveTeleportInHistory(currentPosition);

            return false;
        }

        /// <summary>
        /// Move player to Voxel. Either Asteroid or planet.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="ship"></param>
        /// <param name="safely"></param>
        /// <returns></returns>
        public static bool MovePlayerToVoxel(IMyPlayer player, IMyVoxelBase voxel, bool safely = true)
        {
            if (player == null || voxel == null)
                return false;

            Vector3D position;

            if (voxel is IMyVoxelMap)
            {
                var asteroid = (IMyVoxelMap)voxel;
                position = asteroid.PositionLeftBottomCorner;
            }
            else if (voxel is Sandbox.Game.Entities.MyPlanet)
            {
                var planet = (Sandbox.Game.Entities.MyPlanet)voxel;

                // TODO: change zentih entrypoint to current player position, unless inside planet atmosphere, then need a negative vector out from center.
                var zenithPoint = planet.PositionLeftBottomCorner + planet.SizeInMetresHalf;
                zenithPoint.Y += planet.AtmosphereRadius;

                Vector3D closestSurfacePoint;
                MyVoxelCoordSystems.WorldPositionToLocalPosition(planet.PositionLeftBottomCorner, ref zenithPoint, out closestSurfacePoint);
                Vector3D vector3D = planet.GetWorldGravityNormalized(ref zenithPoint);
                closestSurfacePoint = planet.GetClosestSurfacePoint(ref closestSurfacePoint, ref vector3D, 20, 0);
                MyVoxelCoordSystems.LocalPositionToWorldPosition(planet.PositionLeftBottomCorner, ref closestSurfacePoint, out position);
                position.Y += 0.5d; // add a small margin because the voxel LOD can sometimes push a player down when first loading a distant cluster.
            }
            else
            {
                return false;
            }

            if (safely)
            {
                if (!FindPlayerFreePosition(ref position, player))
                {
                    MyAPIGateway.Utilities.ShowMessage("Failed", "Could not find safe location to transport to.");
                    return false;
                }
            }

            var currentPosition = player.Controller.ControlledEntity.Entity.GetPosition();

            // TODO: orientation matrix will have to match new entrypoint position above.
            player.Controller.ControlledEntity.Entity.SetWorldMatrix(MatrixD.CreateWorld(position, Vector3D.Forward, Vector3D.Up));

            // The SetWorldMatrix doesn't rotate the player quickly enough before SetPosition is called, as 
            // the player will bounce off objects before it's correct orentation is actually registered.
            player.Controller.ControlledEntity.Entity.SetPosition(position);

            // save teleport in history
            CommandTeleportBack.SaveTeleportInHistory(currentPosition);

            return true;
        }

        public static bool MoveShipToVoxel(IMyEntity shipGrid, IMyVoxelBase ship, bool safely = true)
        {
            // TODO: complete code.

            return false;
        }

        private static bool FindPlayerFreePosition(ref Vector3D position, IMyPlayer player)
        {
            // Find empty location, centering on the target Player.
            var freePos = MyAPIGateway.Entities.FindFreePlace(position, (float)player.Controller.ControlledEntity.Entity.WorldVolume.Radius, 500, 20, 1f);
            if (!freePos.HasValue)
                return false;

            // Offset will center the player character in the middle of the location.
            var offset = player.Controller.ControlledEntity.Entity.WorldAABB.Center - player.Controller.ControlledEntity.Entity.GetPosition();
            position = freePos.Value - offset;

            return true;
        }

        #endregion
    }
}