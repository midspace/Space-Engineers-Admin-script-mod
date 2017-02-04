namespace midspace.adminscripts
{
    using System;
    using System.Text.RegularExpressions;
    using Messages.Sync;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;

    public class CommandShipOff : ChatCommand
    {
        public CommandShipOff()
            : base(ChatCommandSecurity.Admin, "off", new[] { "/off" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/off <#>", "Turns off all reactor and battery power in the specified <#> ship.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/off", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false);
                var shipEntity = entity as IMyCubeGrid;
                if (shipEntity != null)
                {
                    MessageSyncGridChange.SendMessage(SyncGridChangeType.SwitchOffPower, shipEntity.EntityId, null, MyAPIGateway.Session.Player.IdentityId);
                    return true;
                }
                MyAPIGateway.Utilities.ShowMessage("Off", "No ship targeted.");
                return true;
            }

            var match = Regex.Match(messageText, @"/off\s+(?<Key>.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var shipName = match.Groups["Key"].Value;
                MessageSyncGridChange.SendMessage(SyncGridChangeType.SwitchOffPower, 0, shipName, MyAPIGateway.Session.Player.IdentityId);
                return true;
            }

            return false;
        }
    }
}
