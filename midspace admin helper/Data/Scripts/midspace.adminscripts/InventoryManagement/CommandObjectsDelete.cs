namespace midspace.adminscripts
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Sandbox.ModAPI;
    using VRage;
    using VRage.Game.ModAPI;
    using VRageMath;

    public class CommandObjectsDelete : ChatCommand
    {
        public CommandObjectsDelete()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Server, "delobjects", new[] { "/delobjects" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/delobjects <range>", "Deletes any floating objects in <range> of the player to player's location.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.StartsWith("/delobjects ", StringComparison.InvariantCultureIgnoreCase))
            {
                var match = Regex.Match(messageText, @"/delobjects\s+(?<R>[+-]?((\d+(\.\d*)?)|(\.\d+)))", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var range = double.Parse(match.Groups["R"].Value, CultureInfo.InvariantCulture);

                    Vector3D destination;

                    if (MyAPIGateway.Session.CameraController is MySpectator)
                    {
                        destination = MyAPIGateway.Session.Camera.WorldMatrix.Translation;
                    }
                    else if (MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.Parent == null)
                    {
                        var worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.GetHeadMatrix(true, true, false);
                        destination = worldMatrix.Translation + worldMatrix.Forward * 1.5f; // Spawn item 1.5m in front of player for safety.
                    }
                    else
                    {
                        var worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.WorldMatrix;
                        destination = worldMatrix.Translation + worldMatrix.Forward * 1.5f + worldMatrix.Up * 0.5f; // Spawn item 1.5m in front of player in cockpit for safety.
                    }

                    DeleteObjects(0, destination, range);
                    return true;
                }
            }

            return false;
        }

        public static void DeleteObjects(ulong steamId, Vector3D destination, double range)
        {
            var sphere = new BoundingSphereD(destination, range);
            var floatingList = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
            var floatingArray = floatingList.Where(e => (e is IMyFloatingObject) || (e is Sandbox.Game.Entities.MyInventoryBagEntity)).ToArray();
            int counter = 0;

            for (var i = 0; i < floatingArray.Length; i++)
            {
                var item = floatingArray[i];

                // Check for null physics and IsPhantom, to prevent picking up primitives.
                if (item.Physics != null && !item.Physics.IsPhantom)
                {
                    if (item is IMyFloatingObject || item is Sandbox.Game.Entities.MyInventoryBagEntity)
                    {
                        item.Close();
                        counter++;
                    }
                }
            }

            MyAPIGateway.Utilities.SendMessage(steamId, "Deleted", "{0} floating objects.", counter);
        }

    }
}