namespace midspace.adminscripts
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Messages.Sync;
    using Sandbox.ModAPI;

    public class CommandFactionPeace : ChatCommand
    {
        public CommandFactionPeace()
            : base(ChatCommandSecurity.Admin, "fa", new[] { "/fa" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/fa <faction>", "The specified <faction> will accept all proposed peace treaties.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/fa\s{1,}(?<Faction>.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var factionName = match.Groups["Faction"].Value;

                if (!MyAPIGateway.Session.Factions.FactionTagExists(factionName, null) &&
                    !MyAPIGateway.Session.Factions.FactionNameExists(factionName, null))
                {
                    MyAPIGateway.Utilities.ShowMessage("faction", "{0} does not exist.", factionName);
                    return true;
                }

                var fc = MyAPIGateway.Session.Factions.GetObjectBuilder();
                var factionCollectionBuilder = fc.Factions.FirstOrDefault(f => f.Name.Equals(factionName, StringComparison.InvariantCultureIgnoreCase) ||
                f.Tag.Equals(factionName, StringComparison.InvariantCultureIgnoreCase));

                if (factionCollectionBuilder == null)
                {
                    MyAPIGateway.Utilities.ShowMessage("faction", "{0} could not be found.", factionName);
                    return true;
                }

                MessageSyncFaction.AcceptPeace(factionCollectionBuilder.FactionId);
                MyAPIGateway.Utilities.ShowMessage("faction", "{0} has accepted peace.", factionName);
                return true;
            }

            return false;
        }
    }
}
