namespace midspace.adminscripts
{
    using midspace.adminscripts.Messages.Sync;
    using Sandbox.ModAPI;

    public class CommandMeteor : ChatCommand
    {
        private readonly string _defaultOreName;

        public CommandMeteor(string defaultOreName)
            : base(ChatCommandSecurity.Admin, "meteor", new[] { "/meteor" })
        {
            _defaultOreName = defaultOreName;
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/meteor", "Throws a meteor in the direction you face");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            MessageSyncAres.ThrowMeteor(steamId, _defaultOreName);
            return true;
        }
    }
}
