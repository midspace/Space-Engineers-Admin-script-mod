namespace midspace.adminscripts
{
    using System;

    using Sandbox.ModAPI;

    public class CommandTime : ChatCommand
    {
        public CommandTime()
            : base(ChatCommandSecurity.User, "time", new[] { "/time" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/time", "Displays the current time.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.Equals("/time", StringComparison.InvariantCultureIgnoreCase))
            {
                MyAPIGateway.Utilities.ShowMessage("Time", string.Format("{0:T}", DateTime.Now));
                return true;
            }
            return false;
        }
    }
}
