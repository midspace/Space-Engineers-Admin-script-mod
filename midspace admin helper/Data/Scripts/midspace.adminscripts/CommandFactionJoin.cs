namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;

    public class CommandFactionJoin : ChatCommand
    {
        public CommandFactionJoin()
            : base(ChatCommandSecurity.Admin, "fj", new[] { "/fj" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/fj <faction> <#|B>", "The specified <#> player or <B> bot joins <faction>.");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/fj\s{1,}(?<Faction>.+)\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var factionName = match.Groups["Faction"].Value;
                var playerName = match.Groups["Key"].Value;
                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, p => p != null);
                IMyIdentity selectedPlayer = null;

                var identities = new List<IMyIdentity>();
                MyAPIGateway.Players.GetAllIdentites(identities, delegate(IMyIdentity i) { return i.DisplayName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase); });
                selectedPlayer = identities.FirstOrDefault();

                int index;
                if (playerName.Substring(0, 1) == "#" && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= CommandStatus.IdentityCache.Count)
                {
                    selectedPlayer = CommandStatus.IdentityCache[index - 1];
                }

                if (playerName.Substring(0, 1) == "B" && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= CommandListBots.BotCache.Count)
                {
                    selectedPlayer = CommandListBots.BotCache[index - 1];
                }

                if (selectedPlayer == null)
                    return false;

                if (!MyAPIGateway.Session.Factions.FactionTagExists(factionName, null) &&
                    !MyAPIGateway.Session.Factions.FactionNameExists(factionName, null))
                {
                    MyAPIGateway.Utilities.ShowMessage("faction", string.Format("{0} does not exist.", factionName));
                    return true;
                }

                var fc = MyAPIGateway.Session.Factions.GetObjectBuilder();

                var factionBuilder = fc.Factions.FirstOrDefault(f => f.Members.Any(m => m.PlayerId == selectedPlayer.PlayerId));
                if (factionBuilder != null)
                {
                    MyAPIGateway.Utilities.ShowMessage("player", string.Format("{0} is already in faction {1}.{2}", selectedPlayer.DisplayName, factionBuilder.Tag, factionBuilder.Name));
                    return true;
                }

                var factionCollectionBuilder = fc.Factions.FirstOrDefault(f => f.Name.Equals(factionName, StringComparison.InvariantCultureIgnoreCase) ||
                f.Tag.Equals(factionName, StringComparison.InvariantCultureIgnoreCase));

                
                // Doesn't work right on dedicated servers. !@#$!@
                MyAPIGateway.Session.Factions.AddPlayerToFaction(selectedPlayer.PlayerId, factionCollectionBuilder.FactionId);

                MyAPIGateway.Session.Factions.SendJoinRequest(factionCollectionBuilder.FactionId, selectedPlayer.PlayerId);
                MyAPIGateway.Session.Factions.AcceptJoin(factionCollectionBuilder.FactionId, selectedPlayer.PlayerId);

                MyAPIGateway.Utilities.ShowMessage("join", string.Format("{0} has been addded to faction.", selectedPlayer.DisplayName));

                return true;
            }

            return false;
        }
    }
}
