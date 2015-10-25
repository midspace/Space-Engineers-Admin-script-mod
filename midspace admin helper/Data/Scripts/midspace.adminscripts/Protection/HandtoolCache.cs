using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace midspace.adminscripts.Protection
{
    public class HandtoolCache
    {
        private Dictionary<IMyPlayer, IMyEntity> _cache;
        private List<IMyEntity> _uninitializedHandTools; 


        public HandtoolCache()
        {
            _cache = new Dictionary<IMyPlayer, IMyEntity>();
            _uninitializedHandTools = new List<IMyEntity>();

            MyAPIGateway.Entities.OnEntityAdd += Entities_OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += Entities_OnEntityRemove;
        }

        public void Close()
        {
            MyAPIGateway.Entities.OnEntityAdd -= Entities_OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove -= Entities_OnEntityRemove;
        }

        void Entities_OnEntityAdd(IMyEntity entity)
        {
            if (entity is MyEngineerToolBase)
                // when the entity is created, it is not finished (pos is 0, boundingbox does not exist, etc.)
                // therefore we need to wait a moment or two until we can build up the cache
                // but don't worry we won't allow it to damage sth. in that time (if it is even possible)
                _uninitializedHandTools.Add(entity);
        }

        // this method is called twice when switching weapon, idk why
        void Entities_OnEntityRemove(IMyEntity entity)
        {
            if (entity is MyEngineerToolBase)
            {
                if (!_cache.ContainsValue(entity))
                    return;

                var player = _cache.First(p => p.Value.EntityId == entity.EntityId).Key;

                if (_cache.ContainsKey(player))
                    _cache.Remove(player);
            }
        }

        private IMyPlayer FindPlayer(IMyEntity entity)
        {
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);

            double nearestDistance = 5; // usually the distance between player and handtool is about 2 to 3, 5 is plenty 
            IMyPlayer nearestPlayer = null;

            foreach (IMyPlayer player in players)
            {
                var character = player.GetCharacter();
                if (character != null)
                {
                    var distance = (((IMyEntity)character).GetPosition() - entity.GetPosition()).LengthSquared();

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestPlayer = player;
                    }
                }
            }

            return nearestPlayer;
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

            var finished = new List<IMyEntity>();
            foreach (IMyEntity handTool in _uninitializedHandTools)
            {
                if (handTool.GetPosition() == Vector3.Zero) // prototype check for not inited yet, need a better check for that
                    continue;

                var player = FindPlayer(handTool);

                if (player == null)
                    return;

                if (_cache.ContainsKey(player))
                    _cache.Remove(player);

                _cache.Add(player, handTool);
                finished.Add(handTool);
            }

            _uninitializedHandTools.RemoveAll(e => finished.Contains(e));
        }
     }
}