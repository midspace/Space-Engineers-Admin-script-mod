using Sandbox.Common;

namespace midspace.adminscripts
{
    using System;

    using Sandbox.ModAPI;
    using System.Text.RegularExpressions;
    using System.Text;
    using midspace.adminscripts.Messages;

    public class CommandSaveGame : ChatCommand
    {
        public CommandSaveGame()
            : base(ChatCommandSecurity.Admin, "save", new[] { "/save" })
        {
        }

        public override void Help(bool brief)
        {
            if (brief)
                MyAPIGateway.Utilities.ShowMessage("/save <option> [customSaveName]", "Saves the active game to the local computer or saves the game on the server. Options: server, s, local, l.");
            else
            {
                StringBuilder description = new StringBuilder();
                description.Append(@"This command saves the active game to the local computer or saves the game on the server. For showing the time since the last save no additional parameter is needed.

Syntax:
/save <option> [customSaveName]

Options:
server, s, local, l

server or s:
Saves the session on the server. It is forbidden to save the session on a locally hosted server unless you are the host.
Example: /save server

local or l:
Saves the active game to your computer.
Example: /save local

Optionally you can save the world as [customSaveName]. Please note that the world might not appear in the 'Load World' screen if saved locally.
");
            }
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/save(\s+(?<Key>[^\s]+)\s+(?<CustomName>.*))|(\s+(?<Key>.+)|)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var setting = match.Groups["Key"].Value;
                var customName = match.Groups["CustomName"].Value;

                bool hasCustomName = !string.IsNullOrEmpty(customName);

                if (!string.IsNullOrEmpty(setting))
                {
                    if (setting.Equals("local", StringComparison.InvariantCultureIgnoreCase) || setting.Equals("l", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var msg = "";
                        if (hasCustomName)
                        {
                            MyAPIGateway.Session.Save(customName);
                            msg = String.Format("World saved as {0}", customName);
                        }
                        else
                        {
                            MyAPIGateway.Session.Save();
                             msg = Localize.GetResource(Localize.WorldSaved, MyAPIGateway.Session.Name);
                        }

                        MyAPIGateway.Utilities.ShowNotification(msg, 2500);
                    }
                    else if (setting.Equals("server", StringComparison.InvariantCultureIgnoreCase) || setting.Equals("s", StringComparison.InvariantCultureIgnoreCase))
                        ConnectionHelper.SendMessageToServer(new MessageSave() { Name = hasCustomName ? customName : "" });
                    else
                        MyAPIGateway.Utilities.ShowMessage("Option", string.Format("{0} is no valid option. Options: server, s, local, l.", setting));
                }
                else
                    MyAPIGateway.Utilities.ShowMessage("Option", "You have to define an option. Options: server, s, local, l.");

                return true;
            }
            return false;
        }
    }
}
