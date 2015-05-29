namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using Sandbox.Definitions;
    using System.Text;

    /// <summary>
    /// List all available Voxel Definitions.
    /// </summary>
    public class CommandVoxelsList : ChatCommand
    {
        /// <summary>
        /// Temporary hotlist cache created when player requests a list of voxels, populated only by search results.
        /// </summary>
        public readonly static List<string> VoxelCache = new List<string>();

        public CommandVoxelsList()
            : base(ChatCommandSecurity.Admin, "listvoxels", new[] { "/listvoxels" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/listvoxels <filter>", "List stock voxels that can be placed. Optional <filter> to refine your search by name.");
        }

        public override bool Invoke(string messageText)
        {
            string voxelName = null;
            var match = Regex.Match(messageText, @"/listvoxels\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                voxelName = match.Groups["Key"].Value;
            }

            string[] stockVoxels = MyDefinitionManager.Static.GetVoxelMapStorageDefinitions().Where(d => voxelName == null || d.Id.SubtypeName.IndexOf(voxelName, StringComparison.InvariantCultureIgnoreCase) >= 0).Select(d => d.Id.SubtypeName).OrderBy(d => d).ToArray();

            VoxelCache.Clear();
            var description = new StringBuilder();
            var prefix = string.Format("Count: {0}", stockVoxels.Length);
            var index = 1;
            foreach (var voxel in stockVoxels)
            {
                VoxelCache.Add(voxel);
                description.AppendFormat("#{0} {1}\r\n", index++, voxel);
            }

            MyAPIGateway.Utilities.ShowMissionScreen("List Voxels", prefix, " ", description.ToString(), null, "OK");
            return true;
        }
    }
}
