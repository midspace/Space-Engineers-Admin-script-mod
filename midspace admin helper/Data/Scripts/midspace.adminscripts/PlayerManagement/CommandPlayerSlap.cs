namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using midspace.adminscripts.Messages.Sync;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;

    public class CommandPlayerSlap : ChatCommand
    {
        public CommandPlayerSlap()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Server, "slap", new[] { "/slap" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/slap <#>", "Slaps the specified <#> player. Pushs the player around with 1hp health loss. Cannot slap pilots.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/slap\s+(?:(?:""(?<name>[^""]|.*?)"")|(?<name>.*))", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var playerName = match.Groups["name"].Value;
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
                if (playerName.Substring(0, 1) == "#" && int.TryParse(playerName.Substring(1), out index) && index > 0 && index <= cacheList.Count)
                {
                    var listplayers = new List<IMyPlayer>();
                    MyAPIGateway.Players.GetPlayers(listplayers, p => p.IdentityId == cacheList[index - 1].IdentityId);
                    selectedPlayer = listplayers.FirstOrDefault();
                }

                if (selectedPlayer == null)
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "Slap", string.Format("No player named '{0}' found.", playerName));
                    return true;
                }

                MessageSyncAres.Slap(selectedPlayer.SteamUserId);
                return true;
            }

            return false;
        }
    }
}
