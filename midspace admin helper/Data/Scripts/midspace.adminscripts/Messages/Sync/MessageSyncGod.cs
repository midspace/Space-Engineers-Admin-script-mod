namespace midspace.adminscripts.Messages.Sync
{
    using ProtoBuf;

    [ProtoContract]
    public class MessageSyncGod : MessageBase
    {
        [ProtoMember(201)]
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
