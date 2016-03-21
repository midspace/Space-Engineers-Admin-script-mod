using System;
using System.Collections.Generic;
using System.Linq;
using midspace.adminscripts.Messages.Communication;
using midspace.adminscripts.Messages.Permissions;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace midspace.adminscripts.Config
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class Permissions
    {
        [ProtoMember(1)]
        public List<CommandStruct> Commands;

        [ProtoMember(2)]
        public List<PermissionGroup> Groups;

        [ProtoMember(3)]
        public List<PlayerPermission> Players;

        //hotlists
        private readonly Dictionary<ulong, List<CommandStruct>> _commandCache = new Dictionary<ulong, List<CommandStruct>>();
        private readonly Dictionary<ulong, List<PlayerPermission>> _playerCache = new Dictionary<ulong, List<PlayerPermission>>();
        private readonly Dictionary<ulong, List<PermissionGroup>> _groupCache = new Dictionary<ulong, List<PermissionGroup>>();

        #region permissions

        public void SendPermissions(ulong steamId)
        {
            uint playerLevel = 0;

            playerLevel = GetPlayerLevel(steamId);

            var playerPermissions = new List<CommandStruct>(Commands);

            if (Players.Any(p => p.Player.SteamId.Equals(steamId)))
            {
                var playerPermission = Players.FirstOrDefault(p => p.Player.SteamId.Equals(steamId));

                // create new entry if necessary or update the playername
                IMyPlayer myPlayer;
                if (MyAPIGateway.Players.TryGetPlayer(steamId, out myPlayer) &&
                    !playerPermission.Player.PlayerName.Equals(myPlayer.DisplayName))
                {
                    playerPermission = Players.FirstOrDefault(p => p.Player.SteamId == myPlayer.SteamUserId);
                    var i = Players.IndexOf(playerPermission);
                    playerPermission.Player.PlayerName = myPlayer.DisplayName;
                    Players[i] = playerPermission;
                }

                var extendedPermissions = playerPermission.Extensions;
                foreach (string commandName in new List<string>(extendedPermissions))
                {
                    if (!playerPermissions.Any(c => c.Name.Equals(commandName)))
                    {
                        //just cleaning up invalid commands
                        extendedPermissions.Remove(commandName);
                        continue;
                    }

                    playerPermissions.RemoveAll(
                        s => s.Name.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));
                    SendPermissionChange(steamId, new CommandStruct()
                    {
                        Name = commandName,
                        NeededLevel = playerLevel
                    });
                }

                var restrictedPermissions = playerPermission.Restrictions;
                foreach (string commandName in new List<string>(restrictedPermissions))
                {
                    if (!playerPermissions.Any(c => c.Name.Equals(commandName)))
                    {
                        restrictedPermissions.Remove(commandName);
                        continue;
                    }

                    playerPermissions.RemoveAll(
                        s => s.Name.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));
                    SendPermissionChange(steamId, new CommandStruct()
                    {
                        Name = commandName,
                        NeededLevel = playerLevel + 1
                    });
                }
            }

            foreach (CommandStruct commandStruct in playerPermissions)
                SendPermissionChange(steamId, commandStruct);

            ConnectionHelper.SendMessageToPlayer(steamId, new MessagePlayerPermission()
            {
                Action = PlayerPermissionAction.Level,
                PlayerLevel = playerLevel
            });
        }

        public void UpdateAdminLevel(uint adminLevel)
        {
            ChatCommandLogic.Instance.ServerCfg.Config.AdminLevel = adminLevel;


            var onlinePlayers = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(onlinePlayers, p => p != null);

            foreach (IMyPlayer player in onlinePlayers)
            {
                if (!player.IsAdmin())
                    continue;

                if (Players.All(p => p.Player.SteamId != player.SteamUserId) ||
                    (!Players.FirstOrDefault(p => p.Player.SteamId == player.SteamUserId).UsePlayerLevel &&
                     !Groups.Any(g => g.Members.Contains(player.SteamUserId))))
                    SendPermissions(player.SteamUserId);
            }
        }

        private void SendPermissionChange(ulong steamId, CommandStruct commandStruct)
        {
            var message = new MessageCommandPermission()
            {
                Commands = new List<CommandStruct>(),
                CommandAction = CommandActions.Level
            };
            message.Commands.Add(commandStruct);

            ConnectionHelper.SendMessageToPlayer(steamId, message);
        }

        private uint GetPlayerLevel(ulong steamId)
        {
            uint playerLevel = 0;

            IMyPlayer player;
            if (MyAPIGateway.Players.TryGetPlayer(steamId, out player) && player.IsAdmin())
                playerLevel = ChatCommandLogic.Instance.ServerCfg.Config.AdminLevel;

            if (Players.Any(p => p.Player.SteamId == steamId && p.UsePlayerLevel))
            {
                playerLevel = Players.FirstOrDefault(p => p.Player.SteamId == steamId).Level;
            }
            else if (Groups.Any(g => g.Members.Any(l => l == steamId)))
            {
                uint highestLevel = 0;
                foreach (PermissionGroup group in Groups.Where(g => g.Members.Any(l => l == steamId)))
                {
                    if (group.Level > highestLevel)
                        playerLevel = group.Level;
                }
            }

            return playerLevel;
        }

        #region actions

        #region command

        public void UpdateCommandSecurity(CommandStruct command, ulong sender)
        {
            var commandStruct =
                Commands.FirstOrDefault(c => c.Name.Equals(command.Name, StringComparison.InvariantCultureIgnoreCase));

            int index;
            if (_commandCache.ContainsKey(sender) && command.Name.Substring(0, 1) == "#" &&
                Int32.TryParse(command.Name.Substring(1), out index) && index > 0 && index <= _commandCache[sender].Count)
                commandStruct =
                    Commands.FirstOrDefault(
                        c =>
                            c.Name.Equals(_commandCache[sender][index - 1].Name,
                                StringComparison.InvariantCultureIgnoreCase));

            if (string.IsNullOrEmpty(commandStruct.Name))
            {
                MessageClientTextMessage.SendMessage(sender, "Server",
                    string.Format("Command {0} could not be found.", command.Name));
                return;
            }

            command.Name = commandStruct.Name;

            //update security first
            var i = Commands.IndexOf(commandStruct);
            commandStruct.NeededLevel = command.NeededLevel;
            Commands[i] = commandStruct;

            //then send changes
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, p => p != null);
            foreach (IMyPlayer player in players)
            {
                var playerPermission = Players.FirstOrDefault(p => p.Player.SteamId == player.SteamUserId);

                if (playerPermission.Player.SteamId == 0)
                {
                    //no player found -> send changes
                    SendPermissionChange(player.SteamUserId, commandStruct);
                    continue;
                }

                //don't send changes to players with exeptional permissions
                if (playerPermission.Extensions.Any(s => s.Equals(commandStruct.Name)) ||
                    playerPermission.Restrictions.Any(s => s.Equals(commandStruct.Name)))
                    continue;

                SendPermissionChange(player.SteamUserId, commandStruct);
            }

            if (commandStruct.NeededLevel == uint.MaxValue)
                MessageClientTextMessage.SendMessage(sender, "Server",
                    string.Format("The command '{0}' was disabled.", commandStruct.Name));
            else
                MessageClientTextMessage.SendMessage(sender, "Server",
                    string.Format("The level of command '{0}' was set to {1}.", commandStruct.Name,
                        commandStruct.NeededLevel));
        }

        public void CreateCommandHotlist(ulong sender, string param = null)
        {
            List<CommandStruct> commands = new List<CommandStruct>(Commands);

            if (!string.IsNullOrEmpty(param))
            {
                commands =
                    new List<CommandStruct>(
                        Commands.Where(c => c.Name.IndexOf(param, StringComparison.InvariantCultureIgnoreCase) >= 0));

                if (commands.Count == 0)
                {
                    MessageClientTextMessage.SendMessage(sender, "Server",
                        string.Format("No command matching with {0} could be found.", param));
                    return;
                }
            }

            if (!_commandCache.ContainsKey(sender))
                _commandCache.Add(sender, commands);
            else
                _commandCache[sender] = commands;

            var message = new MessageCommandPermission()
            {
                Commands = commands,
                CommandAction = CommandActions.List
            };

            ConnectionHelper.SendMessageToPlayer(sender, message);
        }

        #endregion

        #region player

        public void SetPlayerLevel(string playerName, uint level, ulong sender)
        {
            PlayerPermission player;
            if (TryGetPlayerPermission(playerName, out player, sender))
            {
                playerName = player.Player.PlayerName;

                //change level
                var i = Players.IndexOf(player);
                player.Level = level;
                Players[i] = player;

                //send changes to player
                SendPermissions(player.Player.SteamId);
            }
            else
            {
                MessageClientTextMessage.SendMessage(sender, "Server",
                    string.Format("Player {0} could not be found.", playerName));
                return;
            }

            MessageClientTextMessage.SendMessage(sender, "Server",
                string.Format("{0}'s level was set to {1}.", playerName, level));
        }

        public void ExtendRights(string playerName, string commandName, ulong sender)
        {
            PlayerPermission playerPermission;
            if (TryGetPlayerPermission(playerName, out playerPermission, sender))
            {
                playerName = playerPermission.Player.PlayerName;

                var commandStruct =
                    Commands.FirstOrDefault(c => c.Name.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));

                int index;
                if (_commandCache.ContainsKey(sender) && commandName.Substring(0, 1) == "#" &&
                    Int32.TryParse(commandName.Substring(1), out index) && index > 0 &&
                    index <= _commandCache[sender].Count)
                    commandStruct =
                        Commands.FirstOrDefault(
                            c =>
                                c.Name.Equals(_commandCache[sender][index - 1].Name,
                                    StringComparison.InvariantCultureIgnoreCase));

                if (string.IsNullOrEmpty(commandStruct.Name))
                {
                    MessageClientTextMessage.SendMessage(sender, "Server",
                        string.Format("Command {0} could not be found.", commandName));
                    return;
                }

                commandName = commandStruct.Name;
                var i = Players.IndexOf(playerPermission);

                if (Players[i].Extensions.Any(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    MessageClientTextMessage.SendMessage(sender, "Server",
                        string.Format("Player {0} already has extended access to {1}.", playerName, commandName));
                    return;
                }

                if (Players[i].Restrictions.Any(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    var command =
                        Players[i].Restrictions.FirstOrDefault(
                            s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));
                    Players[i].Restrictions.Remove(command);
                    SendPermissionChange(playerPermission.Player.SteamId, commandStruct);
                    MessageClientTextMessage.SendMessage(sender, "Server",
                        string.Format("Player {0} has normal access to {1} from now.", playerName, commandName));
                    return;
                }

                Players[i].Extensions.Add(commandStruct.Name);

                SendPermissionChange(playerPermission.Player.SteamId, new CommandStruct()
                {
                    Name = commandStruct.Name,
                    NeededLevel = GetPlayerLevel(playerPermission.Player.SteamId)
                });
                MessageClientTextMessage.SendMessage(sender, "Server",
                    string.Format("Player {0} has extended access to {1} from now.", playerName, commandName));
            }
            else
            {
                MessageClientTextMessage.SendMessage(sender, "Server",
                    string.Format("Player {0} could not be found.", playerName));
            }
        }

        public void RestrictRights(string playerName, string commandName, ulong sender)
        {
            PlayerPermission playerPermission;
            if (TryGetPlayerPermission(playerName, out playerPermission, sender))
            {
                playerName = playerPermission.Player.PlayerName;

                var commandStruct =
                    Commands.FirstOrDefault(c => c.Name.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));

                int index;
                if (_commandCache.ContainsKey(sender) && commandName.Substring(0, 1) == "#" &&
                    Int32.TryParse(commandName.Substring(1), out index) && index > 0 &&
                    index <= _commandCache[sender].Count)
                    commandStruct =
                        Commands.FirstOrDefault(
                            c =>
                                c.Name.Equals(_commandCache[sender][index - 1].Name,
                                    StringComparison.InvariantCultureIgnoreCase));

                if (string.IsNullOrEmpty(commandStruct.Name))
                {
                    MessageClientTextMessage.SendMessage(sender, "Server",
                        string.Format("Command {0} could not be found.", commandName));
                    return;
                }

                commandName = commandStruct.Name;
                var i = Players.IndexOf(playerPermission);

                if (Players[i].Restrictions.Any(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    MessageClientTextMessage.SendMessage(sender, "Server",
                        string.Format("Player {0} already has restricted access to {1}.", playerName, commandName));
                    return;
                }

                if (Players[i].Extensions.Any(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    var command =
                        Players[i].Extensions.FirstOrDefault(
                            s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));
                    Players[i].Extensions.Remove(command);
                    SendPermissionChange(playerPermission.Player.SteamId, commandStruct);
                    MessageClientTextMessage.SendMessage(sender, "Server",
                        string.Format("Player {0} has normal access to {1} from now.", playerName, commandName));
                    return;
                }

                Players[i].Restrictions.Add(commandStruct.Name);

                SendPermissionChange(playerPermission.Player.SteamId, new CommandStruct()
                {
                    Name = commandStruct.Name,
                    NeededLevel = GetPlayerLevel(playerPermission.Player.SteamId) + 1
                });
                MessageClientTextMessage.SendMessage(sender, "Server",
                    string.Format("Player {0} has no access to {1} from now.", playerName, commandName));
            }
            else
            {
                MessageClientTextMessage.SendMessage(sender, "Server",
                    string.Format("Player {0} could not be found.", playerName));
            }
        }

        public void UsePlayerLevel(string playerName, bool usePlayerLevel, ulong sender)
        {
            PlayerPermission playerPermission;
            if (TryGetPlayerPermission(playerName, out playerPermission, sender))
            {
                playerName = playerPermission.Player.PlayerName;

                var i = Players.IndexOf(playerPermission);
                playerPermission.UsePlayerLevel = usePlayerLevel;
                Players[i] = playerPermission;

                SendPermissions(playerPermission.Player.SteamId);

                MessageClientTextMessage.SendMessage(sender, "Server",
                    string.Format("{0} uses the {1} level now. Current level: {2}", playerName,
                        usePlayerLevel ? "player" : "group", GetPlayerLevel(playerPermission.Player.SteamId)));
            }
            else
            {
                MessageClientTextMessage.SendMessage(sender, "Server",
                    string.Format("Player {0} could not be found.", playerName));
            }
        }

        public void CreatePlayerHotlist(ulong sender, string param)
        {
            List<PlayerPermission> players = new List<PlayerPermission>(Players);

            var onlinePlayers = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(onlinePlayers, p => p != null);

            if (onlinePlayers.Count == 0 && Players.Count == 0)
            {
                MessageClientTextMessage.SendMessage(sender, "Server", "No players found.");
                return;
            }

            foreach (IMyPlayer player in onlinePlayers)
                if (players.All(p => p.Player.SteamId != player.SteamUserId))
                    players.Add(new PlayerPermission()
                    {
                        Player = new Player()
                        {
                            PlayerName = player.DisplayName,
                            SteamId = player.SteamUserId
                        },
                        Level = GetPlayerLevel(player.SteamUserId),
                        UsePlayerLevel = false
                    });


            if (!string.IsNullOrEmpty(param))
            {
                players =
                    new List<PlayerPermission>(
                        players.Where(
                            p => p.Player.PlayerName.IndexOf(param, StringComparison.InvariantCultureIgnoreCase) >= 0));

                if (players.Count == 0)
                {
                    MessageClientTextMessage.SendMessage(sender, "Server",
                        string.Format("No player matching with {0} could be found.", param));
                    return;
                }
            }
            players = new List<PlayerPermission>(players.OrderBy(p => p.Player.PlayerName));
            if (!_playerCache.ContainsKey(sender))
                _playerCache.Add(sender, players);
            else
                _playerCache[sender] = players;

            ConnectionHelper.SendMessageToPlayer(sender, new MessagePlayerPermission()
            {
                Action = PlayerPermissionAction.List,
                PlayerPermissions = players
            });
        }

        #endregion

        #region group

        public void CreateGroup(string name, uint level, ulong sender)
        {
            if (Groups.Any(g => g.GroupName.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                MessageClientTextMessage.SendMessage(sender, "Server",
                    string.Format("There is already a group named {0}.", name));
                return;
            }

            Groups.Add(new PermissionGroup()
            {
                GroupName = name,
                Level = level,
                Members = new List<ulong>(),
            });

            MessageClientTextMessage.SendMessage(sender, "Server",
                string.Format("Group {0} with level {1} was created.", name, level));
        }

        public void SetGroupLevel(string groupName, uint level, ulong sender)
        {
            PermissionGroup group;
            if (TryGetGroup(groupName, out group, sender))
            {
                groupName = group.GroupName;

                var i = Groups.IndexOf(group);
                group.Level = level;
                Groups[i] = group;

                foreach (ulong steamId in group.Members)
                {
                    SendPermissions(steamId);
                }

                MessageClientTextMessage.SendMessage(sender, "Server",
                    string.Format("The level of group {0} was updated to {1}.", groupName, level));
            }
            else
            {
                MessageClientTextMessage.SendMessage(sender, "Server",
                    string.Format("Group {0} could not be found.", groupName));
            }
        }

        public void SetGroupName(string groupName, string newName, ulong sender)
        {
            PermissionGroup group;
            if (TryGetGroup(groupName, out group, sender))
            {
                if (Groups.Any(g => g.GroupName.Equals(newName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    MessageClientTextMessage.SendMessage(sender, "Server",
                        string.Format("There is already a group named {0}.", newName));
                    return;
                }

                groupName = group.GroupName;

                var i = Groups.IndexOf(group);
                group.GroupName = newName;
                Groups[i] = group;

                MessageClientTextMessage.SendMessage(sender, "Server",
                    string.Format("Group {0} was renamed to {1}.", groupName, newName));
            }
            else
            {
                MessageClientTextMessage.SendMessage(sender, "Server",
                    string.Format("Group {0} could not be found.", groupName));
            }
        }

        public void AddPlayerToGroup(string groupName, string playerName, ulong sender)
        {
            PermissionGroup group;
            if (TryGetGroup(groupName, out group, sender))
            {
                groupName = group.GroupName;

                PlayerPermission playerPermission;
                if (TryGetPlayerPermission(playerName, out playerPermission, sender))
                {
                    playerName = playerPermission.Player.PlayerName;
                    if (group.Members.Contains(playerPermission.Player.SteamId))
                    {
                        MessageClientTextMessage.SendMessage(sender, "Server",
                            string.Format("Player {0} is already a member of group {1}.", playerName, groupName));
                        return;
                    }

                    var i = Groups.IndexOf(group);
                    group.Members.Add(playerPermission.Player.SteamId);
                    Groups[i] = group;

                    SendPermissions(playerPermission.Player.SteamId);
                }
                else
                {
                    MessageClientTextMessage.SendMessage(sender, "Server",
                        string.Format("Player {0} could not be found.", playerName));
                    return;
                }

                MessageClientTextMessage.SendMessage(sender, "Server",
                    string.Format("Added player {0} to group {1}.", playerName, groupName));
            }
            else
            {
                MessageClientTextMessage.SendMessage(sender, "Server",
                    string.Format("Group {0} could not be found.", groupName));
            }
        }

        public void RemovePlayerFromGroup(string groupName, string playerName, ulong sender)
        {
            PermissionGroup group;
            if (TryGetGroup(groupName, out group, sender))
            {
                groupName = group.GroupName;

                PlayerPermission playerPermission;
                if (TryGetPlayerPermission(playerName, out playerPermission, sender))
                {
                    playerName = playerPermission.Player.PlayerName;
                    if (!group.Members.Contains(playerPermission.Player.SteamId))
                    {
                        MessageClientTextMessage.SendMessage(sender, "Server",
                            string.Format("Player {0} is not a member of group {1}.", playerName, groupName));
                        return;
                    }

                    var i = Groups.IndexOf(group);
                    group.Members.Remove(playerPermission.Player.SteamId);
                    Groups[i] = group;

                    SendPermissions(playerPermission.Player.SteamId);
                }
                else
                {
                    MessageClientTextMessage.SendMessage(sender, "Server",
                        string.Format("Player {0} could not be found.", playerName));
                    return;
                }

                MessageClientTextMessage.SendMessage(sender, "Server",
                    string.Format("Removed player {0} from group {1}.", playerName, groupName));
            }
            else
            {
                MessageClientTextMessage.SendMessage(sender, "Server",
                    string.Format("Group {0} could not be found.", groupName));
            }
        }

        public void DeleteGroup(string groupName, ulong sender)
        {
            PermissionGroup group;
            if (TryGetGroup(groupName, out group, sender))
            {
                groupName = group.GroupName;
                Groups.Remove(group);

                foreach (ulong steamId in group.Members)
                {
                    SendPermissions(steamId);
                }

                MessageClientTextMessage.SendMessage(sender, "Server",
                    string.Format("Group {0} has been deleted.", groupName));
            }
            else
            {
                MessageClientTextMessage.SendMessage(sender, "Server",
                    string.Format("Group {0} could not be found.", groupName));
            }
        }

        public void CreateGroupHotlist(ulong sender, string param = null)
        {
            if (Groups.Count == 0)
            {
                MessageClientTextMessage.SendMessage(sender, "Server", "No groups found.");
                return;
            }

            List<PermissionGroup> groups = new List<PermissionGroup>(Groups);

            if (!string.IsNullOrEmpty(param))
            {
                groups =
                    new List<PermissionGroup>(
                        Groups.Where(g => g.GroupName.IndexOf(param, StringComparison.InvariantCultureIgnoreCase) >= 0));

                if (groups.Count == 0)
                {
                    MessageClientTextMessage.SendMessage(sender, "Server",
                        string.Format("No group matching with {0} could be found.", param));
                    return;
                }
            }

            if (!_groupCache.ContainsKey(sender))
                _groupCache.Add(sender, groups);
            else
                _groupCache[sender] = groups;

            var memberNames = new List<string>();

            groups = new List<PermissionGroup>(groups.OrderBy(g => g.GroupName));

            foreach (PermissionGroup group in groups)
            {
                List<string> names = new List<string>();
                foreach (ulong steamId in group.Members)
                    names.Add(Players.FirstOrDefault(p => p.Player.SteamId == steamId).Player.PlayerName);

                memberNames.Add(string.Join(", ", names));
            }

            ConnectionHelper.SendMessageToPlayer(sender, new MessageGroupPermission()
            {
                Action = PermissionGroupAction.List,
                Groups = groups,
                MemberNames = memberNames
            });
        }

        #endregion

        #region utils

        private bool TryGetPlayerPermission(string playerName, out PlayerPermission playerPermission, ulong sender)
        {
            playerPermission = new PlayerPermission();

            int index;
            if (_playerCache.ContainsKey(sender) && playerName.Substring(0, 1) == "#" &&
                Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= _playerCache[sender].Count)
                playerName = _playerCache[sender][index - 1].Player.PlayerName;

            if (!Players.Any(p => p.Player.PlayerName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase)))
            {
                IMyPlayer myPlayer;
                if (MyAPIGateway.Players.TryGetPlayer(playerName, out myPlayer))
                {
                    if (Players.Any(p => p.Player.SteamId == myPlayer.SteamUserId))
                    {
                        playerPermission = Players.FirstOrDefault(p => p.Player.SteamId == myPlayer.SteamUserId);
                        var i = Players.IndexOf(playerPermission);
                        playerPermission.Player.PlayerName = myPlayer.DisplayName;
                        Players[i] = playerPermission;
                    }
                    else
                    {
                        playerPermission = new PlayerPermission()
                        {
                            Player = new Player()
                            {
                                PlayerName = myPlayer.DisplayName,
                                SteamId = myPlayer.SteamUserId
                            },
                            Level = myPlayer.IsAdmin() ? ChatCommandLogic.Instance.ServerCfg.Config.AdminLevel : 0,
                            UsePlayerLevel = false,
                            Extensions = new List<string>(),
                            Restrictions = new List<string>()
                        };
                        Players.Add(playerPermission);
                    }
                }
                else
                    return false;
            }
            else
                playerPermission =
                    Players.FirstOrDefault(
                        p => p.Player.PlayerName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase));

            return true;
        }

        private bool TryGetGroup(string groupName, out PermissionGroup group, ulong sender)
        {
            group =
                Groups.FirstOrDefault(g => g.GroupName.Equals(groupName, StringComparison.InvariantCultureIgnoreCase));

            int index;
            if (_groupCache.ContainsKey(sender) && groupName.Substring(0, 1) == "#" &&
                Int32.TryParse(groupName.Substring(1), out index) && index > 0 && index <= _groupCache[sender].Count)
                group =
                    Groups.FirstOrDefault(
                        g =>
                            g.GroupName.Equals(_groupCache[sender][index - 1].GroupName,
                                StringComparison.InvariantCultureIgnoreCase));

            if (string.IsNullOrEmpty(group.GroupName))
                return false;

            return true;
        }

        #endregion

        #endregion

        #endregion
    }

    public struct PermissionGroup
    {
        public string GroupName;
        public uint Level;
        public List<ulong> Members;
    }

    public struct CommandStruct
    {
        public string Name;
        public uint NeededLevel;
    }

    public struct PlayerPermission
    {
        public Player Player;
        public uint Level;
        public bool UsePlayerLevel;
        public List<string> Extensions;
        public List<string> Restrictions;
    }
}