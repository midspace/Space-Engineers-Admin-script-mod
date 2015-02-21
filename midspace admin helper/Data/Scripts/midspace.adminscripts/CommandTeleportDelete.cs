namespace midspace.adminscripts
{
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;

    public class CommandTeleportDelete : ChatCommand
    {
        public CommandTeleportDelete()
            : base(ChatCommandSecurity.Admin, "tpdel", new[] { "/tpdel" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/tpdel <name>", "Delete the previously saved location named <name>.");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/tpdel\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var saveName = match.Groups["Key"].Value;

                if (CommandTeleportList.DeletePoint(saveName))
                    return true;
            }

            MyAPIGateway.Utilities.ShowMessage("Unknown location", "Could not find the specified name.");

            return false;
        }

    }
}
