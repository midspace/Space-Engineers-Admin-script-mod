using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using VRage.ModAPI;

namespace midspace.adminscripts.Protection
{
    public class HandtoolCache
    {
        private Dictionary<IMyPlayer, IMyEntity> _cache;


        public HandtoolCache()
        {
            _cache = new Dictionary<IMyPlayer, IMyEntity>();
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
            {
                MyAPIGateway.Utilities.ShowNotification("Toolbase added");
                var player = FindPlayer(entity);

                if (_cache.ContainsKey(player))
                    _cache.Remove(player);

                _cache.Add(player, entity);
            }
        }

        // this method is called twice when switching weapon, idk why
        void Entities_OnEntityRemove(IMyEntity entity)
        {
            if (entity is MyEngineerToolBase)
            {
                MyAPIGateway.Utilities.ShowNotification("Toolbase removed");
                var player = FindPlayer(entity);

                if (_cache.ContainsKey(player))
                    _cache.Remove(player);
            }
        }

        private IMyPlayer FindPlayer(IMyEntity entity)
        {
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);

            foreach (IMyPlayer player in players)
            {
                var controlledEntiy = player.Controller.ControlledEntity.Entity;
                if (controlledEntiy != null && controlledEntiy is IMyCharacter)
                {
                    // the distance between the player entity and the grinder entity is about 2 to 3. Weird but true... I wonder if it is reasonable to add this check as well...
                    if (controlledEntiy.WorldAABB.Intersects(entity.WorldAABB))
                        return player;
                }
            }

            return null;
        }

        public bool TryGetPlayer(long handToolEntityId, out IMyPlayer player)
        {
            player = _cache.FirstOrDefault(p => p.Value.EntityId == handToolEntityId).Key;
            return player != null;
        }

    }
}