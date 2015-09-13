using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace midspace.adminscripts.Messages.Sync
{
    [ProtoContract]
    public class MessageSyncGod : MessageBase
    {
        [ProtoMember(1)]
        public bool Enable;

        public override void ProcessClient()
        {
            // never processed
        }

        public override void ProcessServer()
        {
            CommandGodMode.ChangeGodMode(SenderSteamId, Enable);
        }
    }
}
