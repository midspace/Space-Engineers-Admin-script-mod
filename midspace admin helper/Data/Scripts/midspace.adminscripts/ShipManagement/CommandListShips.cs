namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using VRage.ModAPI;

    public class CommandListShips : ChatCommand
    {
        /// <summary>
        /// Temporary hotlist cache created when player requests a list of in game ships, populated only by search results.
        /// </summary>
        private readonly static Dictionary<ulong, List<IMyEntity>> ServerShipCache = new Dictionary<ulong, List<IMyEntity>>();

        public CommandListShips()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Server, "listships", new[] { "/listships" })
        {
            ServerShipCache.Clear();
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/listships <filter>", "List in-game ships/stations. Optional <filter> to refine your search by ship name or antenna/beacon name.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.StartsWith("/listships", StringComparison.InvariantCultureIgnoreCase))
            {
                string shipName = null;
                var match = Regex.Match(messageText, @"/listships\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    shipName = match.Groups["Key"].Value;
                }

                var currentShipList = Support.FindShipsByName(shipName);

                ServerShipCache[steamId] = new List<IMyEntity>();

                //only display the list in chat if the chat allows to fully show it, else display it in a mission screen.
                if (currentShipList.Count <= 9)
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "Count", currentShipList.Count.ToString());
                    var index = 1;
                    foreach (var ship in currentShipList.OrderBy(s => s.DisplayName))
                    {
                        ServerShipCache[steamId].Add(ship);
                        MyAPIGateway.Utilities.SendMessage(steamId, string.Format("#{0}", index++), ship.DisplayName);
                    }
                }
                else
                {
                    var description = new StringBuilder();
                    var prefix = string.Format("Count: {0}", currentShipList.Count);
                    var index = 1;
                    foreach (var ship in currentShipList.OrderBy(s => s.DisplayName))
                    {
                        ServerShipCache[steamId].Add(ship);
                        description.AppendFormat("#{0}: {1}\r\n", index++, ship.DisplayName);
                    }

                    MyAPIGateway.Utilities.SendMissionScreen(steamId, "List Ships", prefix, " ", description.ToString());
                }

                return true;
            }

            return false;
        }

        public static List<IMyEntity> GetShipCache(ulong steamId)
        {
            List<IMyEntity> cacheList;
            if (!ServerShipCache.TryGetValue(steamId, out cacheList))
            {
                ServerShipCache[steamId] = new List<IMyEntity>();
                cacheList = ServerShipCache[steamId];
            }
            return cacheList;
        }
    }
}
