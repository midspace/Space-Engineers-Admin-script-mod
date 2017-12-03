namespace midspace.adminscripts.Messages
{
    using ProtoBuf;

    [ProtoContract]
    public class MessageGlobalMessage : MessageBase
    {
        [ProtoMember(201)]
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
