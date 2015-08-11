namespace midspace.adminscripts
{
    using System.Globalization;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;

    public class CommandTeleportJump : ChatCommand
    {
        public CommandTeleportJump()
            : base(ChatCommandSecurity.Admin, "j", new[] { "/j", "/jump" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/j <Distance>", "Teleport player or piloted ship +<Distance> forward or -<Distance> Backward. (A simpler version of /to) Includes rotors and pistons!");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/((j)|(jump))\s{1,}(?<D>[+-]?((\d+(\.\d*)?)|(\.\d+)))", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var distance = double.Parse(match.Groups["D"].Value, CultureInfo.InvariantCulture);

                // Use the player to determine direction of offset.
                var worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.GetHeadMatrix(true, true, false, false); // dead center of player cross hairs.
                var currentPosition = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetPosition();

                if (MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.Parent == null)
                {
                    // Move the player only.

                    // Adjust for offset between head and foot, as SetPosition uses feet of player, whilst GetHeadMatrix(...) uses head.
                    // TODO: use player.WorldMatrix to precicely adjust for offset.
                    var position = worldMatrix.Translation + (worldMatrix.Forward * distance) + (worldMatrix.Down * 1.70292f); 
                    MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.SetPosition(position);
                }
                else
                {
                    // Move the ship the player is piloting.
                    var position = worldMatrix.Translation + worldMatrix.Forward * distance;
                    // TODO: adjust for offset between entity.WorldMatrix and GetHeadMatrix.
                    var cubeGrid = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetTopMostParent();
                    currentPosition = cubeGrid.GetPosition();
                    var grids = cubeGrid.GetAttachedGrids();
                    var worldOffset = position - MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetPosition();

                    foreach (var grid in grids)
                    {
                        grid.SetPosition(grid.GetPosition() + worldOffset);
                    }
                }

                //save teleport in history
                CommandTeleportBack.SaveTeleportInHistory(currentPosition);
                return true;
            }

            return false;
        }
    }
}
