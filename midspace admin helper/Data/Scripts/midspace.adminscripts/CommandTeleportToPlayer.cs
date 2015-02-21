namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using VRageMath;
    using Sandbox.Definitions;

    public class CommandTeleportToPlayer : ChatCommand
    {
        /// <summary>
        /// Still working on this one.
        /// Need to make it safer to teleport when either player is a pilot.
        /// </summary>
        public CommandTeleportToPlayer()
            : base(ChatCommandSecurity.Admin, "tpp", new[] { "/tpp" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/tpp <#>", "Teleport you to the specified player <#>.");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/tpp\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);

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

                if (selectedPlayer == null)
                {
                    MyAPIGateway.Utilities.ShowMessage("Player name", string.Format("'{0}' not found", playerName));
                    return true;
                }

                var listplayers = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(listplayers, p => p.PlayerID == selectedPlayer.PlayerId);
                var player = listplayers.FirstOrDefault();

                if (player == null)
                {
                    MyAPIGateway.Utilities.ShowMessage("Player", "no longer exists");
                    return true;
                }


                if (MyAPIGateway.Session.Player.Controller.ControlledEntity is IMyCubeBlock)
                {
                    MyAPIGateway.Utilities.ShowMessage("Incomplete", "This function not complete. Cannot transport piloted Ship to another player.");
                    return true;
                }
                else
                {
                    if (player.Controller.ControlledEntity is IMyCubeBlock)
                    {
                        var cockpit = (IMyCubeBlock)player.Controller.ControlledEntity;

                        var definition = MyDefinitionManager.Static.GetCubeBlockDefinition(cockpit.BlockDefinition);
                        var cockpitDefintion = definition as MyCockpitDefinition;
                        var remoteDefintion = definition as MyRemoteControlDefinition;

                        // target is a pilot in cockpit.
                        if (cockpitDefintion != null)
                        {
                            if (cockpit.CubeGrid.GridSizeEnum != Sandbox.Common.ObjectBuilders.MyCubeSize.Small)
                            {
                                Support.MovePlayerToCockpit(MyAPIGateway.Session.Player, player.Controller.ControlledEntity.Entity);
                            }
                            else
                            {
                                return Support.MovePlayerToShipGrid(MyAPIGateway.Session.Player, cockpit.CubeGrid);
                            }
                            
                            return true;
                        }

                        if (remoteDefintion != null)
                        {
                            MyAPIGateway.Utilities.ShowMessage("Failed", string.Format("Cannot determine player location. Is Remote controlling '{0}'", cockpit.CubeGrid.DisplayName));

                            // where is the player? in a cockpit/chair or freefloating?

                            // player.GetPosition() is actually the remote ship location.

                            //var freePos = MyAPIGateway.Entities.FindFreePlace(player.GetPosition(), (float)player.Controller.ControlledEntity.Entity.WorldVolume.Radius, 500, 20, 1f);
                            //if (!freePos.HasValue)
                            //{
                            //    MyAPIGateway.Utilities.ShowMessage("Failed", "Could not find safe location to transport to.");
                            //    return true;
                            //}

                            //MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.SetPosition(freePos.Value);
                            return true;
                        }
                    }
                    else
                    {
                        // target is a player only.
                        Support.MovePlayerToPlayer(MyAPIGateway.Session.Player, player);
                        return true;
                    }
                }

                MyAPIGateway.Utilities.ShowMessage("Failed", "Unable to determine player location.");
                return true;
            }

            return false;
        }
    }
}
