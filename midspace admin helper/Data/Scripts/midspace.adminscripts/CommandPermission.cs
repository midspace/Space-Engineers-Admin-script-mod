using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace midspace.adminscripts
{
    class CommandPermission : ChatCommand
    {
        public CommandPermission()
            : base(ChatCommandSecurity.Admin, "perm", new string[] { "/permission", "/perm" })
        {

        }

        public override void Help(bool brief)
        {
            string syntax = "/perm <domain> <action> [<subject> [<parameter>]]";
            if (brief)
                MyAPIGateway.Utilities.ShowMessage(syntax, "This command is used for organizing the permissions ingame.");
            else
            {
                StringBuilder description = new StringBuilder();

                description.Append(string.Format(@"This command is used for organizing the permissions ingame.

Syntax:
{1}


Domains:

command, player, group

Each domain has specific actions. See below:


Actions:

- For 'command':
    setlevel:
Sets the needed level for a command to the specified level.
Example: /perm command setlevel help 100

    list (not implemented yet):
Creates a hotlist containing all commands and provides information about the needed level of the commands.


- For 'player':
    setlevel:
Sets the level of a player to the specified value. Remind that you have to set useplayerlevel to true otherwise using this won't make any difference.
Example: /perm player setlevel {0} 150

    extend:
Grants the specified player to use the specified command regardless of his level. When a player has extended rights use 'restrict' to restore normal permissions. When a player has restricted rights you can use this command to restore normal permissions.
Example: /perm player extend {0} tp
-> If the player had normal access to the command, he will be able to use it from now regardless of his level. If the player had restricted access to the command he will have normal access from now.

    restrict:
Prevents the specified player from using the specified command regardless of his level. When a player has restricted rights use 'extend' to restore normal permissions. When a player has extended rights you can use this command to restore normal permissions. 
Example: /perm player restrict {0} tp
-> If the player had normal access to the command, he won't be able to use it any longer regardless of his level. If the player had extended access to the command he will have normal access from now.

    useplayerlevel:
If set to true the level of the player will be used. By default it is false.
Example: /perm player useplayerlevel {0} true

    list (not implemented yet):
Creates a hotlist containing all players and provides information about them.


- For 'group':
    setlevel:
Sets the level of a group to the specified value.
Example: /perm group setlevel mygroup 150

    setname:
Sets the name of a group to the specified value.
Example: /perm group setname mygroup mynewgroupname

    add:
Adds the specified player to the specified group.
Example: /perm group add mygroup {0}

    remove:
Removes the specified player from the specified group.
Example: /perm group remove mygroup {0}

    create:
Creates a new group with a specified name and a specified level
Example: /perm group create mygroup 150

    delete:
Deletes the specified group.
Example: /perm group delete mygroup

    list (not implemented yet):
Creates a hotlist containing all groups and provides information about them.
", MyAPIGateway.Session.Player.DisplayName, syntax));

                MyAPIGateway.Utilities.ShowMissionScreen("Help", null, Name, description.ToString(), null, null);
            }
        }

        public override bool Invoke(string messageText)
        {
            if (!MyAPIGateway.Multiplayer.MultiplayerActive)
            {
                MyAPIGateway.Utilities.ShowMessage("Permissions", "Command disabled in offline mode.");
                return true;
            }

            var match = Regex.Match(messageText, @"/(perm|permission)\s+(?<CommandParts>.*)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var commandParts = match.Groups["CommandParts"].Value.Split(' ');

                //not enough parameters, return false to show brief help
                if (commandParts.Length < 2)
                    return false;

                switch (commandParts[0].ToLowerInvariant())
                {
                    case "command":
                    case "commands":
                        ProcessCommandPermission(commandParts);
                        break;
                    case "player":
                    case "players":
                        ProcessPlayerPermission(commandParts);
                        break;
                    case "group":
                    case "groups":
                        ProcessGroupPermission(commandParts);
                        break;
                    default:
                        MyAPIGateway.Utilities.ShowMessage("Permissions", string.Format("There is no domain named {0}. Available domains: command, player, group.", commandParts[0]));
                        break;
                }
                //if there is a mistake, we already informed the player about it
                return true;
            }

            return false;
        }

        public void ProcessCommandPermission(string[] args)
        {
            switch (args[1].ToLowerInvariant())
            {
                case "setlevel":
                    if (args.Length < 4)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", "Not enough arguments.");
                        MyAPIGateway.Utilities.ShowMessage("Command setlevel", "/perm command setlevel <commandName> <level>");
                        return;
                    }

                    uint level;
                    if (uint.TryParse(args[3], out level))
                    {
                        var dict = new Dictionary<string, string>();
                        dict.Add(args[2], args[3]);
                        ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.CommandLevel, ConnectionHelper.ConvertData(dict));
                    }
                    else
                        MyAPIGateway.Utilities.ShowMessage("Permissions", string.Format("{0} is no valid level. It must be an integer and can't be below 0.", args[3]));
                    break;
                case "list":
                    MyAPIGateway.Utilities.ShowMessage("Error", "Not implemented yet.");
                    break;
                default:
                    MyAPIGateway.Utilities.ShowMessage("Permissions", string.Format("There is no action named {0}. Available actions: setlevel, list.", args[1]));
                    break;
            }
        }

        public void ProcessPlayerPermission(string[] args)
        {
            var dict = new Dictionary<string, string>();
            switch (args[1].ToLowerInvariant())
            {
                case "setlevel":
                    if (args.Length < 4)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", "Not enough arguments.");
                        MyAPIGateway.Utilities.ShowMessage("Player setlevel", "/perm player setlevel <playerName> <level>");
                        return;
                    }

                    uint level;
                    if (uint.TryParse(args[3], out level))
                    {
                        dict.Add(args[2], args[3]);
                        ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.PlayerLevel, ConnectionHelper.ConvertData(dict));
                    }
                    else
                        MyAPIGateway.Utilities.ShowMessage("Permissions", string.Format("{0} is no valid level. It must be an integer and can't be below 0.", args[3]));
                    break;
                case "extend":
                    if (args.Length < 4)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", "Not enough arguments.");
                        MyAPIGateway.Utilities.ShowMessage("Player extend", "/perm player extend <playerName> <commandName>");
                        Help(true);
                        return;
                    }

                    if (!ChatCommandService.IsCommandRegistered(args[3]))
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", string.Format("There is no command named {0}.", args[3]));
                        return;
                    }
                    
                    dict.Add(args[2], args[3]);
                    ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.PlayerExtend, ConnectionHelper.ConvertData(dict));
                    break;
                case "restrict":
                    if (args.Length < 4)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", "Not enough arguments.");
                        MyAPIGateway.Utilities.ShowMessage("Player restrict", "/perm player restrict <playerName> <commandName>");
                        return;
                    }

                    if (!ChatCommandService.IsCommandRegistered(args[3]))
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", string.Format("There is no command named {0}.", args[3]));
                        return;
                    }

                    dict.Add(args[2], args[3]);
                    ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.PlayerRestrict, ConnectionHelper.ConvertData(dict));
                    break;
                case "upl":
                case "useplayerlevel":
                    if (args.Length < 4)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", "Not enough arguments.");
                        MyAPIGateway.Utilities.ShowMessage("Player useplayerlevel", "/perm player upl <playerName> <true|false>");
                        Help(true);
                        return;
                    }

                    bool usePlayerLevel;
                    if (bool.TryParse(args[3], out usePlayerLevel))
                    {
                        dict.Add(args[2], args[3]);
                        ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.UsePlayerLevel, ConnectionHelper.ConvertData(dict));
                    }
                    else
                        MyAPIGateway.Utilities.ShowMessage("Permissions", string.Format("{0} is no valid value. It must be either true or false.", args[3]));
                    break;
                case "list":
                    MyAPIGateway.Utilities.ShowMessage("Error", "Not implemented yet.");
                    break;
                default:
                    MyAPIGateway.Utilities.ShowMessage("Permissions", string.Format("There is no action named {0}. Available actions: setlevel, extend, restrict, useplayerlevel, list.", args[1]));
                    break;
            }
        }

        public void ProcessGroupPermission(string[] args)
        {
            var dict = new Dictionary<string, string>();
            switch (args[1].ToLowerInvariant())
            {
                case "setlevel":
                    if (args.Length < 4)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", "Not enough arguments.");
                        MyAPIGateway.Utilities.ShowMessage("Group setlevel", "/perm group setlevel <groupName> <level>");
                        Help(true);
                        return;
                    }

                    uint level;
                    if (uint.TryParse(args[3], out level))
                    {
                        dict.Add(args[2], args[3]);
                        ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.GroupLevel, ConnectionHelper.ConvertData(dict));
                    }
                    else
                        MyAPIGateway.Utilities.ShowMessage("Permissions", string.Format("{0} is no valid level. It must be an integer and can't be below 0.", args[3]));
                    break;
                case "setname":
                case "rename":
                    if (args.Length < 4)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", "Not enough arguments.");
                        MyAPIGateway.Utilities.ShowMessage("Group setname", "/perm group setname <groupName> <newGroupName>");
                        return;
                    }
                    
                    dict.Add(args[2], args[3]);
                    ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.GroupName, ConnectionHelper.ConvertData(dict));
                    break;
                case "add":
                case "addplayer":
                    if (args.Length < 4)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", "Not enough arguments.");
                        MyAPIGateway.Utilities.ShowMessage("Group addplayer", "/perm group add <groupName> <playerName>");
                        return;
                    }

                    dict.Add(args[2], args[3]);
                    ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.GroupAddPlayer, ConnectionHelper.ConvertData(dict));
                    break;
                case "remove":
                case "removeplayer":
                    if (args.Length < 4)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", "Not enough arguments.");
                        MyAPIGateway.Utilities.ShowMessage("Group removeplayer", "/perm group remove <groupName> <playerName>");
                        return;
                    }
                    
                    dict.Add(args[2], args[3]);
                    ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.GroupRemovePlayer, ConnectionHelper.ConvertData(dict));
                    break;
                case "create":
                    if (args.Length < 4)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", "Not enough arguments.");
                        MyAPIGateway.Utilities.ShowMessage("Group create", "/perm group create <groupName> <level>");
                        return;
                    }

                    if (uint.TryParse(args[3], out level))
                    {
                        dict.Add(args[2], args[3]);
                        ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.GroupCreate, ConnectionHelper.ConvertData(dict));
                    }
                    else
                        MyAPIGateway.Utilities.ShowMessage("Permissions", string.Format("{0} is no valid level. It must be an integer and can't be below 0.", args[3]));
                    break;
                case "delete":
                    if (args.Length < 3)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", "Not enough arguments.");
                        MyAPIGateway.Utilities.ShowMessage("Group delete", "/perm group create <groupName>");
                        return;
                    }

                    ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.GroupDelete, args[2]);
                    break;
                case "list":
                    MyAPIGateway.Utilities.ShowMessage("Error", "Not implemented yet.");
                    break;
                default:
                    MyAPIGateway.Utilities.ShowMessage("Permissions", string.Format("There is no action named {0}. Available actions: setlevel, setname, add, remove, create, delete, list.", args[1]));
                    break;
            }
        }
    }
}
