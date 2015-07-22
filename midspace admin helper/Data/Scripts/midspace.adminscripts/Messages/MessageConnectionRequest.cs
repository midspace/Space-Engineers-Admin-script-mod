using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace midspace.adminscripts.Messages
{
    public class MessageConnectionRequest : MessageBase
    {
        public override void ProcessClient()
        {
            // never processed on client
        }

        public override void ProcessServer()
        {
            var data = new Dictionary<string, string>();
            if (ServerConfig.IsServerAdmin(SenderSteamId))
                AdminNotificator.SendEnqueuedNotifications(SenderSteamId);

            if (!ServerConfig.IsServerAdmin(SenderSteamId) && ChatCommandLogic.Instance.ServerCfg.ForceBannedPlayers.Any(p => p.SteamId == SenderSteamId))
                data.Add(ConnectionHelper.ConnectionKeys.ForceKick, SenderSteamId.ToString());

            data.Add(ConnectionHelper.ConnectionKeys.LogPrivateMessages, CommandPrivateMessage.LogPrivateMessages.ToString());
            ConnectionHelper.SendMessageToPlayer(SenderSteamId, data);

            ChatCommandLogic.Instance.ServerCfg.SendPermissions(SenderSteamId);

            if (!ServerConfig.ServerIsClient)
            {
                var motdMessage = new MessageOfTheDayMessage()
                {
                    Content = CommandMessageOfTheDay.Content,
                    HeadLine = CommandMessageOfTheDay.HeadLine,
                    ShowInChat = CommandMessageOfTheDay.ShowInChat,
                    FieldsToUpdate = MessageOfTheDayMessage.ChangedFields.Content | MessageOfTheDayMessage.ChangedFields.HeadLine | MessageOfTheDayMessage.ChangedFields.ShowInChat
                };

                ConnectionHelper.SendMessageToPlayer(SenderSteamId, motdMessage);
            }
        }
    }
}
