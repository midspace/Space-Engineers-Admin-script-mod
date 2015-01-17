namespace midspace.adminscripts
{
    using System.Globalization;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using VRageMath;

    public class CommandTeleport : ChatCommand
    {
        public CommandTeleport()
            : base(ChatCommandSecurity.Admin, "tp", new[] { "/tp" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/tp <X> <Y> <Z>", "Teleport player or piloted ship to the specified location <X Y Z>. Includes rotors and pistons!");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/tp\s{1,}(?<X>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s{1,}(?<Y>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s{1,}(?<Z>[+-]?((\d+(\.\d*)?)|(\.\d+)))", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var position = new Vector3D(
                    double.Parse(match.Groups["X"].Value, CultureInfo.InvariantCulture),
                    double.Parse(match.Groups["Y"].Value, CultureInfo.InvariantCulture),
                    double.Parse(match.Groups["Z"].Value, CultureInfo.InvariantCulture));

                var currentPosition = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetPosition();

                if (MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.Parent == null)
                {
                    // Move the player only.
                    MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.SetPosition(position);
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

                //save teleport in history
                CommandTeleportBack.SaveTeleportInHistory(currentPosition);
                return true;
            }

            return false;
        }
    }
}
