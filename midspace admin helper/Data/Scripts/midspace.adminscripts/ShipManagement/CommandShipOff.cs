namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Messages.Sync;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using VRage.ModAPI;

    public class CommandShipOff : ChatCommand
    {
        public CommandShipOff()
            : base(ChatCommandSecurity.Admin, "off", new[] { "/off" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/off <#>", "Turns off all reactor and battery power in the specified <#> ship.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/off", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false);
                if (entity != null)
                {
                    var shipEntity = entity as Sandbox.ModAPI.IMyCubeGrid;
                    if (shipEntity != null)
                    {
                        int reactors;
                        int batteries;
                        TurnOffShip(entity, out reactors, out batteries);
                        MyAPIGateway.Utilities.ShowMessage(shipEntity.DisplayName, "{0} Reactors, {1} Batteries turned off.", reactors, batteries);
                        return true;
                    }
                }
                MyAPIGateway.Utilities.ShowMessage("Off", "No ship targeted.");
                return true;
            }

            var match = Regex.Match(messageText, @"/off\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
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
                TurnOffShips(currentShipList, out reactors, out batteries);
                MyAPIGateway.Utilities.ShowMessage(currentShipList.First().DisplayName, "{0} Reactors, {1} Batteries turned off.", reactors, batteries);
                return true;
            }

            return false;
        }

        private void TurnOffShips(IEnumerable<IMyEntity> shipList, out int reactorCounter, out int batteryCounter)
        {
            reactorCounter = 0;
            batteryCounter = 0;
            foreach (var selectedShip in shipList)
            {
                int reactors;
                int batteries;
                TurnOffShip(selectedShip, out reactors, out batteries);
                reactorCounter += reactors;
                batteryCounter += batteries;
            }
        }

        private void TurnOffShip(IMyEntity shipEntity, out int reactorCounter, out int batteryCounter)
        {
            reactorCounter = 0;
            batteryCounter = 0;
            var grids = shipEntity.GetAttachedGrids(AttachedGrids.Static);

            foreach (var cubeGrid in grids)
            {
                var blocks = new List<Sandbox.ModAPI.IMySlimBlock>();
                cubeGrid.GetBlocks(blocks, f => f.FatBlock != null
                    && f.FatBlock is IMyFunctionalBlock
                    && (f.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Reactor)
                      || f.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_BatteryBlock)));

                var list = blocks.Select(f => (IMyFunctionalBlock)f.FatBlock).Where(f => f.Enabled).ToArray();

                foreach (var item in list)
                {
                    MessageSyncBlock.Process(item, SyncBlockType.PowerOff);

                    if (item.BlockDefinition.TypeId == typeof(MyObjectBuilder_Reactor))
                        reactorCounter++;
                    else
                        batteryCounter++;
                }
            }
        }
    }
}
