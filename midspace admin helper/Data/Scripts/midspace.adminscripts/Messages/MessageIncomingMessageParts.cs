using System.Linq;
using ProtoBuf;

namespace midspace.adminscripts.Messages
{
    [ProtoContract]
    public class MessageIncomingMessageParts : MessageBase
    {
        [ProtoMember(1)]
        public byte[] Content;

        [ProtoMember(2)]
        public bool LastPart;

        public override void ProcessClient()
        {
            ConnectionHelper.Client_MessageCache.AddRange(Content.ToList());

            if (LastPart)
            {
                ConnectionHelper.ProcessData(ConnectionHelper.Client_MessageCache.ToArray());
                ConnectionHelper.Client_MessageCache.Clear();
            }
        }

        public override void ProcessServer()
        {
            if (ConnectionHelper.Server_MessageCache.ContainsKey(SenderSteamId))
                ConnectionHelper.Server_MessageCache[SenderSteamId].AddRange(Content.ToList());
            else
                ConnectionHelper.Server_MessageCache.Add(SenderSteamId, Content.ToList());

            if (LastPart)
            {
                ConnectionHelper.ProcessData(ConnectionHelper.Server_MessageCache[SenderSteamId].ToArray());
                ConnectionHelper.Server_MessageCache[SenderSteamId].Clear();
            }
        }

    }
}
