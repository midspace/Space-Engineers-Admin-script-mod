namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;

    public class CommandShipScaleUp : ChatCommand
    {
        private const MyCubeSize scale = MyCubeSize.Large;

        public CommandShipScaleUp()
            : base(ChatCommandSecurity.Admin, "scaleup", new[] { "/scaleup" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/scaleup <#>", "Converts a small ship into a large ship, also converts all cubes to large.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/scaleup", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false);
                if (entity != null)
                {
                    if (CommandShipScaleUp.ScaleShip(steamId, entity as IMyCubeGrid, scale))
                        return true;
                }

                MyAPIGateway.Utilities.SendMessage(steamId, "scaleup", "No ship targeted.");
                return true;
            }

            var match = Regex.Match(messageText, @"/scaleup\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var shipName = match.Groups["Key"].Value;

                var currentShipList = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(currentShipList, e => e is IMyCubeGrid && e.DisplayName.Equals(shipName, StringComparison.InvariantCultureIgnoreCase));

                if (currentShipList.Count == 1)
                {
                    if (CommandShipScaleUp.ScaleShip(steamId, currentShipList.First() as IMyCubeGrid, scale))
                        return true;
                }
                else if (currentShipList.Count == 0)
                {
                    int index;
                    List<IMyEntity> shipCache = CommandListShips.GetShipCache(steamId);
                    if (shipName.Substring(0, 1) == "#" && Int32.TryParse(shipName.Substring(1), out index) && index > 0 && index <= shipCache.Count && shipCache[index - 1] != null)
                    {
                        if (CommandShipScaleUp.ScaleShip(steamId, shipCache[index - 1] as IMyCubeGrid, scale))
                        {
                            shipCache[index - 1] = null;
                            return true;
                        }
                    }
                }
                else if (currentShipList.Count > 1)
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "scaleup", "{0} Ships match that name.", currentShipList.Count);
                    return true;
                }

                MyAPIGateway.Utilities.SendMessage(steamId, "scaleup", "Ship name not found.");
                return true;
            }

            return false;
        }

        static Dictionary<string, string> LargeToSmall = new Dictionary<string, string>() { 
            { "LargeBlockConveyor", "SmallBlockConveyor" },
            { "ConveyorTube", "ConveyorTubeSmall" },
            { "ConveyorTubeCurved", "ConveyorTubeSmallCurved" },
            { "LargeBlockLargeContainer", "SmallBlockMediumContainer" }
        };

        static Dictionary<string, string> SmallToLarge = new Dictionary<string, string>() { 
            { "SmallBlockConveyor", "LargeBlockConveyor" },
            { "ConveyorTubeSmall", "ConveyorTube" } ,
            { "ConveyorTubeSmallCurved", "ConveyorTubeCurved" },
            { "SmallBlockMediumContainer", "LargeBlockLargeContainer" },
        };

        // TODO: deal with cubes that need to be rotated.
        // LargeBlockBeacon, SmallBlockBeacon, ConveyorTube, ConveyorTubeSmall
    
        public static bool ScaleShip(ulong steamId, IMyCubeGrid shipEntity, MyCubeSize newScale)
        {
            if (shipEntity == null)
                return false;

            if (shipEntity.GridSizeEnum == newScale)
            {
                MyAPIGateway.Utilities.SendMessage(steamId, "scaledown", "Ship is already the right scale.");
                return true;
            }

            var grids = shipEntity.GetAttachedGrids();

            var newGrids = new MyObjectBuilder_CubeGrid[grids.Count];

            foreach (var cubeGrid in grids)
            {
                // ejects any player prior to deleting the grid.
                cubeGrid.EjectControllingPlayers();
                cubeGrid.Physics.Enabled = false;
            }

            var tempList = new List<MyObjectBuilder_EntityBase>();
            var gridIndex = 0;
            foreach (var cubeGrid in grids)
            {
                var blocks = new List<IMySlimBlock>();
                cubeGrid.GetBlocks(blocks);

                var gridObjectBuilder = cubeGrid.GetObjectBuilder(true) as MyObjectBuilder_CubeGrid;

                gridObjectBuilder.EntityId = 0;
                Regex rgx = new Regex(Regex.Escape(gridObjectBuilder.GridSizeEnum.ToString()));
                var rgxScale = Regex.Escape(newScale.ToString());
                gridObjectBuilder.GridSizeEnum = newScale;
                var removeList = new List<MyObjectBuilder_CubeBlock>();

                foreach (var block in gridObjectBuilder.CubeBlocks)
                {
                    MyCubeBlockDefinition definition; 
                    string newSubType = null;
                    if (newScale == MyCubeSize.Small && LargeToSmall.ContainsKey(block.SubtypeName))
                        newSubType = LargeToSmall[block.SubtypeName];
                    else if (newScale == MyCubeSize.Large && SmallToLarge.ContainsKey(block.SubtypeName))
                        newSubType = SmallToLarge[block.SubtypeName];
                    else
                    {
                        newSubType = rgx.Replace(block.SubtypeName, rgxScale, 1);

                        // Match using the BlockPairName if there is a matching cube.
                        if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(new MyDefinitionId(block.GetType(), block.SubtypeName), out definition))
                        {
                            var newDef = MyDefinitionManager.Static.GetAllDefinitions().Where(d => d is MyCubeBlockDefinition && ((MyCubeBlockDefinition)d).BlockPairName == definition.BlockPairName && ((MyCubeBlockDefinition)d).CubeSize == newScale).FirstOrDefault();
                            if (newDef != null)
                                newSubType = newDef.Id.SubtypeName;
                        }
                    }
                    if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(new MyDefinitionId(block.GetType(), newSubType), out definition) && definition.CubeSize == newScale)
                    {
                        block.SubtypeName = newSubType;
                        //block.EntityId = 0;
                    }
                    else
                    {
                        removeList.Add(block);
                    }
                }

                foreach (var block in removeList)
                {
                    gridObjectBuilder.CubeBlocks.Remove(block);
                }

                // This will Delete the entity and sync to all.
                // Using this, also works with player ejection in the same Tick.
                cubeGrid.SyncObject.SendCloseRequest(); 

                var name = cubeGrid.DisplayName;
                MyAPIGateway.Utilities.SendMessage(steamId, "ship", "'{0}' resized.", name);

                tempList.Add(gridObjectBuilder);

                gridIndex++;
            }

            // TODO: reposition multiple grids so rotors and pistons re-attach.

            tempList.CreateAndSyncEntities();
            return true;
        }
    }
}
