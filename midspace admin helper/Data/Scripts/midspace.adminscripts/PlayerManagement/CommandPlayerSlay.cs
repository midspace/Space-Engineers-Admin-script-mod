namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.Common.ObjectBuilders.Definitions;
    using Sandbox.ModAPI;

    public class CommandPlayerSlay : ChatCommand
    {
        public CommandPlayerSlay()
            : base(ChatCommandSecurity.Admin, "slay", new[] { "/slay" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/slay <#>", "Kills the specified <#> player. Instant death in Survival mode. Cannot slay pilots.");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/slay\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
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
                    MyAPIGateway.Utilities.ShowMessage("Slay", string.Format("No player named {0} found.", playerName));
                    return true;
                }

                if (selectedPlayer.KillPlayer(MyDamageType.Environment))
                    MyAPIGateway.Utilities.ShowMessage("slaying", selectedPlayer.DisplayName);
                else
                    MyAPIGateway.Utilities.ShowMessage("could not slay", "{0} as player is Pilot. Use /eject first.", selectedPlayer.DisplayName);
                return true;
            }

            return false;
        }
    }
}
