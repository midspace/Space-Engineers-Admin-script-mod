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
            IMyGps gps = MyAPIGateway.Session.GPS.Create("Sun observation", "", origin, true, false);
            MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, gps);

            long sunRotationInterval = (long)(TimeSpan.TicksPerMinute * (decimal)MyAPIGateway.Session.SessionSettings.SunRotationIntervalMinutes);
            long stage = sunRotationInterval / 20;
            // Sun interval for 360 degrees.
            for (long rotation = 0; rotation < sunRotationInterval; rotation += stage)
            {
                // copied from Sandbox.Game.Gui.MyGuiScreenGamePlay.Draw()
                var stageTime = new TimeSpan(rotation);
                float angle = MathHelper.TwoPi * (float)(stageTime.TotalMinutes / MyAPIGateway.Session.SessionSettings.SunRotationIntervalMinutes);
                var sunDirection = baseSunDirection;
                float originalSunCosAngle = Math.Abs(Vector3.Dot(sunDirection, Vector3.Up));
                Vector3 sunRotationAxis = Vector3.Cross(Vector3.Cross(sunDirection, originalSunCosAngle > 0.95f ? Vector3.Left : Vector3.Up), sunDirection);
                sunDirection = Vector3.Normalize(Vector3.Transform(sunDirection, Matrix.CreateFromAxisAngle(sunRotationAxis, angle)));
                var finalSunDirection = -sunDirection;

                gps = MyAPIGateway.Session.GPS.Create("Sun " + stageTime.ToString("hh\\:mm\\:ss"), "", origin + (finalSunDirection * 100000), true, false);
                MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, gps);
            }

            return true;
        }
    }
}
