using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace midspace.adminscripts.Messages
{
    [ProtoContract]
    public class MessageIncomingMessageParts : MessageBase
    {
        [ProtoMember]
        public byte[] Content;

        [ProtoMember]
        public bool LastPart;

        public override void ProcessClient()
        {
            ConnectionHelper.Client_MessageCache.AddRange(Content.ToList());

            if (LastPart)
            {
                ConnectionHelper.ProcessClientData(ConnectionHelper.Client_MessageCache.ToArray());
                ConnectionHelper.Client_MessageCache.Clear();
            }
        }

        public override void ProcessServer()
        {
            ConnectionHelper.Server_MessageCache[SenderSteamId].AddRange(Content.ToList());

            if (LastPart)
            {
                ConnectionHelper.ProcessServerData(ConnectionHelper.Server_MessageCache[SenderSteamId].ToArray());
                ConnectionHelper.Server_MessageCache[SenderSteamId].Clear();
            }
        }

    }
}
