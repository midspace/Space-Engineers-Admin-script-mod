namespace midspace.adminscripts.Messages.Sync
{
    using System.Collections.Generic;
    using ProtoBuf;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;

    [ProtoContract]
    public class MessageSyncShare : MessageBase
    {
        [ProtoMember(1)]
        public long EntityId;

        public override void ProcessClient()
        {
            // never called on client
        }

        public override void ProcessServer()
        {
            var selectedShip = MyAPIGateway.Entities.GetEntityById(EntityId) as IMyCubeGrid;

            var grids = selectedShip.GetAttachedGrids(AttachedGrids.Static);
            foreach (var grid in grids)
            {
                var blocks = new List<IMySlimBlock>();
                grid.GetBlocks(blocks, f => f.FatBlock != null && f.FatBlock.OwnerId != 0);

                foreach (var block in blocks)
                    block.FatBlock.ChangeOwner(block.FatBlock.OwnerId, MyOwnershipShareModeEnum.All);
            }
        }
    }
}
