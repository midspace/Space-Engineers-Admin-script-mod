namespace midspace.adminscripts.Messages
{
    using ProtoBuf;
    using Sandbox.ModAPI;

    [ProtoContract]
    public class MessagePrivateMessage : MessageBase
    {
        [ProtoMember(201)]
        public ChatMessage ChatMessage;

        [ProtoMember(202)]
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
