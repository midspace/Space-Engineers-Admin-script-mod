namespace midspace.adminscripts.Messages
{
    using ProtoBuf;
    using VRageMath;

    [ProtoContract]
    public class MessageSaveTeleportHistory : MessageBase
    {
        [ProtoMember(1)]
        public long PlayerId;

        [ProtoMember(2)]
        public Vector3D Position;

        public static void SaveToHistory(long playerId, Vector3D position)
        {
            ConnectionHelper.SendMessageToServer(new MessageSaveTeleportHistory { PlayerId = playerId, Position = position });
        }

        public override void ProcessClient()
        {
        }

        public override void ProcessServer()
        {
            CommandTeleportBack.SaveTeleportInHistory(PlayerId, Position);
        }
    }
}
