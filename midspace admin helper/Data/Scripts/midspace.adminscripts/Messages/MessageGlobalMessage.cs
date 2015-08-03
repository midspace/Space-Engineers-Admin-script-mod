using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace midspace.adminscripts.Messages
{
    [ProtoContract]
    public class MessageGlobalMessage : MessageBase
    {
        [ProtoMember(1)]
        public ChatMessage ChatMessage;

        public override void ProcessClient()
        {
            // never processed on clients
        }

        public override void ProcessServer()
        {
            ChatCommandLogic.Instance.ServerCfg.LogGlobalMessage(ChatMessage);
        }
    }
}
