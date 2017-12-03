namespace midspace.adminscripts
{
    using midspace.adminscripts.Config;
    using midspace.adminscripts.Config.Files;
    using midspace.adminscripts.Messages;
    using midspace.adminscripts.Protection;
    using ProtoBuf;
    using Sandbox.ModAPI;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using VRage.Game;
    using VRage.Game.ModAPI;

    /// <summary>
    /// Represents the server configuration of the mod.
    /// </summary>
    public class ServerConfig
    {
        /// <summary>
        /// Used for saving and loading things.
        /// </summary>
        public ServerConfigurationStruct Config { get { return _serverConfigFile.Config; } private set { _serverConfigFile.Config = value; } }

        /// <summary>
        /// True for listen server.
        /// </summary>
        public static bool ServerIsClient = true;

        private MotdFile _motdFile;
        private ServerConfigFile _serverConfigFile;
        private GlobalChatLogFile _globalChatLogFile;
        private PrivateMessageLogFile _privateMessageLogFile;
        private PermissionsFile _permissionsFile;

        private bool _registeredIndestructibleDamageHandler;

        public ServerConfig(List<ChatCommand> commands)
        {
            string pathName = Path.GetFileNameWithoutExtension(MyAPIGateway.Session.CurrentPath);

            if (MyAPIGateway.Utilities.IsDedicated)
                ServerIsClient = false;

            //cfg
            _serverConfigFile = new ServerConfigFile(pathName);

            if (Config.EnableLog)
            {
                ChatCommandLogic.Instance.Debug = true;
                Logger.Init();
                Logger.Debug("Log Enabled.");
            }

            _motdFile = new MotdFile(Config.MotdFileSuffix);
            SendMotd();

            //chat log
            _globalChatLogFile = new GlobalChatLogFile(pathName);

            //permissions
            _permissionsFile = new PermissionsFile(pathName, commands);

            //pm log
            if (Config.LogPrivateMessages)
            {
                _privateMessageLogFile = new PrivateMessageLogFile(pathName);
            }

            if (Config.NoGrindIndestructible)
            {
                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, IndestructibleDamageHandler);
                _registeredIndestructibleDamageHandler = true;
                Logger.Debug("Registered indestructible damage handler.");
            }

            Logger.Debug("Config loaded.");
        }

        public void Save(string customSaveName = null)
        {
            //write values into cfgFile
            Config.MotdHeadLine = CommandMessageOfTheDay.HeadLine;
            Config.MotdShowInChat = CommandMessageOfTheDay.ShowInChat;

            //cfg
            Config.WorldLocation = MyAPIGateway.Session.CurrentPath;
            _serverConfigFile.Save(customSaveName);

            //motd
            _motdFile.Save();

            SaveLogs(customSaveName);

            if (customSaveName != null)
                _permissionsFile.Save(customSaveName);

            ProtectionHandler.Save(customSaveName);
            Logger.Debug("Config saved.");
        }

        public void ReloadConfig()
        {
            _serverConfigFile.Load();
            _motdFile.Load();
        }

        public void SaveLogs(string customSaveName = null)
        {
            _globalChatLogFile.Save(customSaveName);

            if (Config.LogPrivateMessages)
                _privateMessageLogFile.Save(customSaveName);

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

            if (noGrindIndestructible && !_registeredIndestructibleDamageHandler)
            {
                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, IndestructibleDamageHandler);
                _registeredIndestructibleDamageHandler = true;
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
                message.Content = SetMessageOfTheDay(_motdFile.MessageOfTheDay);
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
            CommandMessageOfTheDay.Content = motd;

            return motd;
        }

        #endregion

        #region private messages

        public void LogPrivateMessage(ChatMessage chatMessage, ulong receiver)
        {
            if (!Config.LogPrivateMessages)
                return;

            List<PrivateConversation> senderConversations = _privateMessageLogFile.PrivateConversations.FindAll(c => c.Participants.Exists(p => p.SteamId == chatMessage.Sender.SteamId));

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

                _privateMessageLogFile.PrivateConversations.Add(new PrivateConversation()
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
            _globalChatLogFile.ChatMessages.Add(chatMessage);
        }

        /// <summary>
        /// Sends the given amount of chat entries to the client to display the chat history.
        /// </summary>
        /// <param name="receiver">The Steamid of the receiving client.</param>
        /// <param name="entryCount">The amount of entries that are requested.</param>
        public void SendChatHistory(ulong receiver, uint entryCount)
        {
            // we just append new chat messages to the log. To get the most recent on top we have to sort it.
            List<ChatMessage> cache = new List<ChatMessage>(_globalChatLogFile.ChatMessages.OrderByDescending(m => m.Date));

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
            _permissionsFile.Permissions.SendPermissions(steamId);
        }

        public void UpdateAdminLevel(uint adminLevel)
        {
            _permissionsFile.Permissions.UpdateAdminLevel(adminLevel);
        }

        #region actions

        #region command

        public void UpdateCommandSecurity(CommandStruct command, ulong sender)
        {
            _permissionsFile.Permissions.UpdateCommandSecurity(command, sender);
            _permissionsFile.Save();
        }

        public void CreateCommandHotlist(ulong sender, string param = null)
        {
            _permissionsFile.Permissions.CreateCommandHotlist(sender, param);
        }

        #endregion

        #region player

        public void SetPlayerLevel(string playerName, uint level, ulong sender)
        {
            _permissionsFile.Permissions.SetPlayerLevel(playerName, level, sender);
            _permissionsFile.Save();
        }

        public void ExtendRights(string playerName, string commandName, ulong sender)
        {
            _permissionsFile.Permissions.ExtendRights(playerName, commandName, sender);
            _permissionsFile.Save();
        }

        public void RestrictRights(string playerName, string commandName, ulong sender)
        {
            _permissionsFile.Permissions.RestrictRights(playerName, commandName, sender);
            _permissionsFile.Save();
        }

        public void UsePlayerLevel(string playerName, bool usePlayerLevel, ulong sender)
        {
            _permissionsFile.Permissions.UsePlayerLevel(playerName, usePlayerLevel, sender);
            _permissionsFile.Save();
        }

        public void CreatePlayerHotlist(ulong sender, string param)
        {
            _permissionsFile.Permissions.CreatePlayerHotlist(sender, param);
        }

        #endregion

        #region group

        public void CreateGroup(string name, uint level, ulong sender)
        {
            _permissionsFile.Permissions.CreateGroup(name, level, sender);
            _permissionsFile.Save();
        }

        public void SetGroupLevel(string groupName, uint level, ulong sender)
        {
            _permissionsFile.Permissions.SetGroupLevel(groupName, level, sender);
            _permissionsFile.Save();
        }

        public void SetGroupName(string groupName, string newName, ulong sender)
        {
            _permissionsFile.Permissions.SetGroupName(groupName, newName, sender);
            _permissionsFile.Save();
        }

        public void AddPlayerToGroup(string groupName, string playerName, ulong sender)
        {
            _permissionsFile.Permissions.AddPlayerToGroup(groupName, playerName, sender);
            _permissionsFile.Save();
        }

        public void RemovePlayerFromGroup(string groupName, string playerName, ulong sender)
        {
            _permissionsFile.Permissions.RemovePlayerFromGroup(groupName, playerName, sender);
            _permissionsFile.Save();
        }

        public void DeleteGroup(string groupName, ulong sender)
        {
            _permissionsFile.Permissions.DeleteGroup(groupName, sender);
            _permissionsFile.Save();
        }

        public void CreateGroupHotlist(ulong sender, string param = null)
        {
            _permissionsFile.Permissions.CreateGroupHotlist(sender, param);
        }

        #endregion
        
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
    [ProtoContract]
    public class ServerConfigurationStruct
    {
        public string WorldLocation;

        /// <summary>
        /// The suffix for the motd file. For a better identification.
        /// </summary>
        [ProtoMember(1)]
        public string MotdFileSuffix;

        [ProtoMember(2)]
        [DefaultValue("")]
        public string MotdHeadLine { get; set; } = "";

        [ProtoMember(3)]
        [DefaultValue(false)]
        public bool MotdShowInChat { get; set; } = false;

        [ProtoMember(4)]
        [DefaultValue(true)]
        public bool LogPrivateMessages { get; set; } = true;

        [ProtoMember(5)]
        [XmlArray("ForceBannedPlayers")]
        [XmlArrayItem("BannedPlayer")]
        public List<Player> ForceBannedPlayers;

        [ProtoMember(6)]
        [DefaultValue(ChatCommandSecurity.Admin)]
        public uint AdminLevel { get; set; } = ChatCommandSecurity.Admin;

        [ProtoMember(7)]
        [DefaultValue(false)]
        public bool EnableLog { get; set; } = false;

        [ProtoMember(8)]
        [DefaultValue(false)]
        public bool NoGrindIndestructible { get; set; } = false;


        public ServerConfigurationStruct()
        {
            // init default values
            WorldLocation = MyAPIGateway.Session.CurrentPath;
            MotdFileSuffix = MyAPIGateway.Session.Name.ReplaceForbiddenChars();
            ForceBannedPlayers = new List<Player>();
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

    [ProtoContract]
    public struct Player
    {
        [ProtoMember(1)]
        public ulong SteamId;

        [ProtoMember(2)]
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
        [ProtoMember(Name = "Message")]
        public string Text;
    }

    [ProtoContract]
    public struct ChatMessage
    {
        [ProtoMember(1)]
        public Player Sender;

        [ProtoMember(2)]
        public DateTime Date;

        [XmlElement("Message")]
        [ProtoMember(3, Name="Message")]
        public string Text;
    }

    #endregion
}
