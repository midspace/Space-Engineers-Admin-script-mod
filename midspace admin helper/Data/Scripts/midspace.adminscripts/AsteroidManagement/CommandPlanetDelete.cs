namespace midspace.adminscripts
{
    using System;
    using System.Text.RegularExpressions;
    using Messages.Sync;
    using Sandbox.ModAPI;

    public class CommandPlanetDelete : ChatCommand
    {
        public CommandPlanetDelete()
            : base(ChatCommandSecurity.Admin, "deleteplanet", new[] { "/deleteplanet", "/delplanet" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/deleteplanet <#>", "Deletes the specified <#> planet.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/deleteplanet", StringComparison.InvariantCultureIgnoreCase) ||
                messageText.Equals("/delplanet", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, false, false, false, false, true, false);
                var planetEntity = entity as Sandbox.Game.Entities.MyPlanet;
                if (planetEntity != null)
                {
                    MessageSyncVoxelChange.SendMessage(SyncVoxelChangeType.DeletePlanet, planetEntity.EntityId, null, true);
                    return true;
                }

                MyAPIGateway.Utilities.SendMessage(steamId, "deleteplanet", "No planet targeted.");
                return true;
            }

            var match = Regex.Match(messageText, @"/((delplanet)|(deleteplanet))\s+(?<Key>.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var planetName = match.Groups["Key"].Value;
                MessageSyncVoxelChange.SendMessage(SyncVoxelChangeType.DeletePlanet, 0, planetName, true);
                return true;
            }

            return false;
        }
    }
}
