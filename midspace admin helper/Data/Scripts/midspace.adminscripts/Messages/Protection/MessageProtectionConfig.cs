using midspace.adminscripts.Messages.Communication;
using midspace.adminscripts.Protection;
using ProtoBuf;

namespace midspace.adminscripts.Messages.Protection
{
    [ProtoContract]
    public class MessageProtectionConfig : MessageBase
    {
        [ProtoMember(1)] public bool Value;

        [ProtoMember(2)] public ProtectionConfigType Type;

        public override void ProcessClient()
        {
            // never processed on client
        }

        public override void ProcessServer()
        {
            bool sync = false;

            switch (Type)
            {
                case ProtectionConfigType.Invert:
                    if (ProtectionHandler.Config.ProtectionInverted != Value)
                    {
                        sync = true;
                        ProtectionHandler.Config.ProtectionInverted = Value;
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Server", string.Format("The protection is {0} now.", Value ? "inverted" : "normal"));
                    }
                    break;
                case ProtectionConfigType.Enable:
                    if (ProtectionHandler.Config.ProtectionEnabled != Value)
                    {
                        sync = true;
                        ProtectionHandler.Config.ProtectionEnabled = Value;
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Server", string.Format("The protection is {0} now", Value ? "enabled" : "disabled"));
                    }
                    break;
            }

            if (sync)
            {
                ProtectionHandler.Save();
                ConnectionHelper.SendMessageToAllPlayers(new MessageSyncProtection {Config = ProtectionHandler.Config});
            }
            else
                MessageClientTextMessage.SendMessage(SenderSteamId, "Server", "The setting was already set to the specified value.");
        }
    }

    public enum ProtectionConfigType
    {
        Invert,
        Enable
    }
}