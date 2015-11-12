namespace midspace.adminscripts
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using VRageMath;

    /// <summary>
    /// Creates an asteroid from the definitions.
    /// </summary>
    public class CommandVoxelAdd : ChatCommand
    {
        public CommandVoxelAdd()
            : base(ChatCommandSecurity.Admin, "addvoxel", new[] { "/addvoxel" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/addvoxel <X> <Y> <Z> <Name>", "Add stock voxel asteroid <Name> at location <X,Y,Z>.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/addvoxel\s+(?<X>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s+(?<Y>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s+(?<Z>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s+(?<Name>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var position = new Vector3D(
                    double.Parse(match.Groups["X"].Value, CultureInfo.InvariantCulture),
                    double.Parse(match.Groups["Y"].Value, CultureInfo.InvariantCulture),
                    double.Parse(match.Groups["Z"].Value, CultureInfo.InvariantCulture));

                var searchName = match.Groups["Name"].Value;
                string voxelName = null;

                int index;
                if (searchName.Substring(0, 1) == "#" && Int32.TryParse(searchName.Substring(1), out index) && index > 0 && index <= CommandVoxelsList.VoxelCache.Count)
                {
                    voxelName = CommandVoxelsList.VoxelCache[index - 1];
                }

                if (voxelName == null)
                {
                    var stockVoxel = MyDefinitionManager.Static.GetVoxelMapStorageDefinitions().FirstOrDefault(d => d.Id.SubtypeName.Equals(searchName, StringComparison.InvariantCultureIgnoreCase));

                    if (stockVoxel != null)
                    {
                        voxelName = stockVoxel.Id.SubtypeName;
                    }
                }

                if (voxelName == null)
                {
                    MyAPIGateway.Utilities.ShowMessage("Failed", "Cannot find the specified Voxel '{0}'", searchName);
                    return true;
                }

                if (position.IsValid())
                {
                    var uniqueName = Support.CreateUniqueStorageName(voxelName);

                    MyAPIGateway.Utilities.ShowMessage("Creating", "Asteroid '{0}'", uniqueName);

                    var newVoxelMap = MyAPIGateway.Session.VoxelMaps.CreateVoxelMapFromStorageName(uniqueName, voxelName, position);
                    return true;
                }
            }

            return false;
        }
    }
}
