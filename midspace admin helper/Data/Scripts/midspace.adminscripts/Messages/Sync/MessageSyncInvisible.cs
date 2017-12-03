namespace midspace.adminscripts.Messages
{
    using ProtoBuf;

    [ProtoContract]
    public class MessageSyncInvisible : MessageBase
    {
        [ProtoMember(201)]
        public long PlayerId;

        [ProtoMember(202)]
        public bool VisibleState;

        public override void ProcessClient()
        {
            CommandInvisible.ProcessCommon(this);
        }

        public override void ProcessServer()
        {
            CommandInvisible.ProcessCommon(this);
        }
    }
}
