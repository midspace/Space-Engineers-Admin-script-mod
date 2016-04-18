namespace midspace.adminscripts.Messages.Sync
{
    using ProtoBuf;
    using Sandbox.ModAPI;
    using VRage.ModAPI;

    [ProtoContract]
    public class MessageSyncBlock : MessageBase
    {
        #region fields

        [ProtoMember(1)]
        public long EntityId;

        [ProtoMember(2)]
        public SyncBlockType SyncType;

        #endregion

        #region Process

        public static void Process(IMyEntity entity, SyncBlockType syncType)
        {
            Process(entity, new MessageSyncBlock() { EntityId = entity.EntityId, SyncType = syncType });
        }

        private static void Process(IMyEntity entity, MessageSyncBlock syncEntity)
        {
            if (MyAPIGateway.Multiplayer.MultiplayerActive)
                ConnectionHelper.SendMessageToAll(syncEntity);
            else
                syncEntity.CommonProcess(entity);
        }

        #endregion

        public override void ProcessClient()
        {
            // TODO: security check.

            if (!MyAPIGateway.Entities.EntityExists(EntityId))
                return;

            CommonProcess(MyAPIGateway.Entities.GetEntityById(EntityId));
        }

        public override void ProcessServer()
        {
            // TODO: security check.

            if (!MyAPIGateway.Entities.EntityExists(EntityId))
                return;

            CommonProcess(MyAPIGateway.Entities.GetEntityById(EntityId));
        }

        private void CommonProcess(IMyEntity entity)
        {
            var block = entity as IMyFunctionalBlock;

            if (block == null)
                return;

            if (SyncType == SyncBlockType.PowerOn)
                block.RequestEnable(true);

            if (SyncType == SyncBlockType.PowerOff)
                block.RequestEnable(false);
        }
    }

    public enum SyncBlockType
    {
        PowerOn = 0x01,
        PowerOff = 0x02,
    }
}
