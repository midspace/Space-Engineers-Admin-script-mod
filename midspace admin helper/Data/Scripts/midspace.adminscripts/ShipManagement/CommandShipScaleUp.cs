namespace midspace.adminscripts
{
    using System;
    using System.Text.RegularExpressions;
    using Messages.Sync;
    using Sandbox.ModAPI;

    public class CommandShipScaleUp : ChatCommand
    {
        public CommandShipScaleUp()
            : base(ChatCommandSecurity.Admin, "scaleup", new[] { "/scaleup" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/scaleup <#>", "Converts a small ship into a large ship, also converts all cubes to large.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/scaleup", StringComparison.InvariantCultureIgnoreCase))
            {
                var shipEntity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false);
                if (shipEntity != null)
                {
                    MessageSyncGridChange.SendMessage(SyncGridChangeType.ScaleUp, shipEntity.EntityId, null, MyAPIGateway.Session.Player.PlayerID);
                    return true;
                }

                MyAPIGateway.Utilities.ShowMessage("scaleup", "No ship targeted.");
                return true;
            }

            var match = Regex.Match(messageText, @"/scaleup\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var shipName = match.Groups["Key"].Value;
                MessageSyncGridChange.SendMessage(SyncGridChangeType.ScaleUp, 0, shipName, MyAPIGateway.Session.Player.PlayerID);
                return true;
            }

            return false;
        }
    }
}
