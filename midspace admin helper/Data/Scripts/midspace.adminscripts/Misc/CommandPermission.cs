using midspace.adminscripts.Messages.Permissions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using midspace.adminscripts.Config;

namespace midspace.adminscripts
{
    class CommandPermission : ChatCommand
    {
        public CommandPermission()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Client | ChatCommandFlag.MultiplayerOnly, "perm", new string[] { "/permission", "/perm" })
        {
        }

        public override void Help(ulong steamId, bool brief)
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

Example to remove access:
/perm command setlevel motd none

    list:
Creates a hotlist containing all commands and provides information about the needed level of the commands. Use a keyword to refine your search
Example: /perm command list roid


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

    list:
Creates a hotlist containing all players and provides information about them. Use a keyword to refine your search
Example: /perm player list


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

    list:
Creates a hotlist containing all groups and provides information about them. Use a keyword to refine your search
Example: /perm group list
", MyAPIGateway.Session.Player.DisplayName, syntax));

                MyAPIGateway.Utilities.ShowMissionScreen("Admin Helper Commands", "Help : ", Name, description.ToString(), null, null);
            }
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/(perm|permission)\s+(?<CommandParts>.*)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var commandParts = match.Groups["CommandParts"].Value.SplitOnQuotes();

                //not enough parameters, return false to show brief help
                if (commandParts.Length < 2)
                    return false;

                switch (commandParts[0].ToLowerInvariant())
                {
                    case "command":
                    case "commands":
                        ProcessCommandPermission(steamId, commandParts);
                        break;
                    case "player":
                    case "players":
                        ProcessPlayerPermission(steamId, commandParts);
                        break;
                    case "group":
                    case "groups":
                        ProcessGroupPermission(steamId, commandParts);
                        break;
                    default:
                        MyAPIGateway.Utilities.ShowMessage("Permissions", $"There is no domain named {commandParts[0]}. Available domains: command, player, group.");
                        break;
                }
                //if there is a mistake, we already informed the player about it
                return true;
            }

            return false;
        }

        public void ProcessCommandPermission(ulong steamId, string[] args)
        {
            var commandMessage = new MessageCommandPermission();
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
                        commandMessage.Commands = new List<CommandStruct>();
                        commandMessage.CommandAction = CommandActions.Level;

                        commandMessage.Commands.Add(new CommandStruct()
                        {
                            Name = args[2],
                            NeededLevel = level
                        });
                    }
                    else if (string.Equals(args[3], "none", StringComparison.InvariantCultureIgnoreCase))
                    {
                        commandMessage.Commands = new List<CommandStruct>();
                        commandMessage.CommandAction = CommandActions.Level;

                        commandMessage.Commands.Add(new CommandStruct()
                        {
                            Name = args[2],
                            NeededLevel = uint.MaxValue
                        });
                    }
                    else
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", $"{args[3]} is no valid level. It must be an integer and can't be below 0.");
                        return;
                    }
                    break;
                case "list":
                    commandMessage.CommandAction = CommandActions.List;
                    if (args.Length > 2)
                        commandMessage.ListParameter = args[2];
                    break;
                default:
                    MyAPIGateway.Utilities.ShowMessage("Permissions", $"There is no action named {args[1]}. Available actions: setlevel, list.");
                    return;
            }

            ConnectionHelper.SendMessageToServer(commandMessage);
        }

        public void ProcessPlayerPermission(ulong steamId, string[] args)
        {
            var playerMessage = new MessagePlayerPermission();
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
                        playerMessage.Action = PlayerPermissionAction.Level;
                        playerMessage.PlayerName = args[2];
                        playerMessage.PlayerLevel = level;
                    }
                    else
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", $"{args[3]} is no valid level. It must be an integer and can't be below 0.");
                        return;
                    }
                    break;
                case "extend":
                    if (args.Length < 4)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", "Not enough arguments.");
                        MyAPIGateway.Utilities.ShowMessage("Player extend", "/perm player extend <playerName> <commandName>");
                        Help(steamId, true);
                        return;
                    }

                    playerMessage.Action = PlayerPermissionAction.Extend;
                    playerMessage.PlayerName = args[2];
                    playerMessage.CommandName = args[3];
                    break;
                case "restrict":
                    if (args.Length < 4)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", "Not enough arguments.");
                        MyAPIGateway.Utilities.ShowMessage("Player restrict", "/perm player restrict <playerName> <commandName>");
                        return;
                    }

                    playerMessage.Action = PlayerPermissionAction.Restrict;
                    playerMessage.PlayerName = args[2];
                    playerMessage.CommandName = args[3];
                    break;
                case "upl":
                case "useplayerlevel":
                    if (args.Length < 4)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", "Not enough arguments.");
                        MyAPIGateway.Utilities.ShowMessage("Player useplayerlevel", "/perm player upl <playerName> <true|false>");
                        Help(steamId, true);
                        return;
                    }

                    bool usePlayerLevel;
                    if (bool.TryParse(args[3], out usePlayerLevel))
                    {

                        playerMessage.Action = PlayerPermissionAction.UsePlayerLevel;
                        playerMessage.PlayerName = args[2];
                        playerMessage.UsePlayerLevel = usePlayerLevel;
                    }
                    else
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", $"{args[3]} is no valid value. It must be either true or false.");
                        return;
                    }
                    break;
                case "list":
                    string param = "";
                    if (args.Length > 2)
                        param = args[2];

                    playerMessage.Action = PlayerPermissionAction.List;
                    playerMessage.PlayerName = param;
                    break;
                default:
                    MyAPIGateway.Utilities.ShowMessage("Permissions", $"There is no action named {args[1]}. Available actions: setlevel, extend, restrict, useplayerlevel, list.");
                    return;
            }

            ConnectionHelper.SendMessageToServer(playerMessage);
        }

        public void ProcessGroupPermission(ulong steamId, string[] args)
        {
            var groupMessage = new MessageGroupPermission();
            switch (args[1].ToLowerInvariant())
            {
                case "setlevel":
                    if (args.Length < 4)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", "Not enough arguments.");
                        MyAPIGateway.Utilities.ShowMessage("Group setlevel", "/perm group setlevel <groupName> <level>");
                        Help(steamId, true);
                        return;
                    }

                    uint level;
                    if (uint.TryParse(args[3], out level))
                    {
                        groupMessage.Action = PermissionGroupAction.Level;
                        groupMessage.GroupName = args[2];
                        groupMessage.GroupLevel = level;
                    }
                    else
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", $"{args[3]} is no valid level. It must be an integer and can't be below 0.");
                        return;
                    }
                    break;
                case "setname":
                case "rename":
                    if (args.Length < 4)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", "Not enough arguments.");
                        MyAPIGateway.Utilities.ShowMessage("Group setname", "/perm group setname <groupName> <newGroupName>");
                        return;
                    }

                    groupMessage.Action = PermissionGroupAction.Name;
                    groupMessage.GroupName = args[2];
                    groupMessage.Name = args[3];
                    break;
                case "add":
                case "addplayer":
                    if (args.Length < 4)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", "Not enough arguments.");
                        MyAPIGateway.Utilities.ShowMessage("Group addplayer", "/perm group add <groupName> <playerName>");
                        return;
                    }

                    groupMessage.Action = PermissionGroupAction.Add;
                    groupMessage.GroupName = args[2];
                    groupMessage.Name = args[3];
                    break;
                case "remove":
                case "removeplayer":
                    if (args.Length < 4)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", "Not enough arguments.");
                        MyAPIGateway.Utilities.ShowMessage("Group removeplayer", "/perm group remove <groupName> <playerName>");
                        return;
                    }

                    groupMessage.Action = PermissionGroupAction.Remove;
                    groupMessage.GroupName = args[2];
                    groupMessage.Name = args[3];
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
                        groupMessage.Action = PermissionGroupAction.Create;
                        groupMessage.GroupName = args[2];
                        groupMessage.GroupLevel = level;
                    }
                    else
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", $"{args[3]} is no valid level. It must be an integer and can't be below 0.");
                        return;
                    }
                    break;
                case "delete":
                    if (args.Length < 3)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Permissions", "Not enough arguments.");
                        MyAPIGateway.Utilities.ShowMessage("Group delete", "/perm group create <groupName>");
                        return;
                    }

                    groupMessage.Action = PermissionGroupAction.Delete;
                    groupMessage.GroupName = args[2];
                    break;
                case "list":
                    string param = "";
                    if (args.Length > 2)
                        param = args[2];

                    groupMessage.Action = PermissionGroupAction.List;
                    groupMessage.GroupName = param;
                    break;
                default:
                    MyAPIGateway.Utilities.ShowMessage("Permissions", $"There is no action named {args[1]}. Available actions: setlevel, setname, add, remove, create, delete, list.");
                    return;
            }

            ConnectionHelper.SendMessageToServer(groupMessage);
        }

        public static void ShowCommandList(List<CommandStruct> commands)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($@"{commands.Count} results found:");

            int index = 0;
            foreach (CommandStruct command in commands)
            {
                builder.AppendFormat(@"
#{0} {1}
Level: {2}
", ++index, command.Name, command.NeededLevel);
            }

            MyAPIGateway.Utilities.ShowMissionScreen("Commands", "Command hotlist", null, builder.ToString(), null, null);
        }

        public static void ShowPlayerList(List<PlayerPermission> players)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($@"{players.Count} results found:");

            int index = 0;
            foreach (PlayerPermission playerPermission in players)
            {
                string playerLevelString = "";
                if (playerPermission.UsePlayerLevel)
                    playerLevelString = "(player level)";

                var extentions = string.Join(", ", playerPermission.Extensions);
                var restrictions = string.Join(", ", playerPermission.Restrictions);

                builder.AppendFormat(@"
#{0} {1}, {6}
Level: {2} {5}
Extentions: {3}
Restrictions: {4}
", ++index, playerPermission.Player.PlayerName, playerPermission.Level, string.IsNullOrEmpty(extentions) ? "none" : extentions, string.IsNullOrEmpty(restrictions) ? "none" : restrictions, playerLevelString, playerPermission.Player.SteamId);
            }

            MyAPIGateway.Utilities.ShowMissionScreen("Players", "Player hotlist", null, builder.ToString(), null, null);
        }

        public static void ShowGroupList(List<PermissionGroup> groups, List<string> memberNames)
        {

            StringBuilder builder = new StringBuilder();

            builder.AppendLine($@"{groups.Count} results found:");

            int index = 0;
            foreach (PermissionGroup group in groups)
            {
                var members = memberNames[groups.IndexOf(group)];

                builder.AppendFormat(@"
#{0} {1}
Level: {2}
Members: {3}
", ++index, group.GroupName, group.Level, string.IsNullOrEmpty(members) ? "none" : members);
            }

            MyAPIGateway.Utilities.ShowMissionScreen("Groups", "Group hotlist", null, builder.ToString(), null, null);
        }
    }
}
