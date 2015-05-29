using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
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
        /// Id for messages.
        /// </summary>
        public const ushort StandardClientId = 16103;
        public const ushort StandardServerId = StandardClientId + 1;

        static int MAX_MESSAGE_SIZE = 4096;

        static int client_IncomingMessages = 0;
        static List<byte> client_MessageCache = new List<byte>();

        static int server_IncomingMessages = 0;
        static List<byte> server_MessageCache = new List<byte>();

        #region connections to server

        /// <summary>
        /// Creates and sends an entity with the given information for the server. Never call this on DS instance!
        /// </summary>
        public static void SendMessageToServer(string key, string value)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add(key, value);
            SendMessageToServer(data);
        }

        /// <summary>
        /// Creates and sends an entity with the given information for the server. Never call this on DS instance!
        /// </summary>
        /// <param name="content"></param>
        public static void SendMessageToServer(Dictionary<string, string> content)
        {
            if (!content.ContainsKey(ConnectionKeys.Sender))
                content.Add(ConnectionKeys.Sender, MyAPIGateway.Session.Player.SteamUserId.ToString());
            byte[] byteData = System.Text.Encoding.Unicode.GetBytes(ConvertData(content));
            if (byteData.Length <= MAX_MESSAGE_SIZE)
                MyAPIGateway.Multiplayer.SendMessageToServer(StandardServerId, byteData);
            else
            {
                var byteList = byteData.ToList();
                int parts = byteList.Count / MAX_MESSAGE_SIZE + 1;
                SendMessageToServer(ConnectionKeys.IncomingMessageParts, parts.ToString());
                for (int i = 0; i < parts; i++)
                {
                    List<byte> bytes = new List<byte>();
                    if (i == parts - 1)
                        bytes = byteList.GetRange(i * MAX_MESSAGE_SIZE, byteList.Count - i * MAX_MESSAGE_SIZE - 1);
                    else //get leftover
                        bytes = byteList.GetRange(i * MAX_MESSAGE_SIZE, MAX_MESSAGE_SIZE);

                    MyAPIGateway.Multiplayer.SendMessageToServer(StandardServerId, bytes.ToArray());
                }
            }
        }

        #endregion

        #region connections to all

        /// <summary>
        /// Creates and sends an entity with the given information for all the server and all players.
        /// </summary>
        public static void SendMessageToAll(string key, string value)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add(key, value);
            SendMessageToAll(data);
        }

        /// <summary>
        /// Creates and sends an entity with the given information for the server and all players.
        /// </summary>
        /// <param name="content"></param>
        public static void SendMessageToAll(Dictionary<string, string> content)
        {
            if (!content.ContainsKey(ConnectionKeys.Sender))
                content.Add(ConnectionKeys.Sender, MyAPIGateway.Session.Player.SteamUserId.ToString());
            if (!MyAPIGateway.Multiplayer.IsServer)
                SendMessageToServer(content);
            SendMessageToAllPlayers(content);
        }

        #endregion

        #region connections to clients

        public static void SendMessageToPlayer(IMyPlayer player, Dictionary<string, string> content)
        {
            SendMessageToPlayer(player.SteamUserId, content);
        }

        public static void SendMessageToPlayer(ulong steamId, Dictionary<string, string> content)
        {
            byte[] byteData = System.Text.Encoding.Unicode.GetBytes(ConvertData(content));
            if (byteData.Length <= MAX_MESSAGE_SIZE)
                MyAPIGateway.Multiplayer.SendMessageTo(StandardClientId, byteData, steamId);
            else 
            {
                var byteList = byteData.ToList();
                int parts = byteList.Count / MAX_MESSAGE_SIZE + 1;
                SendMessageToPlayer(steamId, ConnectionKeys.IncomingMessageParts, parts.ToString());
                for (int i = 0; i < parts; i++)
                {
                    List<byte> bytes = new List<byte>();
                    if (i == parts - 1)
                        bytes = byteList.GetRange(i * MAX_MESSAGE_SIZE, byteList.Count - i * MAX_MESSAGE_SIZE - 1);
                    else //get leftover
                        bytes = byteList.GetRange(i * MAX_MESSAGE_SIZE, MAX_MESSAGE_SIZE);

                    MyAPIGateway.Multiplayer.SendMessageTo(StandardClientId, bytes.ToArray(), steamId);
                }
            }
        }

        public static void SendMessageToPlayer(IMyPlayer player, string key, string value)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add(key, value);
            SendMessageToPlayer(player, data);
        }

        public static void SendMessageToPlayer(ulong steamId, string key, string value)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add(key, value);
            SendMessageToPlayer(steamId, data);
        }

        public static void SendMessageToAllPlayers(Dictionary<string, string> content)
        {
            //MyAPIGateway.Multiplayer.SendMessageToOthers(StandardClientId, System.Text.Encoding.Unicode.GetBytes(ConvertData(content))); <- does not work as expected ... so it doesn't work at all?
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, p => p != null);
            foreach (IMyPlayer player in players)
                SendMessageToPlayer(player, content);
        }

        public static void SendMessageToAllPlayers(string key, string value)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add(key, value);
            SendMessageToAllPlayers(data);
        }

        # endregion

        #region Converting and parsing

        /// <summary>
        /// Converts the data into a parsable string
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string ConvertData(Dictionary<string, string> data)
        {
            StringBuilder builder = new StringBuilder();

            foreach (KeyValuePair<string, string> entry in data)
            {
                var key = entry.Key ?? "";
                var value = entry.Value ?? "";
                //escape " -> \" & \ -> \\
                key = entry.Key.Replace(@"\", @"\\");
                value = entry.Value.Replace(@"\", @"\\");
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
                Logger.Debug(string.Format("[Client]Processing KeyValuePair - Key: {0}, Value: {1}", entry.Key, entry.Value));
                switch (entry.Key)
                {
                    #region motd
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
                    case ConnectionKeys.MessageOfTheDay: //motd
                        CommandMessageOfTheDay.Content = entry.Value;
                        if (!CommandMessageOfTheDay.Received)
                        {
                            CommandMessageOfTheDay.Received = true;
                            if (CommandMessageOfTheDay.ShowOnReceive && !String.IsNullOrEmpty(CommandMessageOfTheDay.Content))
                                CommandMessageOfTheDay.ShowMotd();
                            break;
                        }
                        MyAPIGateway.Utilities.ShowMessage("Motd", "The message of the day was updated just now. To see what is new use '/motd'.");
                        break;
                    #endregion

                    #region misc
                    case ConnectionKeys.AdminNotification:
                        ChatCommandLogic.Instance.AdminNotification = entry.Value;
                        if (CommandMessageOfTheDay.ShowOnReceive)
                            MyAPIGateway.Utilities.ShowMissionScreen("Admin Message System", "Error", null, ChatCommandLogic.Instance.AdminNotification, null, null);
                        break;
                    case ConnectionKeys.PrivateMessage: //pm
                        IMyPlayer sender = null;
                        string senderName = null;
                        string message = null;
                        foreach (KeyValuePair<string, string> pmEntry in Parse(entry.Value))
                        {
                            Logger.Debug(string.Format("[Client]Processing PM KeyValuePair - Key: {0}, Value: {1}", pmEntry.Key, pmEntry.Value));
                            switch (pmEntry.Key)
                            {
                                case ConnectionKeys.PmSender:
                                    ulong senderSteamId;
                                    if (ulong.TryParse(pmEntry.Value, out senderSteamId) && senderSteamId != 0)
                                    {
                                        MyAPIGateway.Players.TryGetPlayer(senderSteamId, out sender);
                                    }
                                    break;
                                case ConnectionKeys.PmSenderName:
                                    senderName = pmEntry.Value;
                                    break;
                                case ConnectionKeys.PmMessage:
                                    message = pmEntry.Value;
                                    break;
                            }
                        }

                        if (sender != null)
                        {
                            senderName = sender.DisplayName;
                            CommandPrivateMessage.LastWhisperId = sender.SteamUserId;
                        }

                        MyAPIGateway.Utilities.ShowMessage(string.Format("{0}{1}", senderName, senderName.Equals("Server") ? "" : " whispers"), message);
                        break;
                    case ConnectionKeys.ForceKick:
                        ulong steamId;
                        if (ulong.TryParse(entry.Value, out steamId) && steamId == MyAPIGateway.Session.Player.SteamUserId)
                            CommandForceKick.DropPlayer = true;
                        break;
                    case ConnectionKeys.LogPrivateMessages:
                        bool logPms;
                        if (bool.TryParse(entry.Value, out logPms))
                            CommandPrivateMessage.LogPrivateMessages = logPms;
                        break;
                    case ConnectionKeys.IncomingMessageParts:
                        int parts;
                        if (int.TryParse(entry.Value, out parts))
                        {
                            if (parts < 0)
                                break;
                            client_IncomingMessages = parts;
                            client_MessageCache.Clear();
                        }
                        break;
                    #endregion

                    #region session settings
                    case ConnectionKeys.CargoShips:
                        bool enableCargoShips;
                        if (bool.TryParse(entry.Value, out enableCargoShips))
                        {
                            //already set by server
                            if (!MyAPIGateway.Session.Player.IsHost())
                                MyAPIGateway.Session.GetCheckpoint("null").CargoShipsEnabled = enableCargoShips;
                            if (MyAPIGateway.Session.Player.IsAdmin())
                                MyAPIGateway.Utilities.ShowMessage("Server CargoShips", enableCargoShips ? "On" : "Off");
                        }
                        break;
                    case ConnectionKeys.CopyPaste:
                        bool enableCopyPaste;
                        if (bool.TryParse(entry.Value, out enableCopyPaste))
                        {
                            if (!MyAPIGateway.Session.Player.IsHost())
                                MyAPIGateway.Session.GetCheckpoint("null").EnableCopyPaste = enableCopyPaste;
                            if (MyAPIGateway.Session.Player.IsAdmin())
                                MyAPIGateway.Utilities.ShowMessage("Server CopyPaste", enableCopyPaste ? "On" : "Off");
                        }
                        break;
                    case ConnectionKeys.Creative:
                        bool enableCreative;
                        if (bool.TryParse(entry.Value, out enableCreative))
                        {
                            if (!MyAPIGateway.Session.Player.IsHost())
                            {
                                MyGameModeEnum gameMode = enableCreative ? MyGameModeEnum.Creative : MyGameModeEnum.Survival;
                                MyAPIGateway.Session.GetCheckpoint("null").GameMode = gameMode;
                            }
                            if (MyAPIGateway.Session.Player.IsAdmin())
                                MyAPIGateway.Utilities.ShowMessage("Server Creative", enableCreative ? "On" : "Off");
                        }
                        break;
                    case ConnectionKeys.Spectator:
                        bool enableSpectator;
                        if (bool.TryParse(entry.Value, out enableSpectator))
                        {
                            if (!MyAPIGateway.Session.Player.IsHost())
                            {
                                MyAPIGateway.Session.GetCheckpoint("null").Settings.EnableSpectator = enableSpectator;
                            }
                            if (MyAPIGateway.Session.Player.IsAdmin())
                                MyAPIGateway.Utilities.ShowMessage("Server Spectator", enableSpectator ? "On" : "Off");
                        }
                        break;
                    case ConnectionKeys.Weapons:
                        bool enableWeapons;
                        if (bool.TryParse(entry.Value, out enableWeapons))
                        {
                            if (!MyAPIGateway.Session.Player.IsHost())
                            {
                                MyAPIGateway.Session.GetCheckpoint("null").WeaponsEnabled = enableWeapons;
                            }
                            if (MyAPIGateway.Session.Player.IsAdmin())
                                MyAPIGateway.Utilities.ShowMessage("Server Weapons", enableWeapons ? "On" : "Off");
                        }
                        break;
                    #endregion

                    #region permissions
                    case ConnectionKeys.CommandLevel:
                        uint level;
                        string[] values = entry.Value.Split(':');

                        if (values.Length < 2)
                            break;

                        if (uint.TryParse(values[1], out level))
                        {
                            ChatCommandService.UpdateCommandSecurity(values[0], level);
                        }
                        break;
                    case ConnectionKeys.CommandList:
                        string commandName = "";
                        string commandLevel = "";
                        bool newCommandList = false;
                        bool showCommandList = false;

                        foreach (KeyValuePair<string, string> pair in Parse(entry.Value))
                        {
                            switch (pair.Key)
                            {
                                case ConnectionKeys.PermEntryName:
                                    commandName = pair.Value;
                                    break;
                                case ConnectionKeys.PermEntryLevel:
                                    commandLevel = pair.Value;
                                    break;
                                case ConnectionKeys.PermNewHotlist:
                                    newCommandList = true;
                                    break;
                                case ConnectionKeys.PermLastEntry:
                                    showCommandList = true;
                                    break;
                            }
                        }

                        CommandPermission.AddToCommandCache(commandName, commandLevel, showCommandList, newCommandList);
                        break;
                    case ConnectionKeys.PlayerLevel:
                        uint newUserSecurity;
                        if (uint.TryParse(entry.Value, out newUserSecurity))
                            ChatCommandService.UserSecurity = newUserSecurity;
                        ChatCommandLogic.Instance.BlockCommandExecution = false;
                        break;
                    case ConnectionKeys.PlayerList:
                        string playerName = "";
                        string playerLevel = "";
                        string playerListEntrySteamId = "";
                        string extensions = "";
                        string restrictions = "";
                        bool usePlayerLevel = false;
                        bool newPlayerList = false;
                        bool showPlayerList = false;

                        foreach (KeyValuePair<string, string> pair in Parse(entry.Value))
                        {
                            switch (pair.Key)
                            {
                                case ConnectionKeys.PermEntryName:
                                    playerName = pair.Value;
                                    break;
                                case ConnectionKeys.PermEntryLevel:
                                    playerLevel = pair.Value;
                                    break;
                                case ConnectionKeys.PermEntryId:
                                    playerListEntrySteamId = pair.Value;
                                    break;
                                case ConnectionKeys.PermEntryExtensions:
                                    extensions = pair.Value;
                                    break;
                                case ConnectionKeys.PermEntryRestrictions:
                                    restrictions = pair.Value;
                                    break;
                                case ConnectionKeys.PermEntryUsePlayerLevel:
                                    usePlayerLevel = true;
                                    break;
                                case ConnectionKeys.PermNewHotlist:
                                    newPlayerList = true;
                                    break;
                                case ConnectionKeys.PermLastEntry:
                                    showPlayerList = true;
                                    break;
                            }
                        }

                        CommandPermission.AddToPlayerCache(playerName, playerLevel, playerListEntrySteamId, extensions, restrictions, usePlayerLevel, showPlayerList, newPlayerList);
                        break;
                    case ConnectionKeys.GroupList:
                        string groupName = "";
                        string groupLevel = "";
                        string members = "";
                        bool newGroupList = false;
                        bool showGroupList = false;

                        foreach (KeyValuePair<string, string> pair in Parse(entry.Value))
                        {
                            switch (pair.Key)
                            {
                                case ConnectionKeys.PermEntryName:
                                    groupName = pair.Value;
                                    break;
                                case ConnectionKeys.PermEntryLevel:
                                    groupLevel = pair.Value;
                                    break;
                                case ConnectionKeys.PermEntryMembers:
                                    members = pair.Value;
                                    break;
                                case ConnectionKeys.PermNewHotlist:
                                    newGroupList = true;
                                    break;
                                case ConnectionKeys.PermLastEntry:
                                    showGroupList = true;
                                    break;
                            }
                        }

                        CommandPermission.AddToGroupCache(groupName, groupLevel, members, showGroupList, newGroupList);
                        break;
                    #endregion

                    #region sync
                    case ConnectionKeys.Smite:
                        CommandPlayerSmite.Smite(MyAPIGateway.Session.Player);
                        break;
                    case ConnectionKeys.StopAndMove:
                        {
                            string[] properties = entry.Value.Split(':');
                            long entityId;
                            double posX;
                            double posY;
                            double posZ;

                            if (properties.Length > 3 && long.TryParse(properties[0], out entityId) && MyAPIGateway.Entities.EntityExists(entityId)
                                && double.TryParse(properties[1], out posX) && double.TryParse(properties[2], out posY) && double.TryParse(properties[3], out posZ))
                            {
                                var entity = MyAPIGateway.Entities.GetEntityById(entityId);
                                entity.Stop();
                                var destination = new Vector3D(posX, posY, posZ);

                                // This still is not syncing properly. Called on the server, it does not show correctly on the client.
                                entity.SetPosition(destination);
                            }
                        }
                        break;
                    case ConnectionKeys.Delete:
                        {
                            long entityId;
                            if (long.TryParse(entry.Value, out entityId) && MyAPIGateway.Entities.EntityExists(entityId))
                            {
                                var entity = MyAPIGateway.Entities.GetEntityById(entityId);
                                if (entity != null)
                                    entity.Delete();  // Doesn't sync from server to clients, or client to server.
                            }
                        }
                        break;

                    #endregion
                }
                Logger.Debug(string.Format("[Client]Finished processing KeyValuePair for Key: {0}", entry.Key));
            }
        }

        public static void ProcessClientData(byte[] rawData)
        {
            switch (client_IncomingMessages)
            {
                case 0:
                    ProcessClientData(System.Text.Encoding.Unicode.GetString(rawData));
                    break;
                case 1:
                    client_MessageCache.AddArray(rawData);
                    client_IncomingMessages--;
                    ProcessClientData(System.Text.Encoding.Unicode.GetString(client_MessageCache.ToArray()));
                    break;
                default:
                    client_MessageCache.AddArray(rawData);
                    client_IncomingMessages--;
                    break;
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
            string senderIdString = parsedData[ConnectionKeys.Sender];
            ulong senderSteamId;
            if (ulong.TryParse(senderIdString, out senderSteamId))
                parsedData.Remove(ConnectionKeys.Sender);
            else
                return; //if we don't know who sent the request, we don't execute it

            foreach (KeyValuePair<string, string> entry in parsedData)
            {
                Logger.Debug(string.Format("[Server]Processing KeyValuePair - Key: {0}, Value: {1}", entry.Key, entry.Value));
                switch (entry.Key)
                {
                    #region config
                    case ConnectionKeys.MessageOfTheDay:
                        ChatCommandLogic.Instance.ServerCfg.SetMessageOfTheDay(entry.Value);
                        SendChatMessage(senderSteamId, "The message of the day was updated. Please note that you have to use '/cfg save' to save it permanently.");
                        break;
                    case ConnectionKeys.MotdHeadLine:
                        CommandMessageOfTheDay.HeadLine = entry.Value;
                        SendMessageToAllPlayers(ConnectionKeys.MotdHeadLine, entry.Value);
                        SendChatMessage(senderSteamId, "The headline of the message of the day was updated. Please note that you have to use '/cfg save' to save it permanently.");
                        break;
                    case ConnectionKeys.MotdShowInChat:
                        bool motdsic;
                        if (bool.TryParse(entry.Value, out motdsic))
                        {
                            CommandMessageOfTheDay.ShowInChat = motdsic;
                            SendMessageToAllPlayers(ConnectionKeys.MotdShowInChat, entry.Value);
                            SendChatMessage(senderSteamId, string.Format("The setting motdShowInChat was set to {0}. Please note that you have to use '/cfg save' to save it permanently.", motdsic));
                        }
                        break;
                    case ConnectionKeys.AdminLevel:
                        uint adminLevel;
                        if (uint.TryParse(entry.Value, out adminLevel))
                        {
                            ChatCommandLogic.Instance.ServerCfg.UpdateAdminLevel(adminLevel);
                            SendChatMessage(senderSteamId, string.Format("Updated default admin level to {0}. Please note that you have to use '/cfg save' to save it permanently.", adminLevel));
                        }
                        break;
                    #endregion

                    #region misc
                    case ConnectionKeys.Save:
                        if (ServerConfig.ServerIsClient)
                            break; //no one should be able to do that

                        if (string.IsNullOrEmpty(entry.Value))
                            MyAPIGateway.Session.Save();
                        else
                            MyAPIGateway.Session.Save(entry.Value);
                        //TODO implement a command that uses this
                        break;
                    case ConnectionKeys.PrivateMessage:
                        string message = "";
                        ulong receiverSteamId = 0;
                        foreach (KeyValuePair<string, string> pmEntry in Parse(entry.Value))
                        {
                            switch (pmEntry.Key)
                            {
                                case ConnectionKeys.PmReceiver:
                                    if (ulong.TryParse(pmEntry.Value, out receiverSteamId))
                                    {
                                        SendMessageToPlayer(receiverSteamId, ConnectionKeys.PrivateMessage, entry.Value);
                                    }
                                    break;
                                case ConnectionKeys.PmMessage:
                                    message = pmEntry.Value;
                                    break;
                            }
                        }
                        if (!string.IsNullOrEmpty(message) && receiverSteamId != 0)
                            ChatCommandLogic.Instance.ServerCfg.LogPrivateMessage(senderSteamId, receiverSteamId, message);
                        else
                            Logger.Debug("Could not log private message");
                        break;
                    case ConnectionKeys.GlobalMessage:
                        ChatCommandLogic.Instance.ServerCfg.LogGlobalMessage(senderSteamId, entry.Value);
                        break;
                    case ConnectionKeys.ForceKick:
                        {
                            string[] values = entry.Value.Split(':');
                            bool ban = false;
                            ulong steamId;
                            if (ulong.TryParse(values[0], out steamId) && !ChatCommandLogic.Instance.ServerCfg.IsServerAdmin(steamId))
                            {
                                var players = new List<IMyPlayer>();
                                MyAPIGateway.Players.GetPlayers(players, p => p != null && p.SteamUserId == steamId);
                                IMyPlayer player = players.FirstOrDefault();
                                if (values.Length > 1 && bool.TryParse(values[1], out ban) && ban)
                                {
                                    ChatCommandLogic.Instance.ServerCfg.ForceBannedPlayers.Add(new BannedPlayer()
                                    {
                                        SteamId = steamId,
                                        PlayerName = player.DisplayName
                                    });
                                }
                                SendMessageToPlayer(steamId, ConnectionKeys.ForceKick, steamId.ToString());
                                SendChatMessage(senderSteamId, string.Format("{0} player {1}.", ban ? "Forcebanned" : "Forcekicked", player.DisplayName));
                            }
                        }
                        break;
                    case ConnectionKeys.Pardon:
                        BannedPlayer bannedPlayer = ChatCommandLogic.Instance.ServerCfg.ForceBannedPlayers.FirstOrDefault(p => p.PlayerName.Equals(entry.Value, StringComparison.InvariantCultureIgnoreCase));
                        if (bannedPlayer.SteamId != 0)
                        {
                            ChatCommandLogic.Instance.ServerCfg.ForceBannedPlayers.Remove(bannedPlayer);
                            SendChatMessage(senderSteamId, string.Format("Pardoned player {0}", bannedPlayer.PlayerName));
                        }
                        break;
                    case ConnectionKeys.ConfigSave:
                        ChatCommandLogic.Instance.ServerCfg.Save();
                        SendChatMessage(senderSteamId, "Config saved.");
                        break;
                    case ConnectionKeys.ConfigReload:
                        ChatCommandLogic.Instance.ServerCfg.ReloadConfig();
                        SendChatMessage(senderSteamId, "Config reloaded.");
                        break;
                    case ConnectionKeys.IncomingMessageParts:
                        int parts;
                        if (int.TryParse(entry.Value, out parts))
                        {
                            if (parts < 0)
                                break;
                            server_IncomingMessages = parts;
                            server_MessageCache.Clear();
                        }
                        break;
                    #endregion

                    #region Session settings
                    case ConnectionKeys.CargoShips:
                        bool enableCargoShips;
                        if (bool.TryParse(entry.Value, out enableCargoShips))
                        {
                            MyAPIGateway.Session.GetCheckpoint("null").CargoShipsEnabled = enableCargoShips;
                        }
                        SendMessageToAllPlayers(ConnectionKeys.CargoShips, entry.Value);
                        break;
                    case ConnectionKeys.CopyPaste:
                        bool enableCopyPaste;
                        if (bool.TryParse(entry.Value, out enableCopyPaste))
                        {
                            MyAPIGateway.Session.GetCheckpoint("null").EnableCopyPaste = enableCopyPaste;
                        }
                        SendMessageToAllPlayers(ConnectionKeys.CopyPaste, entry.Value);
                        break;
                    case ConnectionKeys.Creative:
                        bool enableCreative;
                        if (bool.TryParse(entry.Value, out enableCreative))
                        {
                            MyGameModeEnum gameMode = enableCreative ? MyGameModeEnum.Creative : MyGameModeEnum.Survival;
                            MyAPIGateway.Session.GetCheckpoint("null").GameMode = gameMode;
                        }
                        SendMessageToAllPlayers(ConnectionKeys.Creative, entry.Value);
                        break;
                    case ConnectionKeys.Spectator:
                        bool enableSpectator;
                        if (bool.TryParse(entry.Value, out enableSpectator))
                        {
                            MyAPIGateway.Session.GetCheckpoint("null").Settings.EnableSpectator = enableSpectator;
                        }
                        SendMessageToAllPlayers(ConnectionKeys.Spectator, entry.Value);
                        break;
                    case ConnectionKeys.Weapons:
                        bool enableWeapons;
                        if (bool.TryParse(entry.Value, out enableWeapons))
                        {
                            MyAPIGateway.Session.GetCheckpoint("null").WeaponsEnabled = enableWeapons;
                        }
                        SendMessageToAllPlayers(ConnectionKeys.Weapons, entry.Value);
                        break;
                    #endregion

                    #region permissions
                    case ConnectionKeys.CommandLevel:
                        foreach (KeyValuePair<string, string> pair in Parse(entry.Value))
                        {
                            uint level;
                            if (uint.TryParse(pair.Value, out level))
                                ChatCommandLogic.Instance.ServerCfg.UpdateCommandSecurity(pair.Key, level, senderSteamId);
                            else
                                SendChatMessage(senderSteamId, "Error in performing changes.");
                        }
                        break;
                    case ConnectionKeys.CommandList:
                        ChatCommandLogic.Instance.ServerCfg.CreateCommandHotlist(senderSteamId, entry.Value);
                        break;
                    case ConnectionKeys.PlayerLevel:
                        foreach (KeyValuePair<string, string> pair in Parse(entry.Value))
                        {
                            uint level;
                            if (uint.TryParse(pair.Value, out level))
                                ChatCommandLogic.Instance.ServerCfg.SetPlayerLevel(pair.Key, level, senderSteamId);
                            else
                                SendChatMessage(senderSteamId, "Error in performing changes.");
                        }
                        break;
                    case ConnectionKeys.PlayerExtend:
                        foreach (KeyValuePair<string, string> pair in Parse(entry.Value))
                        {
                            ChatCommandLogic.Instance.ServerCfg.ExtendRights(pair.Key, pair.Value, senderSteamId);
                        }
                        break;
                    case ConnectionKeys.PlayerRestrict:
                        foreach (KeyValuePair<string, string> pair in Parse(entry.Value))
                        {
                            ChatCommandLogic.Instance.ServerCfg.RestrictRights(pair.Key, pair.Value, senderSteamId);
                        }
                        break;
                    case ConnectionKeys.UsePlayerLevel:
                        foreach (KeyValuePair<string, string> pair in Parse(entry.Value))
                        {
                            bool usePlayerLevel;
                            if (bool.TryParse(pair.Value, out usePlayerLevel))
                                ChatCommandLogic.Instance.ServerCfg.UsePlayerLevel(pair.Key, usePlayerLevel, senderSteamId);
                            else
                                SendChatMessage(senderSteamId, "Error in performing changes.");
                        }
                        break;
                    case ConnectionKeys.PlayerList:
                        ChatCommandLogic.Instance.ServerCfg.CreatePlayerHotlist(senderSteamId, entry.Value);
                        break;
                    case ConnectionKeys.GroupLevel:
                        foreach (KeyValuePair<string, string> pair in Parse(entry.Value))
                        {
                            uint level;
                            if (uint.TryParse(pair.Value, out level))
                                ChatCommandLogic.Instance.ServerCfg.SetGroupLevel(pair.Key, level, senderSteamId);
                            else
                                SendChatMessage(senderSteamId, "Error in performing changes.");
                        }
                        break;
                    case ConnectionKeys.GroupName:
                        foreach (KeyValuePair<string, string> pair in Parse(entry.Value))
                        {
                            ChatCommandLogic.Instance.ServerCfg.SetGroupName(pair.Key, pair.Value, senderSteamId);
                        }
                        break;
                    case ConnectionKeys.GroupAddPlayer:
                        foreach (KeyValuePair<string, string> pair in Parse(entry.Value))
                        {
                            ChatCommandLogic.Instance.ServerCfg.AddPlayerToGroup(pair.Key, pair.Value, senderSteamId);
                        }
                        break;
                    case ConnectionKeys.GroupRemovePlayer:
                        foreach (KeyValuePair<string, string> pair in Parse(entry.Value))
                        {
                            ChatCommandLogic.Instance.ServerCfg.RemovePlayerFromGroup(pair.Key, pair.Value, senderSteamId);
                        }
                        break;
                    case ConnectionKeys.GroupCreate:
                        foreach (KeyValuePair<string, string> pair in Parse(entry.Value))
                        {
                            uint level;
                            if (uint.TryParse(pair.Value, out level))
                                ChatCommandLogic.Instance.ServerCfg.CreateGroup(pair.Key, level, senderSteamId);
                            else
                                SendChatMessage(senderSteamId, "Error in performing changes.");
                        }
                        break;
                    case ConnectionKeys.GroupDelete:
                        ChatCommandLogic.Instance.ServerCfg.DeleteGroup(entry.Value, senderSteamId);
                        break;
                    case ConnectionKeys.GroupList:
                        ChatCommandLogic.Instance.ServerCfg.CreateGroupHotlist(senderSteamId, entry.Value);
                        break;
                    #endregion

                    #region sync
                    case ConnectionKeys.Smite:
                        ulong smitePlayerSteamId;
                        if (ulong.TryParse(entry.Value, out smitePlayerSteamId))
                            SendMessageToPlayer(smitePlayerSteamId, ConnectionKeys.Smite, "Smite yourself :D");
                        break;
                    case ConnectionKeys.Stop:
                        {
                            long entityId;
                            if (long.TryParse(entry.Value, out entityId) && MyAPIGateway.Entities.EntityExists(entityId))
                            {
                                var entity = MyAPIGateway.Entities.GetEntityById(entityId);
                                entity.Stop();
                            }
                        }
                        break;
                    case ConnectionKeys.StopAndMove:
                        {
                            string[] values = entry.Value.Split(':');
                            long entityId;
                            double posX;
                            double posY;
                            double posZ;

                            if (values.Length > 3 && long.TryParse(values[0], out entityId) && MyAPIGateway.Entities.EntityExists(entityId)
                                && double.TryParse(values[1], out posX) && double.TryParse(values[2], out posY) && double.TryParse(values[3], out posZ))
                            {
                                var entity = MyAPIGateway.Entities.GetEntityById(entityId);
                                entity.Stop();
                                var destination = new Vector3D(posX, posY, posZ);

                                // This still is not syncing properly. Called on the server, it does not show correctly on the client.
                                entity.SetPosition(destination);
                            }
                        }
                        break;
                    case ConnectionKeys.Claim:
                        {
                            string[] values = entry.Value.Split(':');
                            long entityId;
                            long playerId;
                            if (values.Length > 1 && long.TryParse(values[0], out playerId) && long.TryParse(values[1], out entityId) && MyAPIGateway.Entities.EntityExists(entityId))
                            {
                                var players = new List<IMyPlayer>();
                                MyAPIGateway.Players.GetPlayers(players, p => p != null && p.PlayerID == playerId);
                                IMyPlayer player = players.FirstOrDefault();
                                var entity = MyAPIGateway.Entities.GetEntityById(entityId) as IMyCubeGrid;
                                
                                if (entity != null && player != null)
                                {
                                    entity.ChangeGridOwnership(player.PlayerID, MyOwnershipShareModeEnum.All);
                                    SendChatMessage(senderSteamId, string.Format("Grid {0} Claimed by player {1}.", entity.DisplayName, player.DisplayName));
                                }
                            }
                        }
                        break;
                    case ConnectionKeys.Delete:
                        {
                            long entityId;
                            if (long.TryParse(entry.Value, out entityId) && MyAPIGateway.Entities.EntityExists(entityId))
                            {
                                var entity = MyAPIGateway.Entities.GetEntityById(entityId);
                                if (entity != null)
                                    entity.Delete();  // Doesn't sync from server to clients, or client to server.
                            }
                        }
                        break;
                    case ConnectionKeys.Revoke:
                        {
                            long entityId;
                            if (long.TryParse(entry.Value, out entityId) && MyAPIGateway.Entities.EntityExists(entityId))
                            {
                                var entity = MyAPIGateway.Entities.GetEntityById(entityId) as IMyCubeGrid;
                                if (entity != null)
                                {
                                    entity.ChangeGridOwnership(0, MyOwnershipShareModeEnum.All);
                                }
                            }
                        }
                        break;
                    #endregion

                    #region connection request
                    case ConnectionKeys.ConnectionRequest:
                        ulong newClientSteamId;
                        if (ulong.TryParse(entry.Value, out newClientSteamId))
                        {
                            var data = new Dictionary<string, string>();
                            if (!string.IsNullOrEmpty(ChatCommandLogic.Instance.AdminNotification) && ChatCommandLogic.Instance.ServerCfg.IsServerAdmin(newClientSteamId))
                                data.Add(ConnectionKeys.AdminNotification, ChatCommandLogic.Instance.AdminNotification);
                            BannedPlayer bannedPlayer1 = ChatCommandLogic.Instance.ServerCfg.ForceBannedPlayers.FirstOrDefault(p => p.SteamId == newClientSteamId);
                            if (bannedPlayer1.SteamId != 0 && !ChatCommandLogic.Instance.ServerCfg.IsServerAdmin(newClientSteamId))
                                data.Add(ConnectionKeys.ForceKick, bannedPlayer1.SteamId.ToString());
                            data.Add(ConnectionKeys.LogPrivateMessages, CommandPrivateMessage.LogPrivateMessages.ToString());
                            //first connection!
                            SendMessageToPlayer(newClientSteamId, data);

                            ChatCommandLogic.Instance.ServerCfg.SendPermissions(newClientSteamId);

                            if (CommandMessageOfTheDay.Content != null && !ServerConfig.ServerIsClient)
                            {
                                //the header must be initialized before the motd otherwise it won't show
                                if (!string.IsNullOrEmpty(CommandMessageOfTheDay.HeadLine))
                                    SendMessageToPlayer(newClientSteamId, ConnectionKeys.MotdHeadLine, CommandMessageOfTheDay.HeadLine);

                                if (CommandMessageOfTheDay.ShowInChat)
                                    SendMessageToPlayer(newClientSteamId, ConnectionKeys.MotdShowInChat, CommandMessageOfTheDay.ShowInChat.ToString());

                                SendMessageToPlayer(newClientSteamId, ConnectionKeys.MessageOfTheDay, CommandMessageOfTheDay.Content);
                            }
                        }
                        break;
                    #endregion
                }
                Logger.Debug(string.Format("[Server]Finished processing KeyValuePair for Key: {0}", entry.Key));
            }
        }


        public static void ProcessServerData(byte[] rawData)
        {
            switch (server_IncomingMessages)
            {
                case 0:
                    ProcessServerData(System.Text.Encoding.Unicode.GetString(rawData));
                    break;
                case 1:
                    server_MessageCache.AddArray(rawData);
                    server_IncomingMessages--;
                    ProcessServerData(System.Text.Encoding.Unicode.GetString(server_MessageCache.ToArray()));
                    break;
                default:
                    server_MessageCache.AddArray(rawData);
                    server_IncomingMessages--;
                    break;
            }
        }

        #endregion

        public static void SendChatMessage(IMyPlayer receiver, string message)
        {
            SendChatMessage(receiver.SteamUserId, message);
        }

        public static void SendChatMessage(ulong steamId, string message)
        {
            if (steamId == 0)
                return;

            var data = new Dictionary<string, string>();
            data.Add(ConnectionHelper.ConnectionKeys.PmReceiver, steamId.ToString());
            data.Add(ConnectionHelper.ConnectionKeys.PmSender, "0");
            data.Add(ConnectionHelper.ConnectionKeys.PmSenderName, "Server");
            data.Add(ConnectionHelper.ConnectionKeys.PmMessage, message);
            string messageData = ConnectionHelper.ConvertData(data);

            SendMessageToPlayer(steamId, ConnectionKeys.PrivateMessage, ConvertData(data));
        }

        public static class ConnectionKeys
        {
            //misc
            public const string AdminLevel = "adminlvl";
            public const string AdminNotification = "adminnot";
            public const string ConfigReload = "cfgrl";
            public const string ConfigSave = "cfgsave";
            public const string ConnectionRequest = "connect";
            public const string ForceKick = "forcekick";
            public const string GlobalMessage = "glmsg";
            public const string IncomingMessageParts = "incmsgpar";
            public const string LogPrivateMessages = "logpm";
            public const string MessageOfTheDay = "motd";
            public const string MessagePart = "msgpart";
            public const string MotdHeadLine = "motdhl";
            public const string MotdShowInChat = "motdsic";
            public const string Pardon = "pard";
            public const string PrivateMessage = "pm";
            public const string Save = "save";
            public const string Sender = "sender";

            //permissions
            public const string CommandLevel = "cpermlvl";
            public const string CommandList = "cpermlst";
            public const string PlayerLevel = "ppermlvl";
            public const string PlayerExtend = "ppermext";
            public const string PlayerRestrict = "ppermres";
            public const string UsePlayerLevel = "ppermupl";
            public const string PlayerList = "ppermlst";
            public const string GroupLevel = "gpermlvl";
            public const string GroupName = "gpermnam";
            public const string GroupAddPlayer = "gpermadd";
            public const string GroupRemovePlayer = "gpermrem";
            public const string GroupCreate = "gpermcre";
            public const string GroupDelete = "gpermdel";
            public const string GroupList = "gpermlst";

            //perm subkeys
            public const string PermEntryName = "pentnam";
            public const string PermEntryLevel = "pentlvl";
            public const string PermEntryId = "pentid";
            public const string PermEntryUsePlayerLevel = "pentupl";
            public const string PermEntryExtensions = "pentext";
            public const string PermEntryRestrictions = "pentres";
            public const string PermEntryMembers = "pentmem";
            public const string PermNewHotlist = "pnewlst";
            public const string PermLastEntry = "plstent";

            //sync
            public const string Claim = "claim";
            public const string Delete = "delete";
            public const string Stop = "stop";
            public const string StopAndMove = "stopmove";
            public const string Revoke = "revoke";

            //pm subkeys
            public const string PmMessage = "msg";
            public const string PmSender = "sender";
            public const string PmSenderName = "sendername";
            public const string Smite = "smite";
            public const string PmReceiver = "receiver";

            //session settings
            public const string Creative = "creative";
            public const string CargoShips = "cargoships";
            public const string CopyPaste = "copypaste";
            public const string Spectator = "spectator";
            public const string Weapons = "weapons";
        }
    }
}
