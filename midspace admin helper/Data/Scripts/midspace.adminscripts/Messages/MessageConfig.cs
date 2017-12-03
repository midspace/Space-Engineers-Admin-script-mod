namespace midspace.adminscripts.Messages
{
    using ProtoBuf;
    using midspace.adminscripts.Messages.Communication;

    [ProtoContract]
    public class MessageConfig : MessageBase
    {
        [ProtoMember(201)]
        public ConfigAction Action;

        [ProtoMember(202)]
        public ServerConfigurationStruct Config;

        public override void ProcessClient()
        {
            switch (Action)
            {
                case ConfigAction.LogPrivateMessages:
                    CommandPrivateMessage.LogPrivateMessages = Config.LogPrivateMessages;
                    break;
                case ConfigAction.Show:
                    Config.Show();
                    break;
            }
        }

        public override void ProcessServer()
        {
            switch (Action)
            {
                case ConfigAction.Save:
                    ChatCommandLogic.Instance.ServerCfg.Save();
                    MessageClientTextMessage.SendMessage(SenderSteamId, "Server", "Config saved.");
                    break;
                case ConfigAction.Reload:
                    ChatCommandLogic.Instance.ServerCfg.ReloadConfig();
                    MessageClientTextMessage.SendMessage(SenderSteamId, "Server", "Config reloaded.");
                    break;
                case ConfigAction.AdminLevel:
                    ChatCommandLogic.Instance.ServerCfg.UpdateAdminLevel(Config.AdminLevel);
                    MessageClientTextMessage.SendMessage(SenderSteamId, "Server", string.Format("Updated default admin level to {0}. Please note that you have to use '/cfg save' to save it permanently.", Config.AdminLevel));
                    break;
                case ConfigAction.NoGrindIndestructible:
                    ChatCommandLogic.Instance.ServerCfg.SetNoGrindIndestructible(Config.NoGrindIndestructible);
                    MessageClientTextMessage.SendMessage(SenderSteamId, "Server", string.Format("Set NoGrindIndestructible to {0}. ", Config.NoGrindIndestructible));
                    break;
                case ConfigAction.Show:
                    Config = ChatCommandLogic.Instance.ServerCfg.Config;
                    ConnectionHelper.SendMessageToPlayer(SenderSteamId, this);
                    break;
            }
        }
    }

    public enum ConfigAction : byte
    {
        Reload = 0,
        Save = 1,
        AdminLevel = 2,
        LogPrivateMessages = 3,
        NoGrindIndestructible = 4,
        Show = 5
    }
}
