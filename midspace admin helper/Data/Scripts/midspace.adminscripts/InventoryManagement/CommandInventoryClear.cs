namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using midspace.adminscripts.Messages.Sync;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using VRage.ModAPI;

    public class CommandInventoryClear : ChatCommand
    {
        public CommandInventoryClear()
            : base(ChatCommandSecurity.Admin, "invclear", new[] { "/invclear" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/invclear <#>", "The specified <#> player is cleared of all inventory.");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/invclear\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var playerName = match.Groups["Key"].Value;
                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, p => p != null);
                IMyIdentity selectedPlayer = null;

                var identities = new List<IMyIdentity>();
                MyAPIGateway.Players.GetAllIdentites(identities, delegate (IMyIdentity i) { return i.DisplayName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase); });
                selectedPlayer = identities.FirstOrDefault();

                int index;
                if (playerName.Substring(0, 1) == "#" && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= CommandPlayerStatus.IdentityCache.Count)
                {
                    selectedPlayer = CommandPlayerStatus.IdentityCache[index - 1];
                }

                if (selectedPlayer == null)
                    return false;

                if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                {
                    ClearInventory(0, selectedPlayer.PlayerId);
                }
                else
                {
                    ConnectionHelper.SendMessageToServer(new MessageSyncCreateObject()
                    {
                        EntityId = selectedPlayer.PlayerId,
                        Type = SyncCreateObjectType.Clear,
                    });
                }
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
                MyAPIGateway.Players.GetPlayers(listplayers, p => p.PlayerID == entityId);
                
                var player = listplayers.FirstOrDefault();
                if (player != null)
                    entity = (IMyEntity)player.GetCharacter();
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
