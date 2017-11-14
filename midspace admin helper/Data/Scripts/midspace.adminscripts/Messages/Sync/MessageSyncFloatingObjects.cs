namespace midspace.adminscripts.Messages.Sync
{
    using ProtoBuf;
    using VRageMath;

    public enum SyncFloatingObject
    {
        Count, Collect, Pull, Delete
    }

    [ProtoContract]
    public class MessageSyncFloatingObjects : MessageBase
    {
        [ProtoMember(1)]
        public SyncFloatingObject Type;

        [ProtoMember(2)]
        public Vector3D Position;

        [ProtoMember(3)]
        public double Range;

        [ProtoMember(4)]
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
