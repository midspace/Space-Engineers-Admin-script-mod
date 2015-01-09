namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Sandbox.ModAPI;

    public class CommandStatus : ChatCommand
    {
        /// <summary>
        /// Temporary hotlist cache created when player requests a list of in game players, populated only by search results.
        /// </summary>
        public readonly static List<IMyIdentity> IdentityCache = new List<IMyIdentity>();

        public CommandStatus()
            : base(ChatCommandSecurity.User, "status", new[] { "/status" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/status", "Displays the current players and steam Ids.");
        }

        public override bool Invoke(string messageText)
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

                foreach (var identity in identities)
                {
                    var status = "Player";

                    // is player Admin, or Host?
                    // The Dedicated Config with admins and more is now to be found in Utilities 

                    var steamPlayer = players.FirstOrDefault(p => p.PlayerID == identity.PlayerId);

                    if (steamPlayer != null)
                    {
                        if (steamPlayer.IsAdmin())
                            status = "Admin";

                        MyAPIGateway.Utilities.ShowMessage(string.Format("#{0}", index++), string.Format("{0} {1} '{2}'", status, steamPlayer.SteamUserId, identity.DisplayName));
                        IdentityCache.Add(identity);
                    }
                }
                return true;
            }
            return false;
        }
    }
}
