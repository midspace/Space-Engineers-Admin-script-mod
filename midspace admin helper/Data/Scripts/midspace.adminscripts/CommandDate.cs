namespace midspace.adminscripts
{
    using System;

    using Sandbox.ModAPI;

    public class CommandDate : ChatCommand
    {
        public CommandDate()
            : base(ChatCommandSecurity.User, "date", new[] { "/date" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/date", "Displays the current date.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.Equals("/date", StringComparison.InvariantCultureIgnoreCase))
            {
                MyAPIGateway.Utilities.ShowMessage("Date", string.Format("{0:d}", DateTime.Today));
                return true;
            }
            return false;
        }
    }
}
