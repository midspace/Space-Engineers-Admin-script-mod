namespace midspace.adminscripts
{
    using System.Collections.Generic;

    using Sandbox.Common;
    using Sandbox.Common.Components;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using VRageMath;
    using VRage.Common.Voxels;

    /// <summary>
    /// For editing Voxels.
    /// </summary>
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_HandDrill))]
    public class VoxelEditLogic : MyGameLogicComponent
    {
        private bool _isInRange;
        private MyObjectBuilder_EntityBase _objectBuilder;

        public override void Close()
        {
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            _objectBuilder = objectBuilder;
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void MarkForClose()
        {
        }

        /// <summary>
        /// called once only for stations, as it doesn't get updated again.
        /// </summary>
        public override void UpdateAfterSimulation()
        {
        }

        /// <summary>
        /// called multiple times for ships, to be kept up to date.
        /// </summary>
        public override void UpdateAfterSimulation10()
        {
            if (CommandVoxelClear.ActiveVoxelDeleter)
            {
                var worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.WorldMatrix;
                var position = worldMatrix.Translation + worldMatrix.Forward * 1.6f + worldMatrix.Up * 1.35f + worldMatrix.Right * 0.1f;

                var currentAsteroidList = new List<IMyVoxelMap>();
                var bb = new BoundingBoxD(position - 0.2f, position + 0.2f);
                MyAPIGateway.Session.VoxelMaps.GetInstances(currentAsteroidList, v => v.IsBoxIntersectingBoundingBoxOfThisVoxelMap(ref bb));

                if (currentAsteroidList.Count > 0)
                {
                    _isInRange = true;
                    var storage = currentAsteroidList[0].Storage;

                    var point = new Vector3I(position - currentAsteroidList[0].PositionLeftBottomCorner);
                    var cache = new MyStorageDataCache();
                    var min = (point / 64) * 64;
                    var max = min + 63;
                    var size = max - min;
                    cache.Resize(size);
                    storage.ReadRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, (int)VRageRender.MyLodTypeEnum.LOD0, min, max);

                    Vector3I p = point - min;
                    var content = cache.Content(ref p);
                    if (content > 0)
                    {
                        content = 0x00;
                        cache.Content(ref p, content);
                        storage.WriteRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, min, max);
                    }
                    //storage = null;
                }
                else
                {
                    _isInRange = false;
                }
            }

            if (CommandVoxelSet.ActiveVoxelSetter)
            {
                var worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.WorldMatrix;
                CommandVoxelSet.ActiveVoxelSetterPosition = worldMatrix.Translation + worldMatrix.Forward * 1.6f + worldMatrix.Up * 1.35f + worldMatrix.Right * 0.1f;
            }
            else
            {
                CommandVoxelSet.ActiveVoxelSetterPosition = null;
            }
        }

        public override void UpdateAfterSimulation100()
        {
            if (CommandVoxelClear.ActiveVoxelDeleter)
            {
                if (_isInRange)
                    MyAPIGateway.Utilities.ShowNotification("Voxel Clear active", 1000, MyFontEnum.Green);
                else
                    MyAPIGateway.Utilities.ShowNotification("Voxel Clear active", 1000, MyFontEnum.Red);
            }
            if (CommandVoxelSet.ActiveVoxelSetter)
            {
                //MyAPIGateway.Utilities.ShowNotification(string.Format("Voxel setter is active [{0}]", MyWorldLogic.ActiveVoxelSetterPosition), 1000, MyFontEnum.Green);
                MyAPIGateway.Utilities.ShowNotification("Voxel setter is active", 1000, MyFontEnum.Green);
            }
        }

        public override void UpdateBeforeSimulation()
        {
        }

        public override void UpdateBeforeSimulation10()
        {
        }

        public override void UpdateBeforeSimulation100()
        {
        }

        public override void UpdateOnceBeforeFrame()
        {
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return copy ? Entity.GetObjectBuilder() : _objectBuilder;
        }
    }
}