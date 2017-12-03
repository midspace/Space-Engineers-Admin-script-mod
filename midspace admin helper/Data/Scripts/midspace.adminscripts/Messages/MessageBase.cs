namespace midspace.adminscripts.Messages
{
    using System;
    using midspace.adminscripts.Messages.Communication;
    using midspace.adminscripts.Messages.Permissions;
    using midspace.adminscripts.Messages.Protection;
    using midspace.adminscripts.Messages.Sync;
    using ProtoBuf;
    using Sandbox.ModAPI;

    [ProtoContract]
    // ALL CLASSES DERIVED FROM MessageBase MUST BE ADDED HERE
    [ProtoInclude(1, typeof(MessageAdminNotification))]
    [ProtoInclude(2, typeof(MessageChatHistory))]
    [ProtoInclude(3, typeof(MessageChatCommand))]
    [ProtoInclude(4, typeof(MessageConfig))]
    [ProtoInclude(5, typeof(MessageConnectionRequest))]
    [ProtoInclude(6, typeof(MessageFactionMessage))]
    [ProtoInclude(7, typeof(MessageForceDisconnect))]
    [ProtoInclude(8, typeof(MessageGlobalMessage))]
    [ProtoInclude(11, typeof(MessageOfTheDayMessage))]
    [ProtoInclude(12, typeof(MessagePermissionRequest))]
    [ProtoInclude(13, typeof(MessagePrivateMessage))]
    [ProtoInclude(14, typeof(MessageSave))]
    [ProtoInclude(15, typeof(MessageSession))]
    [ProtoInclude(16, typeof(MessageTaggedEntityStore))]
    [ProtoInclude(17, typeof(MessageSaveTeleportHistory))]
    //permissions
    [ProtoInclude(30, typeof(MessageCommandPermission))]
    [ProtoInclude(31, typeof(MessageGroupPermission))]
    [ProtoInclude(32, typeof(MessagePlayerPermission))]
    //protection
    [ProtoInclude(40, typeof(MessageProtectionArea))]
    [ProtoInclude(41, typeof(MessageProtectionConfig))]
    [ProtoInclude(42, typeof(MessageSyncProtection))]
    //sync
    [ProtoInclude(50, typeof(MessageSyncAres))]
    [ProtoInclude(51, typeof(MessageSyncBlockOwner))]
    [ProtoInclude(52, typeof(MessageSyncGridChange))]
    [ProtoInclude(53, typeof(MessageSyncVoxelChange))]
    [ProtoInclude(54, typeof(MessageSyncEntity))]
    [ProtoInclude(55, typeof(MessageSyncFaction))]
    [ProtoInclude(56, typeof(MessageSyncBlock))]
    [ProtoInclude(57, typeof(MessageSyncGod))]
    [ProtoInclude(58, typeof(MessageSyncInvisible))]
    [ProtoInclude(59, typeof(MessageSyncCreateObject))]
    [ProtoInclude(60, typeof(MessageSyncCreatePrefab))]
    [ProtoInclude(61, typeof(MessageSyncFloatingObjects))]
    //communication
    [ProtoInclude(70, typeof(MessageClientDialogMessage))]
    [ProtoInclude(71, typeof(MessageClientNotification))]
    [ProtoInclude(72, typeof(MessageClientTextMessage))]
    public abstract class MessageBase
    {
        /// <summary>
        /// The SteamId of the message's sender. Note that this will be set when the message is sent, so there is no need for setting it otherwise.
        /// </summary>
        [ProtoMember(100)]
        public ulong SenderSteamId;

        /// <summary>
        /// Defines on which side the message should be processed. Note that this will be set when the message is sent, so there is no need for setting it otherwise.
        /// </summary>
        [ProtoMember(101)]
        public MessageSide Side;

        public MessageBase()
        {
            if (MyAPIGateway.Multiplayer != null && MyAPIGateway.Multiplayer.IsServer)
                SenderSteamId = MyAPIGateway.Multiplayer.ServerId;
            if (MyAPIGateway.Session != null && MyAPIGateway.Session.Player != null)
                SenderSteamId = MyAPIGateway.Session.Player.SteamUserId;
        }

        /*
        [ProtoAfterDeserialization]
        void InvokeProcessing() // is not invoked after deserialization from xml
        {
            Logger.Debug("START - Processing");
            switch (Side)
            {
                case MessageSide.ClientSide:
                    ProcessClient();
                    break;
                case MessageSide.ServerSide:
                    ProcessServer();
                    break;
            }
            Logger.Debug("END - Processing");
        }
        */

        public void InvokeProcessing()
        {
            switch (Side)
            {
                case MessageSide.ClientSide:
                    InvokeClientProcessing();
                    break;
                case MessageSide.ServerSide:
                    InvokeServerProcessing();
                    break;
            }
        }

        private void InvokeClientProcessing()
        {
            Logger.Debug("START - Processing [Client] {0}", this.GetType().Name);
            try
            {
                ProcessClient();
            }
            catch (Exception)
            {
                // TODO send error to server and notify admins
            }
            Logger.Debug("END - Processing [Client] {0}", this.GetType().Name);
        }

        private void InvokeServerProcessing()
        {
            Logger.Debug("START - Processing [Server] {0}", this.GetType().Name);

            try
            {
                ProcessServer();
            }
            catch (Exception ex)
            {
                AdminNotificator.StoreExceptionAndNotify(ex);
            }

            Logger.Debug("END - Processing [Server] {0}", this.GetType().Name);
        }

        public abstract void ProcessClient();
        public abstract void ProcessServer();
    }
}
