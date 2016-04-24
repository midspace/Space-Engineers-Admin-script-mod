namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using VRage.ModAPI;

    public class CommandPlanetsList : ChatCommand
    {
        /// <summary>
        /// Temporary hotlist cache created when player requests a list of in game planets, populated only by search results.
        /// </summary>
        private readonly static Dictionary<ulong, List<IMyVoxelBase>> ServerPlanetCache = new Dictionary<ulong, List<IMyVoxelBase>>();

        public CommandPlanetsList()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Server, "listplanets", new[] { "/listplanets" })
        {
            ServerPlanetCache.Clear();
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/listplanets <filter>", "List in-game planets. Optional <filter> to refine your search by name.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.StartsWith("/listplanets", StringComparison.InvariantCultureIgnoreCase))
            {
                string planetName = null;
                var match = Regex.Match(messageText, @"/listplanets\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    planetName = match.Groups["Key"].Value;
                }

                var currentPlanetList = new List<IMyVoxelBase>();
                MyAPIGateway.Session.VoxelMaps.GetInstances(currentPlanetList, v => v is Sandbox.Game.Entities.MyPlanet && (planetName == null || v.StorageName.IndexOf(planetName, StringComparison.InvariantCultureIgnoreCase) >= 0));

                ServerPlanetCache[steamId] = new List<IMyVoxelBase>();

                // Only display the list in chat if the chat allows to fully show it, else display it in a mission screen.
                if (currentPlanetList.Count <= 9)
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "Count", currentPlanetList.Count.ToString());
                    var index = 1;
                    foreach (var voxelMap in currentPlanetList)
                    {
                        ServerPlanetCache[steamId].Add(voxelMap);
                        MyAPIGateway.Utilities.SendMessage(steamId, string.Format("#{0}", index++), voxelMap.StorageName);
                    }
                }
                else
                {
                    var description = new StringBuilder();
                    var prefix = string.Format("Count: {0}", currentPlanetList.Count);
                    var index = 1;
                    foreach (var voxelMap in currentPlanetList.OrderBy(s => s.StorageName))
                    {
                        ServerPlanetCache[steamId].Add(voxelMap);
                        description.AppendFormat("#{0}: {1}\r\n", index++, voxelMap.StorageName);
                    }

                    MyAPIGateway.Utilities.SendMissionScreen(steamId, "List Planets", prefix, " ", description.ToString());
                }

                return true;
            }

            return false;
        }

        public static List<IMyVoxelBase> GetPlanetCache(ulong steamId)
        {
            List<IMyVoxelBase> cacheList;
            if (!ServerPlanetCache.TryGetValue(steamId, out cacheList))
            {
                ServerPlanetCache.Add(steamId, new List<IMyVoxelBase>());
                cacheList = ServerPlanetCache[steamId];
            }
            return cacheList;
        }
    }
}
