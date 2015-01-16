namespace midspace.adminscripts
{
    using System.Globalization;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using VRageMath;

    public class CommandTeleportOffset : ChatCommand
    {
        public CommandTeleportOffset()
            : base(ChatCommandSecurity.Admin, "to", new[] { "/to" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/to <X> <Y> <Z>", "Teleport Offset a player or piloted ship, thus moving them by the specified values <X Y Z>. Includes rotors and pistons!");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/to\s{1,}(?<X>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s{1,}(?<Y>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s{1,}(?<Z>[+-]?((\d+(\.\d*)?)|(\.\d+)))", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var offset = new Vector3D(
                    double.Parse(match.Groups["X"].Value, CultureInfo.InvariantCulture),
                    double.Parse(match.Groups["Y"].Value, CultureInfo.InvariantCulture),
                    double.Parse(match.Groups["Z"].Value, CultureInfo.InvariantCulture));

                var currentPosition = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetPosition();

                // Use the player to determine direction of offset.
                var worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.WorldMatrix;
                var position = worldMatrix.Translation + worldMatrix.Right * offset.X + worldMatrix.Up * offset.Y + worldMatrix.Backward * offset.Z;

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
                CommandBack.SaveTeleportInHistory(currentPosition);
                return true;
            }

            return false;
        }
    }
}
