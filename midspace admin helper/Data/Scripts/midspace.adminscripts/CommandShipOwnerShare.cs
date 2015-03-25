namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;

    /// <summary>
    /// This was going to allow changing of permissions of blocks owned by the player,
    /// however you cannot change ownership of indivual blocks currently. All blocks in an entire grid must be set all at once.
    /// This wont work if the blocks are currently owned several players, as it will rewrite all ownership.
    /// </summary>
    public class CommandShipOwnerShare : ChatCommand
    {
        public CommandShipOwnerShare()
            : base(ChatCommandSecurity.Admin, "share", new[] { "/share" }, ChatCommandFlag.Experimental)
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/share <#>", "Share ownership of the <#> specified ship.");
        }

        public override bool Invoke(string messageText)
        {
            //var match = Regex.Match(messageText, @"/share\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
            //if (match.Success)
            //{
            //    var shipName = match.Groups["Key"].Value;

            //    var currentShipList = new HashSet<IMyEntity>();
            //    MyAPIGateway.Entities.GetEntities(currentShipList, e => e is IMyCubeGrid && e.DisplayName.Equals(shipName, StringComparison.InvariantCultureIgnoreCase));

            //    if (currentShipList.Count == 0)
            //    {
            //        int index;
            //        if (shipName.Substring(0, 1) == "#" && Int32.TryParse(shipName.Substring(1), out index) && index > 0 && index <= CommandListShips.ShipCache.Count)
            //        {
            //            currentShipList = new HashSet<IMyEntity> { CommandListShips.ShipCache[index - 1] };
            //        }
            //    }

            //    // There may be more than one ship with a matching name.
            //    foreach (var selectedShip in currentShipList)
            //    {
            //        var grids = selectedShip.GetAttachedGrids();
            //        foreach (var grid in grids)
            //        {
            //            // TODO: only change share mode for individual blocks
            //            //grid.ChangeGridOwnership(MyAPIGateway.Session.Player.PlayerID, MyOwnershipShareModeEnum.All);
                        
            //        }
            //    }

            //    return true;
            //}

            return false;
        }
    }
}
