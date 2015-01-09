namespace midspace.adminscripts
{
    using System.Globalization;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;

    public class CommandTeleportJump : ChatCommand
    {
        public CommandTeleportJump()
            : base(ChatCommandSecurity.Admin, "j", new[] { "/j" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/j <Distance>", "Teleport player or piloted ship +<Distance> forward or -<Distance> Backward. (A simpler version of /to) Includes rotors and pistons!");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/j\s{1,}(?<D>[+-]?((\d+(\.\d*)?)|(\.\d+)))", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var distance  = double.Parse(match.Groups["D"].Value, CultureInfo.InvariantCulture);

                // Use the player to determine direction of offset.
                var worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.WorldMatrix;
                var position = worldMatrix.Translation + worldMatrix.Forward * distance;

                if (MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.Parent == null)
                {
                    // Move the player only.
                    MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.SetPosition(position);
                }
                else
                {
                    // Move the ship the player is piloting.
                    var cubeGrid = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetTopMostParent();
                    var grids = cubeGrid.GetAttachedGrids();
                    var worldOffset = position - MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetPosition();

                    foreach (var grid in grids)
                    {
                        grid.SetPosition(grid.GetPosition() + worldOffset);
                    }
                }

                return true;
            }

            return false;
        }
    }
}
