namespace midspace.adminscripts.Messages.Sync
{
    using ProtoBuf;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;

    [ProtoContract]
    public class MessageSyncBlockOwner : MessageBase
    {
        [ProtoMember(201)]
        public long OwnerId;

        [ProtoMember(202)]
        public long EntityId;

        public override void ProcessClient()
        {
            IMyEntity entity;
            if (MyAPIGateway.Entities.TryGetEntityById(EntityId, out entity) && entity is IMyCubeBlock)
                ((MyCubeBlock)entity).ChangeOwner(OwnerId, MyOwnershipShareModeEnum.None);
        }

        public override void ProcessServer()
        {
            // always sent from server
        }
    }
}