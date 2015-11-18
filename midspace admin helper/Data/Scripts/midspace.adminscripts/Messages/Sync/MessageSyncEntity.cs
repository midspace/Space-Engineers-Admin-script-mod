using System;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace midspace.adminscripts.Messages.Sync
{
    [ProtoContract]
    public class MessageSyncEntity : MessageBase
    {
        #region fields

        [ProtoMember(1)]
        public long EntityId;

        [ProtoMember(2)]
        public SyncEntityType SyncType;

        [ProtoMember(3)]
        public Vector3 Velocity;

        [ProtoMember(4)]
        public Vector3D Position;

        [ProtoMember(5)]
        public MatrixD Matrix;

        #endregion

        #region Process

        public static void Process(IMyEntity entity, SyncEntityType syncType)
        {
            Process(entity, new MessageSyncEntity() { EntityId = entity.EntityId, SyncType = syncType });
        }

        public static void Process(IMyEntity entity, SyncEntityType syncType, Vector3D position)
        {
            Process(entity, new MessageSyncEntity() { EntityId = entity.EntityId, SyncType = syncType, Position = position });
        }

        public static void Process(IMyEntity entity, SyncEntityType syncType, Vector3 velocity, Vector3D position)
        {
            Process(entity, new MessageSyncEntity() { EntityId = entity.EntityId, SyncType = syncType, Velocity = velocity, Position = position });
        }

        public static void Process(IMyEntity entity, SyncEntityType syncType, Vector3 velocity, Vector3D position, MatrixD matrix)
        {
            Process(entity, new MessageSyncEntity() { EntityId = entity.EntityId, SyncType = syncType, Velocity = velocity, Position = position, Matrix = matrix });
        }

        private static void Process(IMyEntity entity, MessageSyncEntity syncEntity)
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
            if (entity == null)
                return;

            if (SyncType.HasFlag(SyncEntityType.Stop))
                entity.Stop();

            // The Physics.LinearVelocity doesn't change the player speed quickly enough before SetPosition is called, as
            // the player will smack into the other obejct before it's correct velocity is actually registered.
            if (SyncType.HasFlag(SyncEntityType.Velocity) && entity.Physics != null)
                entity.Physics.LinearVelocity = Velocity;

            // The SetWorldMatrix doesn't rotate the player quickly enough before SetPosition is called, as 
            // the player will bounce off objects before it's correct orentation is actually registered.
            if (SyncType.HasFlag(SyncEntityType.Matrix))
                entity.SetWorldMatrix(Matrix);

            if (SyncType.HasFlag(SyncEntityType.Position))
                entity.SetPosition(Position);
        }
    }

    [Flags]
    public enum SyncEntityType
    {
        Position = 0x01,
        Stop = 0x02,
        Velocity = 0x4,
        Matrix = 0x8
    }
}
