namespace midspace.adminscripts.Messages.Sync
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using midspace.adminscripts.Messages.Communication;
    using ProtoBuf;
    using Sandbox.Common.ObjectBuilders;
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
                    if (searchEntity.Substring(0, 1) == "#" && Int32.TryParse(searchEntity.Substring(1), out index) && index > 0 && index <= shipCache.Count)
                    {
                        selectedShips.Add((IMyCubeGrid)shipCache[index - 1]);
                    }
                }
            }

            if (selectedShips.Count == 0)
            {
                MessageClientTextMessage.SendMessage(steamId, "Server", "No ships selected or found.");
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

                            MessageClientTextMessage.SendMessage(steamId, "Server", string.Format("Grid {0} Claimed by player {1}.", selectedShip.DisplayName, player.DisplayName));
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
                            MessageClientTextMessage.SendMessage(steamId, "Server", string.Format("Grid {0} Revoked of all ownership.", selectedShip.DisplayName));
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
                            MessageClientTextMessage.SendMessage(steamId, "Server", string.Format("Grid {0} Shared.", selectedShip.DisplayName));
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

        public static void SetDestructible(IMyEntity shipEntity, bool destructible, ulong steamId)
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
        Stop
    }
}
