namespace midspace.adminscripts
{
    using System;
    using System.Text.RegularExpressions;
    using Messages.Sync;
    using Sandbox.ModAPI;

    public class CommandShipScaleDown : ChatCommand
    {
        public CommandShipScaleDown()
            : base(ChatCommandSecurity.Admin, "scaledown", new[] { "/scaledown" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/scaledown <#>", "Converts a large ship into a small ship, also converts all cubes to small.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/scaledown", StringComparison.InvariantCultureIgnoreCase))
            {
                var shipEntity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false);
                if (shipEntity != null)
                {
                    MessageSyncGridChange.SendMessage(SyncGridChangeType.ScaleDown, shipEntity.EntityId, null, MyAPIGateway.Session.Player.PlayerID);
                    return true;
                }

                MyAPIGateway.Utilities.ShowMessage("scaledown", "No ship targeted.");
                return true;
            }

            var match = Regex.Match(messageText, @"/scaledown\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var shipName = match.Groups["Key"].Value;
                MessageSyncGridChange.SendMessage(SyncGridChangeType.ScaleDown, 0, shipName, MyAPIGateway.Session.Player.PlayerID);
                return true;
            }

            return false;
        }
    }
}
