namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Messages.Sync;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;

    public class CommandPlayerEject : ChatCommand
    {
        public CommandPlayerEject()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Server, "eject", new[] { "/eject" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/eject <#>", "The specified <#> player is removed from control of any ship. This includes Remote Control and cockpits, thus ejecting them into space.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/eject\s+(?:(?:""(?<name>[^""]|.*?)"")|(?<name>.*))", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var playerName = match.Groups["name"].Value;
                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players);
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
                    MyAPIGateway.Utilities.SendMessage(steamId, "Eject", "No player named '{0}' found.", playerName);
                    return true;
                }

                MessageSyncAres.Eject(selectedPlayer.SteamUserId);
                return true;

                // NPC's do not appears as Players, but Identities.
                // There could be multiple Identities with the same name, for active, inactive and dead.
                //if (playerName.Substring(0, 1) == "B" && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= CommandListBots.BotCache.Count)
                //{
                //    selectedPlayer = CommandListBots.BotCache[index - 1];
                //}

                // TODO: figure out how to eject Autopilot.

                //var entities = new HashSet<IMyEntity>();
                //MyAPIGateway.Entities.GetEntities(entities, e => e is IMyCubeGrid);

                //foreach (var entity in entities)
                //{
                //    var cockpits = entity.FindWorkingCockpits();

                //    foreach (var cockpit in cockpits)
                //    {
                //        var block = (IMyCubeBlock)cockpit;
                //        if (block.OwnerId == selectedPlayer.IdentityId)
                //        {
                //            MyAPIGateway.Utilities.SendMessage(steamId, "ejecting", selectedPlayer.DisplayName);
                //            // Does not appear to eject Autopilot.
                //            cockpit.Use();
                //        }
                //    }
                //}
            }

            return false;
        }
    }
}
