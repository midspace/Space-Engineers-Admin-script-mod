using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace midspace.adminscripts.Messages
{
    [ProtoContract]
    public class MessageFactionMessage : MessageBase
    {
        [ProtoMember(1)]
        public ChatMessage ChatMessage;

        [ProtoMember(2)]
        public ulong Receiver;

        public override void ProcessClient()
        {
            var senderName = ChatMessage.Sender.PlayerName;
            MyAPIGateway.Utilities.ShowMessage(senderName, ChatMessage.Text);
        }

        public override void ProcessServer()
        {
            ConnectionHelper.SendMessageToPlayer(Receiver, this);
            ChatCommandLogic.Instance.ServerCfg.LogPrivateMessage(ChatMessage, Receiver);
        }
    }
}
