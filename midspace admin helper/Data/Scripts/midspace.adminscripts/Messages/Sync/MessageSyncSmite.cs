using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace midspace.adminscripts.Messages.Sync
{
    [ProtoContract]
    public class MessageSyncSmite : MessageBase
    {
        [ProtoMember(1)]
        public ulong SteamId;

        public override void ProcessClient()
        {
            CommandPlayerSmite.Smite(MyAPIGateway.Session.Player);
        }

        public override void ProcessServer()
        {
            ConnectionHelper.SendMessageToPlayer(SteamId, this);
        }
    }
}
