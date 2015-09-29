namespace midspace.adminscripts
{
    using Sandbox.ModAPI;
    using VRage.ModAPI;
    using VRageMath;

    public class CommandLaserRangefinder : ChatCommand
    {
        public CommandLaserRangefinder()
            : base(ChatCommandSecurity.User, "range", new[] { "/range" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/range", "Sets a GPS coordinate on the targeted item under the player crosshairs, showing the range.");
        }

        public override bool Invoke(string messageText)
        {
            IMyEntity entity;
            double distance;
            Vector3D hitPoint;
            Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, out entity, out distance, out hitPoint, true, true, true, true, true);
            if (entity != null && distance < MyAPIGateway.Session.SessionSettings.ViewDistance)
            {
                var gps = MyAPIGateway.Session.GPS.Create("Laser Range", "", hitPoint, true, false);
                MyAPIGateway.Session.GPS.AddLocalGps(gps);
                return true;
            }

            MyAPIGateway.Utilities.ShowMessage("Range", "Could not find object.");
            return true;
        }
    }
}
