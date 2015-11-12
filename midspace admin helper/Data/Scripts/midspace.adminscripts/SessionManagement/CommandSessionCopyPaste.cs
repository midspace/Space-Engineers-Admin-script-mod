namespace midspace.adminscripts
{
    using System;
    using System.Linq;

    using Sandbox.ModAPI;
    using midspace.adminscripts.Messages;

    public class CommandSessionCopyPaste : ChatCommand
    {
        public CommandSessionCopyPaste()
            : base(ChatCommandSecurity.Admin, "copypaste", new[] { "/copypaste" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/copypaste <on|off> [private]", "Turns Copy Paste mode on or off for all players. Add the word \"private\" for you only.");

            // Requires GameMode to be changed to Creative first.
            // On Single player, these changes are permanent to you game.
            // On a Hosted game, anyone connecting after making a change will also inherit them.
            // On a dedicated server, you can copy and paste at will. Other players will not.
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
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
                    ConnectionHelper.SendMessageToServer(new MessageSession() { State = state.Value, Setting = SessionSetting.CopyPaste });
                    return true;
                }
                MyAPIGateway.Session.SessionSettings.EnableCopyPaste = state.Value;
            }

            // Display the current state.
            MyAPIGateway.Utilities.ShowMessage("CopyPaste", MyAPIGateway.Session.GetCheckpoint("null").EnableCopyPaste ? "On" : "Off");
            return true;
        }
    }
}
