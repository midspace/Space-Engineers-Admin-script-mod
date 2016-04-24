namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using midspace.adminscripts.Messages.Sync;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;

    public class CommandFactionJoin : ChatCommand
    {
        private Queue<Action> _workQueue = new Queue<Action>();

        public CommandFactionJoin()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Server, "fj", new[] { "/fj" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/fj <faction> <#|B>", "The specified <#> player or <B> bot joins <faction>.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/fj\s+(?<Faction>.+)\s+(?<Key>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var factionName = match.Groups["Faction"].Value;
                var playerName = match.Groups["Key"].Value;
                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, p => p != null);

                var identities = new List<IMyIdentity>();
                MyAPIGateway.Players.GetAllIdentites(identities, delegate (IMyIdentity i) { return i.DisplayName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase); });
                IMyIdentity selectedPlayer = identities.FirstOrDefault();

                int index;
                List<IMyIdentity> cacheList = CommandPlayerStatus.GetIdentityCache(steamId);
                if (playerName.Substring(0, 1) == "#" && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= cacheList.Count)
                {
                    selectedPlayer = cacheList[index - 1];
                }

                List<IMyIdentity> botCacheList = CommandListBots.GetIdentityCache(steamId);
                if (playerName.Substring(0, 1).Equals("B", StringComparison.InvariantCultureIgnoreCase) && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= botCacheList.Count)
                {
                    selectedPlayer = botCacheList[index - 1];
                }

                if (selectedPlayer == null)
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "fj", "specified player could not be found.");
                    return true;
                }

                if (!MyAPIGateway.Session.Factions.FactionTagExists(factionName) &&
                    !MyAPIGateway.Session.Factions.FactionNameExists(factionName))
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "faction", "{0} does not exist.", factionName);
                    return true;
                }

                var fc = MyAPIGateway.Session.Factions.GetObjectBuilder();

                var factionBuilder = fc.Factions.FirstOrDefault(f => f.Members.Any(m => m.PlayerId == selectedPlayer.PlayerId));
                if (factionBuilder != null)
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "player", "{0} is already in faction {1}.{2}", selectedPlayer.DisplayName, factionBuilder.Tag, factionBuilder.Name);
                    return true;
                }

                var factionCollectionBuilder = fc.Factions.FirstOrDefault(f => f.Name.Equals(factionName, StringComparison.InvariantCultureIgnoreCase) ||
                    f.Tag.Equals(factionName, StringComparison.InvariantCultureIgnoreCase));

                if (factionCollectionBuilder != null)
                {
                    MessageSyncFaction.JoinFaction(factionCollectionBuilder.FactionId, selectedPlayer.PlayerId);
                    MyAPIGateway.Utilities.SendMessage(steamId, "join", "{0} has been addded to faction.", selectedPlayer.DisplayName);
                }

                return true;
            }

            return false;
        }

        public override void UpdateBeforeSimulation100()
        {
            if (_workQueue.Count > 0)
            {
                var action = _workQueue.Dequeue();
                action.Invoke();
            }
        }
    }
}
