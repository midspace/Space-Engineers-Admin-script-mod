namespace midspace.adminscripts
{
    using System.Text.RegularExpressions;
    using midspace.adminscripts.Messages.Sync;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;

    /// <summary>
    /// This changes the BuiltBy of an entire grid to no one.
    /// </summary>
    public class CommandShipBuiltBy : ChatCommand
    {
        public CommandShipBuiltBy()
            : base(ChatCommandSecurity.Admin, "builtby", new[] { "/builtBy" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/builtBy <Player|B#>", "Sets the BuiltBy of the targeted ship to the specified player or bot.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/builtby\s+(?<player>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var playerName = match.Groups["player"].Value;

                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false);
                var shipEntity = entity as IMyCubeGrid;
                if (shipEntity != null)
                {
                    MessageSyncGridChange.SendMessage(SyncGridChangeType.BuiltBy, shipEntity.EntityId, playerName);
                    return true;
                }
                MyAPIGateway.Utilities.ShowMessage("BuiltBy", "No ship targeted.");
                return true;
            }

            return false;
        }
    }
}
