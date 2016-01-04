namespace midspace.adminscripts
{
    using System;
    using System.Linq;

    using Sandbox.ModAPI;
    using midspace.adminscripts.Messages;

    public class CommandSessionSpiders : ChatCommand
    {
        public CommandSessionSpiders()
            : base(ChatCommandSecurity.Admin, "spiders", new[] { "/spiders", "/spider" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/spiders <on|off>", "Turns spiders on or off for all players.");

            // Allows you to turn spiders on or off.
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
                    ConnectionHelper.SendMessageToServer(new MessageSession { State = state.Value, Setting = SessionSetting.Spiders });
                    return true;
                }
                MyAPIGateway.Session.SessionSettings.EnableSpiders = state.Value;
            }

            // Display the current state.
            var currentState = MyAPIGateway.Session.GetCheckpoint("null").Settings.EnableSpiders.HasValue ? MyAPIGateway.Session.GetCheckpoint("null").Settings.EnableSpiders.Value : false;
            MyAPIGateway.Utilities.ShowMessage("Spiders", currentState ? "On" : "Off");
            return true;
        }
    }
}
