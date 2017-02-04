namespace midspace.adminscripts
{
    using System;
    using System.Text.RegularExpressions;
    using Messages.Sync;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;

    public class CommandShipOn : ChatCommand
    {
        public CommandShipOn()
            : base(ChatCommandSecurity.Admin, "on", new[] { "/on" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/on <#>", "Turns on all reactor and battery power in the specified <#> ship.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/on", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false);
                var shipEntity = entity as IMyCubeGrid;
                if (shipEntity != null)
                {
                    MessageSyncGridChange.SendMessage(SyncGridChangeType.SwitchOnPower, shipEntity.EntityId, null, MyAPIGateway.Session.Player.IdentityId);
                    return true;
                }
                MyAPIGateway.Utilities.ShowMessage("On", "No ship targeted.");
                return true;
            }

            var match = Regex.Match(messageText, @"/on\s+(?<Key>.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var shipName = match.Groups["Key"].Value;
                MessageSyncGridChange.SendMessage(SyncGridChangeType.SwitchOnPower, 0, shipName, MyAPIGateway.Session.Player.IdentityId);
                return true;
            }

            return false;
        }
    }
}
