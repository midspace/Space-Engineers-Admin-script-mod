using midspace.adminscripts.Messages;
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

        public static List<byte> Client_MessageCache = new List<byte>();
        public static Dictionary<ulong, List<byte>> Server_MessageCache = new Dictionary<ulong, List<byte>>();

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
        /// Prepares and sends a message to the server. Never call this on DS instance!
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
                SendMessageParts(byteData, MessageSide.ServerSide);
        }

        public static void SendMessageToServer(MessageBase message)
        {
            message.Side = MessageSide.ServerSide;
            message.SenderSteamId = MyAPIGateway.Session.Player.SteamUserId;
            var xml = MyAPIGateway.Utilities.SerializeToXML<MessageContainer>(new MessageContainer() { Content = message });
            byte[] byteData = System.Text.Encoding.Unicode.GetBytes(xml);
            if (byteData.Length <= MAX_MESSAGE_SIZE)
                MyAPIGateway.Multiplayer.SendMessageToServer(StandardServerId, byteData);
            else
                SendMessageParts(byteData, MessageSide.ServerSide);
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
                SendMessageParts(byteData, MessageSide.ClientSide, steamId);
        }

        public static void SendMessageToPlayer(ulong steamId, MessageBase message)
        {
            message.Side = MessageSide.ClientSide;
            var xml = MyAPIGateway.Utilities.SerializeToXML<MessageContainer>(new MessageContainer() { Content = message });
            byte[] byteData = System.Text.Encoding.Unicode.GetBytes(xml);
            if (byteData.Length <= MAX_MESSAGE_SIZE)
                MyAPIGateway.Multiplayer.SendMessageTo(StandardClientId, byteData, steamId);
            else
                SendMessageParts(byteData, MessageSide.ClientSide, steamId);
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

        public static void SendMessageToAllPlayers(MessageBase messageContainer)
        {
            //MyAPIGateway.Multiplayer.SendMessageToOthers(StandardClientId, System.Text.Encoding.Unicode.GetBytes(ConvertData(content))); <- does not work as expected ... so it doesn't work at all?
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, p => p != null);
            foreach (IMyPlayer player in players)
                SendMessageToPlayer(player.SteamUserId, messageContainer);
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
            try
            {
                Logger.Debug("START - Message Serialization");
                var message = MyAPIGateway.Utilities.SerializeFromXML<MessageContainer>(dataString).Content;
                Logger.Debug("END - Message Serialization");

                message.InvokeProcessing();
                return;
            }
            catch (Exception e)
            {
                Logger.Debug(e.ToString());
            }

            var parsedData = Parse(dataString);
            foreach (KeyValuePair<string, string> entry in parsedData)
            {
                Logger.Debug(string.Format("[Client]Processing KeyValuePair - Key: {0}, Value: {1}", entry.Key, entry.Value));
                switch (entry.Key)
                {
                    #region misc
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
                                case ConnectionKeys.ListEntry:
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
                                case ConnectionKeys.NewList:
                                    newPlayerList = true;
                                    break;
                                case ConnectionKeys.ListLastEntry:
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
                                case ConnectionKeys.ListEntry:
                                    groupName = pair.Value;
                                    break;
                                case ConnectionKeys.PermEntryLevel:
                                    groupLevel = pair.Value;
                                    break;
                                case ConnectionKeys.PermEntryMembers:
                                    members = pair.Value;
                                    break;
                                case ConnectionKeys.NewList:
                                    newGroupList = true;
                                    break;
                                case ConnectionKeys.ListLastEntry:
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
            ProcessClientData(System.Text.Encoding.Unicode.GetString(rawData));
        }

        #endregion

        #region Server side processing

        /// <summary>
        /// Server side execution of the actions defined in the data.
        /// </summary>
        /// <param name="dataString"></param>
        public static void ProcessServerData(string dataString)
        {
            try
            {
                Logger.Debug("START - Message Serialization");
                var message = MyAPIGateway.Utilities.SerializeFromXML<MessageContainer>(dataString).Content;
                Logger.Debug("END - Message Serialization");

                message.InvokeProcessing();
                return;
            }
            catch (Exception e)
            {
                Logger.Debug(e.ToString());
            }

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
                    #region misc
                    case ConnectionKeys.Save:
                        if (ServerConfig.ServerIsClient && senderSteamId != MyAPIGateway.Session.Player.SteamUserId) //no one should be able to do that
                        {
                            SendChatMessage(senderSteamId, "Saving the session on a locally hosted server is not allowed.");
                            break; 
                        }

                        if (string.IsNullOrEmpty(entry.Value))
                        {
                            MyAPIGateway.Session.Save();
                            SendChatMessage(senderSteamId, "Session saved.");
                        }
                        else
                        {
                            MyAPIGateway.Session.Save(entry.Value);
                            SendChatMessage(senderSteamId, string.Format("Session saved as {0}.", entry.Value));
                        }
                        break;
                    case ConnectionKeys.ForceKick:
                        {
                            string[] values = entry.Value.Split(':');
                            bool ban = false;
                            ulong steamId;
                            if (ulong.TryParse(values[0], out steamId) && !ServerConfig.IsServerAdmin(steamId))
                            {
                                var players = new List<IMyPlayer>();
                                MyAPIGateway.Players.GetPlayers(players, p => p != null && p.SteamUserId == steamId);
                                IMyPlayer player = players.FirstOrDefault();
                                if (values.Length > 1 && bool.TryParse(values[1], out ban) && ban)
                                {
                                    ChatCommandLogic.Instance.ServerCfg.ForceBannedPlayers.Add(new Player()
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
                        Player bannedPlayer = ChatCommandLogic.Instance.ServerCfg.ForceBannedPlayers.FirstOrDefault(p => p.PlayerName.Equals(entry.Value, StringComparison.InvariantCultureIgnoreCase));
                        if (bannedPlayer.SteamId != 0)
                        {
                            ChatCommandLogic.Instance.ServerCfg.ForceBannedPlayers.Remove(bannedPlayer);
                            SendChatMessage(senderSteamId, string.Format("Pardoned player {0}", bannedPlayer.PlayerName));
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
                }
                Logger.Debug(string.Format("[Server]Finished processing KeyValuePair for Key: {0}", entry.Key));
            }
        }


        public static void ProcessServerData(byte[] rawData)
        {
            ProcessServerData(System.Text.Encoding.Unicode.GetString(rawData));
        }

        #endregion

        /// <summary>
        /// Calculates how many bytes can be stored in the given message.
        /// </summary>
        /// <param name="message">The message in which the bytes will be stored.</param>
        /// <returns>The number of bytes that can be stored until the message is too big to be sent.</returns>
        public static int GetFreeByteElementCount(MessageIncomingMessageParts message)
        {
            message.Content = new byte[1];
            var xmlText = MyAPIGateway.Utilities.SerializeToXML<MessageContainer>(new MessageContainer() { Content = message });
            var oneEntry = System.Text.Encoding.Unicode.GetBytes(xmlText).Length;

            message.Content = new byte[4];
            xmlText = MyAPIGateway.Utilities.SerializeToXML<MessageContainer>(new MessageContainer() { Content = message });
            var twoEntries = System.Text.Encoding.Unicode.GetBytes(xmlText).Length;

            // we calculate the difference between one and two entries in the array to get the count of bytes that describe one entry
            // we divide by 3 because 3 entries are stored in one block of the array
            var difference = (double)(twoEntries - oneEntry) / 3d;

            // get the size of the message without any entries
            var freeBytes = MAX_MESSAGE_SIZE - oneEntry - Math.Ceiling(difference);

            int count = (int)Math.Floor((double)freeBytes / difference);

            // finally we test if the calculation was right
            message.Content = new byte[count];
            xmlText = MyAPIGateway.Utilities.SerializeToXML<MessageContainer>(new MessageContainer() { Content = message });
            var finalLength = System.Text.Encoding.Unicode.GetBytes(xmlText).Length;
            Logger.Debug(string.Format("FinalLength: {0}", finalLength));
            if (MAX_MESSAGE_SIZE >= finalLength)
                return count;
            else
                throw new Exception(string.Format("Calculation failed. OneEntry: {0}, TwoEntries: {1}, Difference: {2}, FreeBytes: {3}, Count: {4}, FinalLength: {5}", oneEntry, twoEntries, difference, freeBytes, count, finalLength));
        }

        private static void SendMessageParts(byte[] byteData, MessageSide side, ulong receiver = 0)
        {
            var byteList = byteData.ToList();

            while (byteList.Count > 0)
            {
                // we create an empty message part
                var messagePart = new MessageIncomingMessageParts()
                {
                    Side = side,
                    SenderSteamId = side == MessageSide.ServerSide ? MyAPIGateway.Session.Player.SteamUserId : 0,
                    LastPart = false,
                };

                try
                {
                    // let's check how much we could store in the message
                    int freeBytes = GetFreeByteElementCount(messagePart);

                    int count = freeBytes;

                    // we check if that might be the last message
                    if (freeBytes > byteList.Count)
                    {
                        messagePart.LastPart = true;

                        // since we changed LastPart, we should make sure that we are still able to send all the stuff
                        if (GetFreeByteElementCount(messagePart) > byteList.Count)
                        {
                            count = byteList.Count;
                        }
                        else
                            throw new Exception("Failed to send message parts. The leftover could not be sent!");
                    }

                    // fill the message with content
                    messagePart.Content = byteList.GetRange(0, count).ToArray();
                    var xmlPart = MyAPIGateway.Utilities.SerializeToXML<MessageContainer>(new MessageContainer() { Content = messagePart });
                    var bytes = System.Text.Encoding.Unicode.GetBytes(xmlPart);

                    // and finally send the message
                    switch (side)
                    {
                        case MessageSide.ClientSide:
                            if (MyAPIGateway.Multiplayer.SendMessageTo(StandardClientId, bytes, receiver))
                                byteList.RemoveRange(0, count);
                            else
                                throw new Exception("Failed to send message parts to client.");
                            break;
                        case MessageSide.ServerSide:
                            if (MyAPIGateway.Multiplayer.SendMessageToServer(StandardServerId, bytes))
                                byteList.RemoveRange(0, count);
                            else
                                throw new Exception("Failed to send message parts to server.");
                            break;
                    }

                }
                catch (Exception ex)
                {
                    AdminNotificator.StoreExceptionAndNotify(ex);
                    return;
                }
            }
        }

        public static void SendChatMessage(IMyPlayer receiver, string message)
        {
            SendChatMessage(receiver.SteamUserId, message);
        }

        public static void SendChatMessage(ulong steamId, string message)
        {
            if (steamId == 0)
                return;

            var chatMessage = new MessagePrivateMessage() {
                ChatMessage = new ChatMessage()
                {
                    Sender = new Player()
                    {
                        PlayerName = "Server",
                        SteamId = 0
                    },
                    Text = message,
                }
            };

            SendMessageToPlayer(steamId, chatMessage);
        }

        public static class ConnectionKeys
        {
            //misc
            public const string AdminLevel = "adminlvl";
            public const string AdminNotification = "adminnot";
            public const string ConnectionRequest = "connect";
            public const string ForceKick = "forcekick";
            public const string LogPrivateMessages = "logpm";
            public const string Pardon = "pard";
            public const string Save = "save";
            public const string SaveTime = "savetime";
            public const string Sender = "sender";

            //permissions
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
            public const string PermEntryLevel = "pentlvl";
            public const string PermEntryId = "pentid";
            public const string PermEntryUsePlayerLevel = "pentupl";
            public const string PermEntryExtensions = "pentext";
            public const string PermEntryRestrictions = "pentres";
            public const string PermEntryMembers = "pentmem";

            //lists
            public const string ListEntry = "lstent";
            public const string NewList = "lstnew";
            public const string ListLastEntry = "lstlastent";

            //sync
            public const string Claim = "claim";
            public const string Delete = "delete";
            public const string Smite = "smite";
            public const string Stop = "stop";
            public const string StopAndMove = "stopmove";
            public const string Revoke = "revoke";

            //session settings
            public const string Creative = "creative";
            public const string CargoShips = "cargoships";
            public const string CopyPaste = "copypaste";
            public const string Spectator = "spectator";
            public const string Weapons = "weapons";
        }
    }
}
