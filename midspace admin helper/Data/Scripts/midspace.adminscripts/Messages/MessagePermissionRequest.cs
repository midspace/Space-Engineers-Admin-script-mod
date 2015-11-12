namespace midspace.adminscripts.Messages
{
    public class MessagePermissionRequest : MessageBase
    {
        public override void ProcessClient()
        {
            // never processed here
        }

        public override void ProcessServer()
        {
            ChatCommandLogic.Instance.ServerCfg.SendPermissions(SenderSteamId);
        }
    }
}
