namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;

    public class CommandFactionKick : ChatCommand
    {
        public CommandFactionKick()
            : base(ChatCommandSecurity.Admin, "fk", new[] { "/fk" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/fk <#>", "Kicks the specified <#> player from their faction.");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/fk\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);

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
                if (playerName.Substring(0, 1) == "#" && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= CommandStatus.IdentityCache.Count)
                {
                    var listplayers = new List<IMyPlayer>();
                    MyAPIGateway.Players.GetPlayers(listplayers, p => p.PlayerID == CommandStatus.IdentityCache[index - 1].PlayerId);
                    selectedPlayer = listplayers.FirstOrDefault();
                }

                if (selectedPlayer == null)
                    return false;

                var fc = MyAPIGateway.Session.Factions.GetObjectBuilder();
                var factionBuilder = fc.Factions.FirstOrDefault(f => f.Members.Any(m => m.PlayerId == selectedPlayer.PlayerID));

                if (factionBuilder == null)
                {
                    MyAPIGateway.Utilities.ShowMessage("demote", string.Format("{0} is not in faction.", selectedPlayer.DisplayName));
                    return true;
                }

                MyAPIGateway.Session.Factions.KickMember(factionBuilder.FactionId, selectedPlayer.PlayerID);
                MyAPIGateway.Utilities.ShowMessage("kick", string.Format("{0} has been removed from faction.", selectedPlayer.DisplayName));

                return true;
            }

            return false;
        }
    }
}
