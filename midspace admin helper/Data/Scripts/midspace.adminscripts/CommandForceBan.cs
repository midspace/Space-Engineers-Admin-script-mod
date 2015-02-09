using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace midspace.adminscripts
{
    public class CommandForceBan : ChatCommand
    {

        public CommandForceBan()
            : base(ChatCommandSecurity.Admin, "forceban", new string[] { "/forceban" })
        {

        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/forceban <#>", "Forces the specified player <#> to disconnect and bans him. Only use this if normal ban does not work.");
        }

        public override bool Invoke(string messageText)
        {
            if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                return false;

            if (messageText.StartsWith("/forceban", StringComparison.InvariantCultureIgnoreCase))
            {
                string playerName = null;
                var match = Regex.Match(messageText, @"/forceban\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    playerName = match.Groups["Key"].Value;
                }

                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, p => p != null);


                var findPlayer = players.FirstOrDefault(p => p.DisplayName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase));
                if (findPlayer != null)
                {
                    MyAPIGateway.Utilities.ShowMessage("ForceBan", findPlayer.DisplayName);
                    ConnectionHelper.CreateAndSendConnectionEntity(ConnectionHelper.ConnectionKeys.ForceKick, string.Format("{0}:true", findPlayer.SteamUserId.ToString()));
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
                        MyAPIGateway.Utilities.ShowMessage("ForceBan", player.DisplayName);
                        ConnectionHelper.CreateAndSendConnectionEntity(ConnectionHelper.ConnectionKeys.ForceKick, string.Format("{0}:true", player.SteamUserId.ToString()));
                        return true;
                    }
                }

                if (playerName != null)
                    MyAPIGateway.Utilities.ShowMessage("ForceBan", string.Format("No player named {0} found.", playerName));
                
                return true;
            }

            return false;
        }
    }
}
