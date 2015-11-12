namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Sandbox.ModAPI;

    public class CommandListBots : ChatCommand
    {
        /// <summary>
        /// Temporary hotlist cache created when player requests a list of npcs, populated only by search results.
        /// </summary>
        public readonly static List<IMyIdentity> BotCache = new List<IMyIdentity>();

        public CommandListBots()
            : base(ChatCommandSecurity.Admin, "listbots", new[] { "/listbots" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/listbots", "Displays the current NPC entities.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/listbots", StringComparison.InvariantCultureIgnoreCase))
            {
                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, p => p != null);
                BotCache.Clear();
                var index = 1;
                var identities = new List<IMyIdentity>();
                MyAPIGateway.Players.GetAllIdentites(identities);

                foreach (var identity in identities)
                {
                    var steamPlayer = players.FirstOrDefault(p => p.PlayerID == identity.PlayerId);
                    if (steamPlayer == null)
                    {
                        MyAPIGateway.Utilities.ShowMessage(string.Format("#{0}", index++), "Bot '{0}'", identity.DisplayName);
                        BotCache.Add(identity);
                    }

                }
                return true;
            }
            return false;
        }
    }
}
