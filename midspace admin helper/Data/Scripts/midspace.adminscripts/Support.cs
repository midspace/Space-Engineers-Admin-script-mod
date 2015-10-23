namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Common.ObjectBuilders.Definitions;
    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using Sandbox.ModAPI.Interfaces;
    using VRage;
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
            Vector3D hitPoint;
            FindLookAtEntity(controlledEntity, true, out entity, out distance, out hitPoint, findShips, findCubes, findPlayers, findAsteroids, findPlanets);
            return entity;
        }

        public static void FindLookAtEntity(IMyControllableEntity controlledEntity, bool ignoreOccupiedGrid, out IMyEntity lookEntity, out double lookDistance, out Vector3D hitPoint, bool findShips, bool findCubes, bool findPlayers, bool findAsteroids, bool findPlanets)
        {
            const float range = 5000000;
            Matrix worldMatrix;
            Vector3D startPosition;
            Vector3D endPosition;
            IMyCubeGrid occupiedGrid = null;

            if (controlledEntity.Entity.Parent == null)
            {
                worldMatrix = controlledEntity.GetHeadMatrix(true, true, false); // dead center of player cross hairs, or the direction the player is looking with ALT.
                startPosition = worldMatrix.Translation + worldMatrix.Forward * 0.5f;
                endPosition = worldMatrix.Translation + worldMatrix.Forward * (range + 0.5f);
            }
            else
            {
                occupiedGrid = controlledEntity.Entity.GetTopMostParent() as IMyCubeGrid;
                worldMatrix = controlledEntity.Entity.WorldMatrix;
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

                    if (cubeGrid != null)
                    {
                        if (ignoreOccupiedGrid && occupiedGrid != null && occupiedGrid.EntityId == cubeGrid.EntityId)
                            continue;

                        // check if the ray comes anywhere near the Grid before continuing.    
                        if (ray.Intersects(entity.WorldAABB).HasValue)
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
                }

                if (findPlayers)
                {
                    var controller = entity as IMyControllableEntity;
                    if (controlledEntity.Entity.EntityId != entity.EntityId && controller != null && ray.Intersects(entity.WorldAABB).HasValue)
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
                            var center = planet.WorldMatrix.Translation;
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
                hitPoint = Vector3D.Zero;
                return;
            }

            // find the closest Entity.
            var item = list.OrderBy(f => f.Value).First();
            lookEntity = item.Key;
            lookDistance = item.Value;
            hitPoint = startPosition + (Vector3D.Normalize(ray.Direction) * lookDistance);
        }

        public static HashSet<IMyEntity> FindShipsByName(string findShipName, bool searchTransmittingBlockNames = true, bool partNameMatch = true)
        {
            var allShips = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(allShips, e => e is IMyCubeGrid);

            // no search name was defined, so add all ships.
            if (string.IsNullOrEmpty(findShipName))
                return allShips;

            var shipList = new HashSet<IMyEntity>();
            foreach (var ship in allShips)
            {
                if (ship.DisplayName.Equals(findShipName, StringComparison.InvariantCultureIgnoreCase) ||
                    (partNameMatch && ship.DisplayName.IndexOf(findShipName, StringComparison.InvariantCultureIgnoreCase) >= 0))
                {
                    shipList.Add(ship);
                }
                else if (searchTransmittingBlockNames)
                {
                    // look for a ship with an antenna or beacon with partially matching name.
                    var blocks = new List<Sandbox.ModAPI.IMySlimBlock>();
                    ((IMyCubeGrid)ship).GetBlocks(blocks, f => f.FatBlock != null && (f.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_RadioAntenna) || f.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Beacon)));
                    if (blocks.Any(b => ((Sandbox.ModAPI.Ingame.IMyTerminalBlock)b.FatBlock).CustomName.Equals(findShipName, StringComparison.InvariantCultureIgnoreCase) ||
                    (partNameMatch && ((Sandbox.ModAPI.Ingame.IMyTerminalBlock)b.FatBlock).CustomName.IndexOf(findShipName, StringComparison.InvariantCultureIgnoreCase) >= 0)))
                    {
                        shipList.Add(ship);
                    }
                }
            }

            return shipList;
        }

        public static bool FindEntitiesNamed(string entityName, bool findPlayers, bool findShips, bool findAsteroids, bool findPlanets, bool findGps,
            out IMyPlayer player, out IMyEntity entity, out IMyGps gps)
        {
            #region Find Exact name match first.

            var playerList = new List<IMyPlayer>();
            var shipList = new HashSet<IMyEntity>();
            var asteroidList = new List<IMyVoxelBase>();
            var planetList = new List<IMyVoxelBase>();
            var gpsList = new List<IMyGps>();

            if (findPlayers)
                MyAPIGateway.Players.GetPlayers(playerList, p => entityName == null || p.DisplayName.Equals(entityName, StringComparison.InvariantCultureIgnoreCase));

            if (findShips)
                shipList = FindShipsByName(entityName);

            if (findAsteroids)
                MyAPIGateway.Session.VoxelMaps.GetInstances(asteroidList, v => v is IMyVoxelMap && (entityName == null || v.StorageName.Equals(entityName, StringComparison.InvariantCultureIgnoreCase)));

            if (findPlanets)
                MyAPIGateway.Session.VoxelMaps.GetInstances(planetList, v => v is Sandbox.Game.Entities.MyPlanet && (entityName == null || v.StorageName.Equals(entityName, StringComparison.InvariantCultureIgnoreCase)));

            if (findGps)
                gpsList = MyAPIGateway.Session.GPS.GetGpsList(MyAPIGateway.Session.Player.IdentityId).Where(g => entityName == null || g.Name.Equals(entityName, StringComparison.InvariantCultureIgnoreCase)).ToList();

            // identify a unique ship or player by the name.
            if (playerList.Count > 1 || shipList.Count > 1 || asteroidList.Count > 1 || planetList.Count > 1 || gpsList.Count > 1)
            {
                // TODO: too many entities. hotlist or sublist?
                string msg = "Too many entries with that name.";

                if (findPlayers)
                    msg += string.Format("  Players: {0}", playerList.Count);
                if (findShips)
                    msg += string.Format("  Ships: {0}", shipList.Count);
                if (findAsteroids)
                    msg += string.Format("  Asteroids: {0}", asteroidList.Count);
                if (findPlayers)
                    msg += string.Format("  Planets: {0}", planetList.Count);
                if (findGps)
                    msg += string.Format("  Gps: {0}", gpsList.Count);

                MyAPIGateway.Utilities.ShowMessage("Cannot match", msg);

                player = null;
                entity = null;
                gps = null;
                return false;
            }

            if (playerList.Count == 1 && shipList.Count == 0 && asteroidList.Count == 0 && planetList.Count == 0 && gpsList.Count == 0)
            {
                player = playerList[0];
                entity = null;
                gps = null;
                return true;
            }

            if (playerList.Count == 0 && shipList.Count == 1 && asteroidList.Count == 0 && planetList.Count == 0 && gpsList.Count == 0)
            {
                player = null;
                entity = shipList.FirstElement();
                gps = null;
                return true;
            }

            if (playerList.Count == 0 && shipList.Count == 0 && asteroidList.Count == 1 && planetList.Count == 0 && gpsList.Count == 0)
            {
                player = null;
                entity = asteroidList[0];
                gps = null;
                return true;
            }

            if (playerList.Count == 0 && shipList.Count == 0 && asteroidList.Count == 0 && planetList.Count == 1 && gpsList.Count == 0)
            {
                player = null;
                entity = planetList[0];
                gps = null;
                return true;
            }

            if (playerList.Count == 0 && shipList.Count == 0 && asteroidList.Count == 0 && planetList.Count == 0 && gpsList.Count == 1)
            {
                player = null;
                entity = null;
                gps = gpsList[0];
                return true;
            }

            #endregion

            #region find partial name matches.

            playerList.Clear();
            shipList.Clear();
            asteroidList.Clear();
            planetList.Clear();
            gpsList.Clear();

            if (findPlayers)
                MyAPIGateway.Players.GetPlayers(playerList, p => entityName == null || p.DisplayName.IndexOf(entityName, StringComparison.InvariantCultureIgnoreCase) >= 0);

            if (findShips)
                shipList = FindShipsByName(entityName, true, false);

            if (findAsteroids)
                MyAPIGateway.Session.VoxelMaps.GetInstances(asteroidList, v => v is IMyVoxelMap && (entityName == null || v.StorageName.IndexOf(entityName, StringComparison.InvariantCultureIgnoreCase) >= 0));

            if (findPlanets)
                MyAPIGateway.Session.VoxelMaps.GetInstances(planetList, v => v is Sandbox.Game.Entities.MyPlanet && (entityName == null || v.StorageName.IndexOf(entityName, StringComparison.InvariantCultureIgnoreCase) >= 0));

            if (findGps)
                gpsList = MyAPIGateway.Session.GPS.GetGpsList(MyAPIGateway.Session.Player.IdentityId).Where(g => entityName == null || g.Name.IndexOf(entityName, StringComparison.InvariantCultureIgnoreCase) >= 0).ToList();

            // identify a unique ship or player by the name.
            if (playerList.Count > 1 || shipList.Count > 1 || asteroidList.Count > 1 || planetList.Count > 1 || gpsList.Count > 1)
            {
                // TODO: too many entities. hotlist or sublist?
                string msg = "Too many entries with that name.";

                if (findPlayers)
                    msg += string.Format("  Players: {0}", playerList.Count);
                if (findShips)
                    msg += string.Format("  Ships: {0}", shipList.Count);
                if (findAsteroids)
                    msg += string.Format("  Asteroids: {0}", asteroidList.Count);
                if (findPlayers)
                    msg += string.Format("  Planets: {0}", planetList.Count);
                if (findGps)
                    msg += string.Format("  Gps: {0}", gpsList.Count);

                MyAPIGateway.Utilities.ShowMessage("Cannot match", msg);

                player = null;
                entity = null;
                gps = null;
                return false;
            }

            if (playerList.Count == 1 && shipList.Count == 0 && asteroidList.Count == 0 && planetList.Count == 0 && gpsList.Count == 0)
            {
                player = playerList[0];
                entity = null;
                gps = null;
                return true;
            }

            if (playerList.Count == 0 && shipList.Count == 1 && asteroidList.Count == 0 && planetList.Count == 0 && gpsList.Count == 0)
            {
                player = null;
                entity = shipList.FirstElement();
                gps = null;
                return true;
            }

            if (playerList.Count == 0 && shipList.Count == 0 && asteroidList.Count == 1 && planetList.Count == 0 && gpsList.Count == 0)
            {
                player = null;
                entity = asteroidList[0];
                gps = null;
                return true;
            }

            if (playerList.Count == 0 && shipList.Count == 0 && asteroidList.Count == 0 && planetList.Count == 1 && gpsList.Count == 0)
            {
                player = null;
                entity = planetList[0];
                gps = null;
                return true;
            }

            if (playerList.Count == 0 && shipList.Count == 0 && asteroidList.Count == 0 && planetList.Count == 0 && gpsList.Count == 1)
            {
                player = null;
                entity = null;
                gps = gpsList[0];
                return true;
            }

            #endregion

            // In reality, this is equivilant to:
            // if (playerList.Count == 0 && shipList.Count == 0 && asteroidList.Count == 0 && planetList.Count == 0 && gpsList.Count == 0)

            player = null;
            entity = null;
            gps = null;
            return false;
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
            if (itemNames.Length > 1 && itemNames[0].Equals("ingot", StringComparison.InvariantCultureIgnoreCase))
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

                    if (motorBase == null || !motorBase.RotorEntityId.HasValue || motorBase.RotorEntityId.Value == 0 || !MyAPIGateway.Entities.EntityExists(motorBase.RotorEntityId.Value))
                        continue;

                    if (motorBase.RotorEntityId == entityId)
                        return block.FatBlock;
                }
            }

            return null;
        }

        public static IMyCubeBlock FindPistonBase(long entityId, IMyCubeGrid parent = null)
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
                    block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_PistonBase));

                foreach (var block in blocks)
                {
                    var pistonBase = block.GetObjectBuilder() as MyObjectBuilder_PistonBase;

                    if (pistonBase == null || pistonBase.TopBlockId == 0 || !MyAPIGateway.Entities.EntityExists(pistonBase.TopBlockId))
                        continue;

                    if (pistonBase.TopBlockId == entityId)
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
            var cache = new MyStorageData();

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
        /// Move an entity (only Grids can be moved) to a location.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="targetLocation"></param>
        /// <param name="safely"></param>
        /// <param name="updatedPosition"></param>
        /// <param name="emptySourceMessage"></param>
        /// <param name="noSafeLocationMessage"></param>
        /// <returns>Will return false if the move operation could not be carried out.</returns>
        public static bool MoveTo(IMyEntity source, Vector3D targetLocation, bool safely,
            Action<Vector3D> updatedPosition, Action emptySourceMessage, Action noSafeLocationMessage)
        {
            if (source == null || source.Closed)
            {
                if (emptySourceMessage != null)
                    emptySourceMessage.Invoke();
                return false;
            }

            var cubeGrid = source as IMyCubeGrid;

            if (cubeGrid != null)
            {
                var worldOffset = targetLocation - cubeGrid.GetPosition();
                return MoveShipByOffset(cubeGrid, worldOffset, safely, updatedPosition, noSafeLocationMessage);
            }

            return false;
        }

        /// <summary>
        /// Move a player to a location.
        /// </summary>
        /// <param name="sourcePlayer"></param>
        /// <param name="targetLocation"></param>
        /// <param name="safely"></param>
        /// <param name="updatedPosition"></param>
        /// <param name="noSafeLocationMessage"></param>
        /// <returns>Will return false if the move operation could not be carried out.</returns>
        public static bool MoveTo(IMyPlayer sourcePlayer, Vector3D targetLocation, bool safely,
            Action<Vector3D> updatedPosition, Action noSafeLocationMessage)
        {
            if (sourcePlayer == null)
                return false;

            if (sourcePlayer.Controller.ControlledEntity is IMyCubeBlock)
            {
                // Move the ship the player is piloting.
                var cubeGrid = (IMyCubeGrid)sourcePlayer.Controller.ControlledEntity.Entity.GetTopMostParent();
                var worldOffset = targetLocation - sourcePlayer.Controller.ControlledEntity.Entity.GetPosition();
                return MoveShipByOffset(cubeGrid, worldOffset, safely, updatedPosition, noSafeLocationMessage);
            }

            // Move the player only.
            if (safely && !FindPlayerFreePosition(ref targetLocation, sourcePlayer))
            {
                if (noSafeLocationMessage != null)
                    noSafeLocationMessage.Invoke();
                return false;
            }

            var currentPosition = sourcePlayer.Controller.ControlledEntity.Entity.GetPosition();

            sourcePlayer.Controller.ControlledEntity.Entity.SetPosition(targetLocation);

            if (updatedPosition != null)
                updatedPosition.Invoke(currentPosition);

            return true;
        }

        /// <summary>
        /// Move an entity (only Grids can be moved), to another entity (which can be of Grid, Voxel).
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="safely"></param>
        /// <param name="updatedPosition"></param>
        /// <param name="emptySourceMessage"></param>
        /// <param name="emptyTargetMessage"></param>
        /// <param name="noSafeLocationMessage"></param>
        /// <returns>Will return false if the move operation could not be carried out.</returns>
        public static bool MoveTo(IMyEntity source, IMyEntity target, bool safely,
            Action<Vector3D> updatedPosition, Action emptySourceMessage, Action emptyTargetMessage, Action noSafeLocationMessage)
        {
            if (source == null || source.Closed)
            {
                if (emptySourceMessage != null)
                    emptySourceMessage.Invoke();
                return false;
            }

            if (target == null || target.Closed)
            {
                if (emptyTargetMessage != null)
                    emptyTargetMessage.Invoke();
                return false;
            }

            // Only grids can be moved.
            var cubeGrid = source as IMyCubeGrid;

            if (cubeGrid != null)
            {
                if (target is IMyCubeGrid)
                {
                    var worldOffset = target.WorldMatrix.Translation - cubeGrid.GetPosition();
                    return MoveShipByOffset(cubeGrid, worldOffset, safely, updatedPosition, noSafeLocationMessage);
                }

                if (target is IMyVoxelBase)
                {
                    return MoveShipToVoxel(cubeGrid, (IMyVoxelBase)target, safely,
                        updatedPosition, emptySourceMessage, emptyTargetMessage, noSafeLocationMessage);
                }
            }

            // Nothing else could be the source that needs moving.
            return false;
        }

        /// <summary>
        /// Move an entity (only Grids can be moved), to a player.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="targetPlayer"></param>
        /// <param name="safely"></param>
        /// <param name="updatedPosition"></param>
        /// <param name="emptySourceMessage"></param>
        /// <param name="emptyTargetMessage"></param>
        /// <param name="noSafeLocationMessage"></param>
        /// <returns>Will return false if the move operation could not be carried out.</returns>
        public static bool MoveTo(IMyEntity source, IMyPlayer targetPlayer, bool safely,
            Action<Vector3D> updatedPosition, Action emptySourceMessage, Action emptyTargetMessage, Action noSafeLocationMessage)
        {
            if (source == null || source.Closed)
            {
                if (emptySourceMessage != null)
                    emptySourceMessage.Invoke();
                return false;
            }

            if (targetPlayer == null)
            {
                if (emptyTargetMessage != null)
                    emptyTargetMessage.Invoke();
                return false;
            }

            // TODO: Is there a better way of targeting the placement of a ship near a player?
            return MoveTo(source, targetPlayer.GetPosition(), safely,
                updatedPosition, emptySourceMessage, noSafeLocationMessage);
        }

        /// <summary>
        /// Move a player, to another entity (which can be of Grid, Voxel).
        /// </summary>
        /// <param name="sourcePlayer"></param>
        /// <param name="target"></param>
        /// <param name="safely"></param>
        /// <param name="updatedPosition"></param>
        /// <param name="emptySourceMessage"></param>
        /// <param name="emptyTargetMessage"></param>
        /// <param name="noSafeLocationMessage"></param>
        /// <returns></returns>
        public static bool MoveTo(IMyPlayer sourcePlayer, IMyEntity target, bool safely,
            Action<Vector3D> updatedPosition, Action emptySourceMessage, Action emptyTargetMessage, Action noSafeLocationMessage)
        {
            if (sourcePlayer == null)
            {
                if (emptySourceMessage != null)
                    emptySourceMessage.Invoke();
                return false;
            }

            if (target == null || target.Closed)
            {
                if (emptyTargetMessage != null)
                    emptyTargetMessage.Invoke();
                return false;
            }

            if (sourcePlayer.Controller.ControlledEntity is IMyCubeBlock)
            {
                // player is piloting a ship. Move ship to entity.
                return MoveTo(sourcePlayer.Controller.ControlledEntity.Entity.GetTopMostParent(), target, safely, updatedPosition, emptySourceMessage, emptyTargetMessage, noSafeLocationMessage);
            }

            // Player is free floating, we move the player only.
            if (target is IMyCubeGrid)
            {
                var grid = (Sandbox.Game.Entities.MyCubeGrid)target;

                // Station or Large ship grids.
                if (((IMyCubeGrid)target).GridSizeEnum != MyCubeSize.Small)
                {
                    IMyControllableEntity targetCockpit = null;

                    if (grid.HasMainCockpit())
                    {
                        // Select the main cockpit.
                        targetCockpit = (IMyControllableEntity)grid.MainCockpit;
                    }
                    else
                    {
                        var cockpits = target.FindWorkingCockpits();
                        var operationalCockpit = cockpits.FirstOrDefault(c => ((Sandbox.ModAPI.Ingame.IMyCockpit)c).IsShipControlEnabled());

                        if (operationalCockpit != null)
                            // find a cockpit which is not a passenger seat.
                            targetCockpit = operationalCockpit;
                        else if (cockpits.Length > 0)
                            targetCockpit = cockpits[0];
                    }

                    if (targetCockpit != null)
                        return MovePlayerToCube(sourcePlayer, (IMyCubeBlock)targetCockpit, safely, updatedPosition, noSafeLocationMessage);
                }

                // Small ship grids. Also the fallback if a large ship does not have a cockpit.
                return MovePlayerToShipGrid(sourcePlayer, (IMyCubeGrid)target, safely, updatedPosition, noSafeLocationMessage);
            }

            if (target is IMyVoxelBase)
            {
                return MovePlayerToVoxel(sourcePlayer, (IMyVoxelBase)target, safely, updatedPosition, emptySourceMessage, emptyTargetMessage, noSafeLocationMessage);
            }

            return false;
        }

        /// <summary>
        /// Move a player, to another player.
        /// </summary>
        /// <param name="sourcePlayer"></param>
        /// <param name="targetPlayer"></param>
        /// <param name="safely">Attempts to find a safe location not inside of a ship wall or asteroid wall.</param>
        /// <param name="agressivePosition">Places the sourcePlayer behind the targetPlayer, otherwise face to face.</param>
        /// <param name="updatedPosition"></param>
        /// <param name="emptySourceMessage"></param>
        /// <param name="emptyTargetMessage"></param>
        /// <param name="noSafeLocationMessage"></param>
        /// <returns></returns>
        public static bool MoveTo(IMyPlayer sourcePlayer, IMyPlayer targetPlayer, bool safely, bool agressivePosition,
            Action<Vector3D> updatedPosition, Action emptySourceMessage, Action emptyTargetMessage, Action noSafeLocationMessage)
        {
            if (sourcePlayer == null)
            {
                if (emptySourceMessage != null)
                    emptySourceMessage.Invoke();
                return false;
            }

            if (targetPlayer == null)
            {
                if (emptyTargetMessage != null)
                    emptyTargetMessage.Invoke();
                return false;
            }

            if (sourcePlayer.PlayerID == targetPlayer.PlayerID)
            {
                MyAPIGateway.Utilities.ShowMessage("Teleport failed", "Cannot teleport player to themself.");
                return false;
            }

            if (targetPlayer.Controller == null || targetPlayer.Controller.ControlledEntity == null)
            {
                MyAPIGateway.Utilities.ShowMessage("Failed", "Player does not have body to teleport to.");
                return false;
            }

            if (targetPlayer.Controller.ControlledEntity is IMyCubeBlock)
            {
                var cockpit = (IMyCubeBlock)targetPlayer.Controller.ControlledEntity;

                var definition = MyDefinitionManager.Static.GetCubeBlockDefinition(cockpit.BlockDefinition);
                var cockpitDefintion = definition as MyCockpitDefinition;
                var remoteDefintion = definition as MyRemoteControlDefinition;

                if (cockpit.CubeGrid.GridSizeEnum != MyCubeSize.Small)
                    // station and large ship grids.
                    // move the player to the ship grid instead, as it is either cockpit, passenger seat, cryo chamber, or remote control.
                    return MovePlayerToCube(sourcePlayer, (IMyCubeBlock)targetPlayer.Controller.ControlledEntity.Entity, safely,
                        updatedPosition, noSafeLocationMessage);

                // small ship grids.
                return MovePlayerToShipGrid(sourcePlayer, cockpit.CubeGrid, safely,
                    updatedPosition, noSafeLocationMessage);

                //// target is a pilot in cockpit.
                //if (cockpitDefintion != null)
                //{
                //    // TODO: the code from above.
                //}
                //else if (remoteDefintion != null)
                //{
                //    // TODO: find player position.
                //    // Cannot determine player location. Is Remote controlling '{0}'", cockpit.CubeGrid.DisplayName);
                //    // where is the player? in a cockpit/chair or freefloating?
                //}
            }

            var worldMatrix = targetPlayer.Controller.ControlledEntity.Entity.WorldMatrix;

            Vector3D position;
            MatrixD matrix;

            if (agressivePosition)
                position = worldMatrix.Translation + worldMatrix.Forward * -2.5d;
            else
                position = worldMatrix.Translation + worldMatrix.Forward * 2.5d;

            var currentPosition = sourcePlayer.Controller.ControlledEntity.Entity.GetPosition();

            if (safely && !FindPlayerFreePosition(ref position, sourcePlayer))
            {
                if (noSafeLocationMessage != null)
                    noSafeLocationMessage.Invoke();
                return false;
            }

            if (agressivePosition)
                matrix = MatrixD.CreateWorld(position, worldMatrix.Forward, worldMatrix.Up);
            else
                matrix = MatrixD.CreateWorld(position, worldMatrix.Backward, worldMatrix.Up);

            var linearVelocity = targetPlayer.Controller.ControlledEntity.Entity.Physics.LinearVelocity;

            sourcePlayer.Controller.ControlledEntity.Entity.Physics.LinearVelocity = linearVelocity;
            sourcePlayer.Controller.ControlledEntity.Entity.SetWorldMatrix(matrix);
            sourcePlayer.Controller.ControlledEntity.Entity.SetPosition(position);

            if (updatedPosition != null)
                updatedPosition.Invoke(currentPosition);

            return true;
        }

        /// <summary>
        /// Move player to specific cube which may be a cockpit.
        /// </summary>
        /// <param name="sourcePlayer"></param>
        /// <param name="targetCube"></param>
        /// <param name="safely"></param>
        /// <param name="updatedPosition"></param>
        /// <param name="noSafeLocationMessage"></param>
        /// <returns></returns>
        public static bool MovePlayerToCube(IMyPlayer sourcePlayer, IMyCubeBlock targetCube, bool safely, Action<Vector3D> updatedPosition, Action noSafeLocationMessage)
        {
            if (sourcePlayer == null || targetCube == null)
                return false;

            var worldMatrix = targetCube.WorldMatrix;
            // TODO: search local grid for empty location.
            var position = worldMatrix.Translation + worldMatrix.Forward * -2.5d + worldMatrix.Up * -0.9d;  // Suitable for Large 1x1x1 cockpit.

            if (safely && !FindPlayerFreePosition(ref position, sourcePlayer))
            {
                if (noSafeLocationMessage != null)
                    noSafeLocationMessage.Invoke();
                return false;
            }

            var currentPosition = sourcePlayer.Controller.ControlledEntity.Entity.GetPosition();

            var matrix = MatrixD.CreateWorld(position, worldMatrix.Forward, worldMatrix.Up);
            var linearVelocity = targetCube.Parent.Physics == null ? Vector3.Zero : targetCube.Parent.Physics.LinearVelocity;

            // The Physics.LinearVelocity doesn't change the player speed quickly enough before SetPosition is called, as
            // the player will smack into the other obejct before it's correct velocity is actually registered.
            sourcePlayer.Controller.ControlledEntity.Entity.Physics.LinearVelocity = linearVelocity;

            sourcePlayer.Controller.ControlledEntity.Entity.SetWorldMatrix(matrix);

            // The SetWorldMatrix doesn't rotate the player quickly enough before SetPosition is called, as 
            // the player will bounce off objects before it's correct orentation is actually registered.
            sourcePlayer.Controller.ControlledEntity.Entity.SetPosition(position);

            if (updatedPosition != null)
                updatedPosition.Invoke(currentPosition);

            return true;
        }

        public static bool MovePlayerToShipGrid(IMyPlayer sourcePlayer, IMyCubeGrid targetGrid, bool safely, Action<Vector3D> updatedPosition, Action noSafeLocationMessage)
        {
            Vector3D destination;

            if (safely)
            {
                // Find empty location, centering on the ship grid.
                var freePos = MyAPIGateway.Entities.FindFreePlace(targetGrid.WorldAABB.Center, (float)sourcePlayer.Controller.ControlledEntity.Entity.WorldVolume.Radius, 500, 20, 1f);
                if (!freePos.HasValue)
                {
                    if (noSafeLocationMessage != null)
                        noSafeLocationMessage.Invoke();
                    return false;
                }

                // Offset will center the player character in the middle of the location.
                var offset = sourcePlayer.Controller.ControlledEntity.Entity.WorldAABB.Center - sourcePlayer.Controller.ControlledEntity.Entity.GetPosition();
                destination = freePos.Value - offset;
            }
            else
            {
                destination = targetGrid.WorldAABB.GetCorners()[0];
            }

            var currentPosition = sourcePlayer.Controller.ControlledEntity.Entity.GetPosition();

            sourcePlayer.Controller.ControlledEntity.Entity.Physics.LinearVelocity = targetGrid.Physics.LinearVelocity;
            sourcePlayer.Controller.ControlledEntity.Entity.SetPosition(destination);

            if (updatedPosition != null)
                updatedPosition.Invoke(currentPosition);

            return true;
        }

        private static bool MoveShipByOffset(IMyEntity cubeGrid, Vector3D worldOffset, bool safely, Action<Vector3D> updatedPosition, Action noSafeLocationMessage)
        {
            var grids = cubeGrid.GetAttachedGrids();
            var currentPosition = cubeGrid.GetPosition();
            Vector3D position = cubeGrid.GetPosition() + worldOffset;

            if (safely)
            {
                var worldVolume = grids[0].WorldVolume;
                foreach (var grid in grids)
                {
                    worldVolume.Include(grid.WorldVolume);
                }

                // TODO: determine good location for moving one ship to another, checking for OrientedBoundingBox.Intersects() for a tighter fit.
                if (!FindEntityFreePosition(ref position, cubeGrid, worldVolume))
                {
                    if (noSafeLocationMessage != null)
                        noSafeLocationMessage.Invoke();
                    return false;
                }
            }

            foreach (var grid in grids)
            {
                grid.SetPosition(position);
            }

            if (updatedPosition != null)
                updatedPosition.Invoke(currentPosition);

            return true;
        }

        /// <summary>
        /// Move player to Voxel. Either Asteroid or planet.
        /// </summary>
        /// <param name="sourcePlayer"></param>
        /// <param name="targetVoxel"></param>
        /// <param name="safely"></param>
        /// <param name="updatedPosition"></param>
        /// <param name="emptySourceMessage"></param>
        /// <param name="emptyTargetMessage"></param>
        /// <param name="noSafeLocationMessage"></param>
        /// <returns></returns>
        public static bool MovePlayerToVoxel(IMyPlayer sourcePlayer, IMyVoxelBase targetVoxel, bool safely,
            Action<Vector3D> updatedPosition, Action emptySourceMessage, Action emptyTargetMessage, Action noSafeLocationMessage)
        {
            if (sourcePlayer == null)
            {
                if (emptySourceMessage != null)
                    emptySourceMessage.Invoke();
                return false;
            }

            if (targetVoxel == null || targetVoxel.Closed)
            {
                if (emptyTargetMessage != null)
                    emptyTargetMessage.Invoke();
                return false;
            }

            Vector3D position;
            MatrixD matrix;

            if (targetVoxel is IMyVoxelMap)
            {
                var asteroid = (IMyVoxelMap)targetVoxel;
                position = asteroid.PositionLeftBottomCorner;
                // have the player facing the asteroid.
                var fwd = asteroid.WorldMatrix.Translation - asteroid.PositionLeftBottomCorner;
                fwd.Normalize();
                // calculate matrix to orient player to asteroid center.
                var up = Vector3D.CalculatePerpendicularVector(fwd);
                matrix = MatrixD.CreateWorld(position, fwd, up);
            }
            else if (targetVoxel is Sandbox.Game.Entities.MyPlanet)
            {
                var planet = (Sandbox.Game.Entities.MyPlanet)targetVoxel;

                // User current player position as starting point to find surface point.
                Vector3D findFromPoint = sourcePlayer.GetPosition();

                Vector3D closestSurfacePoint;
                MyVoxelCoordSystems.WorldPositionToLocalPosition(planet.PositionLeftBottomCorner, ref findFromPoint, out closestSurfacePoint);
                Vector3D vector3D = planet.GetWorldGravityNormalized(ref findFromPoint);
                closestSurfacePoint = planet.GetClosestSurfacePoint(ref closestSurfacePoint, ref vector3D, 20, 0);
                MyVoxelCoordSystems.LocalPositionToWorldPosition(planet.PositionLeftBottomCorner, ref closestSurfacePoint, out position);

                var up = position - planet.WorldMatrix.Translation;
                up.Normalize();
                position = position + (up * 0.5d); // add a small margin because the voxel LOD can sometimes push a player down when first loading a distant cluster.

                // calculate matrix to orient player to planet.
                var fwd = Vector3D.CalculatePerpendicularVector(up);
                matrix = MatrixD.CreateWorld(position, fwd, up);
            }
            else
            {
                return false;
            }

            if (safely && !FindPlayerFreePosition(ref position, sourcePlayer))
            {
                if (noSafeLocationMessage != null)
                    noSafeLocationMessage.Invoke();
                return false;
            }

            var currentPosition = sourcePlayer.Controller.ControlledEntity.Entity.GetPosition();

            // The SetWorldMatrix doesn't rotate the player quickly enough before SetPosition is called, as 
            // the player will bounce off objects before it's correct orentation is actually registered.
            sourcePlayer.Controller.ControlledEntity.Entity.SetWorldMatrix(matrix);
            sourcePlayer.Controller.ControlledEntity.Entity.SetPosition(position);

            if (updatedPosition != null)
                updatedPosition.Invoke(currentPosition);

            return true;
        }

        public static bool MoveShipToVoxel(IMyCubeGrid sourceGrid, IMyVoxelBase targetVoxel, bool safely,
            Action<Vector3D> updatedPosition, Action emptySourceMessage, Action emptyTargetMessage, Action noSafeLocationMessage)
        {
            // TODO: find a more accurate position to transport a ship to around a planet and asteroid.
            var worldOffset = targetVoxel.WorldMatrix.Translation - sourceGrid.GetPosition();
            return MoveShipByOffset(sourceGrid, worldOffset, safely, updatedPosition, noSafeLocationMessage);
        }

        private static bool FindPlayerFreePosition(ref Vector3D position, IMyPlayer player)
        {
            return FindEntityFreePosition(ref position, player.Controller.ControlledEntity.Entity, player.Controller.ControlledEntity.Entity.WorldVolume);
        }

        private static bool FindEntityFreePosition(ref Vector3D position, IMyEntity entity, BoundingSphereD worldVolume)
        {
            // Find empty location.

            // TODO: dynamically adjust the stepSize depending on the size of the entity been teleported.
            // it is currently too slow to find an empty spot for LargeRed sized ships in a busy location.
            var freePos = MyAPIGateway.Entities.FindFreePlace(position, (float)worldVolume.Radius, 500, 20, 1f);
            if (!freePos.HasValue)
                return false;

            // Offset will center the player character in the middle of the location.
            var offset = entity.WorldAABB.Center - entity.GetPosition();
            position = freePos.Value - offset;

            return true;
        }

        #endregion

        #region Inventory

        public static bool InventoryAdd(Sandbox.ModAPI.IMyInventory inventory, MyFixedPoint amount, MyDefinitionId definitionId)
        {
            var content = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(definitionId);

            var gasContainer = content as MyObjectBuilder_GasContainerObject;
            if (gasContainer != null)
                gasContainer.GasLevel = 1f;

            MyObjectBuilder_InventoryItem inventoryItem = new MyObjectBuilder_InventoryItem { Amount = amount, Content = content };

            if (inventory.CanItemsBeAdded(inventoryItem.Amount, definitionId))
            {
                inventory.AddItems(inventoryItem.Amount, (MyObjectBuilder_PhysicalObject)inventoryItem.Content, -1);
                return true;
            }

            // Inventory full. Could not add the item.
            return false;
        }

        public static void InventoryDrop(IMyEntity entity, MyFixedPoint amount, MyDefinitionId definitionId)
        {
            Vector3D position;

            if (entity is IMyCharacter)
                position = entity.WorldMatrix.Translation + entity.WorldMatrix.Forward * 1.5f + entity.WorldMatrix.Up * 1.5f; // Spawn item 1.5m in front of player.
            else
                position = entity.WorldMatrix.Translation + entity.WorldMatrix.Forward * 1.5f; // Spawn item 1.5m in front of player in cockpit.

            MyObjectBuilder_FloatingObject floatingBuilder = new MyObjectBuilder_FloatingObject();
            var content = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(definitionId);

            var gasContainer = content as MyObjectBuilder_GasContainerObject;
            if (gasContainer != null)
                gasContainer.GasLevel = 1f;

            floatingBuilder.Item = new MyObjectBuilder_InventoryItem() { Amount = amount, Content = content };
            floatingBuilder.PersistentFlags = MyPersistentEntityFlags2.InScene; // Very important

            floatingBuilder.PositionAndOrientation = new MyPositionAndOrientation()
            {
                Position = position,
                Forward = entity.WorldMatrix.Forward.ToSerializableVector3(),
                Up = entity.WorldMatrix.Up.ToSerializableVector3(),
            };

            floatingBuilder.CreateAndSyncEntity();
        }

        public static void InventoryDrop(Vector3D position, MyFixedPoint amount, MyDefinitionId definitionId)
        {
            MyObjectBuilder_FloatingObject floatingBuilder = new MyObjectBuilder_FloatingObject();
            var content = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(definitionId);

            var gasContainer = content as MyObjectBuilder_GasContainerObject;
            if (gasContainer != null)
                gasContainer.GasLevel = 1f;

            floatingBuilder.Item = new MyObjectBuilder_InventoryItem() { Amount = amount, Content = content };
            floatingBuilder.PersistentFlags = MyPersistentEntityFlags2.InScene; // Very important

            floatingBuilder.PositionAndOrientation = new MyPositionAndOrientation()
            {
                Position = position,
                Forward = Vector3D.Forward.ToSerializableVector3(),
                Up = Vector3D.Up.ToSerializableVector3(),
            };

            floatingBuilder.CreateAndSyncEntity();
        }

        #endregion
    }
}