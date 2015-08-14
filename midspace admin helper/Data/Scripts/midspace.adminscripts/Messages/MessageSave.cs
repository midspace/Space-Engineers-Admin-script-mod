using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace midspace.adminscripts.Messages
{
    [ProtoContract]
    public class MessageSave : MessageBase
    {
        [ProtoMember(1)]
        public string Name;

        public override void ProcessClient()
        {
            // never processed on client
        }

        public override void ProcessServer()
        {
            if (ServerConfig.ServerIsClient && SenderSteamId != MyAPIGateway.Session.Player.SteamUserId) //no one should be able to do that
            {
                ConnectionHelper.SendChatMessage(SenderSteamId, "Saving the session on a locally hosted server is not allowed.");
                return;
            }

            if (string.IsNullOrEmpty(Name))
            {
                MyAPIGateway.Session.Save();
                ChatCommandLogic.Instance.ServerCfg.SaveLogs();
                ConnectionHelper.SendChatMessage(SenderSteamId, "Session saved.");
            }
            else
            {
                MyAPIGateway.Session.Save(Name);
                ChatCommandLogic.Instance.ServerCfg.SaveLogs();
                ConnectionHelper.SendChatMessage(SenderSteamId, string.Format("Session saved as {0}.", Name));
            }
        }
    }
}
