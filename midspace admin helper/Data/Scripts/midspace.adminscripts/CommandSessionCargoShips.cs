namespace midspace.adminscripts
{
    using System;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;

    public class CommandSessionCargoShips : ChatCommand
    {
        public CommandSessionCargoShips()
            : base(ChatCommandSecurity.Admin, "cargoships", new[] { "/cargoships" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/CargoShips <on|off>", "Turns spawning of Cargo ships and Exploration ships mode on or off.");

            // Turns on spawning of Cargo ships and Exploration ships.

            // On Single player, these changes are permanent to you game.
            // On a Hosted game, anyone connecting after making a change will also inherit them.
            // On a dedicated server it has no effect, as Cargo Ships are spawned by the server or host.
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.StartsWith("/cargoships ", StringComparison.InvariantCultureIgnoreCase))
            {
                var strings = messageText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (strings.Length > 1)
                {
                    if (strings[1].Equals("on", StringComparison.InvariantCultureIgnoreCase) || strings[1].Equals("1", StringComparison.InvariantCultureIgnoreCase))
                    {
                        MyAPIGateway.Session.GetCheckpoint("null").CargoShipsEnabled = true;
                        MyAPIGateway.Utilities.ShowMessage("CargoShips", "On");
                        return true;
                    }

                    if (strings[1].Equals("off", StringComparison.InvariantCultureIgnoreCase) || strings[1].Equals("0", StringComparison.InvariantCultureIgnoreCase))
                    {
                        MyAPIGateway.Session.GetCheckpoint("null").CargoShipsEnabled = false;
                        MyAPIGateway.Utilities.ShowMessage("CargoShips", "Off");
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
