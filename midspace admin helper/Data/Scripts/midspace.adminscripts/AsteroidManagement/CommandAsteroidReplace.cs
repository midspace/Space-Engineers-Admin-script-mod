namespace midspace.adminscripts
{
    using System.Text.RegularExpressions;

    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using VRageMath;
    using VRage.Voxels;

    /// <summary>
    /// Replaces the specified ore in an asteroid with another specified ore.
    /// </summary>
    public class CommandAsteroidReplace : ChatCommand
    {
        public CommandAsteroidReplace()
            : base(ChatCommandSecurity.Admin, "roidreplace", new[] { "/roidreplace" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/roidreplace <name> <material1> <material2>", "In Asteroid <name> will replace <material1> with <material2>. ie, \"/roidreplace baseasteroid1 stone_01 gold_01\"");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/roidreplace\s+(?<Asteroid>[^\s]+)\s+(?<Material1>[^\s]+)\s+(?<Material2>[^\s]+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var searchAsteroidName = match.Groups["Asteroid"].Value;
                IMyVoxelBase originalAsteroid = null;
                if (!Support.FindAsteroid(searchAsteroidName, out originalAsteroid))
                {
                    MyAPIGateway.Utilities.ShowMessage("Cannot find asteroid", string.Format("'{0}'", searchAsteroidName));
                    return true;
                }

                var searchMaterialName1 = match.Groups["Material1"].Value;
                MyVoxelMaterialDefinition material1;
                string suggestedMaterials = "";
                if (!Support.FindMaterial(searchMaterialName1, out material1, ref suggestedMaterials))
                {
                    MyAPIGateway.Utilities.ShowMessage("Invalid Material1 specified.", "Cannot find the material '{0}'.\r\nTry the following: {1}", searchMaterialName1, suggestedMaterials);
                    return true;
                }

                var searchMaterialName2 = match.Groups["Material2"].Value;
                MyVoxelMaterialDefinition material2;
                if (!Support.FindMaterial(searchMaterialName2, out material2, ref suggestedMaterials))
                {
                    MyAPIGateway.Utilities.ShowMessage("Invalid Material2 specified.", "Cannot find the material '{0}'.\r\nTry the following: {1}", searchMaterialName2, suggestedMaterials);
                    return true;
                }

                var oldStorage = originalAsteroid.Storage;
                var oldCache = new MyStorageDataCache();
                oldCache.Resize(oldStorage.Size);
                oldStorage.ReadRange(oldCache, MyStorageDataTypeFlags.ContentAndMaterial, 0, Vector3I.Zero, oldStorage.Size - 1);

                Vector3I p;
                for (p.Z = 0; p.Z < oldStorage.Size.Z; ++p.Z)
                    for (p.Y = 0; p.Y < oldStorage.Size.Y; ++p.Y)
                        for (p.X = 0; p.X < oldStorage.Size.X; ++p.X)
                        {
                            var material = oldCache.Material(ref p);
                            if (material == material1.Index)
                                oldCache.Material(ref p, material2.Index);
                        }

                oldStorage.WriteRange(oldCache, MyStorageDataTypeFlags.ContentAndMaterial, Vector3I.Zero, oldStorage.Size - 1);

                MyAPIGateway.Utilities.ShowMessage("Asteroid", "'{0}' material '{1}' replaced with '{2}'.", originalAsteroid.StorageName, material1.Id.SubtypeName, material2.Id.SubtypeName);
                return true;
            }

            return false;
        }
    }
}
