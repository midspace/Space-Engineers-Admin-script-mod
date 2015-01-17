namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;

    public class CommandAsteroidsList : ChatCommand
    {
        /// <summary>
        /// Temporary hotlist cache created when player requests a list of in game asteroids, populated only by search results.
        /// </summary>
        public readonly static List<IMyVoxelMap> AsteroidCache = new List<IMyVoxelMap>();

        public CommandAsteroidsList()
            : base(ChatCommandSecurity.Admin, "listasteroids", new[] { "/listasteroids" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/listasteroids <filter>", "List in-game asteroids. Optional <filter> to refine your search by name.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.StartsWith("/listasteroids", StringComparison.InvariantCultureIgnoreCase))
            {
                string asteroidName = null;
                var match = Regex.Match(messageText, @"/listasteroids\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    asteroidName = match.Groups["Key"].Value;
                }

                var currentAsteroidList = new List<IMyVoxelMap>();
                MyAPIGateway.Session.VoxelMaps.GetInstances(currentAsteroidList, v => asteroidName == null || v.StorageName.IndexOf(asteroidName, StringComparison.InvariantCultureIgnoreCase) >= 0);

                AsteroidCache.Clear();
                MyAPIGateway.Utilities.ShowMessage("Count", currentAsteroidList.Count.ToString());
                var index = 1;
                foreach (var voxelMap in currentAsteroidList)
                {
                    AsteroidCache.Add(voxelMap);
                    MyAPIGateway.Utilities.ShowMessage(string.Format("#{0}", index++), voxelMap.StorageName);
                }

                return true;
            }

            return false;
        }
    }
}
