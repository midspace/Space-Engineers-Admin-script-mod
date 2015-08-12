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
                var entity = MyAPIGateway.Session.Player.Controller.ControlledEntity;
                var currentPosition = entity.Entity.GetPosition();

                if (entity.Entity.Parent == null)
                {
                    // Move the player only.
                    // Use player HeadMatrix to calculate direction of Jump.
                    // Dead center of player cross hairs, except in thrid person where the view can be shifted with ALT.
                    var worldMatrix = entity.GetHeadMatrix(true, true, false, false);
                    var position = entity.Entity.GetPosition() + (worldMatrix.Forward * distance);
                    entity.Entity.SetPosition(position);
                }
                else
                {
                    // Move the ship the player is piloting.
                    var cubeGrid = entity.Entity.GetTopMostParent();
                    var grids = cubeGrid.GetAttachedGrids();

                    // Use cockpit/chair WorldMatrix to calculate direction of Jump.
                    var worldOffset = (entity.Entity.WorldMatrix.Forward * distance);
                    foreach (var grid in grids)
                        grid.SetPosition(grid.GetPosition() + worldOffset);
                }

                //save teleport in history
                CommandTeleportBack.SaveTeleportInHistory(currentPosition);
                return true;
            }

            return false;
        }
    }
}
