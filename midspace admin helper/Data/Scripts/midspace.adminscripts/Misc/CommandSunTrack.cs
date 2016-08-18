namespace midspace.adminscripts
{
    using System;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;
    using VRage.Game.ObjectBuilders;
    using VRageMath;

    /// <summary>
    /// Sets a bunch of GPS coordinates along the path the sun takes when orbiting the map.
    /// I use orbit loosely, because the sun is a fixed point on the skybox, not a 3 dimentional artifact.            
    /// Also, the axis of rotation is marked off by additional GPS markers.
    /// 
    /// We're using Double for all calculations here, because this is a once of ahoc call.
    /// If performance was an issue, you should use Single (float).
    /// </summary>
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

            const string description = "/suntrack clear";
            bool clear = false;
            int counters = 0;

            var parameters = messageText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string parameter in parameters)
            {
                if (parameter.Equals("clear", StringComparison.InvariantCultureIgnoreCase))
                {
                    clear = true;
                    break;
                }

                if (int.TryParse(parameter, out counters))
                    break;
            }

            if (clear)
            {
                var list = MyAPIGateway.Session.GPS.GetGpsList(MyAPIGateway.Session.Player.IdentityId);

                foreach (IMyGps clearGps in list)
                {
                    if (clearGps.Name.StartsWith("Sun") && clearGps.Description == description)
                        MyAPIGateway.Session.GPS.RemoveGps(MyAPIGateway.Session.Player.IdentityId, clearGps);
                }

                MyAPIGateway.Utilities.ShowMessage("Suntrack", "Cleared all gps coordinates.");
                return true;
            }

            if (counters == 0)
                counters = 20;
            else if (counters < 4)
                counters = 4;
            else if (counters > 24)
                counters = 24;

            Vector3 baseSunDirection;
            Vector3 sunRotationAxis;
            float sunSpeed;
            GetBaseSunDirection(out baseSunDirection, out sunRotationAxis, out sunSpeed);


            var origin = MyAPIGateway.Session.Player.Controller.ControlledEntity.GetHeadMatrix(true, true, false).Translation;
            // TODO: figure out why the RPM doesn't match.
            IMyGps gps = MyAPIGateway.Session.GPS.Create("Sun observation " + (1.0d / MyAPIGateway.Session.SessionSettings.SunRotationIntervalMinutes).ToString("0.000000") + " RPM", description, origin, true, false);
            MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, gps);

            long sunRotationInterval = (long)(TimeSpan.TicksPerMinute * (decimal)MyAPIGateway.Session.SessionSettings.SunRotationIntervalMinutes);
            long stage = sunRotationInterval / counters;
            // Sun interval for 360 degrees.
            for (long rotation = 0; rotation < sunRotationInterval; rotation += stage)
            {
                var stageTime = new TimeSpan(rotation);
                float angle = MathHelper.TwoPi * rotation / sunRotationInterval;
                Vector3 finalSunDirection = Vector3.Transform(baseSunDirection, Matrix.CreateFromAxisAngle(sunRotationAxis, angle));
                finalSunDirection.Normalize();

                gps = MyAPIGateway.Session.GPS.Create("Sun " + stageTime.ToString("hh\\:mm\\:ss"), description, origin + (-finalSunDirection * 10000), true, false);
                MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, gps);
            }

            gps = MyAPIGateway.Session.GPS.Create("Sun Axis+", description, origin + (sunRotationAxis * 10000), true, false);
            MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, gps);

            gps = MyAPIGateway.Session.GPS.Create("Sun Axis-", description, origin + (-sunRotationAxis * 10000), true, false);
            MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, gps);

            // Current sun position.
            //float a = MathHelper.TwoPi * (float)(MyAPIGateway.Session.ElapsedGameTime().TotalMinutes / sunSpeed);
            //Vector3 fsd = Vector3.Transform(baseSunDirection, Matrix.CreateFromAxisAngle(sunRotationAxis, a));
            //fsd.Normalize();

            //gps = MyAPIGateway.Session.GPS.Create("SUN", description, origin + (-fsd * 10000), true, false);
            //MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, gps);

            return true;
        }

        private void GetBaseSunDirection(out Vector3 baseSunDirection, out Vector3 sunRotationAxis, out float sunSpeed)
        {
            baseSunDirection = Vector3.Zero;
            sunSpeed = 0;

            var cpnt = MyAPIGateway.Session.GetCheckpoint("null");
            foreach (var comp in cpnt.SessionComponents)
            {
                var weatherComp = comp as MyObjectBuilder_SectorWeatherComponent;
                if (weatherComp != null)
                {
                    baseSunDirection = weatherComp.BaseSunDirection;
                    sunSpeed = weatherComp.SunSpeed;
                }
            }

            float num = Math.Abs(Vector3.Dot(baseSunDirection, Vector3.Up));
            if (num > 0.95f)
                sunRotationAxis = Vector3.Cross(Vector3.Cross(baseSunDirection, Vector3.Left), baseSunDirection);
            else
                sunRotationAxis = Vector3.Cross(Vector3.Cross(baseSunDirection, Vector3.Up), baseSunDirection);
            sunRotationAxis.Normalize();
        }
    }
}
