namespace midspace.adminscripts
{
    using System;

    using Sandbox.ModAPI;
    using System.Text.RegularExpressions;
    using System.Text;

    public class CommandSaveGame : ChatCommand
    {
        public CommandSaveGame()
            : base(ChatCommandSecurity.Admin, "save", new[] { "/save" })
        {
        }

        public override void Help(bool brief)
        {
            if (brief)
                MyAPIGateway.Utilities.ShowMessage("/save <option>", "Saves the active game to the local computer or saves the game on the server. Options: server, s, local, l.");
            else
            {
                StringBuilder description = new StringBuilder();
                description.Append(@"This command saves the active game to the local computer or saves the game on the server. For showing the time since the last save no additional parameter is needed.

Syntax:
/save <option>

Options:
server, s, local, l

server or s:
Saves the session on the server. It is forbidden to save the session on a locally hosted server unless you are the host.
Example: /save server

local or l:
Saves the active game to your computer.
Example: /save local
");
            }
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/save(\s+(?<Key>.+)|)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var setting = match.Groups["Key"].Value;
                if (!string.IsNullOrEmpty(setting))
                {
                    if (setting.Equals("local", StringComparison.InvariantCultureIgnoreCase) || setting.Equals("l", StringComparison.InvariantCultureIgnoreCase))
                    {
                        MyAPIGateway.Session.Save();
                        var msg = Localize.GetResource(Localize.WorldSaved, MyAPIGateway.Session.Name);
                        MyAPIGateway.Utilities.ShowNotification(msg, 2500, Sandbox.Common.MyFontEnum.White);
                    }
                    else if (setting.Equals("server", StringComparison.InvariantCultureIgnoreCase) || setting.Equals("s", StringComparison.InvariantCultureIgnoreCase))
                        ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.Save, "");
                    else
                        MyAPIGateway.Utilities.ShowMessage("Option", string.Format("{0} is no valid option. Options: server, s, local, l.", setting));
                }
                else
                    MyAPIGateway.Utilities.ShowMessage("Option", "You have to define an option. Options: server, s, local, l.");
                    //ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.SaveTime, "");

                return true;
            }
            return false;
        }
    }
}
