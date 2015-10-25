using System.Collections.Generic;
using System.Linq;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRage.Components;
using VRage.ObjectBuilders;
using IMyCubeGrid = Sandbox.ModAPI.IMyCubeGrid;
using IMyLandingGear = Sandbox.ModAPI.IMyLandingGear;

namespace midspace.adminscripts.Protection.GameLogicComponents
{
    [MyEntityComponentDescriptor(typeof (MyObjectBuilder_LandingGear))]
    public class ProtectedLandingGear : MyGameLogicComponent
    {
        private MyObjectBuilder_EntityBase _objectBuilder;
        private IMyLandingGear _landingGear;

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return copy ? (MyObjectBuilder_EntityBase)_objectBuilder.Clone() : _objectBuilder;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            _objectBuilder = objectBuilder;

            if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                return;

            _landingGear = Entity as IMyLandingGear;

            if (_landingGear == null)
                return;

            _landingGear.StateChanged += LandingGearOnStateChanged;

            base.Init(objectBuilder);
        }

        private void LandingGearOnStateChanged(bool state)
        {
            if (!state)
                return;

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
                return;
            }
            
            if (ProtectionHandler.CanModify(player, attachedEntity))
                return;

            _landingGear.ApplyAction("Unlock");
        }

        public override void Close()
        {
            if (_landingGear == null)
                return;

            _landingGear.StateChanged -= LandingGearOnStateChanged;
            
            base.Close();
        }
    }
}