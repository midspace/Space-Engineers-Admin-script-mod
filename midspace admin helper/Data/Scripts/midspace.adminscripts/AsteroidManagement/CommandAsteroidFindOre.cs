namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using Sandbox.ModAPI.Interfaces;
    using VRage.Voxels;
    using VRageMath;

    /// <summary>
    /// This was the beginning of an idea for an Long range Ore Scanner.
    /// Except, it takes too long to work, and can potentially pause your game for long periods.
    /// The problem is loading Store from the Voxel. At LOD 0, it takes too long.
    /// </summary>
    public class CommandAsteroidFindOre : ChatCommand
    {
        private readonly string[] _oreNames;

        public CommandAsteroidFindOre(string[] oreNames)
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Experimental, "findore", new[] { "/findore" })
        {
            _oreNames = oreNames;
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/findore <filter>", "List in-game asteroids. Optional <filter> to refine your search by name.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.StartsWith("/findore", StringComparison.InvariantCultureIgnoreCase))
            {
                string oreName = null;

                var match = Regex.Match(messageText, @"/findore\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var searchName = match.Groups["Key"].Value;

                    foreach (var ore in _oreNames)
                    {
                        if (ore.Equals(searchName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            oreName = ore;
                            break;
                        }
                    }
                }

                if (oreName == null)
                {
                    MyAPIGateway.Utilities.ShowMessage("Ore Name", "Could not be found.");
                    return false;
                }

                var currentAsteroidList = new List<IMyVoxelBase>();
                var position = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetPosition();
                MyAPIGateway.Session.VoxelMaps.GetInstances(currentAsteroidList, v => Math.Sqrt((position - v.PositionLeftBottomCorner).LengthSquared()) < 5000f);
                var asteroids = new List<IMyVoxelBase>();

                var materials = MyDefinitionManager.Static.GetVoxelMaterialDefinitions().Where(f => f.MinedOre.Equals(oreName, StringComparison.InvariantCultureIgnoreCase)).Select(f => f.Index).ToArray();

                //var index = 1;
                foreach (var voxelMap in currentAsteroidList)
                {
                    if (FindMaterial(voxelMap.Storage, materials))
                    {
                        asteroids.Add(voxelMap);
                    }
                }

                if (asteroids.Count == 0)
                {
                    MyAPIGateway.Utilities.ShowMessage("Scanned", string.Format("{0} asteroids in scanner range. {1} Ore could not be found.", currentAsteroidList.Count, oreName));
                    return true;
                }

                MyAPIGateway.Utilities.ShowMessage(string.Format("{0} Ore", oreName), string.Format("Was found at {0}.", asteroids.Count));
                //MyAPIGateway.Utilities.ShowMessage(string.Format("#{0}", index++), voxelMap.StorageName);


                return true;
            }

            return false;
        }

        private bool FindMaterial(IMyStorage storage, byte[] findMaterial)
        {
            if (findMaterial.Length == 0)
                return false;

            var oldCache = new MyStorageData();
            oldCache.Resize(storage.Size);
            storage.ReadRange(oldCache, MyStorageDataTypeFlags.ContentAndMaterial, 2, Vector3I.Zero, storage.Size - 1);
            //MyAPIGateway.Utilities.ShowMessage("check", string.Format("SizeLinear {0}  {1}.", oldCache.SizeLinear, oldCache.StepLinear));

            Vector3I p;
            for (p.Z = 0; p.Z < storage.Size.Z; ++p.Z)
                for (p.Y = 0; p.Y < storage.Size.Y; ++p.Y)
                    for (p.X = 0; p.X < storage.Size.X; ++p.X)
                    {
                        var content = oldCache.Content(ref p);
                        var material = oldCache.Material(ref p);

                        if (content > 0 && findMaterial.Contains(material))
                        {
                            return true;
                        }
                    }
            return false;
        }
    }
}
