namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;

    public class CommandShipDestructable : ChatCommand
    {
        public CommandShipDestructable()
            : base(ChatCommandSecurity.Admin, "destructable", new[] { "/destructable", "/destruct" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/destructable On|Off <#>", "Set the specified <#> ship as destructable. Ship will be removed and regenerated.");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/((destructable)|(destruct))\s+(?<switch>(on)|(off)|1|2)(\s+|$)(?<Name>.*)|$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                bool switchOn = false;
                var switchString = match.Groups["switch"].Value;
                var shipName = match.Groups["name"].Value;

                if (switchString == "")
                    return false;

                if (switchString.Equals("on", StringComparison.InvariantCultureIgnoreCase) || switchString.Equals("1", StringComparison.InvariantCultureIgnoreCase))
                    switchOn = true;

                if (switchString.Equals("off", StringComparison.InvariantCultureIgnoreCase) || switchString.Equals("0", StringComparison.InvariantCultureIgnoreCase))
                    switchOn = false;


                // set destructable on the ship in the crosshairs.
                if (string.IsNullOrEmpty(shipName))
                {
                    var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false);
                    if (entity != null)
                    {
                        var shipEntity = entity as Sandbox.ModAPI.IMyCubeGrid;
                        if (shipEntity != null)
                        {
                            SetDestructable(shipEntity, switchOn);
                            return true;
                        }
                    }

                    MyAPIGateway.Utilities.ShowMessage("destructable", "No ship targeted.");
                    return true;
                }

                // Find the selected ship.
                var currentShipList = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(currentShipList, e => e is Sandbox.ModAPI.IMyCubeGrid && e.DisplayName.Equals(shipName, StringComparison.InvariantCultureIgnoreCase));

                if (currentShipList.Count == 1)
                {
                    SetDestructable(currentShipList.First(), switchOn);
                    return true;
                }
                else if (currentShipList.Count == 0)
                {
                    int index;
                    if (shipName.Substring(0, 1) == "#" && Int32.TryParse(shipName.Substring(1), out index) && index > 0 && index <= CommandListShips.ShipCache.Count && CommandListShips.ShipCache[index - 1] != null)
                    {
                        SetDestructable(CommandListShips.ShipCache[index - 1], switchOn);
                        CommandListShips.ShipCache[index - 1] = null;
                        return true;
                    }
                }
                else if (currentShipList.Count > 1)
                {
                    MyAPIGateway.Utilities.ShowMessage("destructable", "{0} Ships match that name.", currentShipList.Count);
                    return true;
                }

                MyAPIGateway.Utilities.ShowMessage("destructable", "Ship name not found.");
                return true;
            }

            return false;
        }

        private void SetDestructable(IMyEntity shipEntity, bool destructable)
        {
            var gridObjectBuilder = shipEntity.GetObjectBuilder(true) as MyObjectBuilder_CubeGrid;
            if (gridObjectBuilder.DestructibleBlocks == destructable)
            {
                MyAPIGateway.Utilities.ShowMessage("destructable", "Ship '{0}' destructable is already set to {1}.",  shipEntity.DisplayName, destructable ? "On" : "Off");
                return;
            }

            gridObjectBuilder.EntityId = 0;
            gridObjectBuilder.DestructibleBlocks = destructable;

            // This will Delete the entity and sync to all.
            // Using this, also works with player ejection in the same Tick.
            shipEntity.SyncObject.SendCloseRequest();

            var tempList = new List<MyObjectBuilder_EntityBase>();
            tempList.Add(gridObjectBuilder);
            tempList.CreateAndSyncEntities();

            MyAPIGateway.Utilities.ShowMessage("destructable", "Ship '{0}' destructable has been set to {1}.", shipEntity.DisplayName, destructable ? "On" : "Off");
        }
    }
}
