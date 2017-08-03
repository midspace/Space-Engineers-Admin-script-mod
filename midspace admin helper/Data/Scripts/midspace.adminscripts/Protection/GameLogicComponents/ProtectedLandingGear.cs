namespace midspace.adminscripts.Protection.GameLogicComponents
{
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using SpaceEngineers.Game.ModAPI;
    using VRage.Game.Components;
    using VRage.Game.ModAPI;
    using VRage.ObjectBuilders;

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LandingGear), true)] // leave useEntityUpdate=true, otherwise the protection won't work.
    public class ProtectedLandingGear : MyGameLogicComponent
    {
        private MyObjectBuilder_EntityBase _objectBuilder;
        private IMyLandingGear _landingGear;
        private bool _isInitialized;

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return copy ? (MyObjectBuilder_EntityBase)_objectBuilder.Clone() : _objectBuilder;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            _objectBuilder = objectBuilder;

            if ( MyAPIGateway.Multiplayer != null && MyAPIGateway.Multiplayer.MultiplayerActive)
               _Init();

            base.Init(objectBuilder);
        }

        private void _Init()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;
            _landingGear = Entity as IMyLandingGear;

            if (_landingGear == null)
                return;

            // StateChanged is Obsolete, but the replacement is blocked from use in ModAPI.
            _landingGear.StateChanged += LandingGearOnStateChanged;
            //_landingGear.LockModeChanged += LandingGearOnLockModeChanged;
        }

        public override void UpdateBeforeSimulation()
        {
            if (!_isInitialized && MyAPIGateway.Multiplayer != null && MyAPIGateway.Multiplayer.MultiplayerActive)
                _Init();

            base.UpdateBeforeSimulation();
        }

        private void LandingGearOnStateChanged(bool state)    
        {
                if (!state)
                return;
        //private void LandingGearOnLockModeChanged(IMyLandingGear myLandingGear, SpaceEngineers.Game.ModAPI.Ingame.LandingGearMode landingGearMode)
        //{
        //    if (landingGearMode != SpaceEngineers.Game.ModAPI.Ingame.LandingGearMode.Locked)
        //        return;

            var ship = _landingGear.GetTopMostParent(typeof(IMyCubeGrid));
            if (ship == null)
                return;

            IMyPlayer player = null;
            foreach (var workingCockpit in _landingGear.CubeGrid.FindWorkingCockpits())
            {
                player = MyAPIGateway.Players.GetPlayerControllingEntity(workingCockpit.Entity);

                if (player != null)
                    break;
            }

            var attachedEntity = _landingGear.GetAttachedEntity() as IMyCubeGrid;

            if (attachedEntity == null)
                return;

            if (!ProtectionHandler.IsProtected(attachedEntity))
                return;

            if (player == null)
            {
                _landingGear.ApplyAction("Unlock");
                // we turn it off to prevent 'spamming'
                _landingGear.Enabled = false;
                return;
            }

            if (ProtectionHandler.CanModify(player, attachedEntity))
                return;

            _landingGear.ApplyAction("Unlock");
            // we turn it off to prevent 'spamming'
            _landingGear.Enabled = false;
        }

        public override void Close()
        {
            if (!_isInitialized || _landingGear == null)
                return;

            _landingGear.StateChanged -= LandingGearOnStateChanged;
            //_landingGear.LockModeChanged -= LandingGearOnLockModeChanged;

            base.Close();
        }
    }
}