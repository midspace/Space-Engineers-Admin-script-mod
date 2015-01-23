namespace midspace.adminscripts
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using Sandbox.ModAPI.Interfaces;
    using VRageMath;

    public class CommandObjectsPull : ChatCommand
    {
        public CommandObjectsPull()
            : base(ChatCommandSecurity.Admin, "pullobjects", new[] { "/pullobjects" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/pullobjects <range> <speed>", "Draws any floating objects in <range> to the player at specified <speed>. Negative speed will push objects. Zero speed will stop objects.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.StartsWith("/pullobjects ", StringComparison.InvariantCultureIgnoreCase))
            {
                var match = Regex.Match(messageText, @"/pullobjects\s{1,}(?<R>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s{1,}(?<V>[+-]?((\d+(\.\d*)?)|(\.\d+)))", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var range = double.Parse(match.Groups["R"].Value, CultureInfo.InvariantCulture);
                    var velocity = double.Parse(match.Groups["V"].Value, CultureInfo.InvariantCulture);
                    var playerEntity = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity;
                    var destination = playerEntity.WorldAABB.Center;
                    var sphere = new BoundingSphereD(destination, range);
                    var floatingList = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
                    floatingList = floatingList.Where(e => (e is Sandbox.ModAPI.IMyFloatingObject)).ToList();

                    foreach (var item in floatingList)
                    {
                        // Check for null physics and IsPhantom, to prevent picking up primitives.
                        if (item.Physics != null && !item.Physics.IsPhantom)
                        {
                            var position = item.GetPosition();
                            var vector = Vector3D.Normalize(destination - position) * velocity;
                            item.Physics.LinearVelocity = vector;
                        }
                    }

                    return true;
                }
            }

            return false;
        }
    }
}