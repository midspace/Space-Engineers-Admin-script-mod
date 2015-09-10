namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using VRage.ModAPI;

    public class CommandShipSwitch : ChatCommand
    {
        [Flags]
        private enum Systems
        {
            None = 0x0,
            Power = 0x1, // (reactors, batteries)
            Production = 0x2, // (refineries, arc furnaces, assemblers)
            Programmable = 0x4,
            Projectors = 0x8,
            Timers = 0x10,
            Weapons = 0x20, // all.
        };

        public CommandShipSwitch()
            : base(ChatCommandSecurity.Admin, "switch", new[] { "/switch" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/switch [power] [prod] [prog] [proj] [timer] [weapon] on/off", "Turns globally on/off the selected systems.");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/switch(?:\s+(?<control>[^\s]+))+\s+(?<mode>[^\s]+)\s*$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var modeStr = match.Groups["mode"].Value;
                bool mode = modeStr.Equals("on", StringComparison.InvariantCultureIgnoreCase) || modeStr.Equals("1", StringComparison.InvariantCultureIgnoreCase);
                Systems control = Systems.None;

                for (var i = 0; i < match.Groups["control"].Captures.Count; i++)
                {
                    var controlStr = match.Groups["control"].Captures[i].Value;
                    if (controlStr.IndexOf("pow", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        control |= Systems.Power;
                    else if (controlStr.IndexOf("prod", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        control |= Systems.Production;
                    else if (controlStr.IndexOf("prog", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        control |= Systems.Programmable;
                    else if (controlStr.IndexOf("proj", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        control |= Systems.Projectors;
                    else if (controlStr.IndexOf("tim", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        control |= Systems.Timers;
                    else if (controlStr.IndexOf("wep", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        control |= Systems.Weapons;
                }

                var allShips = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(allShips, e => e is IMyCubeGrid);

                int counter = 0;

                if (control == Systems.None)
                {
                    MyAPIGateway.Utilities.ShowMessage("Switched ", "{0} systems turned {1}.", counter, (mode ? "On" : "Off"));
                    return true;
                }

                foreach (var entity in allShips)
                {
                    var cubeGrid = (IMyCubeGrid)entity;

                    var blocks = new List<Sandbox.ModAPI.IMySlimBlock>();
                    cubeGrid.GetBlocks(blocks, f => f.FatBlock != null);

                    foreach (var block in blocks)
                    {
                        // reactors, batteries
                        if ((Systems.Power & control) == Systems.Power && block.FatBlock is IMyFunctionalBlock
                            && (block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Reactor)
                                || block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_BatteryBlock)))
                        {
                            ((IMyFunctionalBlock)block.FatBlock).RequestEnable(mode); // turn power on/off.
                            counter++;
                        }
                        // refineries, arc furnaces, assemblers
                        if ((Systems.Production & control) == Systems.Production && block.FatBlock is IMyFunctionalBlock
                            && (block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Refinery)
                                || block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Assembler)))
                        {
                            ((IMyFunctionalBlock)block.FatBlock).RequestEnable(mode); // turn power on/off.
                            counter++;
                        }
                        if ((Systems.Programmable & control) == Systems.Programmable && block.FatBlock is IMyFunctionalBlock
                            && block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MyProgrammableBlock))
                        {
                            ((IMyFunctionalBlock)block.FatBlock).RequestEnable(mode); // turn power on/off.
                            counter++;
                        }
                        if ((Systems.Projectors & control) == Systems.Projectors && block.FatBlock is IMyFunctionalBlock
                            && block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Projector))
                        {
                            ((IMyFunctionalBlock)block.FatBlock).RequestEnable(mode); // turn power on/off.
                            counter++;
                        }
                        if ((Systems.Timers & control) == Systems.Timers && block.FatBlock is IMyFunctionalBlock
                            && block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_TimerBlock))
                        {
                            ((IMyFunctionalBlock)block.FatBlock).RequestEnable(mode); // turn power on/off.
                            counter++;
                        }
                        if ((Systems.Weapons & control) == Systems.Weapons && block.FatBlock is IMyFunctionalBlock
                            && (block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_InteriorTurret)
                                || block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_LargeGatlingTurret)
                                || block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_LargeMissileTurret)
                                || block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_SmallGatlingGun)
                                || block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_SmallMissileLauncher)
                                || block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_SmallMissileLauncherReload)))
                        {
                            ((IMyFunctionalBlock)block.FatBlock).RequestEnable(mode); // turn power on/off.
                            counter++;
                        }
                    }
                }

                MyAPIGateway.Utilities.ShowMessage("Switched ", "{0} systems turned {1}.", counter, (mode ? "On" : "Off"));
                return true;
            }

            return false;
        }
    }
}
