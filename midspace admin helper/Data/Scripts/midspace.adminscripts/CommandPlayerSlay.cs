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
            MyAPIGateway.Utilities.ShowMessage("/slay <#>", "Kills your player or the specified <#> player. (Only in survival mode, with prompts.)");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.StartsWith("/slay", StringComparison.InvariantCultureIgnoreCase))
            {
                string playerName = null;
                var match = Regex.Match(messageText, @"/slay\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    playerName = match.Groups["Key"].Value;
                }

                if (playerName == null)
                {
                    MyAPIGateway.Session.Player.KillPlayer(MyDamageType.Environment);
                    return true;
                }

                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, p => p != null);

                var findPlayer = players.FirstOrDefault(p => p.DisplayName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase));
                if (findPlayer != null)
                {
                    MyAPIGateway.Utilities.ShowMessage("slaying", findPlayer.DisplayName);
                    findPlayer.KillPlayer(MyDamageType.Environment);
                    return true;
                }

                int index;
                if (playerName.Substring(0, 1) == "#" && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= CommandPlayerStatus.IdentityCache.Count)
                {
                    var listplayers = new List<IMyPlayer>();
                    MyAPIGateway.Players.GetPlayers(listplayers, p => p.PlayerID == CommandPlayerStatus.IdentityCache[index - 1].PlayerId);
                    var player = listplayers.FirstOrDefault();

                    if (player != null)
                    {
                        MyAPIGateway.Utilities.ShowMessage("slaying", player.DisplayName);
                        player.KillPlayer(MyDamageType.Environment);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
