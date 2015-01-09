namespace midspace.adminscripts
{
    using System;
    using System.Globalization;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using VRageMath;

    public class CommandFlyTo : ChatCommand
    {
        public CommandFlyTo()
            : base(ChatCommandSecurity.Admin, "flyto", new[] { "/flyto" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/flyto <X> <Y> <Z> <Velocity>", "Sends player or piloted ship flying to position <X Y Z> at speed <Velocity>");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/flyto\s{1,}(?<X>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s{1,}(?<Y>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s{1,}(?<Z>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s{1,}(?<V>[+-]?((\d+(\.\d*)?)|(\.\d+)))", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var destination = new Vector3D(
                    double.Parse(match.Groups["X"].Value, CultureInfo.InvariantCulture),
                    double.Parse(match.Groups["Y"].Value, CultureInfo.InvariantCulture),
                    double.Parse(match.Groups["Z"].Value, CultureInfo.InvariantCulture));
                var velocity = double.Parse(match.Groups["V"].Value, CultureInfo.InvariantCulture);

                if (Vector3.IsValid(destination))
                {
                    var entity = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity;
                    if (entity is IMyCubeBlock) entity = entity.Parent;
                    if (entity.Physics != null)
                    {
                        var position = entity.GetPosition();
                        var vector = Vector3D.Normalize(destination - position) * velocity;
                        entity.Physics.LinearVelocity = vector;
                    }
                    return true;
                }
            }

            return false;
        }
    }
}
