namespace midspace.adminscripts
{
    using System;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;

    public class CommandSessionCopyPaste : ChatCommand
    {
        public CommandSessionCopyPaste()
            : base(ChatCommandSecurity.Admin, "copypaste", new[] { "/copypaste" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/copypaste <on|off>", "Turns Copy Paste mode on or off for you.");

            // Requires GameMode to be changed to Creative first.
            // On Single player, these changes are permanent to you game.
            // On a Hosted game, anyone connecting after making a change will also inherit them.
            // On a dedicated server, you can copy and paste at will. Other players will not.
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.StartsWith("/copypaste ", StringComparison.InvariantCultureIgnoreCase))
            {
                var strings = messageText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (strings.Length > 1)
                {
                    if (strings[1].Equals("on", StringComparison.InvariantCultureIgnoreCase) || strings[1].Equals("1", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (MyAPIGateway.Multiplayer.MultiplayerActive)
                        {
                            ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.CopyPaste, bool.TrueString);
                            return true;
                        }
                        MyAPIGateway.Session.GetCheckpoint("null").EnableCopyPaste = true;
                        MyAPIGateway.Utilities.ShowMessage("CopyPaste", "On");
                        return true;
                    }

                    if (strings[1].Equals("off", StringComparison.InvariantCultureIgnoreCase) || strings[1].Equals("0", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (MyAPIGateway.Multiplayer.MultiplayerActive)
                        {
                            ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.CopyPaste, bool.FalseString);
                            return true;
                        }
                        MyAPIGateway.Session.GetCheckpoint("null").EnableCopyPaste = false;
                        MyAPIGateway.Utilities.ShowMessage("CopyPaste", "Off");
                        return true;
                    }
                }
            }

            MyAPIGateway.Utilities.ShowMessage("CopyPaste", MyAPIGateway.Session.GetCheckpoint("null").EnableCopyPaste ? "On" : "Off");
            return true;
        }
    }
}
