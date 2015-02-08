using A8DB07281BA741DFB48BE151DDBFE24F;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace midspace.adminscripts
{
    /// <summary>
    /// Conains useful methods and fields for organizing the connections.
    /// </summary>
    public static class ConnectionHelper
    {
        /// <summary>
        /// Used when no other prefix is set in other words for the 'first contact'.
        /// </summary>
        public const string BasicPrefix = @"\x7FbY2k";

        /// <summary>
        /// Prefix for validation of created entity. Not initialized on server. Used to contact a specific client.
        /// </summary>
        public static string ClientPrefix;

        /// <summary>
        /// Prefix of the server instance. Used to send orders to the server.
        /// </summary>
        public static string ServerPrefix;

        /// <summary>
        /// ´Contains already connected palyers with their steam id and connection id
        /// </summary>
        public static Dictionary<ulong, string> PlayerConnections = new Dictionary<ulong, string>();

        /// <summary>
        /// True if an id request was sent otherwise false.
        /// </summary>
        public static bool SentIdRequest = false;

        #region connections to server

        /// <summary>
        /// Creates and sends an entity with the given information for the server.
        /// </summary>
        public static void CreateAndSendConnectionEntity(string key, string value)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add(key, value);
            CreateAndSendConnectionEntity(ServerPrefix, data);
        }

        /// <summary>
        /// Creates and sends an entity with the given information for the server.
        /// </summary>
        /// <param name="content"></param>
        public static void CreateAndSendConnectionEntity(Dictionary<string, string> content)
        {
            CreateAndSendConnectionEntity(ServerPrefix, content);
        }

        #endregion

        #region connections to clients

        /// <summary>
        /// Creates and sends an entity with the given information.
        /// </summary>
        /// <param name="player">The player who gets the information</param>
        /// <param name="content">The information that will be send to the player</param>
        public static void CreateAndSendConnectionEntity(IMyPlayer player, Dictionary<string, string> content)
        {
            CreateAndSendConnectionEntity(PlayerConnections[player.SteamUserId], content);
        }

        /// <summary>
        /// Creates and sends an entity with the given information.
        /// </summary>
        /// <param name="player">The player who gets the information</param>
        /// <param name="content">The information that will be send to the player</param>
        public static void CreateAndSendConnectionEntity(ulong steamId, Dictionary<string, string> content)
        {
            CreateAndSendConnectionEntity(PlayerConnections[steamId], content);
        }

        /// <summary>
        /// Creates and sends an entity with the given information.
        /// </summary>
        public static void CreateAndSendConnectionEntity(IMyPlayer player, string key, string value)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add(key, value);
            CreateAndSendConnectionEntity(player, data);
        }

        /// <summary>
        /// Creates and sends an entity with the given information.
        /// </summary>
        public static void CreateAndSendConnectionEntity(ulong steamId, string key, string value)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add(key, value);
            CreateAndSendConnectionEntity(steamId, data);
        }

        # endregion
        
        /// <summary>
        /// Creates and sends an entity.
        /// </summary>
        /// <param name="connectionId">The id of the client that gets the information</param>
        /// <param name="content">The information that will be send to the player</param>
        public static void CreateAndSendConnectionEntity(string connectionId, Dictionary<string, string> content)
        {
           SendConnectionEntity(CreateConnectionEntity(connectionId, content));
        }

        /// <summary>
        /// Creates an entity with the given information.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static MyObjectBuilder_CubeGrid CreateConnectionEntity(string id, Dictionary<string, string> content)
        {
            MyObjectBuilder_CubeGrid cubeGrid = new MyObjectBuilder_CubeGrid();
            cubeGrid.PersistentFlags = MyPersistentEntityFlags2.None;
            cubeGrid.IsStatic = true;
            cubeGrid.PositionAndOrientation = new MyPositionAndOrientation()
            {
                Position = Vector3D.Zero,
                Forward = Vector3.Forward,
                Up = Vector3.Up,
            };

            var str = new StringBuilder();
            str.AppendLine(id);
            str.Append(ConvertData(content));
            cubeGrid.DisplayName = str.ToString();

            return cubeGrid;
        }

        /// <summary>
        /// Sends an entity to the other clients
        /// </summary>
        /// <param name="cubeGrid"></param>
        public static void SendConnectionEntity(MyObjectBuilder_CubeGrid cubeGrid)
        {
            var tempList = new List<MyObjectBuilder_EntityBase> { cubeGrid };
            MyAPIGateway.Entities.RemapObjectBuilderCollection(tempList);
            tempList.ForEach(grid => MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(grid));
            MyAPIGateway.Multiplayer.SendEntitiesCreated(tempList);
        }

        /// <summary>
        /// Creates a random string with the given length
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string RandomString(int length)
        {
            //some chars for a string
            string chars = @"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!§$%&/()=[]{}ß@€|<>^°,;.:-_öäü+*#'";
            System.Random rnd = new System.Random();

            char[] buffer = new char[length];

            for (int i = 0; i < length; i++)
            {
                buffer[i] = chars[rnd.Next(chars.Length)];
            }

            return new string(buffer);
        }

        #region Converting and parsing

        /// <summary>
        /// Converts the data into a parsable string
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string ConvertData(Dictionary<string, string> data)
        {
            StringBuilder builder = new StringBuilder();

            foreach(KeyValuePair<string, string> entry in data) 
            {
                //escape " -> \" & \ -> \\
                string key = entry.Key.Replace(@"\", @"\\");
                string value = entry.Value.Replace(@"\", @"\\");
                key = key.Replace("\"", "\\\"");
                value = value.Replace("\"", "\\\"");
                //stick the entry together in the folowing form:
                //"Key":"Value";
                builder.Append("\"");
                builder.Append(key);
                builder.Append("\":\"");
                builder.Append(value);
                builder.Append("\";");
                //new line for new entry
                builder.Append("\r\n");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Reads the KeyValuePairs from the given string and adds them to a dictionary
        /// </summary>
        /// <param name="dataString"></param>
        /// <returns></returns>
        public static Dictionary<string, string> Parse(string dataString) 
        {
            var data = new Dictionary<string, string>();

            StringBuilder strBuild = new StringBuilder();
            bool isEscaped = false;
            bool terminated = true;
            bool isKey = true;

            string key = "";

            foreach (char c in dataString)
            {
                if (isEscaped)
                {
                    isEscaped = false;
                    strBuild.Append(c);
                    continue;
                }

                switch (c)
                {
                    case '"':
                        if (terminated)
                        {
                            //new key or value
                            terminated = false;
                        }
                        else
                        {
                            //end of key or value
                            terminated = true;

                            if (isKey)
                            {
                                key = strBuild.ToString();
                            }
                            else
                            {
                                data.Update(key, strBuild.ToString());
                            }

                            strBuild.Clear();
                        }
                        break;
                    case '\\':
                        if (!terminated)
                            isEscaped = true;
                        break;
                    case ':':
                        if (terminated)
                            isKey = false;
                        else
                            strBuild.Append(c);
                        break;
                    case ';':
                        if (terminated)
                            isKey = true;
                        else
                            strBuild.Append(c);
                        break;
                    default:
                        if (!terminated)
                            strBuild.Append(c);
                        break;
                }
            }
            return data;
        }

        #endregion

        #region Client side processing

        /// <summary>
        /// Client side execution of the actions defined in the data
        /// </summary>
        /// <param name="dataString"></param>
        public static void ProcessClientData(string dataString)
        {
            var parsedData = Parse(dataString);
            foreach (KeyValuePair<string, string> entry in parsedData)
            {
                switch (entry.Key)
                {
                    case ConnectionKeys.MessageOfTheDay:
                        CommandMessageOfTheDay.MessageOfTheDay = entry.Value;
                        CommandMessageOfTheDay.ShowMotd();
                        break;
                    case ConnectionKeys.PrivateMessage:
                        //TODO create private message command
                        break;
                    case ConnectionKeys.Command:
                        //TODO restrict/extend the permissions
                        break;
                    case ConnectionKeys.ForceKick:
                        MyAPIGateway.Utilities.ShowMessage("Process", "Receive Data");
                        ulong steamId;
                        if (ulong.TryParse(entry.Value, out steamId) && steamId == MyAPIGateway.Session.Player.SteamUserId)
                            PlayerTerminal.DropPlayer = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Client side. Process the ids sent from the server.
        /// </summary>
        /// <param name="dataString"></param>
        public static void ProcessIdData(string dataString)
        {
            var parsedData = Parse(dataString);
            foreach (KeyValuePair<string, string> entry in parsedData)
            {
                switch (entry.Key)
                {
                    case ConnectionKeys.ClientId:
                        ClientPrefix = entry.Value;
                        break;
                    case ConnectionKeys.ServerId:
                        ServerPrefix = entry.Value;
                        break;
                    case ConnectionKeys.MotdHeadLine:
                        CommandMessageOfTheDay.HeadLine = entry.Value;
                        break;
                    case ConnectionKeys.MotdShowInChat:
                        bool showInChat = CommandMessageOfTheDay.ShowInChat;
                        if (bool.TryParse(entry.Value, out showInChat)) 
                        {
                            CommandMessageOfTheDay.ShowInChat = showInChat;
                        }
                        break;
                    case ConnectionKeys.MessageOfTheDay:
                        CommandMessageOfTheDay.MessageOfTheDay = entry.Value;
                        CommandMessageOfTheDay.Received = true;
                        if (CommandMessageOfTheDay.ShowOnReceive)
                            CommandMessageOfTheDay.ShowMotd();
                        break;
                    case ConnectionKeys.AdminNotification:
                        ChatCommandLogic.Instance.AdminNotification = entry.Value;
                        if (CommandMessageOfTheDay.ShowOnReceive)
                            MyAPIGateway.Utilities.ShowMissionScreen("Admin Message System", "Error", null, ChatCommandLogic.Instance.AdminNotification, null, null);
                        break;
                    case ConnectionKeys.Command:
                        //TODO restrict/extend the permissions
                        break;
                    case ConnectionKeys.ForceKick:
                        ulong steamId;
                        if (ulong.TryParse(entry.Value, out steamId) && steamId == MyAPIGateway.Session.Player.SteamUserId)
                            PlayerTerminal.DropPlayer = true;
                        break;
                }
            }
        }

        #endregion

        #region Server side processing

        /// <summary>
        /// Server side execution of the actions defined in the data.
        /// </summary>
        /// <param name="dataString"></param>
        public static void ProcessServerData(string dataString)
        {
            var parsedData = Parse(dataString);
            foreach (KeyValuePair<string, string> entry in parsedData)
            {
                switch (entry.Key)
                {
                    case ConnectionKeys.MessageOfTheDay:
                        CommandMessageOfTheDay.MessageOfTheDay = entry.Value;
                        //TODO send it to the connected clients and save it
                        break;
                    case ConnectionKeys.Save:
                        if (string.IsNullOrEmpty(entry.Value))
                            MyAPIGateway.Session.Save();
                        else
                            MyAPIGateway.Session.Save(entry.Value);
                        //TODO implement a command that uses this
                        break;
                    case ConnectionKeys.PrivateMessage:
                        //TODO create private message command
                        break;
                    case ConnectionKeys.Command:
                        //TODO restrict/extend the command security
                        break;
                    case ConnectionKeys.ForceKick:
                        string[] values = entry.Value.Split(':');
                        bool ban = false;
                        ulong steamId;
                        if (ulong.TryParse(values[0], out steamId) && !MyAPIGateway.Utilities.ConfigDedicated.Administrators.Contains(entry.Value))
                        {
                            if (values.Length > 1 && bool.TryParse(values[1], out ban) && ban)
                            {
                                var players = new List<IMyPlayer>();
                                MyAPIGateway.Players.GetPlayers(players, p => p != null && p.SteamUserId == steamId);
                                IMyPlayer player = players.FirstOrDefault();
                                ChatCommandLogic.Instance.ServerCfg.ForceBannedPlayer.Add(new BannedPlayer()
                                {
                                    SteamId = steamId,
                                    PlayerName = player.DisplayName
                                });
                            }
                            CreateAndSendConnectionEntity(steamId, ConnectionKeys.ForceKick, steamId.ToString());
                        }
                        break;
                    case ConnectionKeys.Pardon:
                        BannedPlayer bannedPlayer = ChatCommandLogic.Instance.ServerCfg.ForceBannedPlayer.FirstOrDefault(p => p.PlayerName.Equals(entry.Value, StringComparison.InvariantCultureIgnoreCase));
                        if (bannedPlayer.SteamId != 0)
                            ChatCommandLogic.Instance.ServerCfg.ForceBannedPlayer.Remove(bannedPlayer);
                        break;
                }
            }
        }

        /// <summary>
        /// Server side. Sends the requested ids to the client.
        /// </summary>
        /// <param name="dataString"></param>
        public static void ProcessIdRequest(string dataString)
        {
            var parsedData = Parse(dataString);
            foreach (KeyValuePair<string, string> entry in parsedData)
            {
                switch (entry.Key)
                {
                    case ConnectionKeys.ConnectionRequest:
                        ulong steamId;
                        if (ulong.TryParse(entry.Value, out steamId))
                        {
                            //only register unregistred players
                            if (!PlayerConnections.ContainsKey(steamId))
                            {
                                var connectionId = RandomString(8);

                                //in case we gernerate the same value two times (very unrealistic)
                                while (PlayerConnections.ContainsValue(connectionId))
                                    connectionId = RandomString(8);

                                PlayerConnections.Add(steamId, connectionId);
                            }

                            var data = new Dictionary<string, string>();
                            data.Add(ConnectionKeys.ClientId, PlayerConnections[steamId]);
                            data.Add(ConnectionKeys.ServerId, ServerPrefix);
                            //only send the motd if there is one
                            if (!string.IsNullOrEmpty(CommandMessageOfTheDay.MessageOfTheDay))
                            {
                                //the header must be initialized before the motd otherwise it won't show
                                if (!string.IsNullOrEmpty(CommandMessageOfTheDay.HeadLine))
                                    data.Add(ConnectionKeys.MotdHeadLine, CommandMessageOfTheDay.HeadLine);

                                if (CommandMessageOfTheDay.ShowInChat)
                                    data.Add(ConnectionKeys.MotdShowInChat, CommandMessageOfTheDay.ShowInChat.ToString());

                                data.Add(ConnectionKeys.MessageOfTheDay, CommandMessageOfTheDay.MessageOfTheDay);

                            }
                            if (!string.IsNullOrEmpty(ChatCommandLogic.Instance.AdminNotification) && MyAPIGateway.Utilities.ConfigDedicated.Administrators.Contains(entry.Value))
                                data.Add(ConnectionKeys.AdminNotification, ChatCommandLogic.Instance.AdminNotification);
                            BannedPlayer bannedPlayer = ChatCommandLogic.Instance.ServerCfg.ForceBannedPlayer.FirstOrDefault(p => p.SteamId == steamId);
                            if (bannedPlayer.SteamId != 0 && !MyAPIGateway.Utilities.ConfigDedicated.Administrators.Contains(bannedPlayer.SteamId.ToString()))
                                data.Add(ConnectionKeys.ForceKick, bannedPlayer.SteamId.ToString());
                            //only send the command permission if it is set, disabled by now
                            /*if (!string.IsNullOrEmpty(ChatCommandLogic.Instance.ServerCfg.CommandPermissions))
                                data.Add("cmd", ChatCommandLogic.Instance.ServerCfg.CommandPermissions);*/
                            var firstContact = CreateConnectionEntity(BasicPrefix, data);
                            SendConnectionEntity(firstContact);
                        }
                        break;
                }
            }
        }

        #endregion

        private static void PerformSecurityChanges(string commandSecurityPair)
        {
            var pair = commandSecurityPair.Split(':');
            if (pair.Length < 2)
                return;
            var commandName = pair[0].ToLowerInvariant();
            ChatCommandSecurity security = ChatCommandSecurity.None;
            switch (pair[1].ToLowerInvariant())
            {
                case "admin":
                    security = ChatCommandSecurity.Admin;
                    break;
                case "user":
                    security = ChatCommandSecurity.User;
                    break;
            }
            if (security.Equals(ChatCommandSecurity.None))
                return;
            ChatCommandService.UpdateSecurity(commandName, security);
        }

        public static class ConnectionKeys
        {
            public const string ConnectionRequest = "connect";
            public const string ClientId = "id";
            public const string ServerId =  "serverId";
            public const string MessageOfTheDay = "motd";
            public const string MotdHeadLine = "motdhl";
            public const string MotdShowInChat = "motdsic";
            public const string AdminNotification = "adminnot";
            public const string ForceKick = "forcekick";
            public const string PrivateMessage = "pm";
            public const string Command = "cmd";
            public const string Save = "save";
            public const string Pardon = "pard";
        }
    }
}
