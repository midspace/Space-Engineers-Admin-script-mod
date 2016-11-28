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
            GetBaseSunDirection(out baseSunDirection, out sunRotationAxis);

            IMyGps gps;
            var origin = MyAPIGateway.Session.Player.Controller.ControlledEntity.GetHeadMatrix(true, true, false).Translation;
            if (MyAPIGateway.Session.SessionSettings.EnableSunRotation)
            {
                // TODO: figure out why the RPM doesn't match.
                gps = MyAPIGateway.Session.GPS.Create("Sun observation " + (1.0d / MyAPIGateway.Session.SessionSettings.SunRotationIntervalMinutes).ToString("0.000000") + " RPM", description, origin, true, false);
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

                    gps = MyAPIGateway.Session.GPS.Create("Sun " + stageTime.ToString("hh\\:mm\\:ss"), description, origin + (finalSunDirection * 10000), true, false);
                    MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, gps);
                }

                gps = MyAPIGateway.Session.GPS.Create("Sun Axis+", description, origin + (sunRotationAxis * 10000), true, false);
                MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, gps);

                gps = MyAPIGateway.Session.GPS.Create("Sun Axis-", description, origin + (-sunRotationAxis * 10000), true, false);
                MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, gps);
            }
            else
            {
                // Current sun position.
                Vector3 fsd;
                //if (MyAPIGateway.Session.SessionSettings.EnableSunRotation)
                //{
                //    float a = MathHelper.TwoPi * (float)(MyAPIGateway.Session.ElapsedGameTime().TotalMinutes / MyAPIGateway.Session.SessionSettings.SunRotationIntervalMinutes);
                //    fsd = Vector3.Transform(baseSunDirection, Matrix.CreateFromAxisAngle(sunRotationAxis, a));
                //    fsd.Normalize();
                //}
                //else
                fsd = baseSunDirection;
                gps = MyAPIGateway.Session.GPS.Create("Sun **", description, origin + (fsd * 10000), true, false);
                MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, gps);
            }

            return true;
        }

        private void GetBaseSunDirection(out Vector3 baseSunDirection, out Vector3 sunRotationAxis)
        {
            baseSunDirection = Vector3.Zero;

            // -- MySector.InitEnvironmentSettings --
            var environment = MyAPIGateway.Session.GetSector().Environment;
            Vector3 baseSunDirectionNormalized;
            Vector3.CreateFromAzimuthAndElevation(environment.SunAzimuth, environment.SunElevation, out baseSunDirectionNormalized);

            if (!MyAPIGateway.Session.SessionSettings.EnableSunRotation)
            {
                baseSunDirection = baseSunDirectionNormalized;
                sunRotationAxis = Vector3.Zero;
                return;
            }

            // -- Sandbox.Game.SessionComponents.MySectorWeatherComponent.Init() --
            var cpnt = MyAPIGateway.Session.GetCheckpoint("null");
            MyObjectBuilder_SectorWeatherComponent weatherComp = null;
            foreach (var comp in cpnt.SessionComponents)
            {
                MyObjectBuilder_SectorWeatherComponent component = comp as MyObjectBuilder_SectorWeatherComponent;
                if (component != null)
                    weatherComp = component;
            }

            if (weatherComp != null && !weatherComp.BaseSunDirection.IsZero)
                baseSunDirection = weatherComp.BaseSunDirection;

            // -- Sandbox.Game.SessionComponents.MySectorWeatherComponent.BeforeStart() -- 
            float num = Math.Abs(baseSunDirection.X) + Math.Abs(baseSunDirection.Y) + Math.Abs(baseSunDirection.Z);
            if (num < 0.001D)
                baseSunDirection = baseSunDirectionNormalized;

            // -- VRage.Game.MySunProperties.SunRotationAxis --
            float num2 = Math.Abs(Vector3.Dot(baseSunDirectionNormalized, Vector3.Up));
            Vector3 result;
            if (num2 > 0.95f)
                result = Vector3.Cross(Vector3.Cross(baseSunDirectionNormalized, Vector3.Left), baseSunDirectionNormalized);
            else
                result = Vector3.Cross(Vector3.Cross(baseSunDirectionNormalized, Vector3.Up), baseSunDirectionNormalized);
            result.Normalize();
            sunRotationAxis = result;
        }
    }
}
