namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using midspace.adminscripts.Messages;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;

    public class CommandForceBan : ChatCommand
    {
        public CommandForceBan()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Server | ChatCommandFlag.MultiplayerOnly, "forceban", new string[] { "/forceban" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/forceban <#>", "Forces the specified player <#> to disconnect and bans him. Only use this if normal ban does not work.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/forceban\s+(?<Key>.+)", RegexOptions.IgnoreCase);
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
                    MyAPIGateway.Utilities.SendMessage(steamId, "ForceBan", "No player named {0} found.", playerName);
                    return true;
                }

                ChatCommandLogic.Instance.ServerCfg.Config.ForceBannedPlayers.Add(new Player()
                {
                    SteamId = selectedPlayer.SteamUserId,
                    PlayerName = selectedPlayer.DisplayName
                });

                ConnectionHelper.SendMessageToPlayer(selectedPlayer.SteamUserId, new MessageForceDisconnect { SteamId = selectedPlayer.SteamUserId, Ban = true });
                MyAPIGateway.Utilities.SendMessage(steamId, "Server", "{0} player Forcebanned", selectedPlayer.DisplayName);

                return true;
            }

            return false;
        }
    }
}
