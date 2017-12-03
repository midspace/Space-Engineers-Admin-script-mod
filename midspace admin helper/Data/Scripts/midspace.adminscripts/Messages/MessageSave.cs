namespace midspace.adminscripts.Messages
{
    using ProtoBuf;
    using Sandbox.ModAPI;
    using midspace.adminscripts.Messages.Communication;

    [ProtoContract]
    public class MessageSave : MessageBase
    {
        [ProtoMember(201)]
        public string Name;

        public override void ProcessClient()
        {
            // never processed on client
        }

        public override void ProcessServer()
        {
            if (ServerConfig.ServerIsClient && SenderSteamId != MyAPIGateway.Session.Player.SteamUserId) //no one should be able to do that
            {
                MessageClientTextMessage.SendMessage(SenderSteamId, "Server", "Saving the session on a locally hosted server is not allowed.");
                return;
            }

            if (string.IsNullOrEmpty(Name))
            {
                MyAPIGateway.Session.Save();
                ChatCommandLogic.Instance.ServerCfg.SaveLogs();
                MessageClientTextMessage.SendMessage(SenderSteamId, "Server", "Session saved.");
            }
            else
            {
                MyAPIGateway.Session.Save(Name);
                ChatCommandLogic.Instance.ServerCfg.Save(Name);
                MessageClientTextMessage.SendMessage(SenderSteamId, "Server", string.Format("Session saved as {0}.", Name));
            }
        }
    }
}
