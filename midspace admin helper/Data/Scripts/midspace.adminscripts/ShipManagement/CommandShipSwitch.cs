namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;

    public class CommandShipSwitch : ChatCommand
    {
        [Flags]
        private enum SwitchSystems
        {
            None = 0x0,
            Power = 0x1, // (reactors, batteries)
            Production = 0x2, // (refineries, arc furnaces, assemblers)
            Programmable = 0x4,
            Projectors = 0x8,
            Timers = 0x10,
            Weapons = 0x20, // all types.
            SpotLights = 0x40,
            Sensors = 0x80,
            Medical = 0x100,
            Mass = 0x200,
            Welder = 0x400,
            Grinder = 0x800
        };

        public CommandShipSwitch()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Server, "switch", new[] { "/switch" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/switch [grinder] [power] [production] [program] [projection] [sensor] [spot] [timer] [weapon] [welder] on/off", "Turns globally on/off the selected systems.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/switch(?:\s+(?<control>[^\s]+))+\s+(?<mode>[^\s]+)\s*$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var modeStr = match.Groups["mode"].Value;
                bool mode = modeStr.Equals("on", StringComparison.InvariantCultureIgnoreCase) || modeStr.Equals("1", StringComparison.InvariantCultureIgnoreCase);
                SwitchSystems control = SwitchSystems.None;

                for (var i = 0; i < match.Groups["control"].Captures.Count; i++)
                {
                    var controlStr = match.Groups["control"].Captures[i].Value;
                    if (controlStr.IndexOf("pow", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        control |= SwitchSystems.Power;
                    else if (controlStr.IndexOf("prod", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        control |= SwitchSystems.Production;
                    else if (controlStr.IndexOf("prog", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        control |= SwitchSystems.Programmable;
                    else if (controlStr.IndexOf("proj", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        control |= SwitchSystems.Projectors;
                    else if (controlStr.IndexOf("sens", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        control |= SwitchSystems.Sensors;
                    else if (controlStr.IndexOf("spot", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        control |= SwitchSystems.SpotLights;
                    else if (controlStr.IndexOf("tim", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        control |= SwitchSystems.Timers;
                    else if (controlStr.IndexOf("wep", StringComparison.InvariantCultureIgnoreCase) >= 0 || controlStr.IndexOf("weap", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        control |= SwitchSystems.Weapons;
                    else if (controlStr.IndexOf("medi", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        control |= SwitchSystems.Medical;
                    else if (controlStr.IndexOf("mass", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        control |= SwitchSystems.Mass;
                    else if (controlStr.IndexOf("grin", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        control |= SwitchSystems.Grinder;
                    else if (controlStr.IndexOf("weld", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        control |= SwitchSystems.Welder;
                }

                if (control == SwitchSystems.None)
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "Switched", "No systems specified.");
                    return true;
                }

                var counter = SwitchSystemsOnOff(control, mode);

                MyAPIGateway.Utilities.SendMessage(steamId, "Switched", "{0} systems turned {1}.", counter, (mode ? "On" : "Off"));
                return true;
            }

            return false;
        }

        private int SwitchSystemsOnOff(SwitchSystems control, bool mode)
        {
            int counter = 0;
            var allShips = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(allShips, e => e is IMyCubeGrid);

            foreach (var entity in allShips)
            {
                var cubeGrid = (IMyCubeGrid)entity;
                counter += SwitchShipSystemsOnOff(cubeGrid, control, mode);
            }

            return counter;
        }

        private int SwitchShipSystemsOnOff(IMyCubeGrid cubeGrid, SwitchSystems control, bool mode)
        {
            int counter = 0;
            var blocks = new List<IMySlimBlock>();
            cubeGrid.GetBlocks(blocks, f => f.FatBlock != null);

            foreach (var block in blocks)
            {
                // reactors, batteries
                if ((SwitchSystems.Power & control) == SwitchSystems.Power && block.FatBlock is IMyFunctionalBlock
                    && (block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Reactor)
                        || block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_BatteryBlock)))
                {
                    ((IMyFunctionalBlock)block.FatBlock).RequestEnable(mode); // turn power on/off.
                    counter++;
                }
                // refineries, arc furnaces, assemblers
                if ((SwitchSystems.Production & control) == SwitchSystems.Production && block.FatBlock is IMyFunctionalBlock
                    && (block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Refinery)
                        || block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Assembler)))
                {
                    ((IMyFunctionalBlock)block.FatBlock).RequestEnable(mode); // turn power on/off.
                    counter++;
                }
                if ((SwitchSystems.Programmable & control) == SwitchSystems.Programmable && block.FatBlock is IMyFunctionalBlock
                    && block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MyProgrammableBlock))
                {
                    ((IMyFunctionalBlock)block.FatBlock).RequestEnable(mode); // turn power on/off.
                    counter++;
                }
                if ((SwitchSystems.Projectors & control) == SwitchSystems.Projectors && block.FatBlock is IMyFunctionalBlock
                    && block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Projector))
                {
                    ((IMyFunctionalBlock)block.FatBlock).RequestEnable(mode); // turn power on/off.
                    counter++;
                }
                if ((SwitchSystems.Timers & control) == SwitchSystems.Timers && block.FatBlock is IMyFunctionalBlock
                    && block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_TimerBlock))
                {
                    ((IMyFunctionalBlock)block.FatBlock).RequestEnable(mode); // turn power on/off.
                    counter++;
                }
                if ((SwitchSystems.Weapons & control) == SwitchSystems.Weapons && block.FatBlock is IMyFunctionalBlock
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
                if ((SwitchSystems.SpotLights & control) == SwitchSystems.SpotLights && block.FatBlock is IMyFunctionalBlock
                    && block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_ReflectorLight))
                {
                    ((IMyFunctionalBlock)block.FatBlock).RequestEnable(mode); // turn power on/off.
                    counter++;
                }
                if ((SwitchSystems.Sensors & control) == SwitchSystems.Sensors && block.FatBlock is IMyFunctionalBlock
                    && block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_SensorBlock))
                {
                    ((IMyFunctionalBlock)block.FatBlock).RequestEnable(mode); // turn power on/off.
                    counter++;
                }
                if ((SwitchSystems.Medical & control) == SwitchSystems.Medical && block.FatBlock is IMyFunctionalBlock
                    && (block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MedicalRoom)
                        || block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_CryoChamber)))
                {
                    // Switch the power systems that control the grid instead.
                    // I'm unsure if we should go with it like this, which is why it is as yet undocumented.
                    // The idea is, if you have turned the power off to all ships, you can turn the power back on only for grids with Medical and Cryo.
                    SwitchShipSystemsOnOff(cubeGrid, SwitchSystems.Power, mode);
                    counter++;
                }
                if ((SwitchSystems.Mass & control) == SwitchSystems.Mass && block.FatBlock is IMyFunctionalBlock
                    && (block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_VirtualMass)))
                {
                    ((IMyFunctionalBlock)block.FatBlock).RequestEnable(mode); // turn power on/off.
                    counter++;
                }
                if ((SwitchSystems.Grinder & control) == SwitchSystems.Grinder && block.FatBlock is IMyFunctionalBlock
                    && (block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_ShipGrinder)))
                {
                    ((IMyFunctionalBlock)block.FatBlock).RequestEnable(mode); // turn power on/off.
                    counter++;
                }
                if ((SwitchSystems.Welder & control) == SwitchSystems.Welder && block.FatBlock is IMyFunctionalBlock
                    && (block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_ShipWelder)))
                {
                    ((IMyFunctionalBlock)block.FatBlock).RequestEnable(mode); // turn power on/off.
                    counter++;
                }
            }

            return counter;
        }
    }
}
