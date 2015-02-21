namespace midspace.adminscripts
{
    using Sandbox.ModAPI;
    using System;
    using System.Text.RegularExpressions;

    public class CommandPardon : ChatCommand
    {
        public CommandPardon()
            : base(ChatCommandSecurity.Admin, "pardon", new string[] { "/pardon" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/pardon <#>", "Pardons the specified player <#> if he has been forcebanned.");
        }

        public override bool Invoke(string messageText)
        {
            if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                return false;

            var match = Regex.Match(messageText, @"/pardon\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var playerName = match.Groups["Key"].Value;
                ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.Pardon, playerName);
                MyAPIGateway.Utilities.ShowMessage("Pardoning", playerName);
                return true;
            }

            MyAPIGateway.Utilities.ShowMessage("Pardoning", "Please supply name to pardon from Ban.");
            return true;
        }
    }
}
