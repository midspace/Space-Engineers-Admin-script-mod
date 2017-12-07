namespace midspace.adminscripts.Messages.Protection
{
    using midspace.adminscripts.Messages.Communication;
    using midspace.adminscripts.Protection;
    using ProtoBuf;

    [ProtoContract]
    public class MessageProtectionConfig : MessageBase
    {
        [ProtoMember(201)]
        public bool Value;

        [ProtoMember(202)]
        public ProtectionConfigType Type;

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
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Server", $"The protection is {(Value ? "inverted" : "normal")} now.");
                    }
                    break;
                case ProtectionConfigType.Enable:
                    if (ProtectionHandler.Config.ProtectionEnabled != Value)
                    {
                        sync = true;
                        ProtectionHandler.Config.ProtectionEnabled = Value;
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Server", $"The protection is {(Value ? "enabled" : "disabled")} now");
                    }
                    break;
                case ProtectionConfigType.LandingGear:
                    if (ProtectionHandler.Config.ProtectionAllowLandingGear != Value)
                    {
                        sync = true;
                        ProtectionHandler.Config.ProtectionAllowLandingGear = Value;
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Server", $"Protection area LandingGear is now {(Value ? "allowed" : "disabled")}.");
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

    public enum ProtectionConfigType : byte
    {
        Invert = 0,
        Enable = 1,
        LandingGear = 2
    }
}