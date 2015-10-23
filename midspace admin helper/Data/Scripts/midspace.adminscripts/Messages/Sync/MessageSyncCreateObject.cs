namespace midspace.adminscripts.Messages.Sync
{
    using ProtoBuf;
    using Sandbox.Definitions;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using VRage;
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
                    if (!MyAPIGateway.Entities.EntityExists(EntityId))
                        return;

                    var itemAdded = false;
                    var entity = MyAPIGateway.Entities.GetEntityById(EntityId);
                    var count = ((MyEntity)entity).InventoryCount;

                    // Try to find the right inventory to put the item into.
                    // Ie., Refinery has 2 inventories. One for ore, one for ingots.
                    for (int i = 0; i < count; i++)
                    {
                        var inventory = ((MyEntity)entity).GetInventory(i);
                        if (inventory.CanItemsBeAdded(amount, definition.Id))
                        {
                            itemAdded = true;
                            Support.InventoryAdd(inventory, amount, definition.Id);
                            break;
                        }
                    }

                    // TODO: no messaging to players yet.
                    //if (!itemAdded)
                    //    MyAPIGateway.Utilities.ShowMessage("Failed", "Invalid container or Full container. Could not add the item.");
                    break;
            }
        }
    }

    public enum SyncCreateObjectType
    {
        Floating,
        Inventory
    }
}
