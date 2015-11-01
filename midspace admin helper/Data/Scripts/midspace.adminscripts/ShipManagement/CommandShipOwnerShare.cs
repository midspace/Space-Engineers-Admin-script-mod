namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using VRage.ModAPI;

    /// <summary>
    /// This changes of permissions of single blocks owned by the player only to shared.
    /// </summary>
    public class CommandShipOwnerShare : ChatCommand
    {
        public CommandShipOwnerShare()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Experimental, "share", new[] { "/share" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/share <#>", "Share ownership of the <#> specified ship.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.Equals("/share", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false);
                if (entity != null)
                {
                    var shipEntity = entity as Sandbox.ModAPI.IMyCubeGrid;
                    if (shipEntity != null)
                    {
                        if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                        {
                            ChangeCubeShareMode(shipEntity, MyAPIGateway.Session.Player.PlayerID, MyAPIGateway.Session.Player.PlayerID, MyOwnershipShareModeEnum.All);
                        }
                        else
                        {
                            // TODO: ConnectionHelper.SendMessageToServer(new MessageSyncShare() { EntityId = shipEntity.EntityId, PlayerId = MyAPIGateway.Session.Player.PlayerID });
                        }
                        MyAPIGateway.Utilities.ShowMessage("Share", "Changing ownership of ship '{0}'.", shipEntity.DisplayName);
                        return true;
                    }
                }
                MyAPIGateway.Utilities.ShowMessage("Share", "No ship targeted.");
                return true;
            }

            var match = Regex.Match(messageText, @"/share\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);

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
                        if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                        {
                            ChangeCubeShareMode(grid, MyAPIGateway.Session.Player.PlayerID, MyAPIGateway.Session.Player.PlayerID, MyOwnershipShareModeEnum.All);
                        }
                        else
                        {
// TODO: ConnectionHelper.SendMessageToServer(new MessageSyncShare() { EntityId = grid.EntityId, PlayerId = MyAPIGateway.Session.Player.PlayerID });
                        }
                        MyAPIGateway.Utilities.ShowMessage("Share", "Changing ownership of ship '{0}'.", grid.DisplayName);
                    }
                }

                return true;
            }

            return false;
        }

        private void ChangeCubeShareMode(IMyEntity selectedShip, long oldPlayer, long newPlayer, MyOwnershipShareModeEnum shareMode)
        {
            var grids = selectedShip.GetAttachedGrids();
            foreach (var grid in grids)
            {
                var blocks = new List<IMySlimBlock>();
                // we only want to change the share of blocks you own currently.
                grid.GetBlocks(blocks, f => f.FatBlock != null && f.FatBlock.OwnerId == oldPlayer);

                foreach (var block in blocks)
                    block.FatBlock.ChangeOwner(newPlayer, shareMode);
            }
        }
    }
}
