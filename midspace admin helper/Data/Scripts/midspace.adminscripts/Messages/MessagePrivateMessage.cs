using ProtoBuf;
using Sandbox.ModAPI;

namespace midspace.adminscripts.Messages
{
    [ProtoContract]
    public class MessagePrivateMessage : MessageBase
    {
        [ProtoMember(1)]
        public ChatMessage ChatMessage;

        [ProtoMember(2)]
        public ulong Receiver;

        public override void ProcessClient()
        {
            CommandPrivateMessage.LastWhisperId = ChatMessage.Sender.SteamId;
            MyAPIGateway.Utilities.ShowMessage(string.Format("{0} {1}", ChatMessage.Sender.PlayerName, "whispers"), ChatMessage.Text);
        }

        public override void ProcessServer()
        {
            ConnectionHelper.SendMessageToPlayer(Receiver, this);
            ChatCommandLogic.Instance.ServerCfg.LogPrivateMessage(ChatMessage, Receiver);
        }
    }
}
