namespace midspace.adminscripts
{
    using System;

    using Sandbox.Common;
    using Sandbox.ModAPI;

    public class CommandHelloWorld : ChatCommand
    {
        public CommandHelloWorld()
            : base(ChatCommandSecurity.User, "hello", new[] { "/hello" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/hello", "A simple Hello World test.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.Equals("/hello", StringComparison.InvariantCultureIgnoreCase))
            {
                MyAPIGateway.Utilities.ShowNotification("Hello back to you", 1000, MyFontEnum.Red); // display on your screen.
                MyAPIGateway.Utilities.ShowMessage("Computer", "Hello back to you");  // echo back to yourself.
                MyAPIGateway.Utilities.SendMessage("This player thinks it cool to say hello"); // broadcast to everyone like a standard chat message, but from YOU.
                return true;
            }
            return false;
        }
    }
}
