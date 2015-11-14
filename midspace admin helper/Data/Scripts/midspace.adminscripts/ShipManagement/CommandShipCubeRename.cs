namespace midspace.adminscripts
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Sandbox.ModAPI;

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
            MyAPIGateway.Utilities.ShowMessage("/cuberename <#>", "Renames the custom names of terminal blocks.");
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
                foreach (var grid in grids)
                {
                    var blocks = new List<IMySlimBlock>();
                    grid.GetBlocks(blocks, b => b.FatBlock != null && b.FatBlock is IMyTerminalBlock);

                    int blockCounter = 1;

                    foreach (var block in blocks)
                    {
                        var terminal = (IMyTerminalBlock)block.FatBlock;
                        match = Regex.Match(terminal.CustomName, blockPattern, RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            // newName is from definition.
                            var newName = block.FatBlock.BlockDefinition.GetDisplayName() + " " + blockCounter;
                            MyAPIGateway.Utilities.ShowMessage("namechange", "'{0}' => '{1}'", terminal.CustomName, newName);
                            terminal.SetCustomName(newName);
                            counter++;
                            blockCounter++;
                        }
                    }
                }

                MyAPIGateway.Utilities.ShowMessage("Rename", "check='{0}'.  {1} blocks renamed.", blockName, counter);
                return true;
            }

            return false;
        }
    }
}
