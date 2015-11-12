namespace midspace.adminscripts
{
    using System;

    using Sandbox.Common;
    using Sandbox.ModAPI;

    public class CommandPosition : ChatCommand
    {
        private static bool _reportPosition;

        public CommandPosition()
            : base(ChatCommandSecurity.User, "pos", new[] { "/pos" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/pos", "Displays your position.");
            MyAPIGateway.Utilities.ShowMessage("/pos <on|off>", "Turn <on> to continue displaying your position, updating once a second.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {

            if (messageText.Equals("/pos", StringComparison.InvariantCultureIgnoreCase))
            {
                var position = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetPosition();
                MyAPIGateway.Utilities.ShowMessage("Pos", string.Format("x={0:N},y={1:N},z={2:N}", position.X, position.Y, position.Z));
                return true;
            }

            if (messageText.StartsWith("/pos ", StringComparison.InvariantCultureIgnoreCase))
            {
                var strings = messageText.Split(' ');
                if (strings.Length > 1)
                {
                    if (strings[1].Equals("on", StringComparison.InvariantCultureIgnoreCase) || strings[1].Equals("1", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _reportPosition = true;
                        return true;
                    }

                    if (strings[1].Equals("off", StringComparison.InvariantCultureIgnoreCase) || strings[1].Equals("0", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _reportPosition = false;
                        return true;
                    }
                }
            }

            return false;
        }

        public override void UpdateBeforeSimulation1000()
        {
            if (!_reportPosition) return;

            //var position = MyAPIGateway.Session.ControlledObject.Entity.GetPosition();
            //var position = MyAPIGateway.Session.CameraController.GetViewMatrix().Translation;

            if (MyAPIGateway.Session.Player.Controller.ControlledEntity == null)
                return;

            var position = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetPosition();
            MyAPIGateway.Utilities.ShowNotification(string.Format("[{0:N},{1:N},{2:N}]", position.X, position.Y, position.Z), 1000, MyFontEnum.White);
        }
    }
}
