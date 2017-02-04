namespace midspace.adminscripts.Protection.GameLogicComponents
{
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using SpaceEngineers.Game.ModAPI;
    using VRage.Game.Components;
    using VRage.Game.ModAPI;
    using VRage.ObjectBuilders;

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LandingGear), true)]
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

            _landingGear.LockModeChanged += LandingGearOnLockModeChanged;
        }

        public override void UpdateBeforeSimulation()
        {
            if (!_isInitialized && MyAPIGateway.Multiplayer != null && MyAPIGateway.Multiplayer.MultiplayerActive)
                _Init();

            base.UpdateBeforeSimulation();
        }

        private void LandingGearOnLockModeChanged(IMyLandingGear myLandingGear, SpaceEngineers.Game.ModAPI.Ingame.LandingGearMode landingGearMode)
        {
            if (landingGearMode != SpaceEngineers.Game.ModAPI.Ingame.LandingGearMode.Locked)
                return;

            var ship = myLandingGear.GetTopMostParent(typeof(IMyCubeGrid));
            if (ship == null)
                return;

            IMyPlayer player = null;
            foreach (var workingCockpit in myLandingGear.CubeGrid.FindWorkingCockpits())
            {
                player = MyAPIGateway.Players.GetPlayerControllingEntity(workingCockpit.Entity);

                if (player != null)
                    break;
            }

            var attachedEntity = myLandingGear.GetAttachedEntity() as IMyCubeGrid;

            if (attachedEntity == null)
                return;

            if (!ProtectionHandler.IsProtected(attachedEntity))
                return;

            if (player == null)
            {
                myLandingGear.ApplyAction("Unlock");
                // we turn it off to prevent 'spamming'
                myLandingGear.Enabled = false;
                return;
            }

            if (ProtectionHandler.CanModify(player, attachedEntity))
                return;

            myLandingGear.ApplyAction("Unlock");
            // we turn it off to prevent 'spamming'
            myLandingGear.Enabled = false;
        }

        public override void Close()
        {
            if (!_isInitialized || _landingGear == null)
                return;

            _landingGear.LockModeChanged -= LandingGearOnLockModeChanged;

            base.Close();
        }
    }
}