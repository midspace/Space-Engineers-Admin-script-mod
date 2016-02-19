namespace midspace.adminscripts
{
    using System;
    using Sandbox.Common;
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRageMath;

    public class CommandHeading : ChatCommand
    {
        private static bool _reportHeading;

        public CommandHeading()
            : base(ChatCommandSecurity.User, "head", new[] { "/head" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/head", "Displays your heading.");
            MyAPIGateway.Utilities.ShowMessage("/head <on|off>", "Turn <on> to continue displaying your heading (Elevation and Azimuth), updating once a second.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/head", StringComparison.InvariantCultureIgnoreCase))
            {
                var playerQ = Quaternion.CreateFromRotationMatrix(MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetViewMatrix());
                var referenceQ = Quaternion.CreateFromForwardUp(Vector3.Forward, Vector3.Up); // or the direction to target.
                var fixRotate = Quaternion.Normalize(Quaternion.Inverse(playerQ) * referenceQ);
                var vector = MyMath.QuaternionToEuler(fixRotate);
                var elevation = vector.X * 180 / Math.PI;
                var azimuth = vector.Y * -180 / Math.PI;

                MyAPIGateway.Utilities.ShowMessage("Heading", string.Format("Elevation:{0:N}° Azimuth:{1:N}°", elevation, azimuth));
                return true;
            }

            if (messageText.StartsWith("/head ", StringComparison.InvariantCultureIgnoreCase))
            {
                var strings = messageText.Split(' ');
                if (strings.Length > 1)
                {
                    if (strings[1].Equals("on", StringComparison.InvariantCultureIgnoreCase) || strings[1].Equals("1", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _reportHeading = true;
                        return true;
                    }

                    if (strings[1].Equals("off", StringComparison.InvariantCultureIgnoreCase) || strings[1].Equals("0", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _reportHeading = false;
                        return true;
                    }
                }
            }

            return false;
        }

        public override void UpdateBeforeSimulation1000()
        {
            if (!_reportHeading) return;

            try
            {
                var playerQ = Quaternion.CreateFromRotationMatrix(MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetViewMatrix());
                var referenceQ = Quaternion.CreateFromForwardUp(Vector3.Forward, Vector3.Up);
                var fixRotate = Quaternion.Normalize(Quaternion.Inverse(playerQ) * referenceQ); // or the direction to target.
                var vector = MyMath.QuaternionToEuler(fixRotate);
                var elevation = vector.X * 180 / Math.PI;
                var azimuth = vector.Y * -180 / Math.PI;

                MyAPIGateway.Utilities.ShowNotification(string.Format("El:{0:N}° Az:{1:N}°", elevation, azimuth), 1000, MyFontEnum.White);
            }
            catch
            { 
                // occasional exception caused by GetViewMatrix when viewpoint is Exterior of piloted craft AND Spectator.
            }
        }
    }
}
