using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using midspace.adminscripts.Messages.Communication;

namespace midspace.adminscripts.Messages
{
    [ProtoContract]
    public class MessageOfTheDayMessage : MessageBase
    {
        /// <summary>
        /// The content of the message of the day.
        /// </summary>
        [ProtoMember(1)]
        public string Content;

        /// <summary>
        /// The head line of the message of the day.
        /// </summary>
        [ProtoMember(2)]
        public string HeadLine;

        /// <summary>
        /// Determines if the message of the day is displayed in the chat or in a dialog.
        /// </summary>
        [ProtoMember(3)]
        public bool ShowInChat;

        /// <summary>
        /// The fields are supposed to be updated must be set here.
        /// </summary>
        [ProtoMember(4)]
        public ChangedFields FieldsToUpdate;

        public override void ProcessClient()
        {
            //update the fields
            if (FieldsToUpdate.HasFlag(ChangedFields.Content))
            {
                CommandMessageOfTheDay.Content = Content;
                CommandMessageOfTheDay.ReplaceUserVariables();
            }

            if (FieldsToUpdate.HasFlag(ChangedFields.HeadLine))
                CommandMessageOfTheDay.HeadLine = HeadLine;

            if (FieldsToUpdate.HasFlag(ChangedFields.ShowInChat))
                CommandMessageOfTheDay.ShowInChat = ShowInChat;

            //show the motd if we just received it for the first time
            if (!CommandMessageOfTheDay.Received)
            {
                CommandMessageOfTheDay.Received = true;
                if (ChatCommandLogic.Instance.ShowDialogsOnReceive && !String.IsNullOrEmpty(CommandMessageOfTheDay.Content))
                    CommandMessageOfTheDay.ShowMotd();
                return;
            }

            //let the player know if there were changes
            if (FieldsToUpdate.HasFlag(ChangedFields.Content))
                MyAPIGateway.Utilities.ShowMessage("Motd", "The message of the day was updated just now. To see what is new use '/motd'.");
        }

        public override void ProcessServer()
        {
            //update the fields
            if (FieldsToUpdate.HasFlag(ChangedFields.Content))
            {
                Content = ChatCommandLogic.Instance.ServerCfg.SetMessageOfTheDay(Content);
                MessageClientTextMessage.SendMessage(SenderSteamId, "Server", "The message of the day was updated. Please note that you have to use '/cfg save' to save it permanently.");
            }

            if (FieldsToUpdate.HasFlag(ChangedFields.HeadLine))
            {
                CommandMessageOfTheDay.HeadLine = HeadLine;
                MessageClientTextMessage.SendMessage(SenderSteamId, "Server", "The headline of the message of the day was updated. Please note that you have to use '/cfg save' to save it permanently.");
            }

            if (FieldsToUpdate.HasFlag(ChangedFields.ShowInChat))
            {
                CommandMessageOfTheDay.ShowInChat = ShowInChat;
                MessageClientTextMessage.SendMessage(SenderSteamId, "Server", string.Format("The setting motdShowInChat was set to {0}. Please note that you have to use '/cfg save' to save it permanently.", ShowInChat));
            }

            ConnectionHelper.SendMessageToAllPlayers(this);
        }

        [Flags]
        public enum ChangedFields
        {
            Content = 1,
            HeadLine = 2,
            ShowInChat = 4
        }
    }
}
