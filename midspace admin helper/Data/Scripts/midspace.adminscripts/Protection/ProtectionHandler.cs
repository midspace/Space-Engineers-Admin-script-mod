using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using System.Linq;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.ModAPI.Ingame;
using VRage.ModAPI;
using VRage.Utils;
using IMySlimBlock = Sandbox.ModAPI.IMySlimBlock;

namespace midspace.adminscripts.Protection
{
    public static class ProtectionHandler
    {

        public static ProtectionConfig Config;

        private static bool _isInitialized;
        private static HandtoolCache _handtoolCache;

        public static void Init()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;
            Config = new ProtectionConfig();
            LoadAreas();

            _handtoolCache = new HandtoolCache();
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, DamageHandler);
            Config.ProtectionEnabled = true;
        }

        public static void Close()
        {
            if (!_isInitialized)
                return;

            _isInitialized = false;
            _handtoolCache.Close();
        }

        public static void SaveAreas(string customSaveName = null)
        {
            if (!_isInitialized)
                return;

            Config.Save(customSaveName);
        }

        private static void LoadAreas()
        {
            if (!_isInitialized)
                return;

            Config.Load();
        }

        private static void DamageHandler(object target, ref MyDamageInformation info)
        {
            if (!Config.ProtectionEnabled)
                return;

            IMySlimBlock block = target as IMySlimBlock;

            if (block != null)
            {
                IMyEntity attacker;
                if (MyAPIGateway.Entities.TryGetEntityById(info.AttackerId, out attacker))
                {
                    if (CanDamageBlock(info.AttackerId, block, info.Type))
                        return;
                }

                info.Amount = 0;
                return;
            }

            IMyCharacter character = target as IMyCharacter;
            if (character != null)
            {
                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, p => p != null && p.Controller.ControlledEntity != null && p.Controller.ControlledEntity.Entity != null);

                var player = players.FirstOrDefault(p => p.GetCharacter() == character);
                if (player == null)
                    return;

                if (!Config.Areas.Any(a => a.Contains(player.Controller.ControlledEntity.Entity)))
                    return;

                if (info.Type == MyDamageType.LowPressure || info.Type == MyDamageType.Asphyxia ||
                    info.Type == MyDamageType.Environment || info.Type == MyDamageType.Fall ||
                    info.Type == MyDamageType.Fire || info.Type == MyDamageType.Radioactivity ||
                    info.Type == MyDamageType.Suicide || info.Type == MyDamageType.Unknown)
                    return;

                info.Amount = 0;
            }
        }

        private static bool CanDamageBlock(long attackerEntityId, IMySlimBlock block, MyStringHash type)
        {
            foreach (ProtectionArea area in Config.Areas)
            {
                if (!area.Contains(block)) 
                    continue;

                // if we can't find out who attacks and the entity is inside the area, we don't apply the damage
                IMyEntity attackerEntity;
                if (!MyAPIGateway.Entities.TryGetEntityById(attackerEntityId, out attackerEntity))
                    return false;

                if (type == MyDamageType.Grind)
                {
                    IMyPlayer player;
                    if (attackerEntity is IMyShipGrinder)
                    {
                        player = MyAPIGateway.Players.GetPlayerControllingEntity(attackerEntity.GetTopMostParent());

                        if (player == null)
                            return false;

                        return CanGrind(player, block);
                    }

                    return _handtoolCache.TryGetPlayer(attackerEntity.EntityId, out player) && CanGrind(player, block);
                }

                return true;
            }
            return true;
        }

        /// <summary>
        /// Any player who owns a block on the grid can modify it. If noone owns a block everyone can modify it.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="block"></param>
        /// <returns></returns>
        private static bool CanGrind(IMyPlayer player, IMySlimBlock block)
        {
            var allSmallOwners = block.CubeGrid.GetAllSmallOwners();
            return allSmallOwners.Count == 0 || allSmallOwners.Contains(player.IdentityId);
        }

        /// <summary>
        /// Used to find out whether an entity is inside a protected area or not. 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>True if the entity is inside a protected area. If the entity is null it will return false.</returns>
        public static bool IsProtected(IMyEntity entity)
        {
            if (entity == null || Config == null || Config.Areas == null)
                return false;

            return Config.Areas.Any(area => area.Contains(entity));
        }

        public static void UpdateBeforeSimulation()
        {
            if (!_isInitialized)
                return;

            _handtoolCache.UpdateBeforeSimulation();
        }

        public static bool AddArea(ProtectionArea area)
        {
            if (Config.Areas.Any(a => a.Name.Equals(area.Name, StringComparison.InvariantCultureIgnoreCase)))
                return false;

            Config.Areas.Add(area);
            return true;
        }

        public static bool RemoveArea(ProtectionArea area)
        {
            if (!Config.Areas.Any(a => a.Name.Equals(area.Name, StringComparison.InvariantCultureIgnoreCase)))
                return false;

            Config.Areas.RemoveAll(a => a.Name.Equals(area.Name, StringComparison.InvariantCultureIgnoreCase));
            return true;
        }
    }
}
