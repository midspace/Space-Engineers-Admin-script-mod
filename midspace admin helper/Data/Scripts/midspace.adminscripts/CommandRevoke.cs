namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;

    public class CommandRevoke : ChatCommand
    {
        public CommandRevoke()
            : base(ChatCommandSecurity.Admin, "revoke", new[] { "/revoke" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/revoke <#>", "Removes ownership of all cubes in specified <#> ship.");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/revoke\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var shipName = match.Groups["Key"].Value;

                var currentShipList = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(currentShipList, e => e is IMyCubeGrid && e.DisplayName.Equals(shipName, StringComparison.InvariantCultureIgnoreCase));

                if (currentShipList.Count == 0)
                {
                    int index;
                    if (shipName.Substring(0, 1) == "#" && Int32.TryParse(shipName.Substring(1), out index) && index > 0 && index <= CommandListShips.ShipCache.Count)
                    {
                        currentShipList = new HashSet<IMyEntity> { CommandListShips.ShipCache[index - 1] };
                    }
                }

                // There may be more than one ship with a matching name.
                foreach (var selectedShip in currentShipList)
                {
                    var grids = selectedShip.GetAttachedGrids();
                    foreach (var grid in grids)
                    {
                        grid.ChangeGridOwnership(0, MyOwnershipShareModeEnum.All);
                    }
                }

                return true;
            }

            return false;
        }
    }
}
