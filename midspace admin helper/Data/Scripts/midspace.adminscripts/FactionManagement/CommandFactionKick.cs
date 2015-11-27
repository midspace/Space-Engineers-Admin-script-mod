namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using midspace.adminscripts.Messages.Sync;
    using Sandbox.ModAPI;

    public class CommandFactionKick : ChatCommand
    {
        public CommandFactionKick()
            : base(ChatCommandSecurity.Admin, "fk", new[] { "/fk" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/fk <#|B>", "Kicks the specified <#> player or <B> bot from their current faction.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/fk\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var playerName = match.Groups["Key"].Value;
                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, p => p != null);
                IMyIdentity selectedPlayer = null;

                var identities = new List<IMyIdentity>();
                MyAPIGateway.Players.GetAllIdentites(identities, delegate (IMyIdentity i) { return i.DisplayName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase); });
                selectedPlayer = identities.FirstOrDefault();

                int index;
                if (playerName.Substring(0, 1) == "#" && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= CommandPlayerStatus.IdentityCache.Count)
                {
                    selectedPlayer = CommandPlayerStatus.IdentityCache[index - 1];
                }

                if (playerName.Substring(0, 1).Equals("B", StringComparison.InvariantCultureIgnoreCase) && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= CommandListBots.BotCache.Count)
                {
                    selectedPlayer = CommandListBots.BotCache[index - 1];
                }

                if (selectedPlayer == null)
                    return false;

                var fc = MyAPIGateway.Session.Factions.GetObjectBuilder();

                var request = fc.Factions.FirstOrDefault(f => f.JoinRequests.Any(r => r.PlayerId == selectedPlayer.PlayerId));
                if (request != null)
                {
                    MessageSyncFaction.CancelJoinFaction(request.FactionId, selectedPlayer.PlayerId);
                    //MyAPIGateway.Session.Factions.CancelJoinRequest(request.FactionId, selectedPlayer.PlayerId);
                    MyAPIGateway.Utilities.ShowMessage("kick", "{0} has had join request cancelled.", selectedPlayer.DisplayName);
                    return true;
                }

                var factionBuilder = fc.Factions.FirstOrDefault(f => f.Members.Any(m => m.PlayerId == selectedPlayer.PlayerId));

                if (factionBuilder == null)
                {
                    MyAPIGateway.Utilities.ShowMessage("kick", "{0} is not in faction.", selectedPlayer.DisplayName);
                    return true;
                }

                MessageSyncFaction.KickFaction(factionBuilder.FactionId, selectedPlayer.PlayerId);
                MyAPIGateway.Utilities.ShowMessage("kick", "{0} has been removed from faction.", selectedPlayer.DisplayName);
                return true;
            }

            return false;
        }
    }
}
