namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.Common.ObjectBuilders.Voxels;
    using Sandbox.ModAPI;

    /// <summary>
    /// This one is broken. It may be removed in light of the new 'exploration' game feature.
    /// </summary>
    public class CommandVoxelsList : ChatCommand
    {
        ///// <summary>
        ///// Temporary hotlist cache created when player requests a list of voxels, populated only by search results.
        ///// </summary>
        //public readonly static List<MyMwcVoxelFilesEnum> VoxelCache = new List<MyMwcVoxelFilesEnum>();

        public CommandVoxelsList()
            : base(ChatCommandSecurity.Experimental, "listvoxels", new[] { "/listvoxels" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/listvoxels <filter>", "List stock voxels that can be placed. Optional <filter> to refine your search by name.");
        }

        public override bool Invoke(string messageText)
        {
            //if (messageText.StartsWith("/listvoxels", StringComparison.InvariantCultureIgnoreCase))
            //{
            //    string voxelName = null;
            //    var match = Regex.Match(messageText, @"/listvoxels\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
            //    if (match.Success)
            //    {
            //        voxelName = match.Groups["Key"].Value;
            //    }

            //    var stockVoxels = (MyMwcVoxelFilesEnum[])Enum.GetValues(typeof(MyMwcVoxelFilesEnum));
            //    if (voxelName != null)
            //        stockVoxels = stockVoxels.Where(kvp => kvp.ToString().IndexOf(voxelName, StringComparison.InvariantCultureIgnoreCase) >= 0).ToArray();

            //    VoxelCache.Clear();
            //    MyAPIGateway.Utilities.ShowMessage("Count", stockVoxels.Length.ToString());
            //    var index = 1;
            //    foreach (var voxel in stockVoxels)
            //    {
            //        VoxelCache.Add(voxel);
            //        MyAPIGateway.Utilities.ShowMessage(string.Format("#{0}", index++), voxel.ToString());
            //    }

            //    return true;
            //}

            return false;
        }
    }
}
