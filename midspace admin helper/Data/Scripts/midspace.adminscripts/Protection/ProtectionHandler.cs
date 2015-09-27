using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.ModAPI.Ingame;
using VRage.ModAPI;
using VRage.Utils;
using IMySlimBlock = Sandbox.ModAPI.IMySlimBlock;

namespace midspace.adminscripts.Protection
{
    public class ProtectionHandler
    {
        public List<ProtectionArea> Areas = new List<ProtectionArea>();
        public bool ProtectionEnabled;

        private readonly HandtoolCache _handtoolCache;

        public ProtectionHandler()
        {
            // for testing, to be removed later...
            Areas.Add(new ProtectionArea(new VRageMath.Vector3D(0, 0, 0), 40000, ProtectionAreaType.Cube));

            _handtoolCache = new HandtoolCache();
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, DamageHandler);
            ProtectionEnabled = true;
        }

        public void Close()
        {
            _handtoolCache.Close();
        }

        private void DamageHandler(object target, ref MyDamageInformation info)
        {
            if (!ProtectionEnabled)
                return;

            if (target is IMySlimBlock)
            {

                IMyEntity attacker;
                if (MyAPIGateway.Entities.TryGetEntityById(info.AttackerId, out attacker))
                {
                    if (CanDamageBlock(attacker, target as IMySlimBlock, info.Type)) ;
                }
                else
                    MyAPIGateway.Utilities.ShowNotification("No entity found.");
            }
        }

        private bool CanDamageBlock(IMyEntity attackerEntity, IMySlimBlock block, MyStringHash type)
        {
            foreach (ProtectionArea area in Areas)
            {
                if (area.IsInside(block))
                {
                    MyAPIGateway.Utilities.ShowNotification(string.Format("Type {0}", attackerEntity.GetType()));

                    if (type == MyDamageType.Grind)
                    {
                        IMyPlayer player;
                        if (attackerEntity is IMyShipGrinder)
                        {
                            player = MyAPIGateway.Players.GetPlayerControllingEntity(attackerEntity.GetTopMostParent());
                            
                            if (player == null)
                                return false;

                            return block.CubeGrid.BigOwners.Contains(player.IdentityId);
                        }

                        return _handtoolCache.TryGetPlayer(attackerEntity.EntityId, out player) && block.CubeGrid.BigOwners.Contains(player.IdentityId);
                    }
                    
                    if (type == MyDamageType.Bullet)
                    {

                    }
                }
                else
                    Logger.Debug("Block is not in Area.");
            }

            return false;

        }
    }
}
