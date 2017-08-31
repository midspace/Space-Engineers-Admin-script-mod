namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using VRage.ModAPI;
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
                Vector3D zeroGravity = Vector3D.MinValue;
                var playerVector = closestPlanet.WorldMatrix.Translation - playerPosition;
                playerVector.Normalize();
                bool playerInGravity = false;

                MySphericalNaturalGravityComponent naturalGravity = closestPlanet.Components.Get<MyGravityProviderComponent>() as MySphericalNaturalGravityComponent;
                if (naturalGravity != null)
                {
                    //float gravityLimit = (float)(closestPlanet.MaximumRadius * Math.Pow(closestPlanet.GetInitArguments.SurfaceGravity / 0.05f, 1 / closestPlanet.GetInitArguments.GravityFalloff));
                    zeroGravity = closestPlanet.WorldMatrix.Translation + (playerVector * -naturalGravity.GravityLimit);
                    playerInGravity = playerPosition.IsBetween(zeroGravity, closestPlanet.WorldMatrix.Translation);
                }

                if (messageText.StartsWith("/laserup", StringComparison.InvariantCultureIgnoreCase) ||
                    messageText.StartsWith("/up", StringComparison.InvariantCultureIgnoreCase))
                {
                    MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, MyAPIGateway.Session.GPS.Create("Laser Up", "", playerPosition + (playerVector * -1000), true, false));

                    if (playerInGravity && zeroGravity != Vector3D.MinValue)
                        MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, MyAPIGateway.Session.GPS.Create("Laser Zero Gravity", "", zeroGravity, true, false));
                }
                if (messageText.StartsWith("/laserdown", StringComparison.InvariantCultureIgnoreCase) ||
                    messageText.StartsWith("/down", StringComparison.InvariantCultureIgnoreCase))
                {
                    MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, MyAPIGateway.Session.GPS.Create("Laser Down", "", playerPosition + (playerVector * 1000), true, false));

                    var groundPosition = closestPlanet.GetClosestSurfacePointGlobal(ref playerPosition);
                    MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, MyAPIGateway.Session.GPS.Create("Laser Ground", "", groundPosition, true, false));

                    if (!playerInGravity && zeroGravity != Vector3D.MinValue)
                        MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, MyAPIGateway.Session.GPS.Create("Laser Zero Gravity", "", zeroGravity, true, false));
                }

                return true;
            }

            MyAPIGateway.Utilities.ShowMessage("Range", "No planets detected in range.");
            return true;
        }
    }
}
