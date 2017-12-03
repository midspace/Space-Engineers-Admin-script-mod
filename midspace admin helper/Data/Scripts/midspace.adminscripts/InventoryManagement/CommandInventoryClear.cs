namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using VRage.Game.Entity;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;

    public class CommandInventoryClear : ChatCommand
    {
        public CommandInventoryClear()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Server, "invclear", new[] { "/invclear" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/invclear <#>", "The specified <#> player is cleared of all inventory.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/invclear\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var playerName = match.Groups["Key"].Value;
                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, p => p != null);

                var identities = new List<IMyIdentity>();
                MyAPIGateway.Players.GetAllIdentites(identities, i => i.DisplayName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase));
                IMyIdentity selectedPlayer = identities.FirstOrDefault();

                int index;
                List<IMyIdentity> cacheList = CommandPlayerStatus.GetIdentityCache(steamId);
                if (playerName.Substring(0, 1) == "#" && int.TryParse(playerName.Substring(1), out index) && index > 0 && index <= cacheList.Count)
                {
                    selectedPlayer = cacheList[index - 1];
                }

                if (selectedPlayer == null)
                    return false;

                ClearInventory(steamId, selectedPlayer.IdentityId);
                return true;
            }

            return false;
        }

        public void ClearInventory(ulong steamId, long entityId)
        {
            IMyEntity entity = null;

            if (MyAPIGateway.Entities.EntityExists(entityId))
            {
                entity = MyAPIGateway.Entities.GetEntityById(entityId);
            }
            else
            {
                var listplayers = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(listplayers, p => p.IdentityId == entityId);
                
                var player = listplayers.FirstOrDefault();
                if (player != null)
                    entity = player.Character;
            }

            if (entity == null)
            {
                MyAPIGateway.Utilities.SendMessage(steamId, "Failed", "Cannot find the specified Entity.");
                return;
            }

            MyAPIGateway.Utilities.SendMessage(steamId, "Clearing inventory", entity.DisplayName);
            var count = ((MyEntity)entity).InventoryCount;

            for (int i = 0; i < count; i++)
            {
                var inventory = ((MyEntity)entity).GetInventory(i);
                inventory.Clear();
            }
        }
    }
}
