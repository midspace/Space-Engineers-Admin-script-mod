namespace midspace.adminscripts
{
    using midspace.adminscripts.Messages;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

    public class CommandForceKick : ChatCommand
    {
        public static bool DropPlayer;

        public CommandForceKick()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Client | ChatCommandFlag.MultiplayerOnly, "forcekick", new string[] { "/forcekick" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/forcekick <#>", "Forces the specified player <#> to disconnect. Only use this if normal kick does not work.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/forcekick\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
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
                    MyAPIGateway.Utilities.ShowMessage("ForceKick", "No player named {0} found.", playerName);
                    return true;
                }

                MyAPIGateway.Utilities.ShowMessage("ForceKick", selectedPlayer.DisplayName);
                ConnectionHelper.SendMessageToServer(new MessageForceDisconnect() { SteamId = selectedPlayer.SteamUserId });
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
            if (midspace.adminscripts.CommandForceKick.DropPlayer)
                throw new Exception();
        }
    }
}
