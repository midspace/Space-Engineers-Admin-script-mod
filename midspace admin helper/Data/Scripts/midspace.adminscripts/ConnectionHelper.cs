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
        /// Id for messages.
        /// </summary>
        public const ushort StandardClientId = 16103;
        public const ushort StandardServerId = StandardClientId + 1;

        /// <summary>
        /// True if an id request was sent otherwise false.
        /// </summary>
        public static bool ReceivedInitialRequest;

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
            content.Add(ConnectionKeys.Sender, MyAPIGateway.Session.Player.SteamUserId.ToString());
            MyAPIGateway.Multiplayer.SendMessageToServer(StandardServerId, System.Text.Encoding.Unicode.GetBytes(ConvertData(content)));
        }

        #endregion

        #region connections to clients


        public static void SendMessageToPlayer(IMyPlayer player, Dictionary<string, string> content)
        {
            SendMessageToPlayer(player.SteamUserId, content);
        }

        public static void SendMessageToPlayer(ulong steamId, Dictionary<string, string> content)
        {
            MyAPIGateway.Multiplayer.SendMessageTo(StandardClientId, System.Text.Encoding.Unicode.GetBytes(ConvertData(content)), steamId);
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
                Logger.Debug(string.Format("[Client]Processing KeyValuePair - Key: {0}, Value: {1}", entry.Key, entry.Value));
                switch (entry.Key)
                {
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
                            if (CommandMessageOfTheDay.ShowOnReceive)
                                CommandMessageOfTheDay.ShowMotd();
                            break;
                        }
                        MyAPIGateway.Utilities.ShowMessage("Motd", "The message of the day was updated just now. To see what is new use '/motd'.");
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
                    case ConnectionKeys.Command:
                        {
                            uint level;
                            string[] values = entry.Value.Split(':');

                            if (values.Length < 2)
                                break;

                            if (uint.TryParse(values[1], out level))
                            {
                                ChatCommandService.UpdateCommandSecurity(values[0], level);
                            }
                        }
                        break;
                    case ConnectionKeys.PlayerLevel:
                        uint newUserSecurity;
                        if (uint.TryParse(entry.Value, out newUserSecurity))
                            ChatCommandService.UserSecurity = newUserSecurity;
                        ChatCommandLogic.Instance.BlockCommandExecution = false;
                        break;
                    case ConnectionKeys.ForceKick:
                        ulong steamId;
                        if (ulong.TryParse(entry.Value, out steamId) && steamId == MyAPIGateway.Session.Player.SteamUserId)
                            CommandForceKick.DropPlayer = true;
                        break;
                    case ConnectionKeys.Smite:
                        CommandPlayerSmite.Smite(MyAPIGateway.Session.Player);
                        break;
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
                }
                Logger.Debug(string.Format("[Client]Finished processing KeyValuePair for Key: {0}", entry.Key));
            }
        }

        public static void ProcessClientData(byte[] rawData)
        {
            ProcessClientData(System.Text.Encoding.Unicode.GetString(rawData));
        }

        #region initial data

        /// <summary>
        /// Client side. Process the initial data sent from the server.
        /// </summary>
        /// <param name="dataString"></param>
        public static void ProcessInitialData(string dataString)
        {
            Logger.Debug("[Client]Processing Initital Data...");
            var parsedData = Parse(dataString);
            foreach (KeyValuePair<string, string> entry in parsedData)
            {
                switch (entry.Key)
                {
                    case ConnectionKeys.AdminNotification:
                        ChatCommandLogic.Instance.AdminNotification = entry.Value;
                        if (CommandMessageOfTheDay.ShowOnReceive)
                            MyAPIGateway.Utilities.ShowMissionScreen("Admin Message System", "Error", null, ChatCommandLogic.Instance.AdminNotification, null, null);
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
                }
            }
            Logger.Debug("[Client]Finished processing Initital Data");
        }

        public static void ProcessInitialData(byte[] rawData)
        {
            ProcessInitialData(System.Text.Encoding.Unicode.GetString(rawData));
        }

        #endregion

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
                    case ConnectionKeys.MessageOfTheDay:
                        ChatCommandLogic.Instance.ServerCfg.SetMessageOfTheDay(entry.Value);
                        SendMessageToAllPlayers(ConnectionKeys.MessageOfTheDay, CommandMessageOfTheDay.Content);
                        break;
                    case ConnectionKeys.MotdHeadLine:
                        CommandMessageOfTheDay.HeadLine = entry.Value;
                        SendMessageToAllPlayers(ConnectionKeys.MotdHeadLine, entry.Value);
                        break;
                    case ConnectionKeys.MotdShowInChat:
                        bool motdsic;
                        if (bool.TryParse(entry.Value, out motdsic))
                        {
                            CommandMessageOfTheDay.ShowInChat = motdsic;
                            SendMessageToAllPlayers(ConnectionKeys.MotdShowInChat, entry.Value);
                        }
                        break;
                    case ConnectionKeys.Save:
                        if (ChatCommandLogic.Instance.ServerCfg.ServerIsClient)
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
                    case ConnectionKeys.Command:
                        //TODO restrict/extend the command security
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
                                    ChatCommandLogic.Instance.ServerCfg.ForceBannedPlayer.Add(new BannedPlayer()
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
                        BannedPlayer bannedPlayer = ChatCommandLogic.Instance.ServerCfg.ForceBannedPlayer.FirstOrDefault(p => p.PlayerName.Equals(entry.Value, StringComparison.InvariantCultureIgnoreCase));
                        if (bannedPlayer.SteamId != 0)
                        {
                            ChatCommandLogic.Instance.ServerCfg.ForceBannedPlayer.Remove(bannedPlayer);
                            SendChatMessage(senderSteamId, string.Format("Pardoned player {0}", bannedPlayer.PlayerName));
                        }
                        break;
                    case ConnectionKeys.ConfigSave:
                        ChatCommandLogic.Instance.ServerCfg.Save();
                        SendChatMessage(senderSteamId, "Config saved.");
                        break;
                    case ConnectionKeys.ConfigReload:
                        ChatCommandLogic.Instance.ServerCfg.Load();
                        SendChatMessage(senderSteamId, "Config reloaded.");
                        break;
                    case ConnectionKeys.GlobalMessage:
                        ChatCommandLogic.Instance.ServerCfg.LogGlobalMessage(senderSteamId, entry.Value);
                        break;
                    case ConnectionKeys.Smite:
                        ulong smitePlayerSteamId;
                        if (ulong.TryParse(entry.Value, out smitePlayerSteamId))
                            SendMessageToPlayer(smitePlayerSteamId, ConnectionKeys.Smite, "Smite yourself :D");
                        break;
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
                    case ConnectionKeys.Stop:
                        {
                            long entityId;
                            if (long.TryParse(entry.Value, out entityId) && MyAPIGateway.Entities.ExistsById(entityId))
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

                            if (values.Length > 3 && long.TryParse(values[0], out entityId) && MyAPIGateway.Entities.ExistsById(entityId)
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
                            if (values.Length > 1 && long.TryParse(values[0], out playerId) && long.TryParse(values[1], out entityId) && MyAPIGateway.Entities.ExistsById(entityId))
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
                    case ConnectionKeys.Revoke:
                        {
                            long entityId;
                            if (long.TryParse(entry.Value, out entityId) && MyAPIGateway.Entities.ExistsById(entityId))
                            {
                                var entity = MyAPIGateway.Entities.GetEntityById(entityId) as IMyCubeGrid;
                                if (entity != null)
                                {
                                    entity.ChangeGridOwnership(0, MyOwnershipShareModeEnum.All);
                                }
                            }
                        }
                        break;
                    #region connection request
                    case ConnectionKeys.ConnectionRequest:
                        ulong newClientSteamId;
                        if (ulong.TryParse(entry.Value, out newClientSteamId))
                        {
                            var data = new Dictionary<string, string>();
                            if (!string.IsNullOrEmpty(ChatCommandLogic.Instance.AdminNotification) && ChatCommandLogic.Instance.ServerCfg.IsServerAdmin(newClientSteamId))
                                data.Add(ConnectionKeys.AdminNotification, ChatCommandLogic.Instance.AdminNotification);
                            BannedPlayer bannedPlayer1 = ChatCommandLogic.Instance.ServerCfg.ForceBannedPlayer.FirstOrDefault(p => p.SteamId == newClientSteamId);
                            if (bannedPlayer1.SteamId != 0 && !ChatCommandLogic.Instance.ServerCfg.IsServerAdmin(newClientSteamId))
                                data.Add(ConnectionKeys.ForceKick, bannedPlayer1.SteamId.ToString());
                            //only send the command permission if it is set, disabled by now
                            /*if (!string.IsNullOrEmpty(ChatCommandLogic.Instance.ServerCfg.CommandPermissions))
                                data.Add("cmd", ChatCommandLogic.Instance.ServerCfg.CommandPermissions);*/
                            data.Add(ConnectionKeys.LogPrivateMessages, CommandPrivateMessage.LogPrivateMessages.ToString());
                            //first connection!
                            SendMessageToPlayer(newClientSteamId, data);

                            ChatCommandLogic.Instance.ServerCfg.SendPermissions(newClientSteamId);

                            if (CommandMessageOfTheDay.Content != null)
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
            ProcessServerData(System.Text.Encoding.Unicode.GetString(rawData));
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
            public const string ConnectionRequest = "connect";
            public const string MessageOfTheDay = "motd";
            public const string MotdHeadLine = "motdhl";
            public const string MotdShowInChat = "motdsic";
            public const string AdminNotification = "adminnot";
            public const string ForceKick = "forcekick";
            public const string PrivateMessage = "pm";
            public const string Command = "cmd";
            public const string Save = "save";
            public const string Pardon = "pard";
            public const string ConfigSave = "cfgsave";
            public const string ConfigReload = "cfgrl";
            public const string Sender = "sender";
            public const string LogPrivateMessages = "logpm";
            public const string GlobalMessage = "glmsg";
            public const string Smite = "smite";
            public const string Creative = "creative";
            public const string CargoShips = "cargoships";
            public const string CopyPaste = "copypaste";
            public const string Spectator = "spectator";
            public const string Weapons = "weapons";
            public const string PlayerLevel = "plvl";
            public const string Stop = "stop";
            public const string StopAndMove = "stopmove";
            public const string Claim = "claim";
            public const string Revoke = "revoke";

            //pm subkeys
            public const string PmMessage = "msg";
            public const string PmSender = "sender";
            public const string PmSenderName = "sendername";
            public const string PmReceiver = "receiver";
        }
    }
}
