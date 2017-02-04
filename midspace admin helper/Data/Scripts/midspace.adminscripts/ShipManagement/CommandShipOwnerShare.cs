namespace midspace.adminscripts
{
    using System;
    using System.Text.RegularExpressions;
    using Messages.Sync;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;

    /// <summary>
    /// This changes of permissions of all blocks to shared.
    /// </summary>
    public class CommandShipOwnerShare : ChatCommand
    {
        public CommandShipOwnerShare()
            : base(ChatCommandSecurity.Admin, "share", new[] { "/share" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/share <#>", "Share ownership of the <#> specified ship to All, without removing the original owner.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/share", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false);
                var shipEntity = entity as IMyCubeGrid;
                if (shipEntity != null)
                {
                    MessageSyncGridChange.SendMessage(SyncGridChangeType.OwnerShareAll, shipEntity.EntityId, null, MyAPIGateway.Session.Player.IdentityId);
                    return true;
                }
                MyAPIGateway.Utilities.ShowMessage("Share", "No ship targeted.");
                return true;
            }

            var match = Regex.Match(messageText, @"/share\s+(?<Key>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var shipName = match.Groups["Key"].Value;
                MessageSyncGridChange.SendMessage(SyncGridChangeType.OwnerShareAll, 0, shipName, MyAPIGateway.Session.Player.IdentityId);
                return true;
            }

            return false;
        }
    }
}
