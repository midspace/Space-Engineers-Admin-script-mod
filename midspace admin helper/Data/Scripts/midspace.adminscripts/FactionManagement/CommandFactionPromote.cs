namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Messages.Sync;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;

    public class CommandFactionPromote : ChatCommand
    {
        public CommandFactionPromote()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Server, "fp", new[] { "/fp" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/fp <#>", "Promotes the specified <#> player one level within their faction.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/fp\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
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
                    return false;

                var fc = MyAPIGateway.Session.Factions.GetObjectBuilder();
                var factionBuilder = fc.Factions.FirstOrDefault(f => f.Members.Any(m => m.PlayerId == selectedPlayer.IdentityId));

                if (factionBuilder == null)
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "promote", "{0} is not in faction.", selectedPlayer.DisplayName);
                    return true;
                }

                var fm = factionBuilder.Members.FirstOrDefault(m => m.PlayerId == selectedPlayer.IdentityId);

                if (fm.IsFounder)
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "promote", "{0} is Founder and cannot be promoted.", selectedPlayer.DisplayName);
                    return true;
                }

                if (fm.IsLeader)
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "promote", "{0} is Leader and cannot be promoted.", selectedPlayer.DisplayName);
                    return true;
                }

                MessageSyncFaction.PromotePlayer(factionBuilder.FactionId, selectedPlayer.IdentityId);
                MyAPIGateway.Utilities.SendMessage(steamId, "promote", "{0} from Member to Leader.", selectedPlayer.DisplayName);
                return true;
            }

            return false;
        }
    }
}
