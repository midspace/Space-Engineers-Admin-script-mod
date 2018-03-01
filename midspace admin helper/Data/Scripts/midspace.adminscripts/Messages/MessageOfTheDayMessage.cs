namespace midspace.adminscripts.Messages
{
    using ProtoBuf;
    using Sandbox.ModAPI;
    using System;
    using midspace.adminscripts.Messages.Communication;

    [ProtoContract]
    public class MessageOfTheDayMessage : MessageBase
    {
        /// <summary>
        /// The content of the message of the day.
        /// </summary>
        [ProtoMember(201)]
        public string Content;

        /// <summary>
        /// The head line of the message of the day.
        /// </summary>
        [ProtoMember(202)]
        public string HeadLine;

        /// <summary>
        /// Determines if the message of the day is displayed in the chat or in a dialog.
        /// </summary>
        [ProtoMember(203)]
        public bool ShowInChat;

        /// <summary>
        /// The fields are supposed to be updated must be set here.
        /// </summary>
        [ProtoMember(204)]
        public ChangedFields FieldsToUpdate;

        public override void ProcessClient()
        {
            //update the fields
            if (FieldsToUpdate.HasFlag(ChangedFields.Content))
                CommandMessageOfTheDay.Content = Content;

            if (FieldsToUpdate.HasFlag(ChangedFields.HeadLine))
                CommandMessageOfTheDay.HeadLine = HeadLine;

            CommandMessageOfTheDay.ReplaceUserVariables();

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
                MessageClientTextMessage.SendMessage(SenderSteamId, "Server", $"The setting motdShowInChat was set to {ShowInChat}. Please note that you have to use '/cfg save' to save it permanently.");
            }

            ConnectionHelper.SendMessageToAllPlayers(this);
        }

        [Flags]
        public enum ChangedFields : byte
        {
            None = 0x00,
            Content = 0x01,
            HeadLine = 0x02,
            ShowInChat = 0x04
        }
    }
}
