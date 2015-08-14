using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace midspace.adminscripts.Messages.Sync
{
    [ProtoContract]
    public class MessageSyncClaim : MessageBase
    {
        [ProtoMember(1)]
        public long EntityId;

        [ProtoMember(2)]
        public long PlayerId;

        public override void ProcessClient()
        {
            // never called on client
        }

        public override void ProcessServer()
        {
            var players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, p => p != null && p.PlayerID == PlayerId);
            IMyPlayer player = players.FirstOrDefault();
            var entity = MyAPIGateway.Entities.GetEntityById(EntityId) as IMyCubeGrid;

            if (entity != null && player != null)
            {
                entity.ChangeGridOwnership(player.PlayerID, MyOwnershipShareModeEnum.All);
                ConnectionHelper.SendChatMessage(SenderSteamId, string.Format("Grid {0} Claimed by player {1}.", entity.DisplayName, player.DisplayName));
            }
        }
    }
}
