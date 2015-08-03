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
        [ProtoMember(1)]
        public List<ChatMessage> ChatHistory;

        [ProtoMember(2)]
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
