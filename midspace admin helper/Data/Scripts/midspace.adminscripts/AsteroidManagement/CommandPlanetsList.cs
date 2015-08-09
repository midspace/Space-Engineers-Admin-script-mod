namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;

    public class CommandPlanetsList : ChatCommand
    {
        /// <summary>
        /// Temporary hotlist cache created when player requests a list of in game planets, populated only by search results.
        /// </summary>
        public readonly static List<IMyVoxelBase> PlanetCache = new List<IMyVoxelBase>();

        public CommandPlanetsList()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Experimental, "listplanets", new[] { "/listplanets" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/listplanets <filter>", "List in-game planets. Optional <filter> to refine your search by name.");
        }

        public override bool Invoke(string messageText)
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
                MyAPIGateway.Session.VoxelMaps.GetInstances(currentPlanetList, v => planetName == null || v.StorageName.IndexOf(planetName, StringComparison.InvariantCultureIgnoreCase) >= 0);
                //MyAPIGateway.Session.pl .. ????

                PlanetCache.Clear();

                // Only display the list in chat if the chat allows to fully show it, else display it in a mission screen.
                if (currentPlanetList.Count <= 9)
                {
                    MyAPIGateway.Utilities.ShowMessage("Count", currentPlanetList.Count.ToString());
                    var index = 1;
                    foreach (var voxelMap in currentPlanetList)
                    {
                        PlanetCache.Add(voxelMap);
                        MyAPIGateway.Utilities.ShowMessage(string.Format("#{0}", index++), voxelMap.StorageName);
                    }
                }
                else
                {
                    var description = new StringBuilder();
                    var prefix = string.Format("Count: {0}", currentPlanetList.Count);
                    var index = 1;
                    foreach (var voxelMap in currentPlanetList.OrderBy(s => s.StorageName))
                    {
                        PlanetCache.Add(voxelMap);
                        description.AppendFormat("#{0}: {1}\r\n", index++, voxelMap.StorageName);
                    }

                    MyAPIGateway.Utilities.ShowMissionScreen("List Planets", prefix, " ", description.ToString());
                }

                return true;
            }

            return false;
        }
    }
}
