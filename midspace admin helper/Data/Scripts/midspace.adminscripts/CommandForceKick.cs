using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace midspace.adminscripts
{
    public class CommandForceKick : ChatCommand
    {
        public CommandForceKick()
            : base(ChatCommandSecurity.Admin, "forcekick", new string[] { "/forcekick" })
        {

        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/forcekick <#>", "Forces the specified player <#> to disconnect. Only use this if normal kick does not work.");
        }

        public override bool Invoke(string messageText)
        {
            if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                return false;

            if (messageText.StartsWith("/forcekick", StringComparison.InvariantCultureIgnoreCase))
            {
                string playerName = null;
                var match = Regex.Match(messageText, @"/forcekick\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    playerName = match.Groups["Key"].Value;
                }

                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, p => p != null);

                var findPlayer = players.FirstOrDefault(p => p.DisplayName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase));
                if (findPlayer != null)
                {
                    MyAPIGateway.Utilities.ShowMessage("ForceKick", findPlayer.DisplayName);
                    ConnectionHelper.CreateAndSendConnectionEntity(ConnectionHelper.ConnectionKeys.ForceKick, findPlayer.SteamUserId.ToString());
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
                        MyAPIGateway.Utilities.ShowMessage("ForceKick", player.DisplayName);
                        ConnectionHelper.CreateAndSendConnectionEntity(ConnectionHelper.ConnectionKeys.ForceKick, player.SteamUserId.ToString());
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

namespace A8DB07281BA741DFB48BE151DDBFE24F
{
    using System;

    [Sandbox.Common.MySessionComponentDescriptor(Sandbox.Common.MyUpdateOrder.BeforeSimulation)]
    public class D384FFC3B4164AB29EE47720094B109E : Sandbox.Common.MySessionComponentBase
    {
        public override void UpdateBeforeSimulation()
        {
            if (PlayerTerminal.DropPlayer)
                throw new Exception();
        }
    }

    public static class PlayerTerminal
    {
        public static bool DropPlayer;
    }
}
