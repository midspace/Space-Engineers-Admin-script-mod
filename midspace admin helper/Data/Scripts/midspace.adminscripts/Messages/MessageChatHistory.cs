using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace midspace.adminscripts.Messages
{
    [ProtoContract]
    public class MessageChatHistory : MessageBase
    {
        [ProtoMember]
        public List<ChatMessage> ChatHistory;

        [ProtoMember]
        public uint EntryCount;

        public override void ProcessClient()
        {
            CommandChatHistory.DisplayChatHistory(ChatHistory);
        }

        public override void ProcessServer()
        {
            ChatCommandLogic.Instance.ServerCfg.SendChatHistory(SenderSteamId, EntryCount);
        }
    }
}
