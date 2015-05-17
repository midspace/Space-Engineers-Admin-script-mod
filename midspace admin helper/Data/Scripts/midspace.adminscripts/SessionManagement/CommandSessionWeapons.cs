namespace midspace.adminscripts
{
    using System;
    using System.Linq;

    using Sandbox.ModAPI;

    public class CommandSessionWeapons : ChatCommand
    {
        public CommandSessionWeapons()
            : base(ChatCommandSecurity.Admin, "weapons", new[] { "/weapons" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/weapons <on|off> [private]", "Turns weapons on or off for all players. Add the word \"private\" for you only.");

            // Allows you to turn weapons on or off.

            // Will have adverse affects if only one player is turned on or off.
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
                    ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.Weapons, state.Value.ToString());
                    return true;
                }
                MyAPIGateway.Session.GetCheckpoint("null").WeaponsEnabled = state.Value;
            }

            // Display the current state.
            MyAPIGateway.Utilities.ShowMessage("Weapons", MyAPIGateway.Session.GetCheckpoint("null").WeaponsEnabled ? "On" : "Off");
            return true;
        }
    }
}
