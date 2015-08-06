namespace midspace.adminscripts
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using VRage.ModAPI;
    using VRage.Voxels;
    using VRageMath;

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
            if (brief)
            {
                MyAPIGateway.Utilities.ShowMessage("/roidreplace [name] <material1> <material2>", "In Asteroid <name> will replace <material1> with <material2>. <Name> is optional if you are looking at the asteroid. ie, \"/roidreplace baseasteroid1 stone_01 gold_01\"");
            }
            else
            {
                var validMaterials = MyDefinitionManager.Static.GetVoxelMaterialDefinitions().Select(k => k.Id.SubtypeName).OrderBy(s => s).ToArray();
                var materialNames = String.Join(", ", validMaterials);

                var description = new StringBuilder();
                description.AppendFormat(@"This command is used to replace a material in an asteroid with another material.
/roidreplace [Name] <Old Material> <New Material>

  <Name> - Optional name of the asteroid, or hot list number from /listasteroid. Otherwise look directly at the selected asteroid.
  <Old Material> - the material that you are removing. 
  <New Material> - the material that you are replaceing with.

Note:
  The larger the asteroid, the longer it will take to process.

Examples:
  /roidreplace baseasteroid1 stone_01 gold_01

  /roidreplace #1 stone_01 grass

  /roidreplace grass Grass_Old

The following materials are available:
{0}

", materialNames);
                MyAPIGateway.Utilities.ShowMissionScreen("Create Asteroid Sphere:", null, " ", description.ToString(), null, "OK");
            }
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
                var searchMaterialName2 = match.Groups["Material2"].Value;
                return ReplaceAsteroidMaterial(originalAsteroid, searchMaterialName1, searchMaterialName2);
            }

            match = Regex.Match(messageText, @"/roidreplace\s+(?<Material1>[^\s]+)\s+(?<Material2>[^\s]+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                IMyEntity entity;
                double distance;
                Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, out entity, out distance, false, false, false, true, true);

                if (entity != null && entity is IMyVoxelMap)
                {
                    var voxelMap = (IMyVoxelMap)entity;
                    var searchMaterialName1 = match.Groups["Material1"].Value;
                    var searchMaterialName2 = match.Groups["Material2"].Value;
                    return ReplaceAsteroidMaterial(voxelMap, searchMaterialName1, searchMaterialName2);
                }
                else
                {
                    MyAPIGateway.Utilities.ShowMessage("Asteroid", "Was not targeted.");
                }
            }

            return false;
        }

        private bool ReplaceAsteroidMaterial(IMyVoxelBase originalAsteroid, string searchMaterialName1, string searchMaterialName2)
        {
            MyVoxelMaterialDefinition material1;
            string suggestedMaterials = "";
            if (!Support.FindMaterial(searchMaterialName1, out material1, ref suggestedMaterials))
            {
                MyAPIGateway.Utilities.ShowMessage("Invalid Material1 specified.", "Cannot find the material '{0}'.\r\nTry the following: {1}", searchMaterialName1, suggestedMaterials);
                return true;
            }

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
    }
}
