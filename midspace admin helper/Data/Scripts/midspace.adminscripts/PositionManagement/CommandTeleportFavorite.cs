namespace midspace.adminscripts
{
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;

    public class CommandTeleportFavorite : ChatCommand
    {
        public CommandTeleportFavorite()
            : base(ChatCommandSecurity.Admin, "tpfav", new[] { "/tpfav" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/tpfav <name>", "Teleport player or piloted ship to the previously saved location named <name>.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/tpfav\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var saveName = match.Groups["Key"].Value;

                if (CommandTeleportList.PositionCache.ContainsKey(saveName))
                {
                    var position = CommandTeleportList.PositionCache[saveName];

                    var currentPosition = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetPosition();

                    if (MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.Parent == null)
                    {
                        // Move the player only.
                        var offset = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.WorldAABB.Center - MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetPosition();
                        MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.SetPosition(position - offset);
                    }
                    else
                    {
                        // Move the ship the player is piloting.
                        var cubeGrid = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetTopMostParent();
                        currentPosition = cubeGrid.GetPosition();
                        var grids = cubeGrid.GetAttachedGrids();
                        var worldOffset = position - MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetPosition();

                        foreach (var grid in grids)
                        {
                            grid.SetPosition(grid.GetPosition() + worldOffset);
                        }
                    }

                    // save teleport in history
                    CommandTeleportBack.SaveTeleportInHistory(currentPosition);
                    return true;
                }
            }

            MyAPIGateway.Utilities.ShowMessage("Unknown location", "Could not find the specified name.");

            return false;
        }
    }
}
