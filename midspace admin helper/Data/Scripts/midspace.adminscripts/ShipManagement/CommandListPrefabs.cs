namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using Sandbox.Definitions;
    using Sandbox.ModAPI;

    public class CommandListPrefabs : ChatCommand
    {
        /// <summary>
        /// Temporary hotlist cache created when player requests a list of prefabs, populated only by search results.
        /// </summary>
        public readonly static List<MyPrefabDefinition> PrefabCache = new List<MyPrefabDefinition>();

        public CommandListPrefabs()
            : base(ChatCommandSecurity.Admin, "listprefabs", new[] { "/listprefabs" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/listprefabs <filter>", "List ships in the Prefabs. Optional <filter> to refine your search by name.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.StartsWith("/listprefabs", StringComparison.InvariantCultureIgnoreCase))
            {
                string prefabName = null;
                var match = Regex.Match(messageText, @"/listprefabs\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    prefabName = match.Groups["Key"].Value;
                }

                var list = MyDefinitionManager.Static.GetPrefabDefinitions().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                if (prefabName != null)
                    list = list.Where(kvp => kvp.Key.IndexOf(prefabName, StringComparison.InvariantCultureIgnoreCase) >= 0).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                PrefabCache.Clear();
                var description = new StringBuilder();
                var prefix = string.Format("Count: {0}", list.Count);
                var index = 1;
                foreach (var kvp in list.OrderBy(s => s.Key))
                {
                    PrefabCache.Add(kvp.Value);
                    description.AppendFormat("#{0} {1}\r\n", index++, kvp.Key);
                }

                MyAPIGateway.Utilities.ShowMissionScreen("Prefabs", prefix, " ", description.ToString(), null, "OK");
                return true;
            }

            return false;
        }
    }
}
