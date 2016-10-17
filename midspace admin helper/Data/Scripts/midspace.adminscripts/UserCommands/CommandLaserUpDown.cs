namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using Sandbox.ModAPI;
    using VRage.ModAPI;
    using VRage.Voxels;
    using VRageMath;

    public class CommandLaserUpDown : ChatCommand
    {
        public CommandLaserUpDown()
            : base(ChatCommandSecurity.User, ChatCommandFlag.Experimental | ChatCommandFlag.Client, "up", new[] { "/laserup", "/laserdown", "/up", "/down" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/laserup", "Sets a GPS coordinate on the targeted item under the player crosshairs, showing the range.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var currentPlanetList = new List<IMyVoxelBase>();
            MyAPIGateway.Session.VoxelMaps.GetInstances(currentPlanetList, v => v is Sandbox.Game.Entities.MyPlanet);

            Vector3D playerPosition = MyAPIGateway.Session.Player.GetPosition();
            var closestDistance = Double.MaxValue;
            Sandbox.Game.Entities.MyPlanet closestPlanet = null;

            foreach (var planet in currentPlanetList)
            {
                var center = planet.WorldMatrix.Translation;
                var distance = Vector3D.Distance(playerPosition, center); // use distance to center of planet.
                if (distance < closestDistance)
                {
                    closestPlanet = (Sandbox.Game.Entities.MyPlanet)planet;
                    closestDistance = distance;
                }
            }

            if (closestPlanet != null)
            {
                if (messageText.StartsWith("/laserup", StringComparison.InvariantCultureIgnoreCase) ||
                    messageText.StartsWith("/up", StringComparison.InvariantCultureIgnoreCase))
                {
                    var v1 = closestPlanet.WorldMatrix.Translation - playerPosition;
                    v1.Normalize();
                    var gps = MyAPIGateway.Session.GPS.Create("Laser Up", "", playerPosition + (v1 * -1000), true, false);
                    MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, gps);
                }
                if (messageText.StartsWith("/laserdown", StringComparison.InvariantCultureIgnoreCase) ||
                    messageText.StartsWith("/down", StringComparison.InvariantCultureIgnoreCase))
                {
                    var v1 = closestPlanet.WorldMatrix.Translation - playerPosition;
                    v1.Normalize();

                    var gps = MyAPIGateway.Session.GPS.Create("Laser Down", "", playerPosition + (v1 * 1000), true, false);
                    MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, gps);

                    Vector3D closestSurfacePoint;
                    MyVoxelCoordSystems.WorldPositionToLocalPosition(closestPlanet.PositionLeftBottomCorner, ref playerPosition, out closestSurfacePoint);
                    var groundPosition = closestPlanet.GetClosestSurfacePointGlobal(ref closestSurfacePoint);

                    //closestPlanet.getgrav

                    //var d = Vector3D.Distance(playerPosition, groundPosition);

                    gps = MyAPIGateway.Session.GPS.Create("Laser Ground", "", groundPosition, true, false);
                    MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, gps);
                }


                return true;
            }

            MyAPIGateway.Utilities.ShowMessage("Range", "No planets detected in range.");
            return true;
        }
    }
}
