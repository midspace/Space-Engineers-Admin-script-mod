namespace midspace.adminscripts
{
    using System;
    using System.Linq;

    using Sandbox.ModAPI;

    public class CommandSessionSpectator : ChatCommand
    {
        public CommandSessionSpectator()
            : base(ChatCommandSecurity.Admin, "spectator", new[] { "/spectator" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/spectator <on|off> [private]", "Turns spectator mode on or off for all players. Add the word \"private\" for you only.");

            // Allows you to change the Spectator mode.

            // On Single player, these changes are permanent to you game.
            // On a Hosted game, anyone connecting after making a change will also inherit them.
        }

        public override bool Invoke(string messageText)
        {
            var strings = messageText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var priv = strings.Contains("private", StringComparer.InvariantCultureIgnoreCase);
            bool? state = null;

            if (strings.Contains("on", StringComparer.InvariantCultureIgnoreCase)
                    || strings.Contains("1", StringComparer.InvariantCultureIgnoreCase))
                state = true;

            if (strings.Contains("off", StringComparer.InvariantCultureIgnoreCase)
                || strings.Contains("0", StringComparer.InvariantCultureIgnoreCase))
                state = false;

            if (state.HasValue)
            {
                if (!priv && MyAPIGateway.Multiplayer.MultiplayerActive)
                {
                    ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.Spectator, state.Value.ToString());
                    return true;
                }
                MyAPIGateway.Session.SessionSettings.EnableSpectator = state.Value;
            }

            // Display the current state.
            MyAPIGateway.Utilities.ShowMessage("Spectator", MyAPIGateway.Session.GetCheckpoint("null").Settings.EnableSpectator ? "On" : "Off");
            return true;
        }
    }
}
