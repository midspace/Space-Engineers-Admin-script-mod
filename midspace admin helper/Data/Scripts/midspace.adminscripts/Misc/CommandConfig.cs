using midspace.adminscripts.Messages;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace midspace.adminscripts
{
    public class CommandConfig : ChatCommand
    {

        public CommandConfig()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Client | ChatCommandFlag.MultiplayerOnly, "cfg", new string[] { "/config", "/cfg" })
        {
        }


        public override void Help(ulong steamId, bool brief)
        {
            var syntax = "/config <setting|action> [value]";
            if (brief)
                MyAPIGateway.Utilities.ShowMessage(syntax, "Changes the specified <setting> to the new specified <value> or executes the action. Only used in multiplayer. Type '/help cfg' for more details.");
            else
            {
                StringBuilder description = new StringBuilder();
                description.AppendLine(@"This command changes the specified <setting> of your server to the new specified <value>. This command can only be used in multiplayer.

Syntax:");
                description.AppendLine(syntax);
                description.AppendLine(@"
Available settings:
- 'motd' or 'MessageOfTheDay':
    Sets the content of the message of the day. Note that if you set the motd to an empty value it won't be displayed.

- 'motdsic' or 'MotdShowInChat:
    Determines if the message of the day is shown in chat or in a dialog. Only True or False is valid.

- 'motdhl' or 'MotdHeadLine':
    Sets the headline of the message of the day. Only shown in dialog.

- 'adminlevel'
    Sets the default level for admins to a certain level. Please note that levels that were already set won't be changed.

- 'ngi' or 'NoGrindIndestructible'
    Determines if it is allowed to grind ships that are set indestructible.

Available actions:
- 'save':
    Saves the config to the files.
- 'reload':
    Reloads the config from the files.
- 'show':
    Shows the config.

Examples:
- '/cfg motdhl Welcome on the server!' -> This will set the headline of the motd to 'Welcome on the server!'. Note that it won't be saved until the server shuts down even though it is active. If someone reloads the config before it is saved (manually or at shutdown) the headline will be changed back to that one in the config file.
- '/cfg reload' -> This will reload the config from the file and reverts any changes you made since the last save.");
                MyAPIGateway.Utilities.ShowMissionScreen("Admin Helper Commands", "Help : ", Name, description.ToString(), null, null);
            }
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"(/config|/cfg)\s+(?<Key>[^\s]+)((\s+(?<Value>.+))|)", RegexOptions.IgnoreCase); 
            if (match.Success)
            {
                var key = match.Groups["Key"].Value;
                var value = match.Groups["Value"].Value.Trim();

                switch (key.ToLowerInvariant())
                {
                    case "motd":
                    case "messageoftheday":
                        ConnectionHelper.SendMessageToServer(new MessageOfTheDayMessage() { Content = value , FieldsToUpdate = MessageOfTheDayMessage.ChangedFields.Content });
                        break;
                    case "motdheadline":
                    case "motdhl":
                        ConnectionHelper.SendMessageToServer(new MessageOfTheDayMessage() { HeadLine = value, FieldsToUpdate = MessageOfTheDayMessage.ChangedFields.HeadLine });
                        break;
                    case "motdsic":
                    case "motdshowinchat":
                        bool motdsic;
                        if (bool.TryParse(value, out motdsic))
                            ConnectionHelper.SendMessageToServer(new MessageOfTheDayMessage() { ShowInChat = motdsic, FieldsToUpdate = MessageOfTheDayMessage.ChangedFields.ShowInChat });
                        else
                            MyAPIGateway.Utilities.ShowMessage("Config", "{0} is an invalid argument for {1}. It must be eiter true or false.", new object[] { value, key });
                        break;
                    case "adminlevel":
                        uint adminLevel;
                        if (uint.TryParse(value, out adminLevel))
                            ConnectionHelper.SendMessageToServer(new MessageConfig() { Config = new ServerConfigurationStruct() { AdminLevel = adminLevel }, Action = ConfigAction.AdminLevel });
                        else
                            MyAPIGateway.Utilities.ShowMessage("Config", "{0} is an invalid argument for {1}. It must be an integer higher or equal to {2} and lower or equal to {3}.", new object[] { value, key, uint.MinValue, uint.MaxValue });
                        break;
                    case "nogrindindestructible":
                    case "ngi":
                        bool noGrindIndestructible;
                        if (bool.TryParse(value, out noGrindIndestructible))
                            ConnectionHelper.SendMessageToServer(new MessageConfig() { Config = new ServerConfigurationStruct() { NoGrindIndestructible = noGrindIndestructible }, Action = ConfigAction.NoGrindIndestructible });
                        else
                            MyAPIGateway.Utilities.ShowMessage("Config", "{0} is an invalid argument for {1}. It must be eiter true or false.", new object[] { value, key });
                        break;
                    case "save":
                        ConnectionHelper.SendMessageToServer(new MessageConfig() { Action = ConfigAction.Save });
                        break;
                    case "reload":
                        ConnectionHelper.SendMessageToServer(new MessageConfig() { Action = ConfigAction.Reload });
                        break;
                    case "show":
                        ConnectionHelper.SendMessageToServer(new MessageConfig() { Action = ConfigAction.Show });
                        break;
                    default:
                        MyAPIGateway.Utilities.ShowMessage("Config", "Invalid setting or action. Type '/help cfg' for help.");
                        MyAPIGateway.Utilities.ShowMessage("Available actions and settings:", "adminlevel, motd, motdhl, motdsic, ngi, save, show, reload");
                        break;
                }

                return true;
            }
            return false;
        }
    }
}
