namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using VRage.ModAPI;
    using VRage.Voxels;
    using VRageMath;

    /// <summary>
    /// Find and pinpoints ore in asteroids in the vicinity.
    /// Range is limited to 5000m and LOD6, as it can take a while to work on super sized asteroid, espeically if there are many ore deposits to tag.
    /// GPS tagging lots of coordinates can slow down the processing also.
    /// </summary>
    public class CommandAsteroidScanOre : ChatCommand
    {
        private readonly string[] _oreNames;

        public CommandAsteroidScanOre(string[] oreNames)
            : base(ChatCommandSecurity.Admin, "scanore", new[] { "/scanore" })
        {
            _oreNames = oreNames;
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/scanore ['clear'] <range>", "Looks for ore within <range> in all nearby asteroids and tags them with GPS. Optional word 'clear' to remove gps.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/scanore\s+clear\s+(?<RANGE>[+-]?((\d+(\.\d*)?)|(\.\d+)))", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var scanRange = double.Parse(match.Groups["RANGE"].Value, CultureInfo.InvariantCulture);
                if (scanRange < 10) scanRange = 10;

                var list = MyAPIGateway.Session.GPS.GetGpsList(MyAPIGateway.Session.Player.IdentityId);
                var position = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetPosition();

                int counter = 0;
                foreach(var gps in list)
                {
                    if (gps.Description == "scanore" && Regex.IsMatch(gps.Name, @"^Ore [^\s]*$") && Math.Sqrt((gps.Coords - position).LengthSquared()) < scanRange)
                    {
                        MyAPIGateway.Session.GPS.RemoveGps(MyAPIGateway.Session.Player.IdentityId, gps);
                        counter++;
                    }
                }

                MyAPIGateway.Utilities.ShowMessage("Scanner", "Removed {0} gps coordinates within {1}m range.", counter, scanRange);
                return true;
            }

            match = Regex.Match(messageText, @"/scanore\s+(?<RANGE>[+-]?((\d+(\.\d*)?)|(\.\d+)))", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var scanRange = double.Parse(match.Groups["RANGE"].Value, CultureInfo.InvariantCulture);
                if (scanRange < 10) scanRange = 10;
                if (scanRange > 5000) scanRange = 5000;

                var currentAsteroidList = new List<IMyVoxelBase>();
                var position = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetPosition();
                MyAPIGateway.Session.VoxelMaps.GetInstances(currentAsteroidList, v => Math.Sqrt((position - v.PositionLeftBottomCorner).LengthSquared()) < Math.Sqrt(Math.Pow(v.Storage.Size.X, 2) * 3) + scanRange + 500f);
                var asteroids = new List<IMyVoxelBase>();
                int hits = 0;

                //var vm = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, false, false, true, false) as IMyVoxelBase;
                //hits += FindMaterial(vm, position, 3, scanRange);

                foreach(var voxelMap in currentAsteroidList)
                {
                    hits += FindMaterial(voxelMap, position, 3, scanRange);
                }

                MyAPIGateway.Utilities.ShowMessage("Scanned", "{0} ore deposits found on {1} asteroids within {2}m range.", hits, currentAsteroidList.Count, scanRange);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="voxelMap"></param>
        /// <param name="resolution">0 to 8. 0 for fine/slow detail.</param>
        /// <param name="distance"></param>
        /// <returns></returns>
        private int FindMaterial(IMyVoxelBase voxelMap, Vector3D center, int resolution, double distance)
        {
            int hits = 0;
            var materials = MyDefinitionManager.Static.GetVoxelMaterialDefinitions().Where(v => v.IsRare).ToArray();
            var findMaterial = materials.Select(f => f.Index).ToArray();
            var storage = voxelMap.Storage;
            var scale = (int)Math.Pow(2, resolution);

            //MyAPIGateway.Utilities.ShowMessage("center", center.ToString());
            var point = new Vector3I(center - voxelMap.PositionLeftBottomCorner);
            //MyAPIGateway.Utilities.ShowMessage("point", point.ToString());

            var min = ((point - (int)distance) / 64) * 64;
            min = Vector3I.Max(min, Vector3I.Zero);
            //MyAPIGateway.Utilities.ShowMessage("min", min.ToString());

            var max = ((point + (int)distance) / 64) * 64;
            max = Vector3I.Max(max, min + 64);
            //MyAPIGateway.Utilities.ShowMessage("max", max.ToString());

            //MyAPIGateway.Utilities.ShowMessage("size", voxelMap.StorageName + " " + storage.Size.ToString());
            
            if (min.X >= storage.Size.X ||
                min.Y >= storage.Size.Y ||
                min.Z >= storage.Size.Z)
            {
                //MyAPIGateway.Utilities.ShowMessage("size", "out of range");
                return 0;
            }

            var oldCache = new MyStorageData();

            //var smin = new Vector3I(0, 0, 0);
            //var smax = new Vector3I(31, 31, 31);
            ////var size = storage.Size;
            //var size = smax - smin + 1;
            //size = new Vector3I(16, 16, 16);
            //oldCache.Resize(size);
            //storage.ReadRange(oldCache, MyStorageDataTypeFlags.ContentAndMaterial, resolution, Vector3I.Zero, size - 1);

            var smax = (max / scale) - 1;
            var smin = (min / scale);
            var size = smax - smin + 1;
            oldCache.Resize(size);
            storage.ReadRange(oldCache, MyStorageDataTypeFlags.ContentAndMaterial, resolution, smin, smax);

            //MyAPIGateway.Utilities.ShowMessage("smax", smax.ToString());
            //MyAPIGateway.Utilities.ShowMessage("size", size .ToString());
            //MyAPIGateway.Utilities.ShowMessage("size - 1", (size - 1).ToString());

            Vector3I p;
            for (p.Z = 0; p.Z < size.Z; ++p.Z)
                for (p.Y = 0; p.Y < size.Y; ++p.Y)
                    for (p.X = 0; p.X < size.X; ++p.X)
                    {
                        // place GPS in the center of the Voxel
                        var position = voxelMap.PositionLeftBottomCorner + (p * scale) + (scale / 2) + min;

                        if (Math.Sqrt((position - center).LengthSquared()) < distance)
                        {
                            var content = oldCache.Content(ref p);
                            var material = oldCache.Material(ref p);

                            if (content > 0 && findMaterial.Contains(material))
                            {
                                var index = Array.IndexOf(findMaterial, material);
                                var name = materials[index].MinedOre;
                                var gps = MyAPIGateway.Session.GPS.Create("Ore " + name, "scanore", position, true, false);
                                MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, gps);
                                hits++;
                            }
                        }
                    }
            return hits;
        }
    }
}
