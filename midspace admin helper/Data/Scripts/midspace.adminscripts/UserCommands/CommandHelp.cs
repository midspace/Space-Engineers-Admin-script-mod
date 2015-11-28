namespace midspace.adminscripts
{
    using System;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using System.Collections.Generic;

    public class CommandHelp : ChatCommand
    {
        public CommandHelp()
            : base(ChatCommandSecurity.User, "help", new[] { "/help", "/?" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/help <name>", "Displays help on the specified command <name>.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var brief = messageText.StartsWith("/?", StringComparison.InvariantCultureIgnoreCase);

            var match = Regex.Match(messageText, @"(/help|/?)\s{1,}(?<Key>[^\s]+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var ret = ChatCommandService.Help(steamId, match.Groups["Key"].Value, brief);
                if (!ret)
                    MyAPIGateway.Utilities.ShowMessage("help", "could not find specified command.");
                return true;
            }

            if (ChatCommandService.UserSecurity == ChatCommandSecurity.User)
            {
                // Split help details. Regular users, get one list.
                var commands = new List<string>(ChatCommandService.GetListCommands(steamId));
                commands.Sort();

                if (brief)
                    MyAPIGateway.Utilities.ShowMessage("help", string.Join(", ", commands));
                else
                    MyAPIGateway.Utilities.ShowMissionScreen("Admin Helper Commands", "Help : Available commands", " ", "Commands: " + string.Join(", ", commands), null, "OK");
            }
            else
            {
                // Split help details. Admins users, get two lists.
                var commands = new List<string>(ChatCommandService.GetUserListCommands(steamId));
                commands.Sort();

                var nonUserCommands = new List<string>(ChatCommandService.GetNonUserListCommands(steamId));
                nonUserCommands.Sort();

                if (brief)
                {
                    MyAPIGateway.Utilities.ShowMessage("user help", string.Join(", ", commands));
                    MyAPIGateway.Utilities.ShowMessage("help", string.Join(", ", nonUserCommands));
                }
                else
                {
                    MyAPIGateway.Utilities.ShowMissionScreen("Admin Helper Commands", "Help : Available commands", " ",
                        string.Format("User commands:\r\n{0}\r\n\r\nAdmin commands:\r\n{1}", string.Join(", ", commands), string.Join(", ", nonUserCommands))
                        , null, "OK");
                }
            }

            return true;
        }
    }
}
