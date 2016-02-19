namespace midspace.adminscripts.Messages.Sync
{
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using VRage.Game;

    [ProtoContract]
    public class MessageSyncGridOwner : MessageBase
    {
        [ProtoMember(1)]
        public long EntityId;

        [ProtoMember(2)]
        public SyncOwnershipType SyncType;

        [ProtoMember(2)]
        public long PlayerId;

        public static void SendMessage(long entityId, SyncOwnershipType syncType, long playerId = 0)
        {
            ConnectionHelper.SendMessageToServer(new MessageSyncGridOwner() { EntityId = entityId, SyncType = syncType, PlayerId = playerId });
        }

        public override void ProcessClient()
        {
            // never called on client
        }

        public override void ProcessServer()
        {
            var selectedShip = MyAPIGateway.Entities.GetEntityById(EntityId) as IMyCubeGrid;
            if (selectedShip == null)
                return;

            switch (SyncType)
            {
                case SyncOwnershipType.Claim:
                    {
                        var players = new List<IMyPlayer>();
                        MyAPIGateway.Players.GetPlayers(players, p => p != null && p.PlayerID == PlayerId);
                        IMyPlayer player = players.FirstOrDefault();

                        if (player == null)
                            return;

                        var grids = selectedShip.GetAttachedGrids(AttachedGrids.Static);
                        foreach (var grid in grids)
                            grid.ChangeGridOwnership(player.PlayerID, MyOwnershipShareModeEnum.All);
                        ConnectionHelper.SendChatMessage(SenderSteamId, string.Format("Grid {0} Claimed by player {1}.", selectedShip.DisplayName, player.DisplayName));
                    }
                    break;

                case SyncOwnershipType.Revoke:
                    {
                        var grids = selectedShip.GetAttachedGrids(AttachedGrids.Static);
                        foreach (var grid in grids)
                            grid.ChangeGridOwnership(0, MyOwnershipShareModeEnum.All);
                        ConnectionHelper.SendChatMessage(SenderSteamId, string.Format("Grid {0} Revoked of all ownership.", selectedShip.DisplayName));
                    }
                    break;

                case SyncOwnershipType.ShareAll:
                    {
                        var grids = selectedShip.GetAttachedGrids(AttachedGrids.Static);
                        foreach (var grid in grids)
                        {
                            var blocks = new List<IMySlimBlock>();
                            grid.GetBlocks(blocks, f => f.FatBlock != null && f.FatBlock.OwnerId != 0);

                            foreach (var block in blocks)
                                block.FatBlock.ChangeOwner(block.FatBlock.OwnerId, MyOwnershipShareModeEnum.All);
                        }
                        ConnectionHelper.SendChatMessage(SenderSteamId, string.Format("Grid {0} Shared.", selectedShip.DisplayName));
                    }
                    break;
            }
        }
    }

    public enum SyncOwnershipType
    {
        Claim,
        Revoke,
        ShareAll
    }
}
