namespace midspace.adminscripts
{
    using System;
    using System.Text.RegularExpressions;
    using Messages.Sync;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;

    public class CommandShipDelete : ChatCommand
    {
        public CommandShipDelete()
            : base(ChatCommandSecurity.Admin, "deleteship", new[] { "/deleteship", "/delship" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/deleteship <#>", "Deletes the specified <#> ship.  Use `/delship **` to delete the entire hotlist.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/deleteship", StringComparison.InvariantCultureIgnoreCase) ||
                messageText.Equals("/delship", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false);
                var shipEntity = entity as IMyCubeGrid;
                if (shipEntity != null)
                {
                    MessageSyncGridChange.SendMessage(SyncGridChangeType.DeleteShip, shipEntity.EntityId, null, MyAPIGateway.Session.Player.IdentityId);
                    return true;
                }

                MyAPIGateway.Utilities.ShowMessage("deleteship", "No ship targeted.");
                return true;
            }

            var match = Regex.Match(messageText, @"/((delship)|(deleteship))\s+(?<Key>.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var shipName = match.Groups["Key"].Value;
                MessageSyncGridChange.SendMessage(SyncGridChangeType.DeleteShip, 0, shipName, MyAPIGateway.Session.Player.IdentityId);
                return true;
            }

            return false;
        }
    }
}
