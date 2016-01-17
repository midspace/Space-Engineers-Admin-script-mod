namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Messages.Sync;
    using Sandbox.ModAPI;

    public class CommandPlayerEject : ChatCommand
    {
        private Queue<Action> _workQueue = new Queue<Action>();

        public CommandPlayerEject()
            : base(ChatCommandSecurity.Admin, "eject", new[] { "/eject" })
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
                MyAPIGateway.Players.GetPlayers(players, p => p != null);
                IMyPlayer selectedPlayer = null;

                var findPlayer = players.FirstOrDefault(p => p.DisplayName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase));
                if (findPlayer != null)
                {
                    selectedPlayer = findPlayer;
                }

                int index;
                if (playerName.Substring(0, 1) == "#" && int.TryParse(playerName.Substring(1), out index) && index > 0 && index <= CommandPlayerStatus.IdentityCache.Count)
                {
                    var listplayers = new List<IMyPlayer>();
                    MyAPIGateway.Players.GetPlayers(listplayers, p => p.PlayerID == CommandPlayerStatus.IdentityCache[index - 1].PlayerId);
                    selectedPlayer = listplayers.FirstOrDefault();
                }

                if (selectedPlayer == null)
                {
                    MyAPIGateway.Utilities.ShowMessage("Eject", "No player named '{0}' found.", playerName);
                    return true;
                }

                MessageSyncAres.Eject(selectedPlayer.SteamUserId);

                //if (selectedPlayer.Controller.ControlledEntity.Entity.Parent != null)
                //{
                //    MyAPIGateway.Utilities.ShowMessage("ejecting", selectedPlayer.DisplayName);
                //    selectedPlayer.Controller.ControlledEntity.Use();

                //    // Enqueue the command a second time, to make sure the player is ejected from a remote controlled ship and a piloted ship.
                //    _workQueue.Enqueue(delegate() {
                //        if (selectedPlayer.Controller.ControlledEntity.Entity.Parent != null)
                //        {
                //            selectedPlayer.Controller.ControlledEntity.Use();
                //        }
                //    });

                //    // Neither of these do what I expect them to. In fact, I'm not sure what they do.
                //    //MyAPIGateway.Players.RemoveControlledEntity(player.Controller.ControlledEntity.Entity);
                //    //MyAPIGateway.Players.RemoveControlledEntity(player.Controller.ControlledEntity.Entity.Parent);
                //}
                //else
                //{
                //    MyAPIGateway.Utilities.ShowMessage("player", string.Format("{0} is not a pilot", selectedPlayer.DisplayName));
                //}
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
                //        if (block.OwnerId == selectedPlayer.PlayerId)
                //        {
                //            MyAPIGateway.Utilities.ShowMessage("ejecting", selectedPlayer.DisplayName);
                //            // Does not appear to eject Autopilot.
                //            cockpit.Use();
                //        }
                //    }
                //}
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
