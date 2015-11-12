namespace midspace.adminscripts
{
    using System;

    using Sandbox.ModAPI;

    public class CommandVersion : ChatCommand
    {
        public CommandVersion()
            : base(ChatCommandSecurity.User, "version", new[] { "/version" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/version", "Displays the game version number.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/version", StringComparison.InvariantCultureIgnoreCase))
            {
                MyAPIGateway.Utilities.ShowMessage("Space Engineers Version", Sandbox.Common.MyFinalBuildConstants.APP_VERSION_STRING.ToString().Replace("_", "."));
                return true;
            }
            return false;
        }
    }
}
