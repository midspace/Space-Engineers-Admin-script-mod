namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using Sandbox.ModAPI.Interfaces;

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
                MyAPIGateway.Players.GetAllIdentites(identities, delegate(IMyIdentity i) { return i.DisplayName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase); });
                selectedPlayer = identities.FirstOrDefault();

                int index;
                if (playerName.Substring(0, 1) == "#" && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= CommandPlayerStatus.IdentityCache.Count)
                {
                    selectedPlayer = CommandPlayerStatus.IdentityCache[index - 1];
                }

                if (selectedPlayer == null)
                    return false;

                var listplayers = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(listplayers, p => p.PlayerID == selectedPlayer.PlayerId);
                var player = listplayers.FirstOrDefault();

                if (player != null)
                {
                    MyAPIGateway.Utilities.ShowMessage("Clearing inventory", player.DisplayName);
                    var inventoryOwnwer = player.Controller.ControlledEntity as IMyInventoryOwner;
                    var inventory = inventoryOwnwer.GetInventory(0) as Sandbox.ModAPI.IMyInventory;
                    inventory.Clear();
                    return true;
                }
            }

            return false;
        }
    }
}
