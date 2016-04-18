namespace midspace.adminscripts
{
    using System;
    using System.Text.RegularExpressions;
    using midspace.adminscripts.Messages.Sync;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;

    public class CommandShipOwnerRevoke : ChatCommand
    {
        public CommandShipOwnerRevoke()
            : base(ChatCommandSecurity.Admin, "revoke", new[] { "/revoke" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/revoke <#>", "Removes ownership of all cubes in specified <#> ship.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/revoke", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false);
                var shipEntity = entity as IMyCubeGrid;
                if (shipEntity != null)
                {
                    MessageSyncGridChange.SendMessage(SyncGridChangeType.OwnerRevoke, shipEntity.EntityId, null, MyAPIGateway.Session.Player.PlayerID);
                    return true;
                }
                MyAPIGateway.Utilities.SendMessage(steamId, "Revoke", "No ship targeted.");
                return true;
            }

            var match = Regex.Match(messageText, @"/revoke\s+(?<Key>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var shipName = match.Groups["Key"].Value;
                MessageSyncGridChange.SendMessage(SyncGridChangeType.OwnerRevoke, 0, shipName, MyAPIGateway.Session.Player.PlayerID);
                return true;
            }

            return false;
        }
    }
}
