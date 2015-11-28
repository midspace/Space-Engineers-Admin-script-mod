namespace midspace.adminscripts
{

    using midspace.adminscripts.Messages.Sync;
    using Sandbox.ModAPI;

    public class CommandBomb : ChatCommand
    {
        public CommandBomb()
            : base(ChatCommandSecurity.Admin, "bomb", new[] { "/bomb" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/bomb", "Throws a warhead in the direction you face");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            MessageSyncAres.ThrowBomb(steamId);
            return true;
        }
    }
}
