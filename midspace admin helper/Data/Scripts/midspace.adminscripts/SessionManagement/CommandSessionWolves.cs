namespace midspace.adminscripts
{
    using midspace.adminscripts.Messages;
    using Sandbox.ModAPI;
    using System;
    using System.Linq;

    public class CommandSessionWolves : ChatCommand
    {
        public CommandSessionWolves()
            : base(ChatCommandSecurity.Admin, "wolves", new[] { "/wolves", "/wolf", "/wolfs" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/wolves <on|off>", "Turns wolves on or off for all players.");

            // Allows you to turn wolves on or off.
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var strings = messageText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            bool? state = null;

            if (strings.Contains("on", StringComparer.InvariantCultureIgnoreCase)
                    || strings.Contains("1", StringComparer.InvariantCultureIgnoreCase))
                state = true;

            if (strings.Contains("off", StringComparer.InvariantCultureIgnoreCase)
                || strings.Contains("0", StringComparer.InvariantCultureIgnoreCase))
                state = false;

            if (state.HasValue)
            {
                if (MyAPIGateway.Multiplayer.MultiplayerActive)
                {
                    ConnectionHelper.SendMessageToServer(new MessageSession { State = state.Value, Setting = SessionSetting.Wolves });
                    return true;
                }
                MyAPIGateway.Session.SessionSettings.EnableWolfs = state.Value;
            }

            // Display the current state.

            var checkpoint = MyAPIGateway.Session.GetCheckpoint("null");
            bool currentState = checkpoint.Settings.EnableWolfs;

            MyAPIGateway.Utilities.ShowMessage("Cyber Hounds", currentState ? "On" : "Off");
            return true;
        }
    }
}
