using System.Collections.Generic;
using System.Linq;
using midspace.adminscripts.Messages.Sync;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace midspace.adminscripts.Protection.GameLogicComponents
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CubeGrid))]
    public class ProtectedCubeGrid : MyGameLogicComponent
    {
        private IMyCubeGrid _cubeGrid;
        private bool _isInitialized;
        private MyObjectBuilder_EntityBase _objectBuilder;

        private List<long> _cachedOwners;
        private bool _firstOwnershipChange = true;
        private List<long> _allowedChanges; 

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return copy ? (MyObjectBuilder_EntityBase)_objectBuilder.Clone() : _objectBuilder;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            _objectBuilder = objectBuilder;

            if (MyAPIGateway.Multiplayer != null && MyAPIGateway.Multiplayer.MultiplayerActive)
                _Init();

            base.Init(objectBuilder);
        }

        public override void Close()
        {
            if (_cubeGrid != null)
            {
                _cubeGrid.OnBlockOwnershipChanged -= _cubeGrid_OnBlockOwnershipChanged;
            }

            base.Close();
        }

        private void _Init()
        {
            if (_isInitialized)
                return;
            
            _isInitialized = true;

            // only init in mp

            IMyCubeGrid cubeGrid = Entity as IMyCubeGrid;
            if (cubeGrid != null)
                _cubeGrid = cubeGrid;
            else
                return;

            // init to prevent null references
            // cannot be init with cubeGrid.SmallOwners as it is null, I suspect that there are thread safety issues that's why I read it when the event is called
            _cachedOwners = new List<long>();
            _cubeGrid.OnBlockOwnershipChanged += _cubeGrid_OnBlockOwnershipChanged;
        }

        public override void UpdateBeforeSimulation()
        {
            if (!_isInitialized &&  MyAPIGateway.Multiplayer != null && MyAPIGateway.Multiplayer.MultiplayerActive)
                _Init();

            base.UpdateBeforeSimulation();
        }

        private void _cubeGrid_OnBlockOwnershipChanged(IMyCubeGrid cubeGrid)
        {
            // only execute on server instance
            if (ChatCommandLogic.Instance != null && ChatCommandLogic.Instance.ServerCfg == null)
                return;

            if (_firstOwnershipChange)
            {
                _firstOwnershipChange = false;
                _cachedOwners = new List<long>(cubeGrid.GetAllSmallOwners());
                return;
            }

            var allSmallOwners = cubeGrid.GetAllSmallOwners(); 

            if (_cachedOwners == allSmallOwners)
                return;

            // if the grid wasn't owned or a owner was removed, we dont need to do anything but update the cached owners
            if (_cachedOwners.Count == 0 || _cachedOwners.Count > allSmallOwners.Count)
            {
                _cachedOwners = new List<long>(allSmallOwners);
                return;
            }

            var newOwners = allSmallOwners.Except(_cachedOwners).ToList();

            if (newOwners.Count == 0)
                return;

            if (!ProtectionHandler.IsProtected(cubeGrid))
            {
                _cachedOwners = new List<long>(allSmallOwners);
                return;
            }

            Dictionary<long, int> blocksPerOwner = new Dictionary<long, int>();

            foreach (IMyCubeGrid attachedCubeGrid in cubeGrid.GetAttachedGrids(AttachedGrids.Static))
            {

                List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                attachedCubeGrid.GetBlocks(blocks, b => b.FatBlock != null);


                foreach (IMySlimBlock block in blocks)
                {
                    long ownerId = block.FatBlock.OwnerId;

                    // we dont want the new owners, the small owners or the 'nobody' (0)
                    if (ownerId == 0 || !attachedCubeGrid.BigOwners.Contains(ownerId) || newOwners.Contains(ownerId))
                        continue;

                    if (!blocksPerOwner.ContainsKey(ownerId))
                        blocksPerOwner.Add(ownerId, 1);
                    else
                        blocksPerOwner[ownerId]++;
                }
            }

            var sortedBpo = new List<KeyValuePair<long, int>>(blocksPerOwner.OrderBy(pair => pair.Value));

            // if we cannot identify an owner we allow the change
            if (sortedBpo.Count == 0)
            {
                _cachedOwners = new List<long>(allSmallOwners);
                return;
            }

            var bigOwner = sortedBpo[0].Key;

            List<IMySlimBlock> ownershipChangedBlocks = new List<IMySlimBlock>();
            cubeGrid.GetBlocks(ownershipChangedBlocks, b => b.FatBlock != null && newOwners.Contains(b.FatBlock.OwnerId));
            foreach (IMySlimBlock slimBlock in ownershipChangedBlocks)
            {
                var block = (Sandbox.Game.Entities.MyCubeBlock)slimBlock.FatBlock; // TODO check if the block was created/built just moments ago, do not change owner otherwise
                block.ChangeOwner(bigOwner, MyOwnershipShareModeEnum.None);
                ConnectionHelper.SendMessageToAllPlayers(new MessageSyncBlockOwner() {OwnerId = bigOwner, EntityId = block.EntityId});
                // no need to update the cached owners as we don't want them to change
            }

            // TODO maybe allow the faction to build...
        }

    }
}