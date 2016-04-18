namespace midspace.adminscripts
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;

    public class CommandPlayerStatus : ChatCommand
    {
        /// <summary>
        /// Temporary hotlist cache created when player requests a list of in game players, populated only by search results.
        /// </summary>
        private readonly static Dictionary<ulong, List<IMyIdentity>> ServerIdentityCache = new Dictionary<ulong, List<IMyIdentity>>();

        public CommandPlayerStatus()
            : base(ChatCommandSecurity.User, ChatCommandFlag.Server, "status", new[] { "/status" })
        {
            ServerIdentityCache.Clear();
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/status", "Displays the current players and steam Ids.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, p => p != null);
            ServerIdentityCache[steamId] = new List<IMyIdentity>();
            var index = 1;
            var identities = new List<IMyIdentity>();
            MyAPIGateway.Players.GetAllIdentites(identities);

            var description = new StringBuilder();
            var count = 0;

            foreach (var identity in identities.OrderBy(i => i.DisplayName))
            {
                var status = "Player";

                // is player Admin, or Host?
                // The Dedicated Config with admins and more is now to be found in Utilities 

                var steamPlayer = players.FirstOrDefault(p => p.PlayerID == identity.PlayerId);

                if (steamPlayer != null)
                {
                    if (steamPlayer.IsAdmin())
                        status = "Admin";

                    description.AppendFormat("#{0} {1} {2} '{3}'\r\n", index++, status, steamPlayer.SteamUserId, identity.DisplayName);
                    ServerIdentityCache[steamId].Add(identity);
                    count++;
                }
            }

            var prefix = string.Format("Count: {0}", count);
            MyAPIGateway.Utilities.SendMissionScreen(steamId, "List Players", prefix, " ", description.ToString(), null, "OK");
            return true;
        }

        public static List<IMyIdentity> GetIdentityCache(ulong steamId)
        {
            List<IMyIdentity> cacheList;
            if (!ServerIdentityCache.TryGetValue(steamId, out cacheList))
            {
                ServerIdentityCache.Add(steamId, new List<IMyIdentity>());
                cacheList = ServerIdentityCache[steamId];
            }
            return cacheList;
        }
    }
}
