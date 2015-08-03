using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace midspace.adminscripts.Messages
{
    // ALL CLASSES DERIVED FROM MessageBase MUST BE ADDED HERE
    [XmlInclude(typeof(MessageAdminNotification))]
    [XmlInclude(typeof(MessageChatHistory))]
    [XmlInclude(typeof(MessageCommandPermissions))]
    [XmlInclude(typeof(MessageConfig))]
    [XmlInclude(typeof(MessageConnectionRequest))]
    [XmlInclude(typeof(MessageGlobalMessage))]
    [XmlInclude(typeof(MessageIncomingMessageParts))]
    [XmlInclude(typeof(MessageOfTheDayMessage))]
    [XmlInclude(typeof(MessagePrivateMessage))]
    [XmlInclude(typeof(MessageSession))]
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
