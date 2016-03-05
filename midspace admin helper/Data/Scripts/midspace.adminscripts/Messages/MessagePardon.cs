using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using midspace.adminscripts.Messages.Communication;

namespace midspace.adminscripts.Messages
{
    [ProtoContract]
    public class MessagePardon : MessageBase
    {
        [ProtoMember(1)]
        public string PlayerName;

        public override void ProcessClient()
        {
            // never processed here
        }

        public override void ProcessServer()
        {
            Player bannedPlayer = ChatCommandLogic.Instance.ServerCfg.Config.ForceBannedPlayers.FirstOrDefault(p => p.PlayerName.Equals(PlayerName, StringComparison.InvariantCultureIgnoreCase));
            if (bannedPlayer.SteamId != 0)
            {
                ChatCommandLogic.Instance.ServerCfg.Config.ForceBannedPlayers.Remove(bannedPlayer);
                MessageClientTextMessage.SendMessage(SenderSteamId, "Server", string.Format("Pardoned player {0}", bannedPlayer.PlayerName));
            }
            else
                MessageClientTextMessage.SendMessage(SenderSteamId, "Server", string.Format("Can't find a banned player named {0}", PlayerName));
        }
    }
}
