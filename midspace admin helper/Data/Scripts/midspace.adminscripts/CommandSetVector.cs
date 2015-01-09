namespace midspace.adminscripts
{
    using System;
    using System.Globalization;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using VRageMath;

    public class CommandSetVector : ChatCommand
    {
        public CommandSetVector()
            : base(ChatCommandSecurity.Admin, "setvector", new[] { "/setvector" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/setvector <X> <Y> <Z>", "Set player or piloted ship flying on the specified vector <X Y Z>");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/setvector\s{1,}(?<X>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s{1,}(?<Y>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s{1,}(?<Z>[+-]?((\d+(\.\d*)?)|(\.\d+)))", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var targetVector = new Vector3D(
                    double.Parse(match.Groups["X"].Value, CultureInfo.InvariantCulture),
                    double.Parse(match.Groups["Y"].Value, CultureInfo.InvariantCulture),
                    double.Parse(match.Groups["Z"].Value, CultureInfo.InvariantCulture));

                if (Vector3D.IsValid(targetVector))
                {
                    var entity = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity;
                    if (entity is IMyCubeBlock) entity = entity.Parent;
                    if (entity.Physics != null)
                    {
                        entity.Physics.LinearVelocity = targetVector;
                    }
                    return true;
                }
            }

            return false;
        }
    }
}
