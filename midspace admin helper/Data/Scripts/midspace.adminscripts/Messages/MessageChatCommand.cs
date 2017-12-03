namespace midspace.adminscripts.Messages
{
    using ProtoBuf;

    [ProtoContract]
    public class MessageChatCommand : MessageBase
    {
        [ProtoMember(201)]
        public long PlayerId;

        [ProtoMember(202)]
        public string TextCommand;

        public override void ProcessClient()
        {
            // never processed on client
        }

        public override void ProcessServer()
        {
            if (!ChatCommandService.ProcessServerMessage(SenderSteamId, PlayerId, TextCommand))
            {
                //MyAPIGateway.Utilities.SendMessage(SenderSteamId, "CHECK", "ProcessServerMessage failed.");
            }
        }
    }
}
