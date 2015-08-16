namespace midspace.adminscripts.Messages
{
    using ProtoBuf;

    [ProtoContract]
    public class MessageSyncInvisible : MessageBase
    {
        [ProtoMember(1)]
        public long PlayerId;

        [ProtoMember(2)]
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
