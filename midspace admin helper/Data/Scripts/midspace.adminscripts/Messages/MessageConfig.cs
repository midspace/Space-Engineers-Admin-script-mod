using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace midspace.adminscripts.Messages
{
    [ProtoContract]
    public class MessageConfig : MessageBase
    {
        [ProtoMember(1)]
        public ConfigAction Action;

        [ProtoMember(2)]
        public uint AdminLevel;

        public override void ProcessClient()
        {
            // never processed on client
        }

        public override void ProcessServer()
        {
            switch (Action)
            {
                case ConfigAction.Save:
                    ChatCommandLogic.Instance.ServerCfg.Save();
                    ConnectionHelper.SendChatMessage(SenderSteamId, "Config saved.");
                    break;
                case ConfigAction.Reload:
                    ChatCommandLogic.Instance.ServerCfg.ReloadConfig();
                    ConnectionHelper.SendChatMessage(SenderSteamId, "Config reloaded.");
                    break;
                case ConfigAction.AdminLevel:
                    ChatCommandLogic.Instance.ServerCfg.UpdateAdminLevel(AdminLevel);
                    ConnectionHelper.SendChatMessage(SenderSteamId, string.Format("Updated default admin level to {0}. Please note that you have to use '/cfg save' to save it permanently.", AdminLevel));
                    break;
            }
        }
    }

    public enum ConfigAction
    {
        Reload,
        Save,
        AdminLevel
    }
}
