namespace midspace.adminscripts
{
    using System;
    using System.Linq;

    using Sandbox.ModAPI;
    using midspace.adminscripts.Messages;

    public class CommandSessionCyberHounds : ChatCommand
    {
        public CommandSessionCyberHounds()
            : base(ChatCommandSecurity.Admin, "cyberhounds", new[] { "/cyberhounds", "/cyberhound", "/hounds", "/hound" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/cyberhounds <on|off>", "Turns cyberhounds on or off for all players.");

            // Allows you to turn cyberhounds on or off.
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
                    ConnectionHelper.SendMessageToServer(new MessageSession { State = state.Value, Setting = SessionSetting.Cyberhounds });
                    return true;
                }
                MyAPIGateway.Session.SessionSettings.EnableCyberhounds = state.Value;
            }

            // Display the current state.

            var currentState = MyAPIGateway.Session.GetCheckpoint("null").Settings.EnableCyberhounds.HasValue ? MyAPIGateway.Session.GetCheckpoint("null").Settings.EnableCyberhounds.Value : false;

            MyAPIGateway.Utilities.ShowMessage("Cyber Hounds", currentState ? "On" : "Off");
            return true;
        }
    }
}
