namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;

    public class CommandPlayerEject : ChatCommand
    {
        public CommandPlayerEject()
            : base(ChatCommandSecurity.Admin, "eject", new[] { "/eject" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/eject <#>", "The specified <#> player is removed from control of any ship.");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/eject\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var playerName = match.Groups["Key"].Value;
                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, p => p != null);
                IMyIdentity selectedPlayer = null;

                var identities = new List<IMyIdentity>();
                MyAPIGateway.Players.GetAllIdentites(identities, delegate(IMyIdentity i) { return i.DisplayName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase); });
                selectedPlayer = identities.FirstOrDefault();

                int index;
                if (playerName.Substring(0, 1) == "#" && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= CommandPlayerStatus.IdentityCache.Count)
                {
                    selectedPlayer = CommandPlayerStatus.IdentityCache[index - 1];
                }

                if (playerName.Substring(0, 1) == "B" && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= CommandListBots.BotCache.Count)
                {
                    selectedPlayer = CommandListBots.BotCache[index - 1];
                }

                if (selectedPlayer == null)
                    return false;

                var listplayers = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(listplayers, p => p.PlayerID == selectedPlayer.PlayerId);
                var player = listplayers.FirstOrDefault();

                if (player != null)
                {
                    if (player.Controller.ControlledEntity.Entity.Parent != null)
                    {
                        MyAPIGateway.Utilities.ShowMessage("ejecting", player.DisplayName);
                        player.Controller.ControlledEntity.Use();

                        // Neither of these do what I expect them to. In fact, I'm not sure what they do.
                        //MyAPIGateway.Players.RemoveControlledEntity(player.Controller.ControlledEntity.Entity);
                        //MyAPIGateway.Players.RemoveControlledEntity(player.Controller.ControlledEntity.Entity.Parent);
                    }
                    else
                    {
                        MyAPIGateway.Utilities.ShowMessage("player", string.Format("{0} is not a pilot", player.DisplayName));
                    }

                    return true;
                }

                // selectedPlayer is a Bot? 
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
    }
}
