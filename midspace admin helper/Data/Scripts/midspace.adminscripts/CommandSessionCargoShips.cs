namespace midspace.adminscripts
{
    using System;
    using System.Linq;

    using Sandbox.ModAPI;

    public class CommandSessionCargoShips : ChatCommand
    {
        public CommandSessionCargoShips()
            : base(ChatCommandSecurity.Experimental, "cargoships", new[] { "/cargoships" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/CargoShips <on|off> [private]", "Turns spawning of Cargo ships and Exploration ships mode on or off.");

            // Turns on spawning of Cargo ships and Exploration ships.

            // On Single player, these changes are permanent to you game.
            // On a Hosted game, anyone connecting after making a change will also inherit them.
            // On a dedicated server it has no effect, as Cargo Ships are spawned by the server or host.
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
                    ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.CargoShips, state.Value.ToString());
                    return true;
                }
                MyAPIGateway.Session.GetCheckpoint("null").EnableCopyPaste = state.Value;
            }

            // Display the current state.
            MyAPIGateway.Utilities.ShowMessage("CargoShips", MyAPIGateway.Session.GetCheckpoint("null").CargoShipsEnabled ? "On" : "Off");
            return true;
        }
    }
}
