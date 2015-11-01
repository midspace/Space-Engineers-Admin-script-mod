namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using VRage.ModAPI;
    using midspace.adminscripts.Messages.Sync;

    public class CommandShipOwnerRevoke : ChatCommand
    {
        public CommandShipOwnerRevoke()
            : base(ChatCommandSecurity.Admin, "revoke", new[] { "/revoke" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/revoke <#>", "Removes ownership of all cubes in specified <#> ship.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.Equals("/revoke", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false);
                if (entity != null)
                {
                    var shipEntity = entity as Sandbox.ModAPI.IMyCubeGrid;
                    if (shipEntity != null)
                    {
                        if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                        {
                            shipEntity.ChangeGridOwnership(0, MyOwnershipShareModeEnum.All);
                        }
                        else
                        {
                            ConnectionHelper.SendMessageToServer(new MessageSyncEntity() { EntityId = shipEntity.EntityId, Type = SyncEntityType.Revoke});
                        }
                        MyAPIGateway.Utilities.ShowMessage("Revoke", "Changing ownership of ship '{0}'.", shipEntity.DisplayName);
                        return true;
                    }
                }
                MyAPIGateway.Utilities.ShowMessage("Revoke", "No ship targeted.");
                return true;
            }

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
                        if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                        {
                            grid.ChangeGridOwnership(0, MyOwnershipShareModeEnum.All);
                        }
                        else
                        {
                            ConnectionHelper.SendMessageToServer(new MessageSyncEntity() { EntityId = grid.EntityId, Type = SyncEntityType.Revoke });
                        }
                        MyAPIGateway.Utilities.ShowMessage("Revoke", "Changing ownership of ship '{0}'.", grid.DisplayName);
                    }
                }

                return true;
            }

            return false;
        }
    }
}
