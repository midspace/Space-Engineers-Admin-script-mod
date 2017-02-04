namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using midspace.adminscripts.Messages;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;

    public class CommandForceKick : ChatCommand
    {
        public static bool DropPlayer;

        public CommandForceKick()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Server | ChatCommandFlag.MultiplayerOnly, "forcekick", new string[] { "/forcekick" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/forcekick <#>", "Forces the specified player <#> to disconnect. Only use this if normal kick does not work.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/forcekick\s+(?<Key>.+)", RegexOptions.IgnoreCase);
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
                List<IMyIdentity> cacheList = CommandPlayerStatus.GetIdentityCache(steamId);
                if (playerName.Substring(0, 1) == "#" && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= cacheList.Count)
                {
                    var listplayers = new List<IMyPlayer>();
                    MyAPIGateway.Players.GetPlayers(listplayers, p => p.IdentityId == cacheList[index - 1].IdentityId);
                    selectedPlayer = listplayers.FirstOrDefault();
                }

                if (selectedPlayer == null)
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "ForceKick", "No player named {0} found.", playerName);
                    return true;
                }

                ConnectionHelper.SendMessageToPlayer(selectedPlayer.SteamUserId, new MessageForceDisconnect { SteamId = selectedPlayer.SteamUserId, Ban = false });
                MyAPIGateway.Utilities.SendMessage(steamId, "Server", "{0} player Forcekicked", selectedPlayer.DisplayName);
                return true;
            }

            return false;
        }
    }
}

namespace Sandbox.Game.World
{
    using System;
    using VRage.Game.Components;

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class MySessions : MySessionComponentBase
    {
        public override void UpdateBeforeSimulation()
        {
            if (midspace.adminscripts.CommandForceKick.DropPlayer)
            {
                VRage.Utils.MyLog.Default.WriteLine("Player kicked from Server");
                throw new Exception();
            }
        }
    }
}
