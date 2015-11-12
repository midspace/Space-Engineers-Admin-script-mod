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

    public class CommandShipRepair : ChatCommand
    {
        public CommandShipRepair()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Client | ChatCommandFlag.Experimental, "repair", new[] { "/repair" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/repair <#>", "Repairs the specified <#> ship.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/repair", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false);
                if (entity != null)
                {
                    var shipEntity = entity as Sandbox.ModAPI.IMyCubeGrid;
                    if (shipEntity != null)
                    {
                        RepairShip(entity);
                        return true;
                    }
                }

                MyAPIGateway.Utilities.ShowMessage("repair", "No ship targeted.");
                return true;
            }

            var match = Regex.Match(messageText, @"/repair\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var shipName = match.Groups["Key"].Value;

                var currentShipList = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(currentShipList, e => e is Sandbox.ModAPI.IMyCubeGrid && e.DisplayName.Equals(shipName, StringComparison.InvariantCultureIgnoreCase));

                if (currentShipList.Count == 1)
                {
                    RepairShip(currentShipList.First());
                    return true;
                }
                else if (currentShipList.Count == 0)
                {
                    int index;
                    if (shipName.Substring(0, 1) == "#" && Int32.TryParse(shipName.Substring(1), out index) && index > 0 && index <= CommandListShips.ShipCache.Count && CommandListShips.ShipCache[index - 1] != null)
                    {
                        RepairShip(CommandListShips.ShipCache[index - 1]);
                        CommandListShips.ShipCache[index - 1] = null;
                        return true;
                    }
                }
                else if (currentShipList.Count > 1)
                {
                    MyAPIGateway.Utilities.ShowMessage("repair", "{0} Ships match that name.", currentShipList.Count);
                    return true;
                }

                MyAPIGateway.Utilities.ShowMessage("repair", "Ship name not found.");
                return true;
            }

            return false;
        }

        private void RepairShip(IMyEntity shipEntity)
        {
            // We SHOULD NOT make any changes directly to the prefab, we need to make a Value copy using Clone(), and modify that instead.
            var gridObjectBuilder = shipEntity.GetObjectBuilder().Clone() as MyObjectBuilder_CubeGrid;

            shipEntity.Physics.Deactivate();

            // This will Delete the entity and sync to all.
            // Using this, also works with player ejection in the same Tick.
            shipEntity.SyncObject.SendCloseRequest();

            gridObjectBuilder.EntityId = 0;

            if (gridObjectBuilder.Skeleton == null)
                gridObjectBuilder.Skeleton = new List<BoneInfo>();
            else
                gridObjectBuilder.Skeleton.Clear();

            foreach (var cube in gridObjectBuilder.CubeBlocks)
            {
                cube.IntegrityPercent = cube.BuildPercent;
                cube.DeformationRatio = 0;
                // No need to set bones for individual blocks like rounded armor, as this is taken from the definition within the game itself.
            }

            var tempList = new List<MyObjectBuilder_EntityBase>();
            tempList.Add(gridObjectBuilder);
            tempList.CreateAndSyncEntities();

            MyAPIGateway.Utilities.ShowMessage("repair", "Ship '{0}' has been repairded.", shipEntity.DisplayName);
        }
    }
}
