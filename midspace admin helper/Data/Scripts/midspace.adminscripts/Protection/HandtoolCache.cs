namespace midspace.adminscripts.Protection
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Timers;
    using midspace.adminscripts.Utils.Timer;
    using Sandbox.ModAPI;
    using Sandbox.ModAPI.Weapons;
    using VRage.Game;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;

    public class HandtoolCache
    {
        private readonly Dictionary<IMyPlayer, IMyEngineerToolBase> _cache;
        private readonly List<IMyEngineerToolBase> _uninitializedHandTools;


        public HandtoolCache()
        {
            _cache = new Dictionary<IMyPlayer, IMyEngineerToolBase>();
            _uninitializedHandTools = new List<IMyEngineerToolBase>();

            // as the conditions of "FindPlayer(...)" aren't very safe (players could be very near),
            // we'll check the cache from time to time in order to prevent it from ´containing wrong data
            var safetyChecker = new ThreadsafeTimer(5000);
            safetyChecker.Elapsed += Safety_Elapsed;
            safetyChecker.Start();

            MyAPIGateway.Entities.OnEntityAdd += Entities_OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += Entities_OnEntityRemove;
        }

        public void Close()
        {
            MyAPIGateway.Entities.OnEntityAdd -= Entities_OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove -= Entities_OnEntityRemove;
        }

        private void Entities_OnEntityAdd(IMyEntity entity)
        {
            IMyEngineerToolBase handTool = entity as IMyEngineerToolBase;
            if (handTool != null)
            {
                // when the entity is created, it is not finished (pos is 0, boundingbox does not exist, etc.)
                // therefore we need to wait a moment or two until we can build up the cache
                // but don't worry we won't allow it to damage sth. in that time (if it is even possible)
                _uninitializedHandTools.Add(handTool);
            }   
        }

        // this method is called twice when switching weapon, idk why
        private void Entities_OnEntityRemove(IMyEntity entity)
        {
            IMyEngineerToolBase tool = entity as IMyEngineerToolBase;

            if (tool == null || !_cache.ContainsValue(tool))
                return;

            var player = _cache.First(p => p.Value.EntityId == entity.EntityId).Key;

            if (_cache.ContainsKey(player))
                _cache.Remove(player);
        }

        private IMyPlayer FindPlayer(IMyEngineerToolBase handTool)
        {
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);

            foreach (IMyPlayer player in players)
            {
                IMyCharacter character = player.Controller.ControlledEntity as IMyCharacter;

                if (character == null)
                    continue;

                // The most inefficient way of finding which player is equiped with what tool.
                // What is needed is either MyCharacter.CurrentWeapon, or MyEngineerToolBase.Owner exposed through the appropriate interface.
                MyObjectBuilder_Character c = character.GetObjectBuilder(true) as MyObjectBuilder_Character;
                if (c != null && c.HandWeapon != null && c.HandWeapon.EntityId == handTool.EntityId)
                    return player;
            }

            return null;
        }

        public bool TryGetPlayer(long handToolEntityId, out IMyPlayer player)
        {
            player = _cache.FirstOrDefault(p => p.Value.EntityId == handToolEntityId).Key;
            return player != null;
        }

        public void UpdateBeforeSimulation()
        {
            if (_uninitializedHandTools.Count == 0)
                return;

            var finished = new List<IMyEngineerToolBase>();
            foreach (IMyEngineerToolBase handTool in _uninitializedHandTools)
            {
                if (handTool.Closed)
                {
                    finished.Add(handTool);
                    continue;
                }

                var player = FindPlayer(handTool);

                if (player == null)
                    return;

                _cache.Update(player, handTool);
                finished.Add(handTool);
            }

            _uninitializedHandTools.RemoveAll(e => finished.Contains(e));
        }

        private void Safety_Elapsed(object o, ElapsedEventArgs elapsedEventArgs)
        {
            HashSet<IMyPlayer> keySet = new HashSet<IMyPlayer>(_cache.Keys);
            var correctedEntities = new Dictionary<IMyPlayer, IMyEngineerToolBase>();

            foreach (IMyPlayer player in keySet)
            {
                IMyEngineerToolBase handTool = _cache[player];
                IMyPlayer foundPlayer = FindPlayer(handTool);

                if (player == foundPlayer)
                    continue;

                // remove invalid entries
                _cache.Remove(player);

                // somehow it returns null when a player enters a cockpit or so but if the player enters the cockpit the handtool should be removed from the cache
                // ... still, I can't figure out why it returns null O.o
                if (foundPlayer == null)
                    continue;

                correctedEntities.Add(foundPlayer, handTool);
            }

            foreach (KeyValuePair<IMyPlayer, IMyEngineerToolBase> keyValuePair in correctedEntities)
                _cache.Update(keyValuePair.Key, keyValuePair.Value);
        }
    }
}