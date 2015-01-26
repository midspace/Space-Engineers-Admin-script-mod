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
        /// Creates and sends an entity.
        /// </summary>
        /// <param name="id">The id of the client that gets the information</param>
        /// <param name="content">The information that will be send to the player</param>
        public static void CreateAndSendConnectionEntity(string id, Dictionary<string, string> content)
        {
           SendConnectionEntity(CreateConnectionEntity(id, content));
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
                    case "motd":
                        CommandMessageOfTheDay.MessageOfTheDay = entry.Value;
                        CommandMessageOfTheDay.ShowMotd();
                        break;
                    case "msg":
                        //TODO create private message command
                        break;
                    case "cmd":
                        //TODO restrict/extend the permissions
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
                    case "id":
                        ClientPrefix = entry.Value;
                        break;
                    case "serverid":
                        ServerPrefix = entry.Value;
                        break;
                    case "motd":
                        CommandMessageOfTheDay.MessageOfTheDay = entry.Value;
                        CommandMessageOfTheDay.Received = true;
                        if (CommandMessageOfTheDay.ShowMotdOnReceive)
                            CommandMessageOfTheDay.ShowMotd();
                        break;
                    case "cmd":
                        //TODO restrict/extend the permissions
                        break;
                }
            }
        }

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
                    case "motd":
                        CommandMessageOfTheDay.MessageOfTheDay = entry.Value;
                        //TODO send it to the connected clients and save it
                        break;
                    case "save":
                        if (string.IsNullOrEmpty(entry.Value))
                            MyAPIGateway.Session.Save();
                        else
                            MyAPIGateway.Session.Save(entry.Value);
                        //TODO implement a command that uses this
                        break;
                    case "msg":
                        //TODO create private message command
                        break;
                    case "cmd":
                        //TODO restrict/extend the command security
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
                    case "connect":
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
                            data.Add("id", PlayerConnections[steamId]);
                            data.Add("serverid", ServerPrefix);
                            //only send the motd if there is one
                            if (!string.IsNullOrEmpty(CommandMessageOfTheDay.MessageOfTheDay))
                                data.Add("motd", CommandMessageOfTheDay.MessageOfTheDay);

                            var firstContact = CreateConnectionEntity(BasicPrefix, data);
                            SendConnectionEntity(firstContact);
                        }
                        break;
                }
            }
        }
    }
}
