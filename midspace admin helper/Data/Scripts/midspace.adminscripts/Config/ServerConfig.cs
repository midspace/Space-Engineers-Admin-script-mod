using midspace.adminscripts.Config.Files;

namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using midspace.adminscripts.Messages;
    using midspace.adminscripts.Messages.Communication;
    using midspace.adminscripts.Messages.Permissions;
    using midspace.adminscripts.Protection;
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRage.Game.ModAPI;

    /// <summary>
    /// Represents the server configuration of the mod.
    /// </summary>
    public class ServerConfig
    {

        private MotdFile motdFile;
        private ServerConfigFile serverConfigFile;
        private GlobalChatLogFile globalChatLogFile;
        private PrivateMessageLogFile privateMessageLogFile;
        private PermissionsFile permissionsFile;


        //hotlists
        Dictionary<ulong, List<CommandStruct>> CommandCache = new Dictionary<ulong, List<CommandStruct>>();
        Dictionary<ulong, List<PlayerPermission>> PlayerCache = new Dictionary<ulong, List<PlayerPermission>>();
        Dictionary<ulong, List<PermissionGroup>> GroupCache = new Dictionary<ulong, List<PermissionGroup>>();

        /// <summary>
        /// Used for saving and loading things.
        /// </summary>
        public ServerConfigurationStruct Config { get { return serverConfigFile.Config; } private set { serverConfigFile.Config = value; } }

        /// <summary>
        /// True for listen server.
        /// </summary>
        public static bool ServerIsClient = true;

        private bool RegisteredIndestructibleDamageHandler = false;

        public ServerConfig(List<ChatCommand> commands)
        {
            string pathName = Path.GetFileNameWithoutExtension(MyAPIGateway.Session.CurrentPath);

            if (MyAPIGateway.Utilities.IsDedicated)
                ServerIsClient = false;

            //cfg
            serverConfigFile = new ServerConfigFile(pathName);

            if (Config.EnableLog)
            {
                ChatCommandLogic.Instance.Debug = true;
                Logger.Init();
                Logger.Debug("Log Enabled.");
            }

            motdFile = new MotdFile(Config.MotdFileSuffix);
            SendMotd();

            //chat log
            globalChatLogFile = new GlobalChatLogFile(pathName);

            //permissions
            permissionsFile = new PermissionsFile(pathName, commands);

            //pm log
            if (Config.LogPrivateMessages)
            {
                privateMessageLogFile = new PrivateMessageLogFile(pathName);
            }

            if (Config.NoGrindIndestructible)
            {
                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, IndestructibleDamageHandler);
                RegisteredIndestructibleDamageHandler = true;
                Logger.Debug("Registered indestructible damage handler.");
            }

            Logger.Debug("Config loaded.");
        }

        public void Save(string customSaveName = null)
        {
            //write values in cfg
            Config.MotdHeadLine = CommandMessageOfTheDay.HeadLine;
            Config.MotdShowInChat = CommandMessageOfTheDay.ShowInChat;

            //cfg
            Config.WorldLocation = MyAPIGateway.Session.CurrentPath;

            serverConfigFile.Save(customSaveName);
            //motd
            motdFile.Save();

            SaveLogs(customSaveName);

            if (customSaveName != null)
                permissionsFile.Save(customSaveName);

            ProtectionHandler.Save(customSaveName);
            Logger.Debug("Config saved.");
        }

        public void ReloadConfig()
        {
            serverConfigFile.Load();
            motdFile.Load();
        }

        public void SaveLogs(string customSaveName = null)
        {
            globalChatLogFile.Save(customSaveName);

            if (Config.LogPrivateMessages)
                privateMessageLogFile.Save(customSaveName);

            Logger.Debug("Logs saved.");
        }

        #region server config

        private void IndestructibleDamageHandler(object target, ref MyDamageInformation info)
        {
            if (Config.NoGrindIndestructible && target is IMySlimBlock)
            {
                var block = target as IMySlimBlock;
                var grid = block.CubeGrid;

                if (grid != null && !((MyObjectBuilder_CubeGrid)grid.GetObjectBuilder()).DestructibleBlocks)
                    info.Amount = 0;
            }
        }

        public void SetNoGrindIndestructible(bool noGrindIndestructible)
        {
            Config.NoGrindIndestructible = noGrindIndestructible;

            if (noGrindIndestructible && !RegisteredIndestructibleDamageHandler)
            {
                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, IndestructibleDamageHandler);
                RegisteredIndestructibleDamageHandler = true;
                Logger.Debug("Registered indestructible damage handler.");
            }
        }

        #endregion

        #region message of the day

        private void SendMotd()
        {
            var message = new MessageOfTheDayMessage();

            var sendMotd = !Config.MotdHeadLine.Equals(CommandMessageOfTheDay.HeadLine);
            if (sendMotd)
            {
                message.Content = SetMessageOfTheDay(motdFile.MessageOfTheDay);
                message.FieldsToUpdate = message.FieldsToUpdate | MessageOfTheDayMessage.ChangedFields.Content;
            }

            var sendMotdHl = !Config.MotdHeadLine.Equals(CommandMessageOfTheDay.HeadLine);
            CommandMessageOfTheDay.HeadLine = Config.MotdHeadLine;
            if (sendMotdHl)
            {
                message.HeadLine = CommandMessageOfTheDay.HeadLine;
                message.FieldsToUpdate = message.FieldsToUpdate | MessageOfTheDayMessage.ChangedFields.HeadLine;
            }

            var sendMotdSic = Config.MotdShowInChat != CommandMessageOfTheDay.ShowInChat;
            CommandMessageOfTheDay.ShowInChat = Config.MotdShowInChat;
            if (sendMotdSic)
            {
                message.ShowInChat = CommandMessageOfTheDay.ShowInChat;
                message.FieldsToUpdate = message.FieldsToUpdate | MessageOfTheDayMessage.ChangedFields.ShowInChat;
            }

            if (sendMotdHl || sendMotdSic)
                ConnectionHelper.SendMessageToAllPlayers(message);
        }

        private string ReplaceVariables(string text)
        {
            //replace variables
            text = text.Replace("%WORLD_NAME%", MyAPIGateway.Session.Name);
            //text = text.Replace("%SERVER_IP%", dedicatedConfig.IP); returns the 'listen ip' default: 0.0.0.0

            //only for DS
            if (!ServerIsClient)
            {
                var dedicatedConfig = MyAPIGateway.Utilities.ConfigDedicated;
                dedicatedConfig.Load();
                while (dedicatedConfig == null)
                    ;

                text = text.Replace("%SERVER_NAME%", dedicatedConfig.ServerName);
                text = text.Replace("%SERVER_PORT%", dedicatedConfig.ServerPort.ToString());
            }
            return text;
        }

        /// <summary>
        /// Replaces the variables and sets the message of the day.
        /// </summary>
        /// <param name="motd">The message of the day.</param>
        /// <returns>The message of the day with replaced variables.</returns>
        public string SetMessageOfTheDay(string motd)
        {
            if (motd == null)
                motd = "";
            motd = ReplaceVariables(motd);
            var sendChanges = !motd.Equals(CommandMessageOfTheDay.Content);
            CommandMessageOfTheDay.Content = motd;

            return motd;
        }

        #endregion

        #region private messages

        public void LogPrivateMessage(ChatMessage chatMessage, ulong receiver)
        {
            if (!Config.LogPrivateMessages)
                return;

            List<PrivateConversation> senderConversations = privateMessageLogFile.PrivateConversations.FindAll(c => c.Participants.Exists(p => p.SteamId == chatMessage.Sender.SteamId));

            var pm = new PrivateMessage()
            {
                Sender = chatMessage.Sender.SteamId,
                Receiver = receiver,
                Date = chatMessage.Date,
                Text = chatMessage.Text
            };

            if (senderConversations.Exists(c => c.Participants.Exists(p => p.SteamId == receiver)))
            {
                PrivateConversation conversation = senderConversations.Find(c => c.Participants.Exists(p => p.SteamId == receiver));
                conversation.Messages.Add(pm);
            }
            else
            {
                List<IMyPlayer> players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, p => p != null);
                var senderPlayer = players.FirstOrDefault(p => p.SteamUserId == chatMessage.Sender.SteamId);
                var receiverPlayer = players.FirstOrDefault(p => p.SteamUserId == receiver);

                privateMessageLogFile.PrivateConversations.Add(new PrivateConversation()
                {
                    Participants = new List<Player>(new Player[] {
                            new Player() {
                                SteamId = senderPlayer.SteamUserId,
                                PlayerName = senderPlayer.DisplayName
                            },
                            new Player(){
                                SteamId = receiverPlayer.SteamUserId,
                                PlayerName = receiverPlayer.DisplayName
                            }}),
                    Messages = new List<PrivateMessage>(new PrivateMessage[] { pm })
                });
            }
        }

        #endregion

        #region global messages

        public void LogGlobalMessage(ChatMessage chatMessage)
        {
            chatMessage.Date = DateTime.Now;
            globalChatLogFile.ChatMessages.Add(chatMessage);
        }

        /// <summary>
        /// Sends the given amount of chat entries to the client to display the chat history.
        /// </summary>
        /// <param name="senderSteamId">The Steamid of the receiving client.</param>
        /// <param name="entryCount">The amount of entries that are requested.</param>
        public void SendChatHistory(ulong receiver, uint entryCount)
        {
            // we just append new chat messages to the log. To get the most recent on top we have to sort it.
            List<ChatMessage> cache = new List<ChatMessage>(globalChatLogFile.ChatMessages.OrderByDescending(m => m.Date));

            // we have to make sure that we don't throw an exception
            int range = (int)entryCount;
            if (cache.Count < entryCount)
                range = cache.Count;

            var msgHistory = new MessageChatHistory
            {
                ChatHistory = cache.GetRange(0, range)
            };

            ConnectionHelper.SendMessageToPlayer(receiver, msgHistory);
        }

        #endregion

        #region permissions

        public void SendPermissions(ulong steamId)
        {
            uint playerLevel = 0;

            playerLevel = GetPlayerLevel(steamId);

            var playerPermissions = new List<CommandStruct>(permissionsFile.Permissions.Commands);

            if (permissionsFile.Permissions.Players.Any(p => p.Player.SteamId.Equals(steamId)))
            {
                var playerPermission = permissionsFile.Permissions.Players.FirstOrDefault(p => p.Player.SteamId.Equals(steamId));

                // create new entry if necessary or update the playername
                IMyPlayer myPlayer;
                if (MyAPIGateway.Players.TryGetPlayer(steamId, out myPlayer) && !playerPermission.Player.PlayerName.Equals(myPlayer.DisplayName))
                {
                    playerPermission = permissionsFile.Permissions.Players.FirstOrDefault(p => p.Player.SteamId == myPlayer.SteamUserId);
                    var i = permissionsFile.Permissions.Players.IndexOf(playerPermission);
                    playerPermission.Player.PlayerName = myPlayer.DisplayName;
                    permissionsFile.Permissions.Players[i] = playerPermission;
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

                    playerPermissions.RemoveAll(s => s.Name.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));
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

                    playerPermissions.RemoveAll(s => s.Name.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));
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

        public void SendPermissionChange(ulong steamId, CommandStruct commandStruct)
        {
            var message = new MessageCommandPermission()
            {
                Commands = new List<CommandStruct>(),
                CommandAction = CommandActions.Level
            };
            message.Commands.Add(commandStruct);

            ConnectionHelper.SendMessageToPlayer(steamId, message);
        }

        public uint GetPlayerLevel(ulong steamId)
        {
            uint playerLevel = 0;

            IMyPlayer player;
            if (MyAPIGateway.Players.TryGetPlayer(steamId, out player) && player.IsAdmin())
                playerLevel = Config.AdminLevel;

            if (permissionsFile.Permissions.Players.Any(p => p.Player.SteamId == steamId && p.UsePlayerLevel))
            {
                playerLevel = permissionsFile.Permissions.Players.FirstOrDefault(p => p.Player.SteamId == steamId).Level;
            }
            else if (permissionsFile.Permissions.Groups.Any(g => g.Members.Any(l => l == steamId)))
            {
                uint highestLevel = 0;
                foreach (PermissionGroup group in permissionsFile.Permissions.Groups.Where(g => g.Members.Any(l => l == steamId)))
                {
                    if (group.Level > highestLevel)
                        playerLevel = group.Level;
                }
            }

            return playerLevel;
        }

        public void UpdateAdminLevel(uint adminLevel)
        {
            Config.AdminLevel = adminLevel;


            var onlinePlayers = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(onlinePlayers, p => p != null);

            foreach (IMyPlayer player in onlinePlayers)
            {
                if (!player.IsAdmin())
                    continue;

                if (!permissionsFile.Permissions.Players.Any(p => p.Player.SteamId == player.SteamUserId) || (!permissionsFile.Permissions.Players.FirstOrDefault(p => p.Player.SteamId == player.SteamUserId).UsePlayerLevel && !permissionsFile.Permissions.Groups.Any(g => g.Members.Contains(player.SteamUserId))))
                    SendPermissions(player.SteamUserId);
            }
        }

        #region actions

        #region command

        public void UpdateCommandSecurity(CommandStruct command, ulong sender)
        {
            var commandStruct = permissionsFile.Permissions.Commands.FirstOrDefault(c => c.Name.Equals(command.Name, StringComparison.InvariantCultureIgnoreCase));

            int index;
            if (CommandCache.ContainsKey(sender) && command.Name.Substring(0, 1) == "#" && Int32.TryParse(command.Name.Substring(1), out index) && index > 0 && index <= CommandCache[sender].Count)
                commandStruct = permissionsFile.Permissions.Commands.FirstOrDefault(c => c.Name.Equals(CommandCache[sender][index - 1].Name, StringComparison.InvariantCultureIgnoreCase));

            if (string.IsNullOrEmpty(commandStruct.Name))
            {
                MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Command {0} could not be found.", command.Name));
                return;
            }

            command.Name = commandStruct.Name;

            //update security first
            var i = permissionsFile.Permissions.Commands.IndexOf(commandStruct);
            commandStruct.NeededLevel = command.NeededLevel;
            permissionsFile.Permissions.Commands[i] = commandStruct;

            //then send changes
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, p => p != null);
            foreach (IMyPlayer player in players)
            {
                var playerPermission = permissionsFile.Permissions.Players.FirstOrDefault(p => p.Player.SteamId == player.SteamUserId);

                if (playerPermission.Player.SteamId == 0)
                {
                    //no player found -> send changes
                    SendPermissionChange(player.SteamUserId, commandStruct);
                    continue;
                }

                //don't send changes to players with exeptional permissions
                if (playerPermission.Extensions.Any(s => s.Equals(commandStruct.Name)) || playerPermission.Restrictions.Any(s => s.Equals(commandStruct.Name)))
                    continue;

                SendPermissionChange(player.SteamUserId, commandStruct);
            }

            if (commandStruct.NeededLevel == uint.MaxValue)
                MessageClientTextMessage.SendMessage(sender, "Server", string.Format("The command '{0}' was disabled.", commandStruct.Name));
            else
                MessageClientTextMessage.SendMessage(sender, "Server", string.Format("The level of command '{0}' was set to {1}.", commandStruct.Name, commandStruct.NeededLevel));

            permissionsFile.Save();
        }

        public void CreateCommandHotlist(ulong sender, string param = null)
        {
            List<CommandStruct> commands = new List<CommandStruct>(permissionsFile.Permissions.Commands);

            if (!string.IsNullOrEmpty(param))
            {
                commands = new List<CommandStruct>(permissionsFile.Permissions.Commands.Where(c => c.Name.IndexOf(param, StringComparison.InvariantCultureIgnoreCase) >= 0));

                if (commands.Count == 0)
                {
                    MessageClientTextMessage.SendMessage(sender, "Server", string.Format("No command matching with {0} could be found.", param));
                    return;
                }
            }

            if (!CommandCache.ContainsKey(sender))
                CommandCache.Add(sender, commands);
            else
                CommandCache[sender] = commands;

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
                var i = permissionsFile.Permissions.Players.IndexOf(player);
                player.Level = level;
                permissionsFile.Permissions.Players[i] = player;

                //send changes to player
                SendPermissions(player.Player.SteamId);
            }
            else
            {
                MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Player {0} could not be found.", playerName));
                return;
            }

            MessageClientTextMessage.SendMessage(sender, "Server", string.Format("{0}'s level was set to {1}.", playerName, level));

            permissionsFile.Save();
        }

        public void ExtendRights(string playerName, string commandName, ulong sender)
        {
            PlayerPermission playerPermission;
            if (TryGetPlayerPermission(playerName, out playerPermission, sender))
            {
                playerName = playerPermission.Player.PlayerName;

                var commandStruct = permissionsFile.Permissions.Commands.FirstOrDefault(c => c.Name.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));

                int index;
                if (CommandCache.ContainsKey(sender) && commandName.Substring(0, 1) == "#" && Int32.TryParse(commandName.Substring(1), out index) && index > 0 && index <= CommandCache[sender].Count)
                    commandStruct = permissionsFile.Permissions.Commands.FirstOrDefault(c => c.Name.Equals(CommandCache[sender][index - 1].Name, StringComparison.InvariantCultureIgnoreCase));

                if (string.IsNullOrEmpty(commandStruct.Name))
                {
                    MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Command {0} could not be found.", commandName));
                    return;
                }

                commandName = commandStruct.Name;
                var i = permissionsFile.Permissions.Players.IndexOf(playerPermission);

                if (permissionsFile.Permissions.Players[i].Extensions.Any(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Player {0} already has extended access to {1}.", playerName, commandName));
                    return;
                }

                if (permissionsFile.Permissions.Players[i].Restrictions.Any(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    var command = permissionsFile.Permissions.Players[i].Restrictions.FirstOrDefault(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));
                    permissionsFile.Permissions.Players[i].Restrictions.Remove(command);
                    SendPermissionChange(playerPermission.Player.SteamId, commandStruct);
                    MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Player {0} has normal access to {1} from now.", playerName, commandName));
                    return;
                }

                permissionsFile.Permissions.Players[i].Extensions.Add(commandStruct.Name);

                SendPermissionChange(playerPermission.Player.SteamId, new CommandStruct()
                {
                    Name = commandStruct.Name,
                    NeededLevel = GetPlayerLevel(playerPermission.Player.SteamId)
                });
                MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Player {0} has extended access to {1} from now.", playerName, commandName));
            }
            else
            {
                MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Player {0} could not be found.", playerName));
                return;
            }

            permissionsFile.Save();
        }

        public void RestrictRights(string playerName, string commandName, ulong sender)
        {
            PlayerPermission playerPermission;
            if (TryGetPlayerPermission(playerName, out playerPermission, sender))
            {
                playerName = playerPermission.Player.PlayerName;

                var commandStruct = permissionsFile.Permissions.Commands.FirstOrDefault(c => c.Name.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));

                int index;
                if (CommandCache.ContainsKey(sender) && commandName.Substring(0, 1) == "#" && Int32.TryParse(commandName.Substring(1), out index) && index > 0 && index <= CommandCache[sender].Count)
                    commandStruct = permissionsFile.Permissions.Commands.FirstOrDefault(c => c.Name.Equals(CommandCache[sender][index - 1].Name, StringComparison.InvariantCultureIgnoreCase));

                if (string.IsNullOrEmpty(commandStruct.Name))
                {
                    MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Command {0} could not be found.", commandName));
                    return;
                }

                commandName = commandStruct.Name;
                var i = permissionsFile.Permissions.Players.IndexOf(playerPermission);

                if (permissionsFile.Permissions.Players[i].Restrictions.Any(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Player {0} already has restricted access to {1}.", playerName, commandName));
                    return;
                }

                if (permissionsFile.Permissions.Players[i].Extensions.Any(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    var command = permissionsFile.Permissions.Players[i].Extensions.FirstOrDefault(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));
                    permissionsFile.Permissions.Players[i].Extensions.Remove(command);
                    SendPermissionChange(playerPermission.Player.SteamId, commandStruct);
                    MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Player {0} has normal access to {1} from now.", playerName, commandName));
                    return;
                }

                permissionsFile.Permissions.Players[i].Restrictions.Add(commandStruct.Name);

                SendPermissionChange(playerPermission.Player.SteamId, new CommandStruct()
                {
                    Name = commandStruct.Name,
                    NeededLevel = GetPlayerLevel(playerPermission.Player.SteamId) + 1
                });
                MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Player {0} has no access to {1} from now.", playerName, commandName));
            }
            else
            {
                MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Player {0} could not be found.", playerName));
                return;
            }

            permissionsFile.Save();
        }

        public void UsePlayerLevel(string playerName, bool usePlayerLevel, ulong sender)
        {
            PlayerPermission playerPermission;
            if (TryGetPlayerPermission(playerName, out playerPermission, sender))
            {
                playerName = playerPermission.Player.PlayerName;

                var i = permissionsFile.Permissions.Players.IndexOf(playerPermission);
                playerPermission.UsePlayerLevel = usePlayerLevel;
                permissionsFile.Permissions.Players[i] = playerPermission;

                SendPermissions(playerPermission.Player.SteamId);

                MessageClientTextMessage.SendMessage(sender, "Server", string.Format("{0} uses the {1} level now. Current level: {2}", playerName, usePlayerLevel ? "player" : "group", GetPlayerLevel(playerPermission.Player.SteamId)));
            }
            else
            {
                MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Player {0} could not be found.", playerName));
                return;
            }

            permissionsFile.Save();
        }

        public void CreatePlayerHotlist(ulong sender, string param)
        {
            List<PlayerPermission> players = new List<PlayerPermission>(permissionsFile.Permissions.Players);

            var onlinePlayers = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(onlinePlayers, p => p != null);

            if (onlinePlayers.Count == 0 && permissionsFile.Permissions.Players.Count == 0)
            {
                MessageClientTextMessage.SendMessage(sender, "Server", "No players found.");
                return;
            }

            foreach (IMyPlayer player in onlinePlayers)
                if (!players.Any(p => p.Player.SteamId == player.SteamUserId))
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
                players = new List<PlayerPermission>(players.Where(p => p.Player.PlayerName.IndexOf(param, StringComparison.InvariantCultureIgnoreCase) >= 0));

                if (players.Count == 0)
                {
                    MessageClientTextMessage.SendMessage(sender, "Server", string.Format("No player matching with {0} could be found.", param));
                    return;
                }
            }
            players = new List<PlayerPermission>(players.OrderBy(p => p.Player.PlayerName));
            if (!PlayerCache.ContainsKey(sender))
                PlayerCache.Add(sender, players);
            else
                PlayerCache[sender] = players;

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
            if (permissionsFile.Permissions.Groups.Any(g => g.GroupName.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                MessageClientTextMessage.SendMessage(sender, "Server", string.Format("There is already a group named {0}.", name));
                return;
            }

            permissionsFile.Permissions.Groups.Add(new PermissionGroup()
            {
                GroupName = name,
                Level = level,
                Members = new List<ulong>(),
            });

            MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Group {0} with level {1} was created.", name, level));

            permissionsFile.Save();
        }

        public void SetGroupLevel(string groupName, uint level, ulong sender)
        {
            PermissionGroup group;
            if (TryGetGroup(groupName, out group, sender))
            {
                groupName = group.GroupName;

                var i = permissionsFile.Permissions.Groups.IndexOf(group);
                group.Level = level;
                permissionsFile.Permissions.Groups[i] = group;

                foreach (ulong steamId in group.Members)
                {
                    SendPermissions(steamId);
                }

                MessageClientTextMessage.SendMessage(sender, "Server", string.Format("The level of group {0} was updated to {1}.", groupName, level));
            }
            else
            {
                MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Group {0} could not be found.", groupName));
                return;
            }

            permissionsFile.Save();
        }

        public void SetGroupName(string groupName, string newName, ulong sender)
        {
            PermissionGroup group;
            if (TryGetGroup(groupName, out group, sender))
            {
                if (permissionsFile.Permissions.Groups.Any(g => g.GroupName.Equals(newName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    MessageClientTextMessage.SendMessage(sender, "Server", string.Format("There is already a group named {0}.", newName));
                    return;
                }

                groupName = group.GroupName;

                var i = permissionsFile.Permissions.Groups.IndexOf(group);
                group.GroupName = newName;
                permissionsFile.Permissions.Groups[i] = group;

                MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Group {0} was renamed to {1}.", groupName, newName));
            }
            else
            {
                MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Group {0} could not be found.", groupName));
                return;
            }

            permissionsFile.Save();
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
                        MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Player {0} is already a member of group {1}.", playerName, groupName));
                        return;
                    }

                    var i = permissionsFile.Permissions.Groups.IndexOf(group);
                    group.Members.Add(playerPermission.Player.SteamId);
                    permissionsFile.Permissions.Groups[i] = group;

                    SendPermissions(playerPermission.Player.SteamId);
                }
                else
                {
                    MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Player {0} could not be found.", playerName));
                    return;
                }

                MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Added player {0} to group {1}.", playerName, groupName));
            }
            else
            {
                MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Group {0} could not be found.", groupName));
                return;
            }

            permissionsFile.Save();
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
                        MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Player {0} is not a member of group {1}.", playerName, groupName));
                        return;
                    }

                    var i = permissionsFile.Permissions.Groups.IndexOf(group);
                    group.Members.Remove(playerPermission.Player.SteamId);
                    permissionsFile.Permissions.Groups[i] = group;

                    SendPermissions(playerPermission.Player.SteamId);
                }
                else
                {
                    MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Player {0} could not be found.", playerName));
                    return;
                }

                MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Removed player {0} from group {1}.", playerName, groupName));
            }
            else
            {
                MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Group {0} could not be found.", groupName));
                return;
            }

            permissionsFile.Save();
        }

        public void DeleteGroup(string groupName, ulong sender)
        {
            PermissionGroup group;
            if (TryGetGroup(groupName, out group, sender))
            {
                groupName = group.GroupName;
                permissionsFile.Permissions.Groups.Remove(group);

                foreach (ulong steamId in group.Members)
                {
                    SendPermissions(steamId);
                }

                MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Group {0} has been deleted.", groupName));
            }
            else
            {
                MessageClientTextMessage.SendMessage(sender, "Server", string.Format("Group {0} could not be found.", groupName));
                return;
            }

            permissionsFile.Save();
        }

        public void CreateGroupHotlist(ulong sender, string param = null)
        {
            if (permissionsFile.Permissions.Groups.Count == 0)
            {
                MessageClientTextMessage.SendMessage(sender, "Server", "No groups found.");
                return;
            }

            List<PermissionGroup> groups = new List<PermissionGroup>(permissionsFile.Permissions.Groups);

            if (!string.IsNullOrEmpty(param))
            {
                groups = new List<PermissionGroup>(permissionsFile.Permissions.Groups.Where(g => g.GroupName.IndexOf(param, StringComparison.InvariantCultureIgnoreCase) >= 0));

                if (groups.Count == 0)
                {
                    MessageClientTextMessage.SendMessage(sender, "Server", string.Format("No group matching with {0} could be found.", param));
                    return;
                }
            }

            if (!GroupCache.ContainsKey(sender))
                GroupCache.Add(sender, groups);
            else
                GroupCache[sender] = groups;

            var memberNames = new List<string>();

            groups = new List<PermissionGroup>(groups.OrderBy(g => g.GroupName));

            foreach (PermissionGroup group in groups)
            {
                List<string> names = new List<string>();
                foreach (ulong steamId in group.Members)
                    names.Add(permissionsFile.Permissions.Players.FirstOrDefault(p => p.Player.SteamId == steamId).Player.PlayerName);

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

        public bool TryGetPlayerPermission(string playerName, out PlayerPermission playerPermission, ulong sender)
        {
            playerPermission = new PlayerPermission();

            int index;
            if (PlayerCache.ContainsKey(sender) && playerName.Substring(0, 1) == "#" && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= PlayerCache[sender].Count)
                playerName = PlayerCache[sender][index - 1].Player.PlayerName;

            if (!permissionsFile.Permissions.Players.Any(p => p.Player.PlayerName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase)))
            {
                IMyPlayer myPlayer;
                if (MyAPIGateway.Players.TryGetPlayer(playerName, out myPlayer))
                {
                    if (permissionsFile.Permissions.Players.Any(p => p.Player.SteamId == myPlayer.SteamUserId))
                    {
                        playerPermission = permissionsFile.Permissions.Players.FirstOrDefault(p => p.Player.SteamId == myPlayer.SteamUserId);
                        var i = permissionsFile.Permissions.Players.IndexOf(playerPermission);
                        playerPermission.Player.PlayerName = myPlayer.DisplayName;
                        permissionsFile.Permissions.Players[i] = playerPermission;
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
                            Level = myPlayer.IsAdmin() ? Config.AdminLevel : 0,
                            UsePlayerLevel = false,
                            Extensions = new List<string>(),
                            Restrictions = new List<string>()
                        };
                        permissionsFile.Permissions.Players.Add(playerPermission);
                    }
                }
                else
                    return false;
            }
            else
                playerPermission = permissionsFile.Permissions.Players.FirstOrDefault(p => p.Player.PlayerName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase));

            return true;
        }

        public bool TryGetGroup(string groupName, out PermissionGroup group, ulong sender)
        {
            group = permissionsFile.Permissions.Groups.FirstOrDefault(g => g.GroupName.Equals(groupName, StringComparison.InvariantCultureIgnoreCase));

            int index;
            if (GroupCache.ContainsKey(sender) && groupName.Substring(0, 1) == "#" && Int32.TryParse(groupName.Substring(1), out index) && index > 0 && index <= GroupCache[sender].Count)
                group = permissionsFile.Permissions.Groups.FirstOrDefault(g => g.GroupName.Equals(GroupCache[sender][index - 1].GroupName, StringComparison.InvariantCultureIgnoreCase));

            if (string.IsNullOrEmpty(group.GroupName))
                return false;

            return true;
        }

        #endregion

        #endregion

        #region utils

        /// <summary>
        /// Determines if the client is an admin.
        /// </summary>
        /// <param name="steamId">The Steamid of the client.</param>
        /// <returns>True if the client is a server admin, false if it is not .</returns>
        public static bool IsServerAdmin(ulong steamId)
        {
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, p => p != null);
            IMyPlayer player = players.FirstOrDefault(p => p.SteamUserId == steamId);

            if (player == null)
                return false;

            if (ServerIsClient)
                return player.IsHost();
            else
                return MyAPIGateway.Utilities.ConfigDedicated.Administrators.Contains(player.SteamUserId.ToString());
        }
        #endregion
    }

    #region XMLStructs
    /// <summary>
    /// Contains the settings from the file.
    /// </summary>
    //must be a class otherwise we can't define a ctor without parameters
    public class ServerConfigurationStruct
    {
        public string WorldLocation;

        /// <summary>
        /// The suffix for the motd file. For a better identification.
        /// </summary>
        public string MotdFileSuffix;
        public string MotdHeadLine;
        public bool MotdShowInChat;
        public bool LogPrivateMessages;
        [XmlArray("ForceBannedPlayers")]
        [XmlArrayItem("BannedPlayer")]
        public List<Player> ForceBannedPlayers;
        public uint AdminLevel;
        public bool EnableLog;
        public bool NoGrindIndestructible;

        public ServerConfigurationStruct()
        {
            //init default values
            WorldLocation = MyAPIGateway.Session.CurrentPath;
            MotdFileSuffix = MyAPIGateway.Session.Name.ReplaceForbiddenChars();
            MotdHeadLine = "";
            MotdShowInChat = false;
            LogPrivateMessages = true;
            ForceBannedPlayers = new List<Player>();
            AdminLevel = ChatCommandSecurity.Admin;
            EnableLog = false;
            NoGrindIndestructible = false;
        }

        public void Show()
        {
            StringBuilder description = new StringBuilder();
            description.AppendFormat(@"Settings:

  motd headline: {0}
  motd show in chat: {1}
  log private messages: {2}
  admin level: {3}
  enable log: {4}
  no grind indestructible: {5}", MotdHeadLine, MotdShowInChat, LogPrivateMessages, AdminLevel, EnableLog, NoGrindIndestructible);


            MyAPIGateway.Utilities.ShowMissionScreen("Server Config", "", null, description.ToString());
        }
    }

    public struct Player
    {
        public ulong SteamId;
        public string PlayerName;
    }

    public struct PrivateConversation
    {
        public List<Player> Participants;
        public List<PrivateMessage> Messages;
    }

    public struct PrivateMessage
    {
        public ulong Sender;
        public ulong Receiver;
        public DateTime Date;
        [XmlElement("Message")]
        public string Text;
    }

    public struct ChatMessage
    {
        public Player Sender;
        public DateTime Date;
        [XmlElement("Message")]
        public string Text;
    }

    public struct Permissions
    {
        public List<CommandStruct> Commands;
        public List<PermissionGroup> Groups;
        public List<PlayerPermission> Players;
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
    #endregion
}
