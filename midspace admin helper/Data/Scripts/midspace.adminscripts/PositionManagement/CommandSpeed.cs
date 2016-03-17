namespace midspace.adminscripts
{
    using System.Globalization;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;
    using VRageMath;

    public class CommandSpeed : ChatCommand
    {
        public CommandSpeed()
            : base(ChatCommandSecurity.Admin, "speed", new[] { "/speed" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/speed <#>", "Set player or piloted ship flying forward at the specified speed m/s.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/speed\s+(?<V>[+-]?((\d+(\.\d*)?)|(\.\d+)))", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var vector = double.Parse(match.Groups["V"].Value, CultureInfo.InvariantCulture);
                var entity = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity;

                if (entity is IMyCubeBlock)
                {
                    var cubeGrid = entity.GetTopMostParent();
                    var grids = cubeGrid.GetAttachedGrids();
                    var targetVector = entity.WorldMatrix.Forward;
                    targetVector = Vector3D.Normalize(targetVector) * vector;

                    foreach (var grid in grids)
                    {
                        // accelerate all ship parts togeather, so bits don't go missing.
                        if (grid.Physics != null)
                            grid.Physics.LinearVelocity = targetVector;
                    }
                    return true;
                }
                else
                {
                    var worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.GetHeadMatrix(true, true, false); // dead center of player cross hairs.
                    var targetVector = worldMatrix.Forward;
                    targetVector = Vector3D.Normalize(targetVector) * vector;
                    entity.Physics.LinearVelocity = targetVector;
                    return true;
                }
            }

            return false;
        }
    }
}
