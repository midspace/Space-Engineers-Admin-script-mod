using System.Timers;
using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace midspace.adminscripts.Protection
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CubePlacer))]
    public class ProtectedCubePlacer : MyGameLogicComponent
    {
        private MyObjectBuilder_EntityBase _objectBuilder;
        private bool _initialized;
        private bool _multiplayerActive;
        private Timer _cooldown;
        private bool _cooldownElapsed;
        private bool _cooldownActive;
        private IMyCubeGrid _cachedGrid;

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return copy ? (MyObjectBuilder_EntityBase)_objectBuilder.Clone() : _objectBuilder;
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
            _multiplayerActive = MyAPIGateway.Multiplayer != null &&
                                 (MyAPIGateway.Multiplayer.MultiplayerActive ||
                                  (MyAPIGateway.Session.Player != null && MyAPIGateway.Session.Player.IsHost()));

            if (!_multiplayerActive)
                return;

            _cooldown = new Timer(2000) {AutoReset = false};
            _cooldown.Elapsed += _cooldown_Elapsed;
        }

        void _cooldown_Elapsed(object sender, ElapsedEventArgs e)
        {
            _cooldownElapsed = true;
        }

        public override void UpdateBeforeSimulation()
        {
            if (_cooldownElapsed)
            {
                _cooldownElapsed = false;
                _cooldownActive = false;
            }

            if (_multiplayerActive && MyAPIGateway.CubeBuilder != null)
            {
                var cubeGrid = MyAPIGateway.CubeBuilder.FindClosestGrid();
                if (ChatCommandLogic.Instance != null && !ChatCommandLogic.Instance.AllowBuilding)
                {
                    MyAPIGateway.CubeBuilder.DeactivateBlockCreation();
                    if (!_cooldownActive)
                    {
                        MyAPIGateway.Utilities.ShowNotification("Protection is not loaded yet so any building is not allowed. Please try again later.",
                            2000,
                            MyFontEnum.Red);
                        _cooldownActive = true;
                        _cooldown.Start();
                    }
                }
                else if (_cachedGrid != null && cubeGrid != _cachedGrid)
                {
                    if (cubeGrid != null && MyAPIGateway.Session.Player != null)
                    {
                        // TODO consider permission request from server instead of client side check... downside: might take a while
                        var allSmallOwners = cubeGrid.GetAllSmallOwners();
                        if (allSmallOwners.Count > 0 && !allSmallOwners.Contains(MyAPIGateway.Session.Player.IdentityId) && ProtectionHandler.IsProtected(cubeGrid))
                        {
                            _cachedGrid = cubeGrid;
                            Deactivate();
                        }
                    }
                }
                else
                    Deactivate();
            }

            base.UpdateBeforeSimulation();
        }

        public override void Close()
        {
            if (_multiplayerActive)
                _cooldown.Close();

            base.Close();
        }

        private void Deactivate()
        {
            MyAPIGateway.CubeBuilder.DeactivateBlockCreation();
            if (!_cooldownActive)
            {
                MyAPIGateway.Utilities.ShowNotification("You are not allowed to build on this grid.",
                    2000,
                    MyFontEnum.Red);
                _cooldownActive = true;
                _cooldown.Start();
            }
        }
    }
}