using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace midspace.adminscripts
{
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
        /// True for listen server
        /// </summary>
        public bool ServerIsClient = true;

        public List<BannedPlayer> ForceBannedPlayer { get { return Config.ForceBannedPlayers; } }

        public ServerConfig(List<ChatCommand> commands)
        {
            ChatCommands = commands;

            if (MyAPIGateway.Utilities.IsDedicated)
                ServerIsClient = false;

            Config = new ServerConfigurationStruct();

            //cfg
            ConfigFileName = string.Format(ConfigFileNameFormat, Path.GetFileNameWithoutExtension(MyAPIGateway.Session.CurrentPath));
            LoadOrCreateConfig();
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
            Logger.Debug("Config loaded.");
        }

        public uint AdminLevel { get { return Config.AdminLevel; } set { Config.AdminLevel = value; } }

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

            SaveGlobalChatLog();

            if (Config.LogPrivateMessages)
                SavePmLog();
            Logger.Debug("Config saved.");
        }

        public void Load()
        {
            LoadConfig();
            LoadMotd();

            //send changes to clients
            var data = new Dictionary<string, string>();
            data.Add(ConnectionHelper.ConnectionKeys.MessageOfTheDay, CommandMessageOfTheDay.Content);
            data.Add(ConnectionHelper.ConnectionKeys.MotdHeadLine, CommandMessageOfTheDay.HeadLine);
            data.Add(ConnectionHelper.ConnectionKeys.MotdShowInChat, CommandMessageOfTheDay.ShowInChat.ToString());
            data.Add(ConnectionHelper.ConnectionKeys.LogPrivateMessages, CommandPrivateMessage.LogPrivateMessages.ToString());

            ConnectionHelper.SendMessageToAllPlayers(data);
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
                ChatCommandLogic.Instance.AdminNotification = string.Format(@"There is an error in the config file. It couldn't be read. The server was started with default settings.

Message:
{0}

If you can't find the error, simply delete the file. The server will create a new one with default settings on restart.", ex.Message);
            }

            CommandMessageOfTheDay.HeadLine = Config.MotdHeadLine;
            CommandMessageOfTheDay.ShowInChat = Config.MotdShowInChat;
            CommandPrivateMessage.LogPrivateMessages = Config.LogPrivateMessages;
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

            if (!string.IsNullOrEmpty(text))
                SetMessageOfTheDay(text);
        }

        /// <summary>
        /// Create motd file
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

        public void SetMessageOfTheDay(string motd)
        {
            motd = ReplaceVariables(motd);
            if (motd == null)
                motd = "";
            CommandMessageOfTheDay.Content = motd;
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

        public void LogPrivateMessage(ulong sender, ulong receiver, string message)
        {
            if (!Config.LogPrivateMessages)
                return;

            List<PrivateConversation> senderConversations = PrivateConversations.FindAll(c => c.Participants.Exists(p => p.SteamId == sender));

            var pm = new PrivateMessage()
                {
                    Sender = sender,
                    Receiver = receiver,
                    Date = DateTime.Now,
                    Message = message
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
                var senderPlayer = players.FirstOrDefault(p => p.SteamUserId == sender);
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

        public void LogGlobalMessage(ulong sender, string message)
        {
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, p => p != null && p.SteamUserId == sender);
            IMyPlayer player = players.FirstOrDefault();
            ChatMessages.Add(new ChatMessage()
            {
                Sender = new Player()
                {
                    SteamId = player.SteamUserId,
                    PlayerName = player.DisplayName
                },
                Date = DateTime.Now,
                Message = message
            });
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

        public void SendPermissions(ulong steamId) //TODO update playername if it has changed!
        {
            uint playerLevel = 0;

            playerLevel = GetPlayerLevel(steamId);

            var playerPermissions = new List<CommandStruct>(Permissions.Commands);

            if (Permissions.Players.Any(p => p.Player.SteamId.Equals(steamId)))
            {
                var playerPermission = Permissions.Players.FirstOrDefault(p => p.Player.SteamId.Equals(steamId));
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

                    playerPermissions.Remove(playerPermissions.FirstOrDefault(s => s.Equals(commandName)));
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

                    playerPermissions.Remove(playerPermissions.FirstOrDefault(s => s.Equals(commandName)));
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
            ConnectionHelper.SendMessageToPlayer(steamId, ConnectionHelper.ConnectionKeys.CommandLevel, string.Format("{0}:{1}", commandStruct.Name, commandStruct.NeededLevel));
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

        #region actions

        #region command

        public void UpdateCommandSecurity(string commandName, uint level, ulong sender)
        {
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

            //update security first
            var i = Permissions.Commands.IndexOf(commandStruct);
            commandStruct.NeededLevel = level;
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
                if (playerPermission.Extensions.Any(s => s.Equals(commandName)) || playerPermission.Restrictions.Any(s => s.Equals(commandName)))
                    continue;

                SendPermissionChange(player.SteamUserId, commandStruct);
            }

            ConnectionHelper.SendChatMessage(sender, string.Format("The level of command {0} was set to {1}.", commandName, level));

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

            foreach (CommandStruct command in commands)
            {
                var dict = new Dictionary<string, string>();
                dict.Add(ConnectionHelper.ConnectionKeys.PermEntryName, command.Name);
                dict.Add(ConnectionHelper.ConnectionKeys.PermEntryLevel, command.NeededLevel.ToString());

                if (commands.IndexOf(command) == 0)
                    dict.Add(ConnectionHelper.ConnectionKeys.PermNewHotlist, "");
                if (commands.IndexOf(command) == commands.Count - 1)
                    dict.Add(ConnectionHelper.ConnectionKeys.PermLastEntry, "");

                ConnectionHelper.SendMessageToPlayer(sender, ConnectionHelper.ConnectionKeys.CommandList, ConnectionHelper.ConvertData(dict));
            }
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
                    ConnectionHelper.SendMessageToPlayer(playerPermission.Player.SteamId, ConnectionHelper.ConnectionKeys.CommandLevel, string.Format("{0}:{1}", commandStruct.Name, commandStruct.NeededLevel));
                    ConnectionHelper.SendChatMessage(sender, string.Format("Player {0} has normal access to {1} from now.", playerName, commandName));
                    return;
                }

                Permissions.Players[i].Extensions.Add(commandStruct.Name);
                ConnectionHelper.SendMessageToPlayer(playerPermission.Player.SteamId, ConnectionHelper.ConnectionKeys.CommandLevel, string.Format("{0}:{1}", commandStruct.Name, GetPlayerLevel(playerPermission.Player.SteamId)));
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
                    ConnectionHelper.SendMessageToPlayer(playerPermission.Player.SteamId, ConnectionHelper.ConnectionKeys.CommandLevel, string.Format("{0}:{1}", commandStruct.Name, commandStruct.NeededLevel));
                    ConnectionHelper.SendChatMessage(sender, string.Format("Player {0} has normal access to {1} from now.", playerName, commandName));
                    return;
                }

                Permissions.Players[i].Restrictions.Add(commandStruct.Name);
                ConnectionHelper.SendMessageToPlayer(playerPermission.Player.SteamId, ConnectionHelper.ConnectionKeys.CommandLevel, string.Format("{0}:{1}", commandStruct.Name, GetPlayerLevel(playerPermission.Player.SteamId) + 1));
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
                dict.Add(ConnectionHelper.ConnectionKeys.PermEntryName, player.PlayerName);
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
                    dict.Add(ConnectionHelper.ConnectionKeys.PermNewHotlist, "");
                if (players.IndexOf(player) == players.Count - 1)
                    dict.Add(ConnectionHelper.ConnectionKeys.PermLastEntry, "");

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
                dict.Add(ConnectionHelper.ConnectionKeys.PermEntryName, group.GroupName);
                dict.Add(ConnectionHelper.ConnectionKeys.PermEntryLevel, group.Level.ToString());

                if (group.Members.Count > 0)
                {
                    List<string> memberNames = new List<string>();
                    foreach (ulong steamId in group.Members)
                        memberNames.Add(Permissions.Players.FirstOrDefault(p => p.Player.SteamId == steamId).Player.PlayerName);

                    dict.Add(ConnectionHelper.ConnectionKeys.PermEntryMembers, string.Join(", ", memberNames));
                }

                if (groups.IndexOf(group) == 0)
                    dict.Add(ConnectionHelper.ConnectionKeys.PermNewHotlist, "");
                if (groups.IndexOf(group) == groups.Count - 1)
                    dict.Add(ConnectionHelper.ConnectionKeys.PermLastEntry, "");

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
        /// Replaces the chars from the given string that aren't allowed for a filename with a whitespace.
        /// </summary>
        /// <param name="originalText"></param>
        /// <returns></returns>
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

        public bool IsServerAdmin(ulong steamId)
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
        public List<BannedPlayer> ForceBannedPlayers;
        public uint AdminLevel;

        public ServerConfigurationStruct()
        {
            //init default values
            WorldLocation = MyAPIGateway.Session.CurrentPath;
            MotdFileSuffix = ServerConfig.ReplaceForbiddenChars(MyAPIGateway.Session.Name);
            MotdHeadLine = "";
            MotdShowInChat = false;
            LogPrivateMessages = true;
            ForceBannedPlayers = new List<BannedPlayer>();
            AdminLevel = ChatCommandSecurity.Admin;
        }
    }

    //Need to change this to Player... Don't know how without breaking the downward compatibility because forcebanned players are saved as 'BannedPlayer'.
    //They would not be read in if they are 'Player'
    public struct BannedPlayer
    {
        public ulong SteamId;
        public string PlayerName;
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
        public string Message;
    }

    public struct ChatMessage
    {
        public Player Sender;
        public DateTime Date;
        public string Message;
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
