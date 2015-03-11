namespace midspace.adminscripts
{
    using System;

    using Sandbox.ModAPI;

    public class CommandSessionSpectator : ChatCommand
    {
        public CommandSessionSpectator()
            : base(ChatCommandSecurity.Experimental, "spectator", new[] { "/spectator" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/spectator <on|off>", "Turns creative mode on or off for you.");

            // Allows you to change the Spectator mode.

            // On Single player, these changes are permanent to you game.
            // On a Hosted game, anyone connecting after making a change will also inherit them.
            // On a dedicated server, you can:
            // * drop single blocks at any range, however they will only have 1% construction even though they appear 100% to you.
            // * remove any block instantly as per normal.
            // * drop a line or grid of blocks, these will be 100% constructed unlike single blocks.
            // * allows you to have copypaste.
        }

        public override bool Invoke(string messageText)
        {
            var strings = messageText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (strings.Length > 1)
            {
                if (strings[1].Equals("on", StringComparison.InvariantCultureIgnoreCase) || strings[1].Equals("1", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (MyAPIGateway.Multiplayer.MultiplayerActive)
                    {
                        ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.Spectator, bool.TrueString);
                        return true;
                    }
                    MyAPIGateway.Session.GetCheckpoint("null").Settings.EnableSpectator = true;
                    MyAPIGateway.Utilities.ShowMessage("Spectator", "On");
                    return true;
                }

                if (strings[1].Equals("off", StringComparison.InvariantCultureIgnoreCase) || strings[1].Equals("0", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (MyAPIGateway.Multiplayer.MultiplayerActive)
                    {
                        ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.Spectator, bool.FalseString);
                        return true;
                    }
                    MyAPIGateway.Session.GetCheckpoint("null").Settings.EnableSpectator = false;

                    // Still some issue with this, because if the player is in Spectator mode when this is turned off, they can't get out of it.
                    // Need some way of resetting the view back to the player before turning off.
                    // It may have something to do with the VRage.Common.MySpectator.Static, which it not whitelisted for use in the Mod API.
                    //VRage.Common.MySpectator.Static.ForceFirstPersonCamera = true;

                    MyAPIGateway.Utilities.ShowMessage("Spectator", "Off");
                    return true;
                }
            }

            MyAPIGateway.Utilities.ShowMessage("Spectator", MyAPIGateway.Session.GetCheckpoint("null").Settings.EnableSpectator ? "On" : "Off");
            return true;
        }
    }
}
