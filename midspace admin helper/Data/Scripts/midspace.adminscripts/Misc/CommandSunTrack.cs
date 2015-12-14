namespace midspace.adminscripts
{
    using System;
    using Sandbox.ModAPI;
    using VRageMath;

    public class CommandSunTrack : ChatCommand
    {
        public CommandSunTrack()
            : base(ChatCommandSecurity.Admin, "suntrack", new[] { "/suntrack" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/suntrack", "Sets GPS coordinates showing the movement of the sun.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (!MyAPIGateway.Session.SessionSettings.EnableSunRotation)
            {
                MyAPIGateway.Utilities.ShowMessage("Suntrack", "The sun is not configured to orbit.");
                return true;
            }

            var environment = MyAPIGateway.Session.GetSector().Environment;
            Vector3D baseSunDirection;
            Vector3D.CreateFromAzimuthAndElevation(environment.SunAzimuth, environment.SunElevation, out baseSunDirection);
            baseSunDirection = -baseSunDirection;

            var origin = MyAPIGateway.Session.Player.Controller.ControlledEntity.GetHeadMatrix(true, true, false).Translation;
            // TODO: figure out why the RPM doesn't match.
            //IMyGps gps = MyAPIGateway.Session.GPS.Create("Sun observation " + (1.0d / MyAPIGateway.Session.SessionSettings.SunRotationIntervalMinutes).ToString("N") + " RPM", "", origin, true, false);
            IMyGps gps = MyAPIGateway.Session.GPS.Create("Sun observation", "", origin, true, false);
            MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, gps);

            long sunRotationInterval = (long)(TimeSpan.TicksPerMinute * (decimal)MyAPIGateway.Session.SessionSettings.SunRotationIntervalMinutes);
            long stage = sunRotationInterval / 20;
            // Sun interval for 360 degrees.
            for (long rotation = 0; rotation < sunRotationInterval; rotation += stage)
            {
                var stageTime = new TimeSpan(rotation);
                var finalSunDirection = GetSunDirection(baseSunDirection, stageTime.TotalMinutes);

                gps = MyAPIGateway.Session.GPS.Create("Sun " + stageTime.ToString("hh\\:mm\\:ss"), "", origin + (finalSunDirection * 10000), true, false);
                MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, gps);
            }

            var vector1 = GetSunDirection(baseSunDirection, 0);
            var vector2 = GetSunDirection(baseSunDirection, new TimeSpan(sunRotationInterval / 4).TotalMinutes);

            var zenith = Vector3D.Normalize(Vector3D.Cross(vector1, vector2));
            gps = MyAPIGateway.Session.GPS.Create("Sun Axis+", "", origin + (zenith * 10000), true, false);
            MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, gps);

            gps = MyAPIGateway.Session.GPS.Create("Sun Axis-", "", origin + (-zenith * 10000), true, false);
            MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, gps);

            return true;
        }

        private Vector3D GetSunDirection(Vector3D baseSunDirection, double elapsedMinutes)
        {
            // copied from Sandbox.Game.Gui.MyGuiScreenGamePlay.Draw()
            double angle = MathHelper.TwoPi * (elapsedMinutes / MyAPIGateway.Session.SessionSettings.SunRotationIntervalMinutes);
            var sunDirection = baseSunDirection;
            double originalSunCosAngle = Math.Abs(Vector3D.Dot(sunDirection, Vector3D.Up));
            Vector3D sunRotationAxis = Vector3D.Cross(Vector3D.Cross(sunDirection, originalSunCosAngle > 0.95f ? Vector3D.Left : Vector3D.Up), sunDirection);
            sunDirection = Vector3D.Normalize(Vector3D.Transform(sunDirection, MatrixD.CreateFromAxisAngle(sunRotationAxis, angle)));
            return -sunDirection;
        }
    }
}
