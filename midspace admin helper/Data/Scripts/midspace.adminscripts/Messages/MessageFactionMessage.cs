using System;
using ProtoBuf;
using Sandbox.ModAPI;

namespace midspace.adminscripts.Messages
{
    [ProtoContract]
    public class MessageFactionMessage : MessageBase
    {
        [ProtoMember(1)]
        public ChatMessage ChatMessage;

        [ProtoMember(2)]
        public ulong Receiver;

        [ProtoMember(3)]
        public FactionMessageType Type;

        public override void ProcessClient()
        {
            var senderName = ChatMessage.Sender.PlayerName;
            var prefix = "[F]";
            switch (Type)
            {
                case FactionMessageType.OwnFaction:
                    prefix = "[F]";
                    break;
                case FactionMessageType.AlliedFacitons: 
                    prefix = "[A]";
                    break;
                case FactionMessageType.AlliedWithHub:
                    prefix = "[H]";
                    break;
                case FactionMessageType.Broadcast:
                    prefix = "[B]";
                    break;
            }

            MyAPIGateway.Utilities.ShowMessage(String.Format("{0} {1}", prefix, senderName), ChatMessage.Text);
        }

        public override void ProcessServer()
        {
            ConnectionHelper.SendMessageToPlayer(Receiver, this);
            // TODO log faction messages
            // the code below creates too many entries in a file where I don't want to have them...
            //ChatCommandLogic.Instance.ServerCfg.LogPrivateMessage(ChatMessage, Receiver);
        }
    }

    public enum FactionMessageType
    {
        OwnFaction,
        AlliedFacitons,
        AlliedWithHub,
        Broadcast
    }
}
