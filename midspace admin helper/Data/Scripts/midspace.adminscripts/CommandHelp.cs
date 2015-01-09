namespace midspace.adminscripts
{
    using System;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;

    public class CommandHelp : ChatCommand
    {
        public CommandHelp()
            : base(ChatCommandSecurity.User, "help", new[] { "/help" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/help <name>", "Displays help on the specified command <name>.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.StartsWith("/help", StringComparison.InvariantCultureIgnoreCase))
            {
                var match = Regex.Match(messageText, @"/help\s{1,}(?<Key>[^\s]+)", RegexOptions.IgnoreCase);

                if (!match.Success)
                {
                    if (ChatCommandService.UserSecurity == ChatCommandSecurity.User)
                    {
                        // Split help details. Regular users, get one list.
                        var commands = ChatCommandService.GetListCommands();
                        MyAPIGateway.Utilities.ShowMessage("help", String.Join(", ", commands));
                    }
                    else
                    {
                        // Split help details. Admins users, get two lists.
                        var commands = ChatCommandService.GetUserListCommands();
                        MyAPIGateway.Utilities.ShowMessage("user help", String.Join(", ", commands));
                        commands = ChatCommandService.GetNonUserListCommands();
                        MyAPIGateway.Utilities.ShowMessage("help", String.Join(", ", commands));
                    }
                    return true;
                }
                else
                {
                    return ChatCommandService.Help(match.Groups["Key"].Value);
                }
            }

            return false;
        }
    }
}
