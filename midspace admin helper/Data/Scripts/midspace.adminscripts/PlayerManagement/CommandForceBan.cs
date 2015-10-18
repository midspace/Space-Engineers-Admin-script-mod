namespace midspace.adminscripts
{
    using midspace.adminscripts.Messages;
    using Sandbox.ModAPI;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class CommandForceBan : ChatCommand
    {
        public CommandForceBan()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.MultiplayerOnly, "forceban", new string[] { "/forceban" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/forceban <#>", "Forces the specified player <#> to disconnect and bans him. Only use this if normal ban does not work.");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/forceban\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
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
                    MyAPIGateway.Utilities.ShowMessage("ForceBan", "No player named {0} found.", playerName);
                    return true;
                }

                MyAPIGateway.Utilities.ShowMessage("ForceBan", selectedPlayer.DisplayName);
                ConnectionHelper.SendMessageToServer(new MessageForceDisconnect() { SteamId = selectedPlayer.SteamUserId, Ban = true });
                return true;
            }

            return false;
        }
    }
}
