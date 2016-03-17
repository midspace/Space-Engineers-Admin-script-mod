namespace midspace.adminscripts
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;
    using VRage.ObjectBuilders;

    /// <summary>
    /// This renames the custom names of terminal cubes.
    /// </summary>
    public class CommandShipCubeRename : ChatCommand
    {
        public CommandShipCubeRename()
            : base(ChatCommandSecurity.Admin, "cuberename", new[] { "/cuberename", "/cubename" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/cuberename <name>", "Renames and renumbers the custom names of terminal blocks back to their original. Use '*' for wildcards.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var occupiedBlock = MyAPIGateway.Session.ControlledObject as IMyCubeBlock;

            if (occupiedBlock == null)
            {
                MyAPIGateway.Utilities.ShowMessage("Rename", "Ship not occupied.");
                return true;
            }

            var match = Regex.Match(messageText, @"(?<command>(/cuberename)|(/cubename))\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var blockName = match.Groups["Key"].Value;

                if (blockName.Trim() == "")
                    return false;

                var searchNameExpression = Regex.Escape(blockName);
                searchNameExpression = searchNameExpression.Replace(@"\*", ".*");
                var blockPattern = @"^(?:(?<name>" + searchNameExpression + @")\s+(?<Value>(\d+?))\s*)|(?<name>" + searchNameExpression + ")$";

                int counter = 0;

                var ship = occupiedBlock.GetTopMostParent();
                var grids = ship.GetAttachedGrids(AttachedGrids.Static);
                var description = new StringBuilder();

                foreach (var grid in grids)
                {
                    var blocks = new List<IMySlimBlock>();
                    grid.GetBlocks(blocks, b => b.FatBlock is IMyTerminalBlock);
                    blocks = blocks.OrderBy(b => b.FatBlock.BlockDefinition.TypeId.ToString()).ThenBy(b => b.FatBlock.BlockDefinition.SubtypeName).ToList();

                    var counters = new Dictionary<SerializableDefinitionId, int>();
                    foreach (var block in blocks)
                    {
                        var terminal = (IMyTerminalBlock)block.FatBlock;
                        match = Regex.Match(terminal.CustomName, blockPattern, RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            int blockCounter = 1;
                            if (counters.ContainsKey(block.FatBlock.BlockDefinition))
                                blockCounter = counters[block.FatBlock.BlockDefinition];
                            else
                                counters.Add(block.FatBlock.BlockDefinition, blockCounter);

                            // newName is from definition.
                            var newName = block.FatBlock.BlockDefinition.GetDisplayName() + " " + blockCounter;
                            description.AppendFormat("Changed '{0}' => '{1}'\r\n", terminal.CustomName, newName);
                            terminal.SetCustomName(newName);
                            counter++;
                            counters[block.FatBlock.BlockDefinition] = blockCounter + 1;
                        }
                    }
                }

                MyAPIGateway.Utilities.ShowMissionScreen("Cube Rename", string.Format("{0} cubes changed", counter), " ", description.ToString());
                return true;
            }

            return false;
        }
    }
}
