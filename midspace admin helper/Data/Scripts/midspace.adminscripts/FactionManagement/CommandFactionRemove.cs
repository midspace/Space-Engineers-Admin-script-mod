namespace midspace.adminscripts
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;

    public class CommandFactionRemove : ChatCommand
    {
        public CommandFactionRemove()
            : base(ChatCommandSecurity.Admin, "fr", new[] { "/fr" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/fr <faction>", "The specified <faction> is removed.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/fr\s{1,}(?<Faction>.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var factionName = match.Groups["Faction"].Value;

                if (!MyAPIGateway.Session.Factions.FactionTagExists(factionName, null) &&
                    !MyAPIGateway.Session.Factions.FactionNameExists(factionName, null))
                {
                    MyAPIGateway.Utilities.ShowMessage("faction", string.Format("{0} does not exist.", factionName));
                    return true;
                }

                var fc = MyAPIGateway.Session.Factions.GetObjectBuilder();
                var factionCollectionBuilder = fc.Factions.FirstOrDefault(f => f.Name.Equals(factionName, StringComparison.InvariantCultureIgnoreCase) ||
                f.Tag.Equals(factionName, StringComparison.InvariantCultureIgnoreCase));

                if (factionCollectionBuilder == null)
                {
                    MyAPIGateway.Utilities.ShowMessage("faction", string.Format("{0} could not be found.", factionName));
                    return true;
                }

                MyAPIGateway.Session.Factions.RemoveFaction(factionCollectionBuilder.FactionId);
                MyAPIGateway.Utilities.ShowMessage("faction", string.Format("{0} has been removed.", factionName));

                return true;
            }

            return false;
        }
    }
}
