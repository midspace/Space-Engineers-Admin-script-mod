namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using VRage.ModAPI;
    using Sandbox.Common.ObjectBuilders;
    using VRage.ObjectBuilders;

    public class CommandShipDestructable : ChatCommand
    {
        public CommandShipDestructable()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Experimental, "destructable", new[] { "/destructable", "/destruct" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/destructable <#>", "Set the specified <#> ship.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.Equals("/destructable", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false);
                if (entity != null)
                {
                    var shipEntity = entity as Sandbox.ModAPI.IMyCubeGrid;
                    if (shipEntity != null)
                    {
                        SetDestructable(shipEntity, false);
                        return true;
                    }
                }

                MyAPIGateway.Utilities.ShowMessage("deleteship", "No ship targeted.");
                return true;
            }

            //var match = Regex.Match(messageText, @"/destructable\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
            //if (match.Success)
            //{
            //    var shipName = match.Groups["Key"].Value;

            //    var currentShipList = new HashSet<IMyEntity>();
            //    MyAPIGateway.Entities.GetEntities(currentShipList, e => e is Sandbox.ModAPI.IMyCubeGrid && e.DisplayName.Equals(shipName, StringComparison.InvariantCultureIgnoreCase));

            //    if (currentShipList.Count == 1)
            //    {
            //        DeleteShip(currentShipList.First());
            //        return true;
            //    }
            //    else if (currentShipList.Count == 0)
            //    {
            //        int index;
            //        if (shipName.Substring(0, 1) == "#" && Int32.TryParse(shipName.Substring(1), out index) && index > 0 && index <= CommandListShips.ShipCache.Count && CommandListShips.ShipCache[index - 1] != null)
            //        {
            //            DeleteShip(CommandListShips.ShipCache[index - 1]);
            //            CommandListShips.ShipCache[index - 1] = null;
            //            return true;
            //        }
            //    }
            //    else if (currentShipList.Count > 1)
            //    {
            //        MyAPIGateway.Utilities.ShowMessage("deleteship", "{0} Ships match that name.", currentShipList.Count);
            //        return true;
            //    }

            //    MyAPIGateway.Utilities.ShowMessage("deleteship", "Ship name not found.");
            //    return true;
            //}

            return false;
        }

        private void SetDestructable(IMyEntity shipEntity, bool destructable)
        {
            var gridObjectBuilder = shipEntity.GetObjectBuilder(true) as MyObjectBuilder_CubeGrid;
            gridObjectBuilder.EntityId = 0;
            gridObjectBuilder.DestructibleBlocks = destructable;

            // This will Delete the entity and sync to all.
            // Using this, also works with player ejection in the same Tick.
            shipEntity.SyncObject.SendCloseRequest();

            var tempList = new List<MyObjectBuilder_EntityBase>();
            tempList.Add(gridObjectBuilder);
            tempList.CreateAndSyncEntities();
        }
    }
}
