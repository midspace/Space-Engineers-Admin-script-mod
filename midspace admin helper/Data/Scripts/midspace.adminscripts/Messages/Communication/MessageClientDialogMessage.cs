namespace midspace.adminscripts.Messages.Communication
{
    using ProtoBuf;
    using Sandbox.ModAPI;

    [ProtoContract]
    public class MessageClientDialogMessage : MessageBase
    {
        [ProtoMember(201)]
        public string Title;

        [ProtoMember(202)]
        public string Prefix;

        [ProtoMember(203)]
        public string Content;

        public override void ProcessClient()
        {
            MyAPIGateway.Utilities.ShowMissionScreen(Title, Prefix, " ", Content);
        }

        public override void ProcessServer()
        {
            // never processed on server.
        }

        public static void SendMessage(ulong steamId, string title, string prefix, string content)
        {
            ConnectionHelper.SendMessageToPlayer(steamId, new MessageClientDialogMessage { Title = title, Prefix = prefix, Content = content });
        }
    }
}
