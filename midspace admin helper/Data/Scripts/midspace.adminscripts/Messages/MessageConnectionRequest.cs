namespace midspace.adminscripts.Messages
{
    using midspace.adminscripts.Messages.Protection;
    using midspace.adminscripts.Protection;
    using ProtoBuf;
    using Sandbox.ModAPI;
    using System;
    using System.Linq;
    using VRage.Game.ModAPI;

    [ProtoContract]
    public class MessageConnectionRequest : MessageBase
    {
        public override void ProcessClient()
        {
            // never processed on client
        }

        public override void ProcessServer()
        {
            if (ServerConfig.IsServerAdmin(SenderSteamId))
                AdminNotificator.SendEnqueuedNotifications(SenderSteamId);

            if (!ServerConfig.IsServerAdmin(SenderSteamId) && ChatCommandLogic.Instance.ServerCfg.Config.ForceBannedPlayers.Any(p => p.SteamId == SenderSteamId))
                ConnectionHelper.SendMessageToPlayer(SenderSteamId, new MessageForceDisconnect { SteamId = SenderSteamId });

            ConnectionHelper.SendMessageToPlayer(SenderSteamId, new MessageConfig
            {
                Config = new ServerConfigurationStruct
                {
                    LogPrivateMessages = CommandPrivateMessage.LogPrivateMessages
                },
                Action = ConfigAction.LogPrivateMessages
            });

            ChatCommandLogic.Instance.ServerCfg.SendPermissions(SenderSteamId);
            ConnectionHelper.SendMessageToPlayer(SenderSteamId, new MessageSyncProtection
            {
                Config = ProtectionHandler.Config
            });

            if (!ServerConfig.ServerIsClient)
            {
                var motdMessage = new MessageOfTheDayMessage
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
