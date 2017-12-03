namespace midspace.adminscripts.Messages
{
    using ProtoBuf;
    using Sandbox.ModAPI;

    [ProtoContract]
    public class MessageForceDisconnect : MessageBase
    {
        [ProtoMember(201)]
        public ulong SteamId;

        [ProtoMember(202)]
        public bool Ban = false;

        public override void ProcessClient()
        {
            if (SteamId == MyAPIGateway.Session.Player.SteamUserId)
                CommandForceKick.DropPlayer = true;
        }

        public override void ProcessServer()
        {
        }
    }
}
