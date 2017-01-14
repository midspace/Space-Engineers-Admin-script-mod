namespace midspace.adminscripts
{
    using System;
    using System.Text.RegularExpressions;
    using Messages.Sync;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;

    public class CommandShipRepair : ChatCommand
    {
        public CommandShipRepair()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Client, "repair", new[] { "/repair" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/repair <#>", "Repairs the specified <#> ship. Does not replace missing components.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/repair", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false);
                var shipEntity = entity as IMyCubeGrid;
                if (shipEntity != null)
                {
                    MessageSyncGridChange.SendMessage(SyncGridChangeType.Repair, shipEntity.EntityId, null, MyAPIGateway.Session.Player.PlayerID);
                    return true;
                }
                MyAPIGateway.Utilities.SendMessage(steamId, "repair", "No ship targeted.");
                return true;
            }

            var match = Regex.Match(messageText, @"/repair\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var shipName = match.Groups["Key"].Value;
                MessageSyncGridChange.SendMessage(SyncGridChangeType.Repair, 0, shipName, MyAPIGateway.Session.Player.PlayerID);
                return true;
            }

            return false;
        }
    }
}
