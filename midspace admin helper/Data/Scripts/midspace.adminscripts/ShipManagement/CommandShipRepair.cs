namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using VRage.Game;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;
    using IMyDestroyableObject = VRage.Game.ModAPI.Interfaces.IMyDestroyableObject;

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
                    var shipEntity = entity as IMyCubeGrid;
                    if (shipEntity != null)
                    {
                        RepairShip(steamId, entity);
                        return true;
                    }
                }

                MyAPIGateway.Utilities.SendMessage(steamId, "repair", "No ship targeted.");
                return true;
            }

            var match = Regex.Match(messageText, @"/repair\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var shipName = match.Groups["Key"].Value;

                var currentShipList = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(currentShipList, e => e is IMyCubeGrid && e.DisplayName.Equals(shipName, StringComparison.InvariantCultureIgnoreCase));

                if (currentShipList.Count == 1)
                {
                    RepairShip(steamId, currentShipList.First());
                    return true;
                }
                else if (currentShipList.Count == 0)
                {
                    int index;
                    List<IMyEntity> shipCache = CommandListShips.GetShipCache(steamId);
                    if (shipName.Substring(0, 1) == "#" && Int32.TryParse(shipName.Substring(1), out index) && index > 0 && index <= shipCache.Count && shipCache[index - 1] != null)
                    {
                        RepairShip(steamId, shipCache[index - 1]);
                        shipCache[index - 1] = null;
                        return true;
                    }
                }
                else if (currentShipList.Count > 1)
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "repair", "{0} Ships match that name.", currentShipList.Count);
                    return true;
                }

                MyAPIGateway.Utilities.SendMessage(steamId, "repair", "Ship name not found.");
                return true;
            }

            return false;
        }

        private void RepairShip(ulong steamId, IMyEntity shipEntity)
        {
            var blocks = new List<IMySlimBlock>();
            var ship = (IMyCubeGrid)shipEntity;
            ship.GetBlocks(blocks, f => f != null);


            var grid = (Sandbox.Game.Entities.MyCubeGrid)shipEntity;
            //MyGridPhysics physics = grid.Physics; // MyGridPhysics not allowed.
            //physics.RecreateWeldedShape();  // MyPhysicsBody not allowed.


            //var physics = new MyGridPhysics(grid, null);

            //grid.ApplyDestructionDeformation();
            //physics.ApplyDeformation(0, 0, 0, Vector3.Zero, Vector3.Zero, MyDamageType.Weld, 0, 0, 0);

            //Sandbox.Game.Multiplayer.MySyncGrid g = grid.SyncObject;  // is marked Internal.

            foreach (IMySlimBlock block in blocks)
            {
                //block.CurrentDamage
                //block.AccumulatedDamage
                //block.DamageRatio
                //block.HasDeformation
                //block.MaxDeformation

                //((IMyDestroyableObject)block).DoDamage(-1000, MyDamageType.Weld, true);
                //block.ApplyAccumulatedDamage();
                //ship.ApplyDestructionDeformation(block);
                //grid.ApplyDestructionDeformation((Sandbox.Game.Entities.Cube.MySlimBlock)block, -1f);

                if ((block.HasDeformation || block.MaxDeformation > 0.0f) || !block.IsFullIntegrity)
                {
                    //float maxAllowedBoneMovement = WELDER_MAX_REPAIR_BONE_MOVEMENT_SPEED * ToolCooldownMs * 0.001f;

                    //var b = block as MySlimBlock; // MySlimBlock is not allowed.
                    //block.IncreaseMountLevel(WeldAmount, Owner.ControllerInfo.ControllingIdentityId, CharacterInventory, maxAllowedBoneMovement);
                    //b.IncreaseMountLevel(1000, 0, null, 0);
                }

                var targetDestroyable = block as IMyDestroyableObject;
                if (targetDestroyable != null)
                {

                }
            }

            MyAPIGateway.Utilities.SendMessage(steamId, "repair", "Ship '{0}' has been repairded.", shipEntity.DisplayName);
        }

        private void RepairShip2(ulong steamId, IMyEntity shipEntity)
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

            MyAPIGateway.Utilities.SendMessage(steamId, "repair", "Ship '{0}' has been repairded.", shipEntity.DisplayName);
        }
    }
}
