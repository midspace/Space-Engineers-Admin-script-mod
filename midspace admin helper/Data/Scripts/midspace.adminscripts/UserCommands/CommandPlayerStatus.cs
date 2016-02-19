namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Sandbox.ModAPI;
    using VRage.Library.Collections;

    public class CommandPlayerStatus : ChatCommand
    {
        /// <summary>
        /// Temporary hotlist cache created when player requests a list of in game players, populated only by search results.
        /// </summary>
        public readonly static List<IMyIdentity> IdentityCache = new List<IMyIdentity>();

        public CommandPlayerStatus()
            : base(ChatCommandSecurity.User, "status", new[] { "/status" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/status", "Displays the current players and steam Ids.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/status", StringComparison.InvariantCultureIgnoreCase))
            {
                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, p => p != null);
                IdentityCache.Clear();
                var index = 1;
                var identities = new List<IMyIdentity>();
                MyAPIGateway.Players.GetAllIdentites(identities);
                var clients = MyAPIGateway.Session.GetCheckpoint("null").Clients;

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
                        IdentityCache.Add(identity);
                        count++;
                    }
                }

                var prefix = string.Format("Count: {0}", count);
                MyAPIGateway.Utilities.ShowMissionScreen("List Players", prefix, " ", description.ToString(), null, "OK");
                return true;
            }

            return false;
        }
    }
}
