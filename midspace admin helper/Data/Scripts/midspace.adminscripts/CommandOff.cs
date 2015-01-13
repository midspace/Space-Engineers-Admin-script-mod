namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;

    public class CommandOff : ChatCommand
    {
        public CommandOff()
            : base(ChatCommandSecurity.Admin, "off", new[] { "/off" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/off <#>", "Turns off all reactor power in the specified <#> ship.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.Equals("/off", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false);
                if (entity != null)
                {
                    var shipEntity = entity as Sandbox.ModAPI.IMyCubeGrid;
                    if (shipEntity != null)
                    {
                        var count = TurnOffShip(entity);
                        MyAPIGateway.Utilities.ShowMessage(shipEntity.DisplayName, string.Format("{0} Reactors turned off.", count));
                        return true;
                    }
                }
            }

            if (messageText.StartsWith("/off ", StringComparison.InvariantCultureIgnoreCase))
            {
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

                    var count = TurnOffShips(currentShipList);
                    MyAPIGateway.Utilities.ShowMessage(currentShipList.First().DisplayName, string.Format("{0} Reactors turned off.", count));
                    return true;
                }
            }

            return false;
        }

        private int TurnOffShips(IEnumerable<IMyEntity> shipList)
        {
            var counter = 0;
            foreach (var selectedShip in shipList)
            {
                counter += TurnOffShip(selectedShip);
            }
            return counter;
        }

        private int TurnOffShip(IMyEntity shipEntity)
        {
            int counter = 0;
            var grids = shipEntity.GetAttachedGrids();

            foreach (var cubeGrid in grids)
            {
                var blocks = new List<Sandbox.ModAPI.IMySlimBlock>();
                cubeGrid.GetBlocks(blocks, f => f.FatBlock != null
                    && f.FatBlock is IMyFunctionalBlock
                    && f.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Reactor));

                var list = blocks.Select(f => (IMyFunctionalBlock)f.FatBlock).Where(f => f.Enabled).ToArray();

                foreach (var item in list)
                {
                    item.RequestEnable(false);
                    counter++;
                }
            }

            return counter;
        }
    }
}
