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

        private string ConfigFileName;
        private string MotdFileName;
        private string PmLogFileName;
        private string GcLogFileName;

        private TextWriter GlobalChatLogger;
        private List<ChatMessage> ChatMessages = new List<ChatMessage>();

        private List<PrivateConversation> PrivateConversations = new List<PrivateConversation>();

        /// <summary>
        /// Used for saving and loading things.
        /// </summary>
        private ServerConfigurationStruct Config;

        /// <summary>
        /// True for listen server
        /// </summary>
        public bool ServerIsClient = true;

        public List<BannedPlayer> ForceBannedPlayer { get { return Config.ForceBannedPlayers; } }

        public ServerConfig()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                ServerIsClient = false;

            Config = new ServerConfigurationStruct()
            {
                //init default values
                WorldLocation = MyAPIGateway.Session.CurrentPath,
                MotdFileSuffix = ReplaceForbiddenChars(MyAPIGateway.Session.Name),
                MotdHeadLine = "",
                MotdShowInChat = false,
                LogPrivateMessages = true,
                ForceBannedPlayers = new List<BannedPlayer>(),
            };
            ConfigFileName = string.Format(ConfigFileNameFormat, MyAPIGateway.Session.WorldID);
            LoadOrCreateConfig();
            MotdFileName = string.Format(MotdFileNameFormat, Config.MotdFileSuffix);
            LoadOrCreateMotdFile();
            GcLogFileName = string.Format(GcLogFileNameFormat, MyAPIGateway.Session.WorldID);
            LoadOrCreateChatLog();
            if (Config.LogPrivateMessages)
            {
                PmLogFileName = string.Format(PmLogFileNameFormat, MyAPIGateway.Session.WorldID);
                LoadOrCreatePmLog();
            }
            Logger.Debug("Config loaded.");
        }

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
            //data.Add(ConnectionHelper.ConnectionKeys.PmLogging, CommandPrivateMessage.PmLogging.ToString());

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
                MyAPIGateway.Players.GetPlayers(players, p => p!= null);
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
    }

    /// <summary>
    /// Contains the settings from the file.
    /// </summary>
    public struct ServerConfigurationStruct
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
        /// <summary>
        /// The permissions in string form. No need to initialize the permissions on the server since it is transmitted as a string anyway.
        /// </summary>
        //public string CommandPermissions = "";
    }

    public struct OldServerConfigurationStruct
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
        /// <summary>
        /// The permissions in string form. No need to initialize the permissions on the server since it is transmitted as a string anyway.
        /// </summary>
        //public string CommandPermissions = "";
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
}
