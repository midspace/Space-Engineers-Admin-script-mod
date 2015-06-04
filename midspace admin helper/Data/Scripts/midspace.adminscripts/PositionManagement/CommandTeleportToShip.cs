namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using VRage.ModAPI;

    public class CommandTeleportToShip : ChatCommand
    {
        public CommandTeleportToShip()
            : base(ChatCommandSecurity.Admin, "tps", new[] { "/tps" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/tps <#>", "Teleport player to the specified ship <#>.");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/tps\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);

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

                var ship = currentShipList.FirstElement();

                if (ship == null)
                {
                    MyAPIGateway.Utilities.ShowMessage("Ship name", string.Format("'{0}' not found", shipName));
                    return true;
                }

                if (ship.Closed)
                {
                    MyAPIGateway.Utilities.ShowMessage("Ship", "no longer exists");
                    return true;
                }

                if (MyAPIGateway.Session.Player.Controller.ControlledEntity is IMyCubeBlock)
                {
                    // TODO: complete code.
                    return Support.MoveShipToShip(MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetTopMostParent(), ship);
                }
                else 
                {
                    // Move the player only.
                    var cockpits = ship.FindWorkingCockpits();
                    if (cockpits.Length > 0 && ((IMyCubeGrid)ship).GridSizeEnum != Sandbox.Common.ObjectBuilders.MyCubeSize.Small)
                    { 
                        return Support.MovePlayerToCockpit(MyAPIGateway.Session.Player, (IMyEntity)cockpits[0]);
                    }
                    else
                    {
                        return Support.MovePlayerToShipGrid(MyAPIGateway.Session.Player, ship);
                    }
                }
            }

            return false;
        }
    }
}
