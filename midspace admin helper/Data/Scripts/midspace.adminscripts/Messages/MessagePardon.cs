using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            Player bannedPlayer = ChatCommandLogic.Instance.ServerCfg.ForceBannedPlayers.FirstOrDefault(p => p.PlayerName.Equals(PlayerName, StringComparison.InvariantCultureIgnoreCase));
            if (bannedPlayer.SteamId != 0)
            {
                ChatCommandLogic.Instance.ServerCfg.ForceBannedPlayers.Remove(bannedPlayer);
                ConnectionHelper.SendChatMessage(SenderSteamId, string.Format("Pardoned player {0}", bannedPlayer.PlayerName));
            }
            else
                ConnectionHelper.SendChatMessage(SenderSteamId, string.Format("Can't find a banned player named {0}", PlayerName));
        }
    }
}
