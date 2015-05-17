namespace midspace.adminscripts
{
    using System;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;

    /// <summary>
    /// I'm not sure there is much value in this command. It's not the same as "Save As". It changes the games name, and when you save it uses the new name.
    /// I need to test this under a dedicated server further.
    /// And test to see what happens if you use an existing game name.
    /// </summary>
    public class CommandGameName : ChatCommand
    {
        public CommandGameName()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Experimental, "gamename", new[] { "/gamename" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/gamename <name>", "Displays or changes the game name permanently to <name>.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.Equals("/gamename", StringComparison.InvariantCultureIgnoreCase))
            {
                MyAPIGateway.Utilities.ShowMessage("Game Name", MyAPIGateway.Session.Name);
                return true;
            }

            var match = Regex.Match(messageText, @"/gamename\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var gameName = match.Groups["Key"].Value.Trim();

                if (gameName.Length < 5)
                {
                    MyAPIGateway.Utilities.ShowMessage("Game Name", "is to short. Min 5 characters");
                    return true;
                }

                MyAPIGateway.Session.Name = gameName;
                MyAPIGateway.Utilities.ShowMessage("Game Name changed to", gameName);
                return true;
            }

            return false;
        }
    }
}
