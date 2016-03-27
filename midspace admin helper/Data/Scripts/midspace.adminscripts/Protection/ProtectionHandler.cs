using System.IO;

namespace midspace.adminscripts.Protection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Timers;
    using midspace.adminscripts.Messages.Communication;
    using midspace.adminscripts.Utils.Timer;
    using midspace.adminscripts.Config.Files;
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRage.Utils;
    using IMyShipGrinder = Sandbox.ModAPI.Ingame.IMyShipGrinder;

    public static class ProtectionHandler
    {
        public static ProtectionConfig Config { get; private set; }

        private static bool _isInitialized;
        private static HandtoolCache _handtoolCache;
        private static Dictionary<IMyPlayer, DateTime> _sentFailedMessage;
        private static ThreadsafeTimer _cleanupTimer;
        private static bool _isServer;
        private static ProtectionConfigFile _configFile;

        private const int GrindFailedMessageInterval = 5000;

        public static void Init_Server()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;
            _isServer = true;
            Config = new ProtectionConfig();
            Load();

            _handtoolCache = new HandtoolCache();
            _sentFailedMessage = new Dictionary<IMyPlayer, DateTime>();
            // every 30 seconds
            _cleanupTimer = new ThreadsafeTimer(30000);
            _cleanupTimer.Elapsed += CleanUp;
            _cleanupTimer.Start();
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, DamageHandler_Server);
            ChatCommandLogic.Instance.AllowBuilding = true;
        }

        public static void InitOrUpdateClient(ProtectionConfig config)
        {
            // still allow update of cfg
            Config = config;

            if (_isInitialized)
                return;

            // but no init, as for servers we won't init it anyway
            _isInitialized = true;
            ChatCommandLogic.Instance.AllowBuilding = true;
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, DamageHandler_Client);
        }

        public static void Close()
        {
            if (!_isInitialized)
                return;

            _isInitialized = false;

            if (_isServer)
                _handtoolCache.Close();
            else
                ChatCommandLogic.Instance.AllowBuilding = false;
        }

        public static void Save(string customSaveName = null)
        {
            if (!_isInitialized || !_isServer)
                return;

            _configFile.Config = Config;
            _configFile.Save(customSaveName);
        }

        private static void Load()
        {
            if (!_isInitialized || !_isServer)
                return;

            _configFile = new ProtectionConfigFile(Path.GetFileNameWithoutExtension(MyAPIGateway.Session.CurrentPath));
            Config = _configFile.Config;
        }

        /// <summary>
        /// Handles the damage in ProtectionAreas if Protection is enabled. This method is designed for client side use.
        /// Usually this should not be needed as all damage should be processed on the server side but due to a bug in the
        /// damage system, clients receive the data so that server and client will be out of sync. That's why this is needed.
        /// It does the same calculations as the server but won't notify players.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="info"></param>
        private static void DamageHandler_Client(object target, ref MyDamageInformation info)
        {
            if (!Config.ProtectionEnabled)
                return;

            IMySlimBlock block = target as IMySlimBlock;

            if (block != null)
            {
                IMyPlayer player;
                if (CanDamageBlock(info.AttackerId, block, info.Type, out player))
                    return;

                info.Amount = 0;
                return;
            }

            // disable pvp in PAs
            IMyCharacter character = target as IMyCharacter;
            if (character != null)
            {
                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, p => p != null && p.Controller.ControlledEntity != null && p.Controller.ControlledEntity.Entity != null);

                var player = players.FirstOrDefault(p => p.GetCharacter() == character);
                if (player == null)
                    return;

                if (!IsProtected(player.Controller.ControlledEntity.Entity))
                    return;

                if (info.Type == MyDamageType.LowPressure || info.Type == MyDamageType.Asphyxia ||
                    info.Type == MyDamageType.Environment || info.Type == MyDamageType.Fall ||
                    info.Type == MyDamageType.Fire || info.Type == MyDamageType.Radioactivity ||
                    info.Type == MyDamageType.Suicide || info.Type == MyDamageType.Unknown)
                    return;

                info.Amount = 0;
            }
        }

        /// <summary>
        /// Handles the damage in ProtectionAreas if Protection is enabled. This method is desinged for server side use and will notify the players if they tried to damage the wrong block.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="info"></param>
        private static void DamageHandler_Server(object target, ref MyDamageInformation info)
        {
            if (!Config.ProtectionEnabled)
                return;

            IMySlimBlock block = target as IMySlimBlock;

            if (block != null)
            {
                IMyPlayer player;
                if (CanDamageBlock(info.AttackerId, block, info.Type, out player))
                    return;

                info.Amount = 0;

                // notify player
                DateTime time;
                if (player != null && (!_sentFailedMessage.TryGetValue(player, out time) || DateTime.Now - time >= TimeSpan.FromMilliseconds(GrindFailedMessageInterval)))
                {
                    MessageClientNotification.SendMessage(player.SteamUserId, "You are not allowed to damage this block.", GrindFailedMessageInterval, MyFontEnum.Red);
                    _sentFailedMessage.Update(player, DateTime.Now);
                }

                return;
            }

            // disable pvp in PAs
            IMyCharacter character = target as IMyCharacter;
            if (character != null)
            {
                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, p => p != null && p.Controller.ControlledEntity != null && p.Controller.ControlledEntity.Entity != null);

                var player = players.FirstOrDefault(p => p.GetCharacter() == character);
                if (player == null)
                    return;

                if (!IsProtected(player.Controller.ControlledEntity.Entity))
                    return;

                if (info.Type == MyDamageType.LowPressure || info.Type == MyDamageType.Asphyxia ||
                    info.Type == MyDamageType.Environment || info.Type == MyDamageType.Fall ||
                    info.Type == MyDamageType.Fire || info.Type == MyDamageType.Radioactivity ||
                    info.Type == MyDamageType.Suicide || info.Type == MyDamageType.Unknown)
                    return;

                info.Amount = 0;
            }
        }

        /// <summary>
        /// Finds out if the entity with the given entityId can damage the given block regarding the given damage type. 
        /// </summary>
        /// <param name="attackerEntityId">The entityId of the entity that damages the given block.</param>
        /// <param name="block">The block that is damaged.</param>
        /// <param name="type">The damage type.</param>
        /// <param name="player">If a player is causing the damage, we return it as well.</param>
        /// <returns>True if the block can be damaged.</returns>
        private static bool CanDamageBlock(long attackerEntityId, IMySlimBlock block, MyStringHash type, out IMyPlayer player)
        {
            player = null;
            if (!IsProtected(block))
                return true;

            IMyEntity attackerEntity;
            if (!MyAPIGateway.Entities.TryGetEntityById(attackerEntityId, out attackerEntity))
                return false;

            if (type == MyDamageType.Grind)
            {
                if (attackerEntity is IMyShipGrinder)
                {
                    player = MyAPIGateway.Players.GetPlayerControllingEntity(attackerEntity.GetTopMostParent());

                    if (player == null)
                        return false;

                    return CanModify(player, block);
                }

                return _handtoolCache.TryGetPlayer(attackerEntity.EntityId, out player) && CanModify(player, block);
            }
            // we don't want players to destroy things in protection areas...
            return false;
        }

        /// <summary>
        /// Any player who owns a block on the grid can modify it. If noone owns a block everyone can modify it.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="block"></param>
        /// <returns></returns>
        public static bool CanModify(IMyPlayer player, IMySlimBlock block)
        {
            return CanModify(player, block.CubeGrid);
        }

        /// <summary>
        /// Any player who owns a block on the grid can modify it. If noone owns a block everyone can modify it.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="cubeGrid"></param>
        /// <returns></returns>
        public static bool CanModify(IMyPlayer player, IMyCubeGrid cubeGrid)
        {
            var allSmallOwners = cubeGrid.GetAllSmallOwners();
            return allSmallOwners.Count == 0 || allSmallOwners.Contains(player.IdentityId);
        }

        /// <summary>
        /// Used to find out whether an block is inside a protected area or not. 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>True if the block is inside a protected area. If the block is null it will return false.</returns>
        public static bool IsProtected(IMyEntity entity)
        {
            if (entity == null || Config == null || Config.Areas == null || !Config.ProtectionEnabled)
                return false;

            return Config.Areas.Any(area => area.Contains(entity)) ^ Config.ProtectionInverted;
        }

        /// <summary>
        /// Used to find out whether an block is inside a protected area or not. 
        /// </summary>
        /// <param name="block"></param>
        /// <returns>True if the block is inside a protected area. If the block is null it will return false.</returns>
        public static bool IsProtected(IMySlimBlock block)
        {
            if (block == null || Config == null || Config.Areas == null || !Config.ProtectionEnabled)
                return false;

            return Config.Areas.Any(area => area.Contains(block)) ^ Config.ProtectionInverted;
        }

        public static void UpdateBeforeSimulation()
        {
            if (!_isInitialized || !_isServer)
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

        /// <summary>
        /// Removes disconnected players from the sent messages dictionary.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="messageEventArgs"></param>
        private static void CleanUp(object o, ElapsedEventArgs messageEventArgs)
        {
            var players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);

            Dictionary<IMyPlayer, DateTime> validEntries = new Dictionary<IMyPlayer, DateTime>();
            foreach (var pair in _sentFailedMessage)
                if (players.Contains(pair.Key))
                    validEntries.Add(pair.Key, pair.Value);

            _sentFailedMessage = validEntries;
        }
    }
}
