namespace midspace.adminscripts.Messages.Sync
{
    using midspace.adminscripts.Messages.Communication;
    using ProtoBuf;
    using Sandbox.Definitions;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using VRage;
    using VRage.Game.Entity;
    using VRageMath;

    [ProtoContract]
    public class MessageSyncCreateObject : MessageBase
    {
        [ProtoMember(1)]
        public long EntityId;

        [ProtoMember(2)]
        public SyncCreateObjectType Type;

        [ProtoMember(3)]
        public string TypeId;

        [ProtoMember(4)]
        public string SubtypeName;

        [ProtoMember(5)]
        public decimal Amount;

        [ProtoMember(6)]
        public Vector3D Position;

        public override void ProcessClient()
        {
            // never processed
        }

        public override void ProcessServer()
        {
            // TODO: check security

            var definition = MyDefinitionManager.Static.GetDefinition(TypeId, SubtypeName);
            if (definition == null)
                return;

            MyFixedPoint amount = (MyFixedPoint)Amount;

            switch (Type)
            {
                case SyncCreateObjectType.Floating:
                    Support.InventoryDrop(Position, amount, definition.Id);
                    break;
                case SyncCreateObjectType.Inventory:
                    {
                        if (!MyAPIGateway.Entities.EntityExists(EntityId))
                        {
                            MessageClientTextMessage.SendMessage(SenderSteamId, "Failed", "Cannot find the specified Entity.");
                            return;
                        }

                        var entity = (MyEntity)MyAPIGateway.Entities.GetEntityById(EntityId);

                        if (!Support.InventoryAdd(entity, amount, definition.Id))
                            // Send message to player.
                            MessageClientTextMessage.SendMessage(SenderSteamId, "Failed", "Invalid container or Full container. Could not add the item.");
                    }
                    break;
                case SyncCreateObjectType.Clear:
                    new CommandInventoryClear().ClearInventory(SenderSteamId, EntityId);
                    break;
            }
        }
    }

    public enum SyncCreateObjectType
    {
        Floating,
        Inventory,
        Clear
    }
}
