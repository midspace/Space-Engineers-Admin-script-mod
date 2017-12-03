namespace midspace.adminscripts.Messages.Sync
{
    using ProtoBuf;
    using VRageMath;

    public enum SyncFloatingObject : byte
    {
        Count = 0,
        Collect = 1,
        Pull = 2,
        Delete = 3
    }

    [ProtoContract]
    public class MessageSyncFloatingObjects : MessageBase
    {
        [ProtoMember(201)]
        public SyncFloatingObject Type;

        [ProtoMember(202)]
        public Vector3D Position;

        [ProtoMember(203)]
        public double Range;

        [ProtoMember(204)]
        public double Velocity;

        public override void ProcessClient()
        {
        }

        public override void ProcessServer()
        {
            // TODO: check security

            switch (Type)
            {
                case SyncFloatingObject.Count:
                    CommandObjectsCount.CountObjects(SenderSteamId);
                    break;
                case SyncFloatingObject.Collect:
                    CommandObjectsCollect.CollectObjects(SenderSteamId, Position, Range);
                    break;
                case SyncFloatingObject.Pull:
                    CommandObjectsPull.PullObjects(SenderSteamId, Position, Range, Velocity);
                    break;
                case SyncFloatingObject.Delete:
                    CommandObjectsDelete.DeleteObjects(SenderSteamId, Position, Range);
                    break;
            }
        }
    }
}
