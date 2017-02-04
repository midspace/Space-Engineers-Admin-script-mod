namespace midspace.adminscripts
{
    using System;
    using System.Text.RegularExpressions;
    using midspace.adminscripts.Messages.Sync;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;

    /// <summary>
    /// This changes the ownership of an entire grid to the player.
    /// </summary>
    public class CommandShipOwnerClaim : ChatCommand
    {
        public CommandShipOwnerClaim()
            : base(ChatCommandSecurity.Admin, "claim", new[] { "/claim" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/claim <#>", "Claims ownership of the <#> specified ship. All own-able blocks are transferred to you.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/claim", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false);
                var shipEntity = entity as IMyCubeGrid;
                if (shipEntity != null)
                {
                    MessageSyncGridChange.SendMessage(SyncGridChangeType.OwnerClaim, shipEntity.EntityId, null, MyAPIGateway.Session.Player.IdentityId);
                    return true;
                }
                MyAPIGateway.Utilities.ShowMessage("Claim", "No ship targeted.");
                return true;
            }

            var match = Regex.Match(messageText, @"/claim\s+(?<Key>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var shipName = match.Groups["Key"].Value;
                MessageSyncGridChange.SendMessage(SyncGridChangeType.OwnerClaim, 0, shipName, MyAPIGateway.Session.Player.IdentityId);
                return true;
            }

            return false;
        }
    }
}
