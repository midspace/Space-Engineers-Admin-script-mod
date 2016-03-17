namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Messages.Sync;
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;

    /// <summary>
    /// This changes of permissions of all blocks to shared.
    /// </summary>
    public class CommandShipOwnerShare : ChatCommand
    {
        public CommandShipOwnerShare()
            : base(ChatCommandSecurity.Admin, "share", new[] { "/share" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/share <#>", "Share ownership of the <#> specified ship to All, without removing the original owner.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/share", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false);
                if (entity != null)
                {
                    var shipEntity = entity as IMyCubeGrid;
                    if (shipEntity != null)
                    {
                        if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                        {
                            ChangeCubeShareMode(shipEntity, MyOwnershipShareModeEnum.All);
                            MyAPIGateway.Utilities.ShowMessage("Share", "Changing ownership of ship '{0}'.", shipEntity.DisplayName);
                        }
                        else
                        {
                            MessageSyncGridOwner.SendMessage(shipEntity.EntityId, SyncOwnershipType.ShareAll);
                        }
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
                    var grids = selectedShip.GetAttachedGrids(AttachedGrids.Static);
                    foreach (var grid in grids)
                    {
                        if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                        {
                            ChangeCubeShareMode(grid, MyOwnershipShareModeEnum.All);
                            MyAPIGateway.Utilities.ShowMessage("Share", "Changing ownership of ship '{0}'.", grid.DisplayName);
                        }
                        else
                        {
                            MessageSyncGridOwner.SendMessage(grid.EntityId, SyncOwnershipType.ShareAll);
                        }
                    }
                }

                return true;
            }

            return false;
        }

        private void ChangeCubeShareMode(IMyEntity selectedShip, MyOwnershipShareModeEnum shareMode)
        {
            var grids = selectedShip.GetAttachedGrids(AttachedGrids.Static);
            foreach (var grid in grids)
            {
                var blocks = new List<IMySlimBlock>();
                grid.GetBlocks(blocks, f => f.FatBlock != null && f.FatBlock.OwnerId != 0);

                foreach (var block in blocks)
                    block.FatBlock.ChangeOwner(block.FatBlock.OwnerId, shareMode);
            }
        }
    }
}
