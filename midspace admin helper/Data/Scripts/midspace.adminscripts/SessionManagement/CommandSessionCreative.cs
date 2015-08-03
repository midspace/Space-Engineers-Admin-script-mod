namespace midspace.adminscripts
{
    using System;
    using System.Linq;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using midspace.adminscripts.Messages;

    public class CommandSessionCreative : ChatCommand
    {
        public CommandSessionCreative()
            : base(ChatCommandSecurity.Admin, "creative", new[] { "/creative" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/creative <on|off> [private]", "Turns creative mode on or off for all players. Add the word \"private\" for you only.");

            // Allows you to change the game mode to Creative.

            // On Single player, these changes are permanent to you game.
            // On a Hosted game, anyone connecting after making a change will also inherit them.
            // On a dedicated server, you can:
            // * drop single blocks at any range, however they will only have 1% construction even though they appear 100% to you.
            // * remove any block instantly as per normal.
            // * drop a line or grid of blocks, these will be 100% constructed unlike single blocks.
            // * allows you to have copypaste.

            // Note, that in the Join Game Screen, which is the Game Lobby, the game mode name will not change, 
            // as this is part of the Steam Game registration when the server is started.
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
                    ConnectionHelper.SendMessageToServer(new MessageSession() { State = state.Value, Setting = SessionSetting.Creative });
                    return true;
                }
                if (MyAPIGateway.Multiplayer.MultiplayerActive)
                    MyAPIGateway.Utilities.ShowMessage("Error", "Cannot change Creative state as Private.");
                else
                    MyAPIGateway.Session.SessionSettings.GameMode = state.Value ? MyGameModeEnum.Creative : MyGameModeEnum.Survival;
            }

            // Display the current state.
            MyAPIGateway.Utilities.ShowMessage("Creative", MyAPIGateway.Session.GetCheckpoint("null").GameMode == MyGameModeEnum.Creative ? "On" : "Off");
            return true;
        }
    }
}
