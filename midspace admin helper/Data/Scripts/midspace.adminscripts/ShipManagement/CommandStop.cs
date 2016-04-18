namespace midspace.adminscripts
{
    using System;
    using System.Text.RegularExpressions;
    using midspace.adminscripts.Messages.Sync;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;

    public class CommandStop : ChatCommand
    {
        public CommandStop()
            : base(ChatCommandSecurity.Admin, "stop", new[] { "/stop" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/stop <#>", "Stops all motion of the specified <#> ship. Turns on dampeners, and initiates thrusters. Unpowered ships will also stop.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/stop", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false);
                var shipEntity = entity as IMyCubeGrid;
                if (shipEntity != null)
                {
                    MessageSyncGridChange.SendMessage(SyncGridChangeType.Stop, shipEntity.EntityId, null, MyAPIGateway.Session.Player.PlayerID);
                    return true;
                }
                MyAPIGateway.Utilities.ShowMessage("Stop", "No ship targeted.");
                return true;
            }

            if (messageText.StartsWith("/stop ", StringComparison.InvariantCultureIgnoreCase))
            {
                var match = Regex.Match(messageText, @"/stop\s+(?<Key>.+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var shipName = match.Groups["Key"].Value;
                    MessageSyncGridChange.SendMessage(SyncGridChangeType.Stop, 0, shipName, MyAPIGateway.Session.Player.PlayerID);
                    return true;

                }
            }

            return false;
        }
    }
}
