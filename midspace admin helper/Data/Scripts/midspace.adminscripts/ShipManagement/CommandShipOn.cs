namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using VRage.ModAPI;

    public class CommandShipOn : ChatCommand
    {
        public CommandShipOn()
            : base(ChatCommandSecurity.Admin, "on", new[] { "/on" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/on <#>", "Turns on all reactor and battery power in the specified <#> ship.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.Equals("/on", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false);
                if (entity != null)
                {
                    var shipEntity = entity as Sandbox.ModAPI.IMyCubeGrid;
                    if (shipEntity != null)
                    {
                        int reactors;
                        int batteries;
                        TurnOnShip(entity, out reactors, out batteries);
                        MyAPIGateway.Utilities.ShowMessage(shipEntity.DisplayName, "{0} Reactors, {1} Batteries turned on.", reactors, batteries);
                        return true;
                    }
                }
                MyAPIGateway.Utilities.ShowMessage("On", "No ship targeted.");
                return true;
            }

            var match = Regex.Match(messageText, @"/on\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var shipName = match.Groups["Key"].Value;

                var currentShipList = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(currentShipList, e => e is Sandbox.ModAPI.IMyCubeGrid && e.DisplayName.Equals(shipName, StringComparison.InvariantCultureIgnoreCase));

                if (currentShipList.Count == 0)
                {
                    int index;
                    if (shipName.Substring(0, 1) == "#" && Int32.TryParse(shipName.Substring(1), out index) && index > 0 && index <= CommandListShips.ShipCache.Count)
                    {
                        currentShipList = new HashSet<IMyEntity> { CommandListShips.ShipCache[index - 1] };
                    }
                }

                int reactors;
                int batteries;
                TurnOnShips(currentShipList, out reactors, out batteries);
                MyAPIGateway.Utilities.ShowMessage(currentShipList.First().DisplayName, "{0} Reactors, {1} Batteries turned on.", reactors, batteries);
                return true;
            }

            return false;
        }

        private void TurnOnShips(IEnumerable<IMyEntity> shipList, out int reactorCounter, out int batteryCounter)
        {
            reactorCounter = 0;
            batteryCounter = 0;
            foreach (var selectedShip in shipList)
            {
                int reactors;
                int batteries;
                TurnOnShip(selectedShip, out reactors, out batteries);
                reactorCounter += reactors;
                batteryCounter += batteries;
            }
        }

        private void TurnOnShip(IMyEntity shipEntity, out int reactorCounter, out int batteryCounter)
        {
            reactorCounter = 0;
            batteryCounter = 0;
            var grids = shipEntity.GetAttachedGrids();

            foreach (var cubeGrid in grids)
            {
                var blocks = new List<Sandbox.ModAPI.IMySlimBlock>();
                cubeGrid.GetBlocks(blocks, f => f.FatBlock != null
                    && f.FatBlock is IMyFunctionalBlock
                    && (f.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Reactor)
                      || f.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_BatteryBlock)));

                var list = blocks.Select(f => (IMyFunctionalBlock)f.FatBlock).Where(f => !f.Enabled).ToArray();

                foreach (var item in list)
                {
                    item.RequestEnable(true);

                    if (item.BlockDefinition.TypeId == typeof(MyObjectBuilder_Reactor))
                        reactorCounter++;
                    else
                        batteryCounter++;
                }
            }
        }
    }
}
