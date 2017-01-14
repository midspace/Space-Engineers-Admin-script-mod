namespace midspace.adminscripts.Messages.Sync
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using ProtoBuf;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Definitions;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;

    [ProtoContract]
    public class MessageSyncGridChange : MessageBase
    {
        [ProtoMember(1)]
        public SyncGridChangeType SyncType;

        [ProtoMember(2)]
        public long EntityId;

        [ProtoMember(3)]
        public string SearchEntity;

        [ProtoMember(4)]
        public long PlayerId;

        [ProtoMember(5)]
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

            if (entityId != 0)
            {
                var selectedShip = MyAPIGateway.Entities.GetEntityById(entityId) as IMyCubeGrid;
                if (selectedShip != null)
                    selectedShips.Add(selectedShip);
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
                        selectedShips.Add((IMyCubeGrid)shipCache[index - 1]);
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
                        MyAPIGateway.Players.GetPlayers(players, p => p != null && p.PlayerID == playerId);
                        IMyPlayer player = players.FirstOrDefault();

                        if (player == null)
                            return;

                        foreach (var selectedShip in selectedShips)
                        {
                            var grids = selectedShip.GetAttachedGrids(AttachedGrids.Static);
                            foreach (var grid in grids)
                                grid.ChangeGridOwnership(player.PlayerID, MyOwnershipShareModeEnum.All);

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

                case SyncGridChangeType.SwitchOnPower:
                    {
                        int reactors;
                        int batteries;
                        TurnOnShips(selectedShips, out reactors, out batteries);
                        MyAPIGateway.Utilities.SendMessage(steamId, selectedShips.First().DisplayName, "{0} Reactors, {1} Batteries turned on.", reactors, batteries);
                    }
                    break;

                case SyncGridChangeType.SwitchOffPower:
                    {
                        int reactors;
                        int batteries;
                        TurnOffShips(selectedShips, out reactors, out batteries);
                        MyAPIGateway.Utilities.SendMessage(steamId, selectedShips.First().DisplayName, "{0} Reactors, {1} Batteries turned off.", reactors, batteries);
                    }
                    break;
                case SyncGridChangeType.DeleteShip:
                    {
                        if (selectedShips.Count == 1)
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
                        MyAPIGateway.Players.GetPlayers(players, p => p != null && p.PlayerID == playerId);
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
            }
        }

        private void TurnOnShips(IEnumerable<IMyEntity> shipList, out int reactorCounter, out int batteryCounter)
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

        private void TurnOffShips(IEnumerable<IMyEntity> shipList, out int reactorCounter, out int batteryCounter)
        {
            reactorCounter = 0;
            batteryCounter = 0;
            foreach (var selectedShip in shipList)
            {
                int reactors;
                int batteries;
                TurnOffShip(selectedShip, out reactors, out batteries);
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
                // ejects any player prior to deleting the grid.
                cubeGrid.EjectControllingPlayers();

                var name = cubeGrid.DisplayName;

                // This will Delete the entity and sync to all.
                // Using this, also works with player ejection in the same Tick.

                cubeGrid.SyncObject.SendCloseRequest();

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
            shipEntity.SyncObject.SendCloseRequest();

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

        // TODO: deal with cubes that need to be rotated.
        // LargeBlockBeacon, SmallBlockBeacon, ConveyorTube, ConveyorTubeSmall

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
                cubeGrid.SyncObject.SendCloseRequest();

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

    public enum SyncGridChangeType
    {
        OwnerClaim,
        OwnerRevoke,
        OwnerShareAll,
        SwitchOnPower,
        SwitchOffPower,
        DeleteShip,
        Destructible,
        Stop,
        ScaleUp,
        ScaleDown,
        BuiltBy,
        Repair
    }
}
