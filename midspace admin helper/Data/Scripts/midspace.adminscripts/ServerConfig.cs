using midspace.adminscripts.Messages;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Xml.Serialization;

namespace midspace.adminscripts
{
    /// <summary>
    /// Represents the server configuration of the mod.
    /// </summary>
    public class ServerConfig
    {
        /// <summary>
        /// The format of the config file name.
        /// </summary>
        private const string ConfigFileNameFormat = "Config_{0}.cfg";
        private const string MotdFileNameFormat = "Motd_{0}.txt";
        private const string PmLogFileNameFormat = "PrivateMessageLog_{0}.xml";
        private const string GcLogFileNameFormat = "GlobalChatLog_{0}.xml";
        private const string PermissionFileNameFormat = "Permissions_{0}.xml";

        private string ConfigFileName;
        private string MotdFileName;
        private string PmLogFileName;
        private string GcLogFileName;
        private string PermissionFileName;

        private List<ChatCommand> ChatCommands;
        private List<ChatMessage> ChatMessages = new List<ChatMessage>();

        private List<PrivateConversation> PrivateConversations = new List<PrivateConversation>();

        //hotlists
        Dictionary<ulong, List<CommandStruct>> CommandCache = new Dictionary<ulong, List<CommandStruct>>();
        Dictionary<ulong, List<Player>> PlayerCache = new Dictionary<ulong, List<Player>>();
        Dictionary<ulong, List<PermissionGroup>> GroupCache = new Dictionary<ulong, List<PermissionGroup>>();

        /// <summary>
        /// Used for saving and loading things.
        /// </summary>
        private ServerConfigurationStruct Config;
        private Permissions Permissions;

        /// <summary>
        /// True for listen server.
        /// </summary>
        public static bool ServerIsClient = true;

        /// <summary>
        /// Saves the log at the same interval as the session saves...
        /// </summary>
        private Timer LogSaveTimer;

        public ServerConfig(List<ChatCommand> commands)
        {
            ChatCommands = commands;

            if (MyAPIGateway.Utilities.IsDedicated)
                ServerIsClient = false;

            Config = new ServerConfigurationStruct();

            //cfg
            ConfigFileName = string.Format(ConfigFileNameFormat, Path.GetFileNameWithoutExtension(MyAPIGateway.Session.CurrentPath));
            LoadOrCreateConfig();

            if (Config.EnableLog)
            {
                ChatCommandLogic.Instance.Debug = true;
                Logger.Init();
                Logger.Debug("Log Enabled.");
            }

            //motd
            MotdFileName = string.Format(MotdFileNameFormat, Config.MotdFileSuffix);
            LoadOrCreateMotdFile();
            //chat log
            GcLogFileName = string.Format(GcLogFileNameFormat, Path.GetFileNameWithoutExtension(MyAPIGateway.Session.CurrentPath));
            LoadOrCreateChatLog();
            //permissions
            PermissionFileName = string.Format(PermissionFileNameFormat, Path.GetFileNameWithoutExtension(MyAPIGateway.Session.CurrentPath));
            LoadOrCreatePermissionFile();
            //pm log
            if (Config.LogPrivateMessages)
            {
                PmLogFileName = string.Format(PmLogFileNameFormat, Path.GetFileNameWithoutExtension(MyAPIGateway.Session.CurrentPath));
                LoadOrCreatePmLog();
            }

            LogSaveTimer = new Timer(MyAPIGateway.Session.AutoSaveInMinutes * 60 * 1000);
            LogSaveTimer.Elapsed += SaveTimer_Elapsed;
            LogSaveTimer.Start();

            Logger.Debug("Config loaded.");
        }

        public uint AdminLevel { get { return Config.AdminLevel; } set { Config.AdminLevel = value; } }

        public List<Player> ForceBannedPlayers { get { return Config.ForceBannedPlayers; } }

        public void Save()
        {
            //write values in cfg
            Config.MotdHeadLine = CommandMessageOfTheDay.HeadLine;
            Config.MotdShowInChat = CommandMessageOfTheDay.ShowInChat;

            //cfg
            Config.WorldLocation = MyAPIGateway.Session.CurrentPath;
            WriteConfig();
            //motd
            SaveMotd();

            SaveLogs();

            Logger.Debug("Config saved.");
        }

        public void Close()
        {
            Save();

            LogSaveTimer.Elapsed -= SaveTimer_Elapsed;
            LogSaveTimer.Close();
        }

        public void ReloadConfig()
        {
            LoadConfig();
            LoadMotd();
        }
        public void SaveLogs()
        {
            SaveGlobalChatLog();

            if (Config.LogPrivateMessages)
                SavePmLog();
            Logger.Debug("Logs saved.");
        }

        void SaveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            SaveLogs();
        }

        #region server config

        private void LoadOrCreateConfig()
        {
            if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(ConfigFileName, typeof(ServerConfig)))
                WriteConfig();
            else
                LoadConfig();
        }

        private void LoadConfig()
        {
            TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(ConfigFileName, typeof(ServerConfig));

            var xmlText = reader.ReadToEnd();
            reader.Close();

            if (string.IsNullOrWhiteSpace(xmlText))
                return;

            try
            {
                Config = MyAPIGateway.Utilities.SerializeFromXML<ServerConfigurationStruct>(xmlText);
            }
            catch (Exception ex)
            {
                AdminNotification notification = new AdminNotification()
                {
                    Date = DateTime.Now,
                    Content = string.Format(@"There is an error in the config file. It couldn't be read. The server was started with default settings.

Message:
{0}

If you can't find the error, simply delete the file. The server will create a new one with default settings on restart.", ex.Message)
                };

                AdminNotificator.StoreAndNotify(notification);
            }

            var sendLogPms = Config.LogPrivateMessages != CommandPrivateMessage.LogPrivateMessages;
            CommandPrivateMessage.LogPrivateMessages = Config.LogPrivateMessages;
            if (sendLogPms)
                ConnectionHelper.SendMessageToAllPlayers(ConnectionHelper.ConnectionKeys.LogPrivateMessages, CommandPrivateMessage.LogPrivateMessages.ToString());
        }

        private void WriteConfig()
        {
            TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(ConfigFileName, typeof(ServerConfig));
            writer.Write(MyAPIGateway.Utilities.SerializeToXML(Config));
            writer.Flush();
            writer.Close();
        }

        #endregion

        #region message of the day

        private void LoadOrCreateMotdFile()
        {
            if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(MotdFileName, typeof(ChatCommandLogic)))
                CreateMotdConfig();

            LoadMotd();
        }

        private void LoadMotd()
        {
            TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(MotdFileName, typeof(ChatCommandLogic));
            var text = reader.ReadToEnd();
            reader.Close();


            var message = new MessageOfTheDayMessage();

            var sendMotd = !Config.MotdHeadLine.Equals(CommandMessageOfTheDay.HeadLine);
            if (sendMotd)
            {
                message.Content = SetMessageOfTheDay(text);
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

        /// <summary>
        /// Create empty motd file.
        /// </summary>
        private void CreateMotdConfig()
        {
            TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(MotdFileName, typeof(ChatCommandLogic));
            writer.Flush();
            writer.Close();
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

        private void SaveMotd()
        {
            var file = string.Format(MotdFileNameFormat, ReplaceForbiddenChars(Config.MotdFileSuffix));

            TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(file, typeof(ChatCommandLogic));
            if (CommandMessageOfTheDay.Content != null)
                writer.Write(CommandMessageOfTheDay.Content);
            else
                writer.Write("");

            writer.Flush();
            writer.Close();
        }

        #endregion

        #region private messages

        private void LoadOrCreatePmLog()
        {
            if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(PmLogFileName, typeof(ServerConfig)))
                return;

            TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(PmLogFileName, typeof(ServerConfig));
            var text = reader.ReadToEnd();
            reader.Close();
            PrivateConversations = MyAPIGateway.Utilities.SerializeFromXML<List<PrivateConversation>>(text);
        }
        
        public void LogPrivateMessage(ChatMessage chatMessage, ulong receiver)
        {
            if (!Config.LogPrivateMessages)
                return;

            List<PrivateConversation> senderConversations = PrivateConversations.FindAll(c => c.Participants.Exists(p => p.SteamId == chatMessage.Sender.SteamId));

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

                PrivateConversations.Add(new PrivateConversation()
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
            //TODO save the log every 5 minutes
        }

        private void SavePmLog()
        {
            TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(PmLogFileName, typeof(ServerConfig));
            writer.Write(MyAPIGateway.Utilities.SerializeToXML<List<PrivateConversation>>(PrivateConversations));
            writer.Flush();
            writer.Close();
            Logger.Debug("Saved private message log.");
        }

        #endregion

        #region global messages

        public void LogGlobalMessage(ChatMessage chatMessage)
        {
            ChatMessages.Add(chatMessage);
        }

        private void LoadOrCreateChatLog()
        {
            if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(GcLogFileName, typeof(ServerConfig)))
                return;

            TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(GcLogFileName, typeof(ServerConfig));
            var text = reader.ReadToEnd();
            reader.Close();

            ChatMessages = MyAPIGateway.Utilities.SerializeFromXML<List<ChatMessage>>(text);
        }

        private void SaveGlobalChatLog()
        {
            TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(GcLogFileName, typeof(ServerConfig));
            writer.Write(MyAPIGateway.Utilities.SerializeToXML<List<ChatMessage>>(ChatMessages));
            writer.Flush();
            writer.Close();
        }

        /// <summary>
        /// Sends the given amount of chat entries to the client to display the chat history.
        /// </summary>
        /// <param name="senderSteamId">The Steamid of the receiving client.</param>
        /// <param name="entryCount">The amount of entries that are requested.</param>
        public void SendChatHistory(ulong receiver, uint entryCount)
        {
            // we just append new chat messages to the log. To get the most recent on top we have to sort it.
            List<ChatMessage> cache = new List<ChatMessage>(ChatMessages.OrderByDescending(m => m.Date));

            // we have to make sure that we don't throw an exception
            int range = (int)entryCount;
            if (cache.Count < entryCount)
                range = cache.Count;
            
            var msgHistory = new MessageChatHistory() {
                ChatHistory = cache.GetRange(0, range)
            };

            ConnectionHelper.SendMessageToPlayer(receiver, msgHistory);
        }

        #endregion

        #region permissions

        private void LoadOrCreatePermissionFile()
        {
            if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(PermissionFileName, typeof(ServerConfig)))
            {
                Permissions = new Permissions()
                {
                    Commands = new List<CommandStruct>(),
                    Groups = new List<PermissionGroup>(),
                    Players = new List<PlayerPermission>()
                };

                foreach (ChatCommand command in ChatCommands)
                {
                    Permissions.Commands.Add(new CommandStruct()
                    {
                        Name = command.Name,
                        NeededLevel = command.Security
                    });
                }

                SavePermissionFile();
                Logger.Debug("Permission File created.");
                return;
            }

            TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(PermissionFileName, typeof(ServerConfig));
            var text = reader.ReadToEnd();
            reader.Close();

            Permissions = MyAPIGateway.Utilities.SerializeFromXML<Permissions>(text);

            //create a copy of the commands in the file
            var invalidCommands = new List<CommandStruct>(Permissions.Commands);

            foreach (ChatCommand command in ChatCommands)
            {
                if (!Permissions.Commands.Any(c => c.Name.Equals(command.Name)))
                {
                    //add a command if it does not exist
                    Permissions.Commands.Add(new CommandStruct()
                    {
                        Name = command.Name,
                        NeededLevel = command.Security
                    });
                }
                else
                {
                    //remove all commands from the list, that are valid
                    invalidCommands.Remove(Permissions.Commands.First(c => c.Name.Equals(command.Name)));
                }
            }

            foreach (CommandStruct cmdStruct in invalidCommands)
            {
                //remove all invalid commands
                Permissions.Commands.Remove(cmdStruct);
            }

            Logger.Debug("Permission File loaded {0} commands.", Permissions.Commands.Count);

            SavePermissionFile();
            return;
        }

        private void SavePermissionFile()
        {
            TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(PermissionFileName, typeof(ServerConfig));
            writer.Write(MyAPIGateway.Utilities.SerializeToXML<Permissions>(Permissions));
            writer.Flush();
            writer.Close();
        }

        public void SendPermissions(ulong steamId)
        {
            uint playerLevel = 0;

            playerLevel = GetPlayerLevel(steamId);

            var playerPermissions = new List<CommandStruct>(Permissions.Commands);

            if (Permissions.Players.Any(p => p.Player.SteamId.Equals(steamId)))
            {
                var playerPermission = Permissions.Players.FirstOrDefault(p => p.Player.SteamId.Equals(steamId));
                
                // create new entry if necessary or update the playername
                IMyPlayer myPlayer;
                if (MyAPIGateway.Players.TryGetPlayer(steamId, out myPlayer) && !playerPermission.Player.PlayerName.Equals(myPlayer.DisplayName))
                {
                    playerPermission = Permissions.Players.FirstOrDefault(p => p.Player.SteamId == myPlayer.SteamUserId);
                    var i = Permissions.Players.IndexOf(playerPermission);
                    playerPermission.Player.PlayerName = myPlayer.DisplayName;
                    Permissions.Players[i] = playerPermission;
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
            {
                SendPermissionChange(steamId, commandStruct);
            }

            ConnectionHelper.SendMessageToPlayer(steamId, ConnectionHelper.ConnectionKeys.PlayerLevel, playerLevel.ToString());
        }

        public void SendPermissionChange(ulong steamId, CommandStruct commandStruct)
        {
            var message = new MessageCommandPermissions()
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

            if (Permissions.Players.Any(p => p.Player.SteamId == steamId && p.UsePlayerLevel))
            {
                playerLevel = Permissions.Players.FirstOrDefault(p => p.Player.SteamId == steamId).Level;
            }
            else if (Permissions.Groups.Any(g => g.Members.Any(l => l == steamId)))
            {
                uint highestLevel = 0;
                foreach (PermissionGroup group in Permissions.Groups.Where(g => g.Members.Any(l => l == steamId))) 
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

                if (!Permissions.Players.Any(p => p.Player.SteamId == player.SteamUserId) || (!Permissions.Players.FirstOrDefault(p => p.Player.SteamId == player.SteamUserId).UsePlayerLevel && !Permissions.Groups.Any(g => g.Members.Contains(player.SteamUserId))))
                    SendPermissions(player.SteamUserId);
            }
        }

        #region actions

        #region command

        public void UpdateCommandSecurity(CommandStruct command, ulong sender)
        {
            var commandStruct = Permissions.Commands.FirstOrDefault(c => c.Name.Equals(command.Name, StringComparison.InvariantCultureIgnoreCase));

            int index;
            if (CommandCache.ContainsKey(sender) && command.Name.Substring(0, 1) == "#" && Int32.TryParse(command.Name.Substring(1), out index) && index > 0 && index <= CommandCache[sender].Count)
                commandStruct = Permissions.Commands.FirstOrDefault(c => c.Name.Equals(CommandCache[sender][index - 1].Name, StringComparison.InvariantCultureIgnoreCase));

            if (string.IsNullOrEmpty(commandStruct.Name))
            {
                ConnectionHelper.SendChatMessage(sender, string.Format("Command {0} could not be found.", command.Name));
                return;
            }

            command.Name = commandStruct.Name;

            //update security first
            var i = Permissions.Commands.IndexOf(commandStruct);
            commandStruct.NeededLevel = command.NeededLevel;
            Permissions.Commands[i] = commandStruct;

            //then send changes
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, p => p != null);
            foreach (IMyPlayer player in players)
            {
                var playerPermission = Permissions.Players.FirstOrDefault(p => p.Player.SteamId == player.SteamUserId);

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

            ConnectionHelper.SendChatMessage(sender, string.Format("The level of command {0} was set to {1}.", commandStruct.Name, commandStruct.NeededLevel));

            SavePermissionFile();
        }

        public void CreateCommandHotlist(ulong sender, string param = null)
        {
            List<CommandStruct> commands = new List<CommandStruct>(Permissions.Commands);

            if (!string.IsNullOrEmpty(param))
            {
                commands = new List<CommandStruct>(Permissions.Commands.Where(c => c.Name.IndexOf(param, StringComparison.InvariantCultureIgnoreCase) >= 0));

                if (commands.Count == 0)
                {
                    ConnectionHelper.SendChatMessage(sender, string.Format("No command matching with {0} could be found.", param));
                    return;
                }
            }

            if (!CommandCache.ContainsKey(sender))
                CommandCache.Add(sender, commands);
            else
                CommandCache[sender] = commands;

            var message = new MessageCommandPermissions()
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
                var i = Permissions.Players.IndexOf(player);
                player.Level = level;
                Permissions.Players[i] = player;

                //send changes to player
                SendPermissions(player.Player.SteamId);
            }
            else
            {
                ConnectionHelper.SendChatMessage(sender, string.Format("Player {0} could not be found.", playerName));
                return;
            }

            ConnectionHelper.SendChatMessage(sender, string.Format("{0}'s level was set to {1}.", playerName, level));

            SavePermissionFile();
        }

        public void ExtendRights(string playerName, string commandName, ulong sender)
        {
            PlayerPermission playerPermission;
            if (TryGetPlayerPermission(playerName, out playerPermission, sender))
            {
                playerName = playerPermission.Player.PlayerName;

                var commandStruct = Permissions.Commands.FirstOrDefault(c => c.Name.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));

                int index;
                if (CommandCache.ContainsKey(sender) && commandName.Substring(0, 1) == "#" && Int32.TryParse(commandName.Substring(1), out index) && index > 0 && index <= CommandCache[sender].Count)
                    commandStruct = Permissions.Commands.FirstOrDefault(c => c.Name.Equals(CommandCache[sender][index - 1].Name, StringComparison.InvariantCultureIgnoreCase));

                if (string.IsNullOrEmpty(commandStruct.Name))
                {
                    ConnectionHelper.SendChatMessage(sender, string.Format("Command {0} could not be found.", commandName));
                    return;
                }

                commandName = commandStruct.Name;
                var i = Permissions.Players.IndexOf(playerPermission);

                if (Permissions.Players[i].Extensions.Any(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    ConnectionHelper.SendChatMessage(sender, string.Format("Player {0} already has extended access to {1}.", playerName, commandName));
                    return;
                }

                if (Permissions.Players[i].Restrictions.Any(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    var command = Permissions.Players[i].Restrictions.FirstOrDefault(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));
                    Permissions.Players[i].Restrictions.Remove(command);
                    SendPermissionChange(playerPermission.Player.SteamId, commandStruct);
                    ConnectionHelper.SendChatMessage(sender, string.Format("Player {0} has normal access to {1} from now.", playerName, commandName));
                    return;
                }

                Permissions.Players[i].Extensions.Add(commandStruct.Name);

                SendPermissionChange(playerPermission.Player.SteamId, new CommandStruct() 
                { 
                    Name = commandStruct.Name, 
                    NeededLevel = GetPlayerLevel(playerPermission.Player.SteamId) 
                });
                ConnectionHelper.SendChatMessage(sender, string.Format("Player {0} has extended access to {1} from now.", playerName, commandName));
            }
            else
            {
                ConnectionHelper.SendChatMessage(sender, string.Format("Player {0} could not be found.", playerName));
                return;
            }

            SavePermissionFile();
        }

        public void RestrictRights(string playerName, string commandName, ulong sender)
        {
            PlayerPermission playerPermission;
            if (TryGetPlayerPermission(playerName, out playerPermission, sender))
            {
                playerName = playerPermission.Player.PlayerName;

                var commandStruct = Permissions.Commands.FirstOrDefault(c => c.Name.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));

                int index;
                if (CommandCache.ContainsKey(sender) && commandName.Substring(0, 1) == "#" && Int32.TryParse(commandName.Substring(1), out index) && index > 0 && index <= CommandCache[sender].Count)
                    commandStruct = Permissions.Commands.FirstOrDefault(c => c.Name.Equals(CommandCache[sender][index - 1].Name, StringComparison.InvariantCultureIgnoreCase));

                if (string.IsNullOrEmpty(commandStruct.Name))
                {
                    ConnectionHelper.SendChatMessage(sender, string.Format("Command {0} could not be found.", commandName));
                    return;
                }

                commandName = commandStruct.Name;
                var i = Permissions.Players.IndexOf(playerPermission);

                if (Permissions.Players[i].Restrictions.Any(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    ConnectionHelper.SendChatMessage(sender, string.Format("Player {0} already has restricted access to {1}.", playerName, commandName));
                    return;
                }

                if (Permissions.Players[i].Extensions.Any(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    var command = Permissions.Players[i].Extensions.FirstOrDefault(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));
                    Permissions.Players[i].Extensions.Remove(command);
                    SendPermissionChange(playerPermission.Player.SteamId, commandStruct);
                    ConnectionHelper.SendChatMessage(sender, string.Format("Player {0} has normal access to {1} from now.", playerName, commandName));
                    return;
                }

                Permissions.Players[i].Restrictions.Add(commandStruct.Name);

                SendPermissionChange(playerPermission.Player.SteamId, new CommandStruct()
                {
                    Name = commandStruct.Name,
                    NeededLevel = GetPlayerLevel(playerPermission.Player.SteamId) + 1
                });
                ConnectionHelper.SendChatMessage(sender, string.Format("Player {0} has no access to {1} from now.", playerName, commandName));
            }
            else
            {
                ConnectionHelper.SendChatMessage(sender, string.Format("Player {0} could not be found.", playerName));
                return;
            }

            SavePermissionFile();
        }

        public void UsePlayerLevel(string playerName, bool usePlayerLevel, ulong sender)
        {
            PlayerPermission playerPermission;
            if (TryGetPlayerPermission(playerName, out playerPermission, sender))
            {
                playerName = playerPermission.Player.PlayerName;

                var i = Permissions.Players.IndexOf(playerPermission);
                playerPermission.UsePlayerLevel = usePlayerLevel;
                Permissions.Players[i] = playerPermission;

                SendPermissions(playerPermission.Player.SteamId);

                ConnectionHelper.SendChatMessage(sender, string.Format("{0} uses the {1} level now. Current level: {2}", playerName, usePlayerLevel ? "player" : "group", GetPlayerLevel(playerPermission.Player.SteamId)));
            }
            else
            {
                ConnectionHelper.SendChatMessage(sender, string.Format("Player {0} could not be found.", playerName));
                return;
            }

            SavePermissionFile();
        }

        public void CreatePlayerHotlist(ulong sender, string param)
        {
            List<Player> players = new List<Player>();

            var onlinePlayers = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(onlinePlayers, p => p != null);

            if (onlinePlayers.Count == 0 && Permissions.Players.Count == 0)
            {
                ConnectionHelper.SendChatMessage(sender, "No players found.");
                return;
            }

            foreach (IMyPlayer player in onlinePlayers)
                players.Add(new Player()
                {
                    PlayerName = player.DisplayName,
                    SteamId = player.SteamUserId
                });

            foreach (PlayerPermission playerPermission in Permissions.Players)
            {
                if (!players.Contains(playerPermission.Player))
                    players.Add(playerPermission.Player);
            }

            if (!string.IsNullOrEmpty(param))
            {
                players = new List<Player>(players.Where(p => p.PlayerName.IndexOf(param, StringComparison.InvariantCultureIgnoreCase) >= 0));

                if (players.Count == 0)
                {
                    ConnectionHelper.SendChatMessage(sender, string.Format("No player matching with {0} could be found.", param));
                    return;
                }
            }

            if (!PlayerCache.ContainsKey(sender))
                PlayerCache.Add(sender, players);
            else
                PlayerCache[sender] = players;

            foreach (Player player in players.OrderBy(p => p.PlayerName))
            {
                var dict = new Dictionary<string, string>();
                dict.Add(ConnectionHelper.ConnectionKeys.ListEntry, player.PlayerName);
                dict.Add(ConnectionHelper.ConnectionKeys.PermEntryLevel, GetPlayerLevel(player.SteamId).ToString());
                dict.Add(ConnectionHelper.ConnectionKeys.PermEntryId, player.SteamId.ToString());

                if (Permissions.Players.Any(p => p.Player.SteamId == player.SteamId))
                {
                    var playerPermission = Permissions.Players.FirstOrDefault(p => p.Player.SteamId == player.SteamId);
                    dict.Add(ConnectionHelper.ConnectionKeys.PermEntryExtensions, string.Join(", ", playerPermission.Extensions));
                    dict.Add(ConnectionHelper.ConnectionKeys.PermEntryRestrictions, string.Join(", ", playerPermission.Restrictions));
                    if (playerPermission.UsePlayerLevel)
                        dict.Add(ConnectionHelper.ConnectionKeys.PermEntryUsePlayerLevel, "");
                }

                if (players.IndexOf(player) == 0)
                    dict.Add(ConnectionHelper.ConnectionKeys.NewList, "");
                if (players.IndexOf(player) == players.Count - 1)
                    dict.Add(ConnectionHelper.ConnectionKeys.ListLastEntry, "");

                ConnectionHelper.SendMessageToPlayer(sender, ConnectionHelper.ConnectionKeys.PlayerList, ConnectionHelper.ConvertData(dict));
            }
        }

        #endregion

        #region group

        public void CreateGroup(string name, uint level, ulong sender)
        {
            if (Permissions.Groups.Any(g => g.GroupName.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                ConnectionHelper.SendChatMessage(sender, string.Format("There is already a group named {0}.", name));
                return;
            }

            Permissions.Groups.Add(new PermissionGroup() {
                GroupName = name,
                Level = level,
                Members = new List<ulong>(),
            });

            ConnectionHelper.SendChatMessage(sender, string.Format("Group {0} with level {1} was created.", name, level));

            SavePermissionFile();
        }

        public void SetGroupLevel(string groupName, uint level, ulong sender)
        {
            PermissionGroup group;
            if (TryGetGroup(groupName, out group, sender))
            {
                groupName = group.GroupName;

                var i = Permissions.Groups.IndexOf(group);
                group.Level = level;
                Permissions.Groups[i] = group;

                foreach (ulong steamId in group.Members)
                {
                    SendPermissions(steamId);
                }

                ConnectionHelper.SendChatMessage(sender, string.Format("The level of group {0} was updated to {1}.", groupName, level));
            }
            else
            {
                ConnectionHelper.SendChatMessage(sender, string.Format("Group {0} could not be found.", groupName));
                return;
            }

            SavePermissionFile();
        }

        public void SetGroupName(string groupName, string newName, ulong sender)
        {
            PermissionGroup group;
            if (TryGetGroup(groupName, out group, sender))
            {
                if (Permissions.Groups.Any(g => g.GroupName.Equals(newName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    ConnectionHelper.SendChatMessage(sender, string.Format("There is already a group named {0}.", newName));
                    return;
                }

                groupName = group.GroupName;

                var i = Permissions.Groups.IndexOf(group);
                group.GroupName = newName;
                Permissions.Groups[i] = group;

                ConnectionHelper.SendChatMessage(sender, string.Format("Group {0} was renamed to {1}.", groupName, newName));
            }
            else
            {
                ConnectionHelper.SendChatMessage(sender, string.Format("Group {0} could not be found.", groupName));
                return;
            }

            SavePermissionFile();
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
                        ConnectionHelper.SendChatMessage(sender, string.Format("Player {0} is already a member of group {1}.", playerName, groupName));
                        return;
                    }

                    var i = Permissions.Groups.IndexOf(group);
                    group.Members.Add(playerPermission.Player.SteamId);
                    Permissions.Groups[i] = group;

                    SendPermissions(playerPermission.Player.SteamId);
                }
                else
                {
                    ConnectionHelper.SendChatMessage(sender, string.Format("Player {0} could not be found.", playerName));
                    return;
                }

                ConnectionHelper.SendChatMessage(sender, string.Format("Added player {0} to group {1}.", playerName, groupName));
            }
            else
            {
                ConnectionHelper.SendChatMessage(sender, string.Format("Group {0} could not be found.", groupName));
                return;
            }

            SavePermissionFile();
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
                        ConnectionHelper.SendChatMessage(sender, string.Format("Player {0} is not a member of group {1}.", playerName, groupName));
                        return;
                    }

                    var i = Permissions.Groups.IndexOf(group);
                    group.Members.Remove(playerPermission.Player.SteamId);
                    Permissions.Groups[i] = group;

                    SendPermissions(playerPermission.Player.SteamId);
                }
                else
                {
                    ConnectionHelper.SendChatMessage(sender, string.Format("Player {0} could not be found.", playerName));
                    return;
                }

                ConnectionHelper.SendChatMessage(sender, string.Format("Removed player {0} from group {1}.", playerName, groupName));
            }
            else
            {
                ConnectionHelper.SendChatMessage(sender, string.Format("Group {0} could not be found.", groupName));
                return;
            }

            SavePermissionFile();
        }

        public void DeleteGroup(string groupName, ulong sender)
        {
            PermissionGroup group;
            if (TryGetGroup(groupName, out group, sender))
            {
                groupName = group.GroupName;
                Permissions.Groups.Remove(group);

                foreach (ulong steamId in group.Members)
                {
                    SendPermissions(steamId);
                }

                ConnectionHelper.SendChatMessage(sender, string.Format("Group {0} has been deleted.", groupName));
            }
            else
            {
                ConnectionHelper.SendChatMessage(sender, string.Format("Group {0} could not be found.", groupName));
                return;
            }

            SavePermissionFile();
        }

        public void CreateGroupHotlist(ulong sender, string param = null)
        {
            if (Permissions.Groups.Count == 0)
            {
                ConnectionHelper.SendChatMessage(sender, "No groups found.");
                return;
            }

            List<PermissionGroup> groups = new List<PermissionGroup>(Permissions.Groups);

            if (!string.IsNullOrEmpty(param))
            {
                groups = new List<PermissionGroup>(Permissions.Groups.Where(g => g.GroupName.IndexOf(param, StringComparison.InvariantCultureIgnoreCase) >= 0));

                if (groups.Count == 0)
                {
                    ConnectionHelper.SendChatMessage(sender, string.Format("No group matching with {0} could be found.", param));
                    return;
                }
            }

            if (!GroupCache.ContainsKey(sender))
                GroupCache.Add(sender, groups);
            else
                GroupCache[sender] = groups;

            foreach (PermissionGroup group in groups.OrderBy(g => g.GroupName))
            {
                var dict = new Dictionary<string, string>();
                dict.Add(ConnectionHelper.ConnectionKeys.ListEntry, group.GroupName);
                dict.Add(ConnectionHelper.ConnectionKeys.PermEntryLevel, group.Level.ToString());

                if (group.Members.Count > 0)
                {
                    List<string> memberNames = new List<string>();
                    foreach (ulong steamId in group.Members)
                        memberNames.Add(Permissions.Players.FirstOrDefault(p => p.Player.SteamId == steamId).Player.PlayerName);

                    dict.Add(ConnectionHelper.ConnectionKeys.PermEntryMembers, string.Join(", ", memberNames));
                }

                if (groups.IndexOf(group) == 0)
                    dict.Add(ConnectionHelper.ConnectionKeys.NewList, "");
                if (groups.IndexOf(group) == groups.Count - 1)
                    dict.Add(ConnectionHelper.ConnectionKeys.ListLastEntry, "");

                ConnectionHelper.SendMessageToPlayer(sender, ConnectionHelper.ConnectionKeys.GroupList, ConnectionHelper.ConvertData(dict));
            }
        }

        #endregion

        public bool TryGetPlayerPermission(string playerName, out PlayerPermission playerPermission, ulong sender)
        {
            playerPermission = new PlayerPermission();

            int index;
            if (PlayerCache.ContainsKey(sender) && playerName.Substring(0, 1) == "#" && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= PlayerCache[sender].Count)
                playerName = PlayerCache[sender][index - 1].PlayerName;

            if (!Permissions.Players.Any(p => p.Player.PlayerName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase)))
            {
                IMyPlayer myPlayer;
                if (MyAPIGateway.Players.TryGetPlayer(playerName, out myPlayer))
                {
                    if (Permissions.Players.Any(p => p.Player.SteamId == myPlayer.SteamUserId))
                    {
                        playerPermission = Permissions.Players.FirstOrDefault(p => p.Player.SteamId == myPlayer.SteamUserId);
                        var i = Permissions.Players.IndexOf(playerPermission);
                        playerPermission.Player.PlayerName = myPlayer.DisplayName;
                        Permissions.Players[i] = playerPermission;
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
                        Permissions.Players.Add(playerPermission);
                    }
                }
                else
                    return false;
            }
            else
                playerPermission = Permissions.Players.FirstOrDefault(p => p.Player.PlayerName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase));
            
            return true;
        }

        public bool TryGetGroup(string groupName, out PermissionGroup group, ulong sender)
        {
            group = Permissions.Groups.FirstOrDefault(g => g.GroupName.Equals(groupName, StringComparison.InvariantCultureIgnoreCase));

            int index;
            if (GroupCache.ContainsKey(sender) && groupName.Substring(0, 1) == "#" && Int32.TryParse(groupName.Substring(1), out index) && index > 0 && index <= GroupCache[sender].Count)
                group = Permissions.Groups.FirstOrDefault(g => g.GroupName.Equals(GroupCache[sender][index - 1].GroupName, StringComparison.InvariantCultureIgnoreCase));

            if (string.IsNullOrEmpty(group.GroupName))
                return false;

            return true;
        }

        #endregion

        #endregion

        #region utils
        /// <summary>
        /// Replaces the chars from the given string that are not allowed for filenames with a whitespace.
        /// </summary>
        /// <param name="originalText">The text containing characters that shall not be used in filenames.</param>
        /// <returns>A string where the characters are replaced with a whitespace.</returns>
        public static string ReplaceForbiddenChars(string originalText)
        {
            if (string.IsNullOrWhiteSpace(originalText))
                return originalText;

            //could be done in one single line but like this we have a better overview
            var convertedText = originalText.Replace('\\', ' ');
            convertedText = convertedText.Replace('/', ' ');
            convertedText = convertedText.Replace(':', ' ');
            convertedText = convertedText.Replace('*', ' ');
            convertedText = convertedText.Replace('?', ' ');
            convertedText = convertedText.Replace('"', ' ');
            convertedText = convertedText.Replace('<', ' ');
            convertedText = convertedText.Replace('>', ' ');
            convertedText = convertedText.Replace('|', ' ');

            return convertedText;
        }

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

        public ServerConfigurationStruct()
        {
            //init default values
            WorldLocation = MyAPIGateway.Session.CurrentPath;
            MotdFileSuffix = ServerConfig.ReplaceForbiddenChars(MyAPIGateway.Session.Name);
            MotdHeadLine = "";
            MotdShowInChat = false;
            LogPrivateMessages = true;
            ForceBannedPlayers = new List<Player>();
            AdminLevel = ChatCommandSecurity.Admin;
            EnableLog = false;
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
