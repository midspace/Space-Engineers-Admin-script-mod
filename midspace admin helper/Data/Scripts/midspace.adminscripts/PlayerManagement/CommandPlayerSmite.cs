namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using midspace.adminscripts.Messages.Sync;
    using Sandbox.ModAPI;

    public class CommandPlayerSmite : ChatCommand
    {
        private readonly string _defaultOreName;
        public static CommandPlayerSmite Instance;

        public CommandPlayerSmite(string defaultOreName)
            : base(ChatCommandSecurity.Admin, "smite", new[] { "/smite" })
        {
            Instance = this;
            _defaultOreName = defaultOreName;
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/smite <#>", "Drops meteor on the specified <#> player. Instant death in Survival mode. Cockpits do protect a little, but can become collateral damage.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/smite\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var playerName = match.Groups["Key"].Value;
                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, p => p != null);
                IMyPlayer selectedPlayer = null;

                var findPlayer = players.FirstOrDefault(p => p.DisplayName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase));
                if (findPlayer != null)
                {
                    selectedPlayer = findPlayer;
                }

                int index;
                if (playerName.Substring(0, 1) == "#" && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= CommandPlayerStatus.IdentityCache.Count)
                {
                    var listplayers = new List<IMyPlayer>();
                    MyAPIGateway.Players.GetPlayers(listplayers, p => p.PlayerID == CommandPlayerStatus.IdentityCache[index - 1].PlayerId);
                    selectedPlayer = listplayers.FirstOrDefault();
                }

                if (selectedPlayer == null)
                {
                    MyAPIGateway.Utilities.ShowMessage("Smite", string.Format("No player named {0} found.", playerName));
                    return true;
                }

                MyAPIGateway.Utilities.ShowMessage("smiting", selectedPlayer.DisplayName);
                MessageSyncAres.Smite(selectedPlayer.SteamUserId, _defaultOreName);
                return true;
            }

            return false;
        }
    }
}
