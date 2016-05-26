using System;
using System.Xml.Serialization;
using midspace.adminscripts.Messages.Communication;
using midspace.adminscripts.Messages.Permissions;
using midspace.adminscripts.Messages.Protection;
using midspace.adminscripts.Messages.Sync;
using ProtoBuf;
using Sandbox.ModAPI;

namespace midspace.adminscripts.Messages
{
    // ALL CLASSES DERIVED FROM MessageBase MUST BE ADDED HERE
    [XmlInclude(typeof(MessageAdminNotification))]
    [XmlInclude(typeof(MessageChatHistory))]
    [XmlInclude(typeof(MessageChatCommand))]
    [XmlInclude(typeof(MessageConfig))]
    [XmlInclude(typeof(MessageConnectionRequest))]
    [XmlInclude(typeof(MessageFactionMessage))]
    [XmlInclude(typeof(MessageForceDisconnect))]
    [XmlInclude(typeof(MessageGlobalMessage))]
    [XmlInclude(typeof(MessageIncomingMessageParts))]
    [XmlInclude(typeof(MessageOfTheDayMessage))]
    [XmlInclude(typeof(MessagePermissionRequest))]
    [XmlInclude(typeof(MessagePrivateMessage))]
    [XmlInclude(typeof(MessageSave))]
    [XmlInclude(typeof(MessageSession))]
    [XmlInclude(typeof(MessageTaggedEntityStore))]
    [XmlInclude(typeof(MessageSaveTeleportHistory))]
    //permissions
    [XmlInclude(typeof(MessageCommandPermission))]
    [XmlInclude(typeof(MessageGroupPermission))]
    [XmlInclude(typeof(MessagePlayerPermission))]
    //protection
    [XmlInclude(typeof(MessageProtectionArea))]
    [XmlInclude(typeof(MessageProtectionConfig))]
    [XmlInclude(typeof(MessageSyncProtection))]
    //sync
    [XmlInclude(typeof(MessageSyncAres))]
    [XmlInclude(typeof(MessageSyncBlockOwner))]
    [XmlInclude(typeof(MessageSyncGridChange))]
    [XmlInclude(typeof(MessageSyncEntity))]
    [XmlInclude(typeof(MessageSyncFaction))]
    [XmlInclude(typeof(MessageSyncBlock))]
    [XmlInclude(typeof(MessageSyncGod))]
    [XmlInclude(typeof(MessageSyncInvisible))]
    [XmlInclude(typeof(MessageSyncCreateObject))]
    [XmlInclude(typeof(MessageSyncCreatePrefab))]
    [XmlInclude(typeof(MessageSyncFloatingObjects))]
    //communication
    [XmlInclude(typeof(MessageClientDialogMessage))]
    [XmlInclude(typeof(MessageClientNotification))]
    [XmlInclude(typeof(MessageClientTextMessage))]
    [ProtoContract]
    public abstract class MessageBase
    {
        /// <summary>
        /// The SteamId of the message's sender. Note that this will be set when the message is sent, so there is no need for setting it otherwise.
        /// </summary>
        [ProtoMember(1)]
        public ulong SenderSteamId;

        /// <summary>
        /// Defines on which side the message should be processed. Note that this will be set when the message is sent, so there is no need for setting it otherwise.
        /// </summary>
        [ProtoMember(2)]
        public MessageSide Side;

        public MessageBase()
        {
            if (MyAPIGateway.Session.Player != null)
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
            catch (Exception ex)
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
