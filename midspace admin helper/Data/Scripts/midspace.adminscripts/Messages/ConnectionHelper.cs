namespace midspace.adminscripts
{
    using midspace.adminscripts.Messages;
    using Sandbox.ModAPI;
    using System;
    using System.Collections.Generic;
    using VRage.Game.ModAPI;

    /// <summary>
    /// Conains useful methods and fields for organizing the connections.
    /// </summary>
    public static class ConnectionHelper
    {
        /// <summary>
        /// Id for messages.
        /// </summary>
        public const ushort ConnectionId = 16103;

        #region connections to server

        /// <summary>
        /// Creates and sends an entity with the given information for the server. Never call this on DS instance!
        /// </summary>

        public static void SendMessageToServer(MessageBase message)
        {
            message.Side = MessageSide.ServerSide;
            if (MyAPIGateway.Multiplayer.IsServer)
                message.SenderSteamId = MyAPIGateway.Multiplayer.ServerId;
            if (MyAPIGateway.Session.Player != null)
                message.SenderSteamId = MyAPIGateway.Session.Player.SteamUserId;

            byte[] byteData = MyAPIGateway.Utilities.SerializeToBinary(message);
            MyAPIGateway.Multiplayer.SendMessageToServer(ConnectionId, byteData);
        }

        #endregion

        #region connections to all

        /// <summary>
        /// Creates and sends an entity with the given information for the server and all players.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="syncAll"></param>
        public static void SendMessageToAll(MessageBase message, bool syncAll = true)
        {
            if (MyAPIGateway.Multiplayer.IsServer)
                message.SenderSteamId = MyAPIGateway.Multiplayer.ServerId;
            if (MyAPIGateway.Session.Player != null)
                message.SenderSteamId = MyAPIGateway.Session.Player.SteamUserId;

            if (syncAll || !MyAPIGateway.Multiplayer.IsServer)
                SendMessageToServer(message);
            SendMessageToAllPlayers(message);
        }

        #endregion

        #region connections to clients

        public static void SendMessageToPlayer(ulong steamId, MessageBase message)
        {
            Logger.Debug("SendMessageToPlayer {0} {1} {2}.", steamId, message.Side, message.GetType().Name);

            message.Side = MessageSide.ClientSide;
            byte[] byteData = MyAPIGateway.Utilities.SerializeToBinary(message);
            MyAPIGateway.Multiplayer.SendMessageTo(ConnectionId, byteData, steamId);
        }

        public static void SendMessageToAllPlayers(MessageBase message)
        {
            //MyAPIGateway.Multiplayer.SendMessageToOthers(StandardClientId, MyAPIGateway.Utilities.SerializeToBinary(message)); <- does not work as expected ... so it doesn't work at all?

            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, p => p != null);
            foreach (IMyPlayer player in players)
                SendMessageToPlayer(player.SteamUserId, message);
        }

        #endregion

        #region processing

        public static void ProcessData(byte[] rawData)
        {
            Logger.Debug("START - Message Serialization");
            MessageBase baseMessage;

            try
            {
                baseMessage = MyAPIGateway.Utilities.SerializeFromBinary<MessageBase>(rawData);
            }
            catch
            {
                Logger.Debug("ERROR - Message cannot Deserialize");
                return;
            }

            Logger.Debug("END - Message Serialization");

            if (baseMessage != null)
            {
                try
                {
                    baseMessage.InvokeProcessing();
                }
                catch (Exception e)
                {
                    Logger.Debug("Processing message exception. Side: {0}, Exception: {1}", baseMessage.Side, e.ToString());
                }
            }
        }

        #endregion
    }
}
