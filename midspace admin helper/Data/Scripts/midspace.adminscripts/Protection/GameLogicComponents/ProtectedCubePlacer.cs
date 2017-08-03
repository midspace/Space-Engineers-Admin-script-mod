namespace midspace.adminscripts.Protection.GameLogicComponents
{
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRage.Game.Components;
    using VRage.Game.ModAPI;
    using VRage.ObjectBuilders;

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CubePlacer), true)]  // leave useEntityUpdate=true, otherwise the protection won't work.
    public class ProtectedCubePlacer : MyGameLogicComponent
    {
        private MyObjectBuilder_EntityBase _objectBuilder;
        private bool _initialized;
        private bool _multiplayerActive;
        private IMyCubeGrid _cachedGrid;

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return copy ? (MyObjectBuilder_EntityBase) _objectBuilder.Clone() : _objectBuilder;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            _objectBuilder = objectBuilder;

            if (!_initialized)
                _Init();

            base.Init(objectBuilder);
        }

        private void _Init()
        {
            _initialized = true;
            _multiplayerActive = MyAPIGateway.Session.OnlineMode != MyOnlineModeEnum.OFFLINE;
        }

        public override void UpdateBeforeSimulation()
        {
            if (_multiplayerActive && MyAPIGateway.CubeBuilder != null &&
                MyAPIGateway.CubeBuilder.BlockCreationIsActivated && MyAPIGateway.Session.Player != null &&
                MyAPIGateway.Session.Player.Controller.ControlledEntity != null)
            {
                if (ChatCommandLogic.Instance != null && !ChatCommandLogic.Instance.AllowBuilding)
                {
                    MyAPIGateway.CubeBuilder.DeactivateBlockCreation();
                    MyAPIGateway.Utilities.ShowNotification(
                        "Protection is not loaded yet so any building is not allowed. Please try again later.",
                        2000,
                        MyFontEnum.Red);
                }
                else if (ProtectionHandler.Config.ProtectionEnabled)
                {
                    IMyCubeGrid cubeGrid = MyAPIGateway.CubeBuilder.FindClosestGrid();
                    if (_cachedGrid == null || (_cachedGrid != null && cubeGrid != _cachedGrid))
                    {
                        if (cubeGrid != null && MyAPIGateway.Session.Player != null)
                        {
                            // TODO consider permission request from server instead of client side check... downside: might take a while
                            if (!ProtectionHandler.CanModify(MyAPIGateway.Session.Player, cubeGrid) &&
                                ProtectionHandler.IsProtected(cubeGrid))
                            {
                                _cachedGrid = cubeGrid;
                                Deactivate();
                            }
                        }
                    }
                    else
                        Deactivate();
                }
            }
            else if (!_multiplayerActive && (_multiplayerActive != (MyAPIGateway.Session.OnlineMode != MyOnlineModeEnum.OFFLINE))) 
                // we need to update it because it is not correctly initialized if the cube placer is created when the game loads
                _multiplayerActive = MyAPIGateway.Session.OnlineMode != MyOnlineModeEnum.OFFLINE;

            base.UpdateBeforeSimulation();
        }

        private void Deactivate()
        {
            MyAPIGateway.CubeBuilder.DeactivateBlockCreation();
            MyAPIGateway.Utilities.ShowNotification("You are not allowed to build on this grid.",
                2000,
                MyFontEnum.Red);

        }
    }
}