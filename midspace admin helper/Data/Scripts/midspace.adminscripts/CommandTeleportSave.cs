namespace midspace.adminscripts
{
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using VRageMath;

    public class CommandTeleportSave : ChatCommand
    {
        public CommandTeleportSave()
            : base(ChatCommandSecurity.Admin, "tpsave", new[] { "/tpsave" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/tpsave <name>", "Saves the current position under the <name> for later teleporting to with command /tpfav.");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/tpsave\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var saveName = match.Groups["Key"].Value;

                // Use the center of the player bounding box.
                // This way, if the rotation is different when it is fetch back, the player should still be in the middle of the specified point.
                Vector3D position = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.WorldAABB.Center;

                return CommandTeleportList.SavePoint(saveName, position);
            }

            return false;
        }
    }
}
