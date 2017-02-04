namespace midspace.adminscripts
{
    using System;
    using System.Text.RegularExpressions;
    using midspace.adminscripts.Messages.Sync;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;

    public class CommandShipDestructible : ChatCommand
    {
        public CommandShipDestructible()
            : base(ChatCommandSecurity.Admin, "destructible", new[] { "/destructible", "/destruct" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/destructible On|Off <#>", "Set the specified <#> ship as destructible. Ship will be removed and regenerated.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/((destructible)|(destruct))\s+(?<switch>(on)|(off)|1|0)(\s+|$)(?<name>.*)|$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                bool switchOn = false;
                var switchString = match.Groups["switch"].Value;
                var shipName = match.Groups["name"].Value;

                if (switchString == "")
                    return false;

                if (switchString.Equals("on", StringComparison.InvariantCultureIgnoreCase) || switchString.Equals("1", StringComparison.InvariantCultureIgnoreCase))
                    switchOn = true;

                if (switchString.Equals("off", StringComparison.InvariantCultureIgnoreCase) || switchString.Equals("0", StringComparison.InvariantCultureIgnoreCase))
                    switchOn = false;

                // set destructible on the ship in the crosshairs.
                if (string.IsNullOrEmpty(shipName))
                {
                    var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false);
                    var shipEntity = entity as IMyCubeGrid;
                    if (shipEntity != null)
                    {
                        MessageSyncGridChange.SendMessage(SyncGridChangeType.Destructible, shipEntity.EntityId, null, MyAPIGateway.Session.Player.IdentityId, switchOn);
                        return true;
                    }

                    MyAPIGateway.Utilities.ShowMessage("destructible", "No ship targeted.");
                    return true;
                }

                MessageSyncGridChange.SendMessage(SyncGridChangeType.Destructible, 0, shipName, MyAPIGateway.Session.Player.IdentityId, switchOn);
                return true;
            }

            return false;
        }
    }
}
