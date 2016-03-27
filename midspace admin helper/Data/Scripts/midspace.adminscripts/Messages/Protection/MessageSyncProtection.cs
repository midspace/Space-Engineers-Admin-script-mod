using midspace.adminscripts.Protection;
using ProtoBuf;

namespace midspace.adminscripts.Messages.Protection
{
    [ProtoContract]
    public class MessageSyncProtection : MessageBase
    {
        [ProtoMember(1)] public ProtectionConfig Config;

        public override void ProcessClient()
        {
            ProtectionHandler.InitOrUpdateClient(Config);
        }

        public override void ProcessServer()
        {
            Config = ProtectionHandler.Config;
            ConnectionHelper.SendMessageToPlayer(SenderSteamId, this);
        }
    }
}