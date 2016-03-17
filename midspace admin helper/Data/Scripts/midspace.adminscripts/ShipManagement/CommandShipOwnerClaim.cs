namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using midspace.adminscripts.Messages.Sync;
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;

    /// <summary>
    /// This changes the ownership of an entire grid to the player.
    /// </summary>
    public class CommandShipOwnerClaim : ChatCommand
    {
        public CommandShipOwnerClaim()
            : base(ChatCommandSecurity.Admin, "claim", new[] { "/claim" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/claim <#>", "Claims ownership of the <#> specified ship. All own-able blocks are transferred to you.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/claim", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false);
                if (entity != null)
                {
                    var shipEntity = entity as IMyCubeGrid;
                    if (shipEntity != null)
                    {
                        if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                        {
                            shipEntity.ChangeGridOwnership(MyAPIGateway.Session.Player.PlayerID, MyOwnershipShareModeEnum.All);
                        }
                        else
                        {
                            MessageSyncGridOwner.SendMessage(shipEntity.EntityId, SyncOwnershipType.Claim, MyAPIGateway.Session.Player.PlayerID);
                        }
                        MyAPIGateway.Utilities.ShowMessage("Claim", "Changing ownership of ship '{0}'.", shipEntity.DisplayName);
                        return true;
                    }
                }
                MyAPIGateway.Utilities.ShowMessage("Claim", "No ship targeted.");
                return true;
            }

            var match = Regex.Match(messageText, @"/claim\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);

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
                            grid.ChangeGridOwnership(MyAPIGateway.Session.Player.PlayerID, MyOwnershipShareModeEnum.All);
                        }
                        else
                        {
                            MessageSyncGridOwner.SendMessage(grid.EntityId, SyncOwnershipType.Claim, MyAPIGateway.Session.Player.PlayerID);
                        }
                        MyAPIGateway.Utilities.ShowMessage("Claim", "Changing ownership of ship '{0}'.", grid.DisplayName);
                    }
                }

                return true;
            }

            return false;
        }
    }
}
