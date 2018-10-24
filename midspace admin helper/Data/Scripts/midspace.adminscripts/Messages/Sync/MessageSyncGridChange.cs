namespace midspace.adminscripts.Messages.Sync
{
    using ProtoBuf;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Definitions;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VRage.Game;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;
    using VRageMath;

    [ProtoContract]
    public class MessageSyncGridChange : MessageBase
    {
        [ProtoMember(201)]
        public SyncGridChangeType SyncType;

        [ProtoMember(202)]
        public long EntityId;

        [ProtoMember(203)]
        public string SearchEntity;

        [ProtoMember(204)]
        public long PlayerId;

        [ProtoMember(205)]
        public bool SwitchOn;

        public static void SendMessage(SyncGridChangeType syncType, long entityId, string searchEntity, long playerId = 0, bool switchOn = false)
        {
            Process(new MessageSyncGridChange { SyncType = syncType, EntityId = entityId, SearchEntity = searchEntity, PlayerId = playerId, SwitchOn = switchOn });
        }

        private static void Process(MessageSyncGridChange syncEntity)
        {
            if (MyAPIGateway.Multiplayer.MultiplayerActive && !MyAPIGateway.Multiplayer.IsServer)
                ConnectionHelper.SendMessageToServer(syncEntity);
            else
                syncEntity.CommonProcess(syncEntity.SenderSteamId, syncEntity.SyncType, syncEntity.EntityId, syncEntity.SearchEntity, syncEntity.PlayerId, syncEntity.SwitchOn);
        }

        public override void ProcessClient()
        {
            // never called on client
        }

        public override void ProcessServer()
        {
            CommonProcess(SenderSteamId, SyncType, EntityId, SearchEntity, PlayerId, SwitchOn);
        }

        private void CommonProcess(ulong steamId, SyncGridChangeType syncType, long entityId, string searchEntity, long playerId, bool switchOn)
        {
            List<IMyCubeGrid> selectedShips = new List<IMyCubeGrid>();
            bool allSelectedShips = false;

            if (entityId != 0)
            {
                var selectedShip = MyAPIGateway.Entities.GetEntityById(entityId) as IMyCubeGrid;
                if (selectedShip != null)
                    selectedShips.Add(selectedShip);
            }
            else if (searchEntity == "**")  // All ships in the players hot list.
            {
                List<IMyEntity> shipCache = CommandListShips.GetShipCache(steamId);
                foreach (IMyEntity ship in shipCache)
                    selectedShips.Add((IMyCubeGrid)ship);
                allSelectedShips = true;
            }
            else if (!string.IsNullOrEmpty(searchEntity))
            {
                // exact name search.
                var currentShipList = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(currentShipList, e => e is IMyCubeGrid && e.DisplayName.Equals(searchEntity, StringComparison.InvariantCultureIgnoreCase));
                selectedShips.AddRange(currentShipList.Cast<IMyCubeGrid>());

                if (currentShipList.Count == 0)
                {
                    // hotlist search.
                    int index;
                    List<IMyEntity> shipCache = CommandListShips.GetShipCache(steamId);
                    if (searchEntity.Substring(0, 1) == "#" && int.TryParse(searchEntity.Substring(1), out index) && index > 0 && index <= shipCache.Count)
                    {
                        selectedShips.Add((IMyCubeGrid) shipCache[index - 1]);
                    }
                }
            }

            if (selectedShips.Count == 0)
            {
                MyAPIGateway.Utilities.SendMessage(steamId, "Server", "No ships selected or found.");
                return;
            }

            switch (syncType)
            {
                case SyncGridChangeType.OwnerClaim:
                    {
                        var players = new List<IMyPlayer>();
                        MyAPIGateway.Players.GetPlayers(players, p => p != null && p.IdentityId == playerId);
                        IMyPlayer player = players.FirstOrDefault();

                        if (player == null)
                            return;

                        foreach (var selectedShip in selectedShips)
                        {
                            var grids = selectedShip.GetAttachedGrids(AttachedGrids.Static);
                            foreach (var grid in grids)
                                grid.ChangeGridOwnership(player.IdentityId, MyOwnershipShareModeEnum.All);

                            MyAPIGateway.Utilities.SendMessage(steamId, "Server", string.Format("Grid {0} Claimed by player {1}.", selectedShip.DisplayName, player.DisplayName));
                        }
                    }
                    break;

                case SyncGridChangeType.OwnerRevoke:
                    {
                        foreach (var selectedShip in selectedShips)
                        {
                            var grids = selectedShip.GetAttachedGrids(AttachedGrids.Static);
                            foreach (var grid in grids)
                                grid.ChangeGridOwnership(0, MyOwnershipShareModeEnum.All);
                            MyAPIGateway.Utilities.SendMessage(steamId, "Server", string.Format("Grid {0} Revoked of all ownership.", selectedShip.DisplayName));
                        }
                    }
                    break;

                case SyncGridChangeType.OwnerShareAll:
                    {
                        foreach (var selectedShip in selectedShips)
                        {
                            var grids = selectedShip.GetAttachedGrids(AttachedGrids.Static);
                            foreach (var grid in grids)
                            {
                                var blocks = new List<IMySlimBlock>();
                                grid.GetBlocks(blocks, f => f.FatBlock != null && f.FatBlock.OwnerId != 0);

                                foreach (var block in blocks)
                                    block.FatBlock.ChangeOwner(block.FatBlock.OwnerId, MyOwnershipShareModeEnum.All);
                            }
                            MyAPIGateway.Utilities.SendMessage(steamId, "Server", string.Format("Grid {0} Shared.", selectedShip.DisplayName));
                        }
                    }
                    break;

                case SyncGridChangeType.OwnerShareNone:
                    {
                        foreach (var selectedShip in selectedShips)
                        {
                            var grids = selectedShip.GetAttachedGrids(AttachedGrids.Static);
                            foreach (var grid in grids)
                            {
                                var blocks = new List<IMySlimBlock>();
                                grid.GetBlocks(blocks, f => f.FatBlock != null && f.FatBlock.OwnerId != 0);

                                foreach (var block in blocks)
                                    block.FatBlock.ChangeOwner(block.FatBlock.OwnerId, MyOwnershipShareModeEnum.None);
                            }
                            MyAPIGateway.Utilities.SendMessage(steamId, "Server", string.Format("Grid {0} Shared.", selectedShip.DisplayName));
                        }
                    }
                    break;

                case SyncGridChangeType.SwitchOnPower:
                    {
                        int reactors;
                        int batteries;
                        TurnOnShips(steamId, selectedShips, out reactors, out batteries);
                    }
                    break;

                case SyncGridChangeType.SwitchOffPower:
                    {
                        int reactors;
                        int batteries;
                        TurnOffShips(steamId, selectedShips, out reactors, out batteries);
                    }
                    break;
                case SyncGridChangeType.DeleteShip:
                    {
                        if (allSelectedShips)
                        {
                            foreach (var selectedShip in selectedShips)
                                DeleteShip(steamId, selectedShip);
                        }
                        else if (selectedShips.Count == 1)
                            DeleteShip(steamId, selectedShips.First());
                        else if (selectedShips.Count > 1)
                            MyAPIGateway.Utilities.SendMessage(steamId, "deleteship", "{0} Ships match that name.", selectedShips.Count);
                    }
                    break;

                case SyncGridChangeType.Destructible:
                    {
                        if (selectedShips.Count == 1)
                            SetDestructible(selectedShips.First(), switchOn, steamId);
                    }
                    break;

                case SyncGridChangeType.Stop:
                    {
                        foreach (var selectedShip in selectedShips)
                        {
                            MessageSyncEntity.Process(selectedShip, SyncEntityType.Stop);
                            MyAPIGateway.Utilities.SendMessage(steamId, selectedShip.DisplayName, "Is stopping.");
                        }
                    }
                    break;

                case SyncGridChangeType.ScaleDown:
                    {
                        ScaleShip(steamId, selectedShips.First(), MyCubeSize.Small);
                    }
                    break;

                case SyncGridChangeType.ScaleUp:
                    {
                        ScaleShip(steamId, selectedShips.First(), MyCubeSize.Large);
                    }
                    break;

                case SyncGridChangeType.BuiltBy:
                    {
                        string playerName = SearchEntity;

                        var players = new List<IMyPlayer>();
                        MyAPIGateway.Players.GetPlayers(players, p => p != null);
                        IMyIdentity selectedPlayer = null;

                        var identities = new List<IMyIdentity>();
                        MyAPIGateway.Players.GetAllIdentites(identities, i => i.DisplayName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase));
                        selectedPlayer = identities.FirstOrDefault();

                        int index;
                        List<IMyIdentity> cacheList = CommandPlayerStatus.GetIdentityCache(steamId);
                        if (playerName.Substring(0, 1) == "#" && int.TryParse(playerName.Substring(1), out index) && index > 0 && index <= cacheList.Count)
                        {
                            selectedPlayer = cacheList[index - 1];
                        }

                        List<IMyIdentity> botCacheList = CommandListBots.GetIdentityCache(steamId);
                        if (playerName.Substring(0, 1).Equals("B", StringComparison.InvariantCultureIgnoreCase) && int.TryParse(playerName.Substring(1), out index) && index > 0 && index <= botCacheList.Count)
                        {
                            selectedPlayer = botCacheList[index - 1];
                        }

                        if (selectedPlayer == null)
                        {
                            MyAPIGateway.Utilities.SendMessage(steamId, "Server", string.Format("Player or Bot '{0}' could not be found.", playerName));
                            return;
                        }

                        // Using the identity list is a crap way, but since we don't have access to BuiltBy for non-functional blocks, this has to do.
                        var listIdentites = new List<IMyIdentity>();
                        MyAPIGateway.Players.GetAllIdentites(listIdentites);
                        foreach (var selectedShip in selectedShips)
                        {
                            var grids = selectedShip.GetAttachedGrids(AttachedGrids.Static);
                            foreach (var grid in grids)
                            {
                                foreach (IMyIdentity identity in listIdentites)
                                {
                                    if (identity.IdentityId != selectedPlayer.IdentityId)
                                    {
                                        // The current API doesn't allow the setting of the BuiltBy to anything but an existing Identity (player or NPC).
                                        ((MyCubeGrid)grid).TransferBlocksBuiltByID(identity.IdentityId, selectedPlayer.IdentityId);
                                    }
                                }
                            }
                            MyAPIGateway.Utilities.SendMessage(steamId, "Server", string.Format("Grid '{0}' Changed of all built to '{1}'.", selectedShip.DisplayName, selectedPlayer.DisplayName));
                        }
                    }
                    break;

                case SyncGridChangeType.Repair:
                    {
                        var players = new List<IMyPlayer>();
                        MyAPIGateway.Players.GetPlayers(players, p => p != null && p.IdentityId == playerId);
                        IMyPlayer player = players.FirstOrDefault();

                        if (player == null)
                            return;

                        foreach (var selectedShip in selectedShips)
                        {
                            var grids = selectedShip.GetAttachedGrids(AttachedGrids.Static);
                            foreach (var grid in grids)
                            {
                                RepairShip(steamId, grid);
                            }

                            MyAPIGateway.Utilities.SendMessage(steamId, "Server", string.Format("Grid {0} Repaired.", selectedShip.DisplayName));
                        }
                    }
                    break;

                case SyncGridChangeType.ConvertToStation:
                    {
                        foreach (var selectedShip in selectedShips)
                        {
                            var grids = selectedShip.GetAttachedGrids(AttachedGrids.Static);
                            foreach (var grid in grids)
                                grid.IsStatic = true;

                            MyAPIGateway.Utilities.SendMessage(steamId, "Server", $"Grid {selectedShip.DisplayName} convert to Station.");
                        }
                    }
                    break;

                case SyncGridChangeType.ConvertToShip:
                    {
                        foreach (var selectedShip in selectedShips)
                        {
                            var grids = selectedShip.GetAttachedGrids(AttachedGrids.Static);
                            foreach (var grid in grids)
                                grid.IsStatic = false;

                            MyAPIGateway.Utilities.SendMessage(steamId, "Server", $"Grid {selectedShip.DisplayName} convert to Ship.");
                        }
                    }
                    break;
            }
        }

        private void TurnOnShips(ulong steamId, IEnumerable<IMyEntity> shipList, out int reactorCounter, out int batteryCounter)
        {
            reactorCounter = 0;
            batteryCounter = 0;
            foreach (var selectedShip in shipList)
            {
                int reactors;
                int batteries;
                TurnOnShip(selectedShip, out reactors, out batteries);
                reactorCounter += reactors;
                batteryCounter += batteries;
                MyAPIGateway.Utilities.SendMessage(steamId, selectedShip.DisplayName, "{0} Reactors, {1} Batteries turned on.", reactors, batteries);
            }
        }

        private void TurnOnShip(IMyEntity shipEntity, out int reactorCounter, out int batteryCounter)
        {
            reactorCounter = 0;
            batteryCounter = 0;
            var grids = shipEntity.GetAttachedGrids(AttachedGrids.Static);

            foreach (var cubeGrid in grids)
            {
                var blocks = new List<IMySlimBlock>();
                cubeGrid.GetBlocks(blocks, f => f.FatBlock != null
                    && f.FatBlock is IMyFunctionalBlock
                    && (f.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Reactor)
                      || f.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_BatteryBlock)));

                var list = blocks.Select(f => (IMyFunctionalBlock)f.FatBlock).Where(f => !f.Enabled).ToArray();

                foreach (var item in list)
                {
                    MessageSyncBlock.Process(item, SyncBlockType.PowerOn);

                    if (item.BlockDefinition.TypeId == typeof(MyObjectBuilder_Reactor))
                        reactorCounter++;
                    else
                        batteryCounter++;
                }
            }
        }

        private void TurnOffShips(ulong steamId, IEnumerable<IMyEntity> shipList, out int reactorCounter, out int batteryCounter)
        {
            reactorCounter = 0;
            batteryCounter = 0;
            foreach (var selectedShip in shipList)
            {
                int reactors;
                int batteries;
                TurnOffShip(selectedShip, out reactors, out batteries);
                MyAPIGateway.Utilities.SendMessage(steamId, selectedShip.DisplayName, "{0} Reactors, {1} Batteries turned off.", reactors, batteries);
                reactorCounter += reactors;
                batteryCounter += batteries;
            }
        }

        private void TurnOffShip(IMyEntity shipEntity, out int reactorCounter, out int batteryCounter)
        {
            reactorCounter = 0;
            batteryCounter = 0;
            var grids = shipEntity.GetAttachedGrids(AttachedGrids.Static);

            foreach (var cubeGrid in grids)
            {
                var blocks = new List<IMySlimBlock>();
                cubeGrid.GetBlocks(blocks, f => f.FatBlock != null
                    && f.FatBlock is IMyFunctionalBlock
                    && (f.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Reactor)
                      || f.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_BatteryBlock)));

                var list = blocks.Select(f => (IMyFunctionalBlock)f.FatBlock).Where(f => f.Enabled).ToArray();

                foreach (var item in list)
                {
                    MessageSyncBlock.Process(item, SyncBlockType.PowerOff);

                    if (item.BlockDefinition.TypeId == typeof(MyObjectBuilder_Reactor))
                        reactorCounter++;
                    else
                        batteryCounter++;
                }
            }
        }

        private void DeleteShip(ulong steamId, IMyEntity shipEntity)
        {
            var grids = shipEntity.GetAttachedGrids(AttachedGrids.Static);

            foreach (var cubeGrid in grids)
            {
                if (cubeGrid == null || cubeGrid.Closed)
                    continue;

                // ejects any player prior to deleting the grid.
                cubeGrid.EjectControllingPlayers();

                var name = cubeGrid.DisplayName;

                // This will Delete the entity and sync to all.
                // Using this, also works with player ejection in the same Tick.

                cubeGrid.Close();

                MyAPIGateway.Utilities.SendMessage(steamId, "ship", "'{0}' deleted.", name);
            }
        }

        private void SetDestructible(IMyEntity shipEntity, bool destructible, ulong steamId)
        {
            var gridObjectBuilder = shipEntity.GetObjectBuilder(true) as MyObjectBuilder_CubeGrid;
            if (gridObjectBuilder.DestructibleBlocks == destructible)
            {
                MyAPIGateway.Utilities.SendMessage(steamId, "destructible", "Ship '{0}' destructible is already set to {1}.", shipEntity.DisplayName, destructible ? "On" : "Off");
                return;
            }

            gridObjectBuilder.EntityId = 0;
            gridObjectBuilder.DestructibleBlocks = destructible;

            // This will Delete the entity and sync to all.
            // Using this, also works with player ejection in the same Tick.
            shipEntity.Close();

            var tempList = new List<MyObjectBuilder_EntityBase>();
            tempList.Add(gridObjectBuilder);
            tempList.CreateAndSyncEntities();

            MyAPIGateway.Utilities.SendMessage(steamId, "destructible", "Ship '{0}' destructible has been set to {1}.", shipEntity.DisplayName, destructible ? "On" : "Off");
        }

        static readonly Dictionary<string, string> LargeToSmall = new Dictionary<string, string> {
            { "LargeBlockConveyor", "SmallBlockConveyor" },
            { "ConveyorTube", "ConveyorTubeSmall" },
            { "ConveyorTubeCurved", "ConveyorTubeSmallCurved" },
            { "LargeBlockLargeContainer", "SmallBlockMediumContainer" }
        };

        static readonly Dictionary<string, string> SmallToLarge = new Dictionary<string, string> {
            { "SmallBlockConveyor", "LargeBlockConveyor" },
            { "ConveyorTubeSmall", "ConveyorTube" } ,
            { "ConveyorTubeSmallCurved", "ConveyorTubeCurved" },
            { "SmallBlockMediumContainer", "LargeBlockLargeContainer" },
        };

        // deal with cubes that need to be rotated, because the small and large grid varients don't have the same orientation.
        private static readonly Dictionary<string, Quaternion> SmallToLargeRotate = new Dictionary<string, Quaternion>
        {
            {"ConveyorTubeSmall", Quaternion.CreateFromForwardUp(Vector3.Left, Vector3.Forward)},
            {"ConveyorTubeSmallCurved", Quaternion.CreateFromForwardUp(Vector3.Right, Vector3.Up)},
            {"SmallBlockBeacon",  Quaternion.CreateFromForwardUp(Vector3.Up, Vector3.Backward)},
            {"SmallBlockSmallGenerator",  Quaternion.CreateFromForwardUp(Vector3.Backward, Vector3.Up)},
        };

        private static readonly Dictionary<string, Quaternion> LargeToSmallRotate = new Dictionary<string, Quaternion>
        {
            {"ConveyorTube", Quaternion.CreateFromForwardUp(Vector3.Up, Vector3.Left)},
            {"ConveyorTubeCurved",  Quaternion.CreateFromForwardUp(Vector3.Left, Vector3.Up)},
            {"LargeBlockBeacon",  Quaternion.CreateFromForwardUp(Vector3.Down, Vector3.Forward)},
            {"LargeBlockSmallGenerator",  Quaternion.CreateFromForwardUp(Vector3.Backward, Vector3.Up)},
        };

        private bool ScaleShip(ulong steamId, IMyCubeGrid shipEntity, MyCubeSize newScale)
        {
            if (shipEntity == null)
                return false;

            if (shipEntity.GridSizeEnum == newScale)
            {
                MyAPIGateway.Utilities.SendMessage(steamId, "scaledown", "Ship is already the right scale.");
                return true;
            }

            var grids = shipEntity.GetAttachedGrids();

            var newGrids = new MyObjectBuilder_CubeGrid[grids.Count];

            foreach (var cubeGrid in grids)
            {
                // ejects any player prior to deleting the grid.
                cubeGrid.EjectControllingPlayers();
                cubeGrid.Physics.Enabled = false;
            }

            var tempList = new List<MyObjectBuilder_EntityBase>();
            var gridIndex = 0;
            foreach (var cubeGrid in grids)
            {
                var blocks = new List<IMySlimBlock>();
                cubeGrid.GetBlocks(blocks);

                var gridObjectBuilder = cubeGrid.GetObjectBuilder(true) as MyObjectBuilder_CubeGrid;

                gridObjectBuilder.EntityId = 0;
                Regex rgx = new Regex(Regex.Escape(gridObjectBuilder.GridSizeEnum.ToString()));
                var rgxScale = Regex.Escape(newScale.ToString());
                gridObjectBuilder.GridSizeEnum = newScale;
                var removeList = new List<MyObjectBuilder_CubeBlock>();

                foreach (var block in gridObjectBuilder.CubeBlocks)
                {
                    MyCubeBlockDefinition definition;
                    string newSubType = null;
                    if (newScale == MyCubeSize.Small && LargeToSmall.ContainsKey(block.SubtypeName))
                        newSubType = LargeToSmall[block.SubtypeName];
                    else if (newScale == MyCubeSize.Large && SmallToLarge.ContainsKey(block.SubtypeName))
                        newSubType = SmallToLarge[block.SubtypeName];
                    else
                    {
                        newSubType = rgx.Replace(block.SubtypeName, rgxScale, 1);

                        // Match using the BlockPairName if there is a matching cube.
                        if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(new MyDefinitionId(block.GetType(), block.SubtypeName), out definition))
                        {
                            var newDef = MyDefinitionManager.Static.GetAllDefinitions().Where(d => d is MyCubeBlockDefinition && ((MyCubeBlockDefinition)d).BlockPairName == definition.BlockPairName && ((MyCubeBlockDefinition)d).CubeSize == newScale).FirstOrDefault();
                            if (newDef != null)
                                newSubType = newDef.Id.SubtypeName;
                        }
                    }

                    Quaternion? rotate = null;
                    if (newScale == MyCubeSize.Small && LargeToSmallRotate.ContainsKey(block.SubtypeName))
                        rotate = LargeToSmallRotate[block.SubtypeName];
                    else if (newScale == MyCubeSize.Large && SmallToLargeRotate.ContainsKey(block.SubtypeName))
                        rotate = SmallToLargeRotate[block.SubtypeName];

                    if (rotate != null)
                    {
                        MyBlockOrientation o1 = block.BlockOrientation;
                        Quaternion q1;
                        o1.GetQuaternion(out q1);

                        Quaternion q2 = Quaternion.Normalize(q1 * rotate.Value);
                        MyBlockOrientation o2 = new MyBlockOrientation(ref q2);
                        //VRage.Utils.MyLog.Default.WriteLine($"##Mod## Q2 {o1.Forward}, {o1.Up} -> {o2.Forward}, {o2.Up}");
                        block.BlockOrientation = o2;
                    }

                    if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(new MyDefinitionId(block.GetType(), newSubType), out definition) && definition.CubeSize == newScale)
                    {
                        block.SubtypeName = newSubType;
                        //block.EntityId = 0;
                    }
                    else
                    {
                        removeList.Add(block);
                    }
                }

                foreach (var block in removeList)
                {
                    gridObjectBuilder.CubeBlocks.Remove(block);
                }

                // This will Delete the entity and sync to all.
                // Using this, also works with player ejection in the same Tick.
                cubeGrid.Close();

                var name = cubeGrid.DisplayName;
                MyAPIGateway.Utilities.SendMessage(steamId, "ship", "'{0}' resized.", name);

                tempList.Add(gridObjectBuilder);

                gridIndex++;
            }

            // TODO: reposition multiple grids so rotors and pistons re-attach.

            tempList.CreateAndSyncEntities();
            return true;
        }

        private void RepairShip(ulong steamId, IMyEntity shipEntity)
        {
            var blocks = new List<IMySlimBlock>();
            var ship = (IMyCubeGrid)shipEntity;
            ship.GetBlocks(blocks, f => f != null);

            foreach (IMySlimBlock block in blocks)
            {
                //block.CurrentDamage
                //block.AccumulatedDamage
                //block.DamageRatio
                //block.HasDeformation
                //block.MaxDeformation

                //MyAPIGateway.Utilities.SendMessage(steamId, "state", "{0}: HasdD:{1} MaxD:{2} Int:{3} BuildInt:{4} MaxInt:{5}", i, block.HasDeformation, block.MaxDeformation, block.Integrity, block.BuildIntegrity, block.MaxIntegrity);
                int j = 0;
                while (block.HasDeformation && j < 20 || j == 0)
                {
                    block.IncreaseMountLevel(1000F, 0, null, 1000F, true);
                    j++;
                }
            }

            //MyAPIGateway.Utilities.SendMessage(steamId, "repair", "Ship '{0}' has been repairded.", shipEntity.DisplayName);
        }
    }

    public enum SyncGridChangeType : byte
    {
        OwnerClaim = 0,
        OwnerRevoke = 1,
        OwnerShareAll = 2,
        OwnerShareNone = 3,
        SwitchOnPower = 4,
        SwitchOffPower = 5,
        DeleteShip = 6,
        Destructible = 7,
        Stop = 8,
        ScaleUp = 9,
        ScaleDown = 10,
        BuiltBy = 11,
        Repair = 12,
        ConvertToShip = 13,
        ConvertToStation = 14,
    }
}
