namespace midspace.adminscripts
{
    using System;
    using System.Text.RegularExpressions;
    using Messages.Sync;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;

    public class CommandShipToStation : ChatCommand
    {
        public CommandShipToStation()
            : base(ChatCommandSecurity.Admin, "tostation", new[] { "/tostation" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/tostation <#>", "Converts the specified ship to a station.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/tostation", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false);
                var shipEntity = entity as IMyCubeGrid;
                if (shipEntity != null)
                {
                    MessageSyncGridChange.SendMessage(SyncGridChangeType.ConvertToStation, shipEntity.EntityId, null, MyAPIGateway.Session.Player.IdentityId);
                    return true;
                }
                MyAPIGateway.Utilities.ShowMessage("ToStation", "No ship targeted.");
                return true;
            }

            var match = Regex.Match(messageText, @"/tostation\s+(?<Key>.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var shipName = match.Groups["Key"].Value;
                MessageSyncGridChange.SendMessage(SyncGridChangeType.ConvertToStation, 0, shipName, MyAPIGateway.Session.Player.IdentityId);
                return true;
            }

            return false;
        }
    }
}
