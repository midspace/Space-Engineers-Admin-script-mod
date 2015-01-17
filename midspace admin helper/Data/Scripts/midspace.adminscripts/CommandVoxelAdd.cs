namespace midspace.adminscripts
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Common.ObjectBuilders.Voxels;
    using Sandbox.ModAPI;
    using VRageMath;

    /// <summary>
    /// This doesn't work now. It was practivally one week after releasing the first APIs, that the API for adding asteroids was then removed.
    /// </summary>
    public class CommandVoxelAdd : ChatCommand
    {
        public CommandVoxelAdd()
            : base(ChatCommandSecurity.Experimental, "addvoxel", new[] { "/addvoxel" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/addvoxel <#>", "Add stock voxel asteroid.");
        }

        public override bool Invoke(string messageText)
        {
            //if (messageText.StartsWith("/addvoxel ", StringComparison.InvariantCultureIgnoreCase))
            //{
            //    var match = Regex.Match(messageText, @"/addvoxel\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
            //    if (match.Success)
            //    {
            //        var searchName = match.Groups["Key"].Value;

            //        // check enums, or search the path?
            //        //var stockvoxels = (MyMwcVoxelFilesEnum[])Enum.GetValues(typeof(MyMwcVoxelFilesEnum));
            //        MyMwcVoxelFilesEnum voxel;
            //        string voxelName = null;
            //        if (Enum.TryParse<MyMwcVoxelFilesEnum>(searchName, true, out voxel))
            //        {
            //            voxelName = voxel.ToString();
            //        }

            //        int index;
            //        if (searchName.Substring(0, 1) == "#" && Int32.TryParse(searchName.Substring(1), out index) && index > 0 && index <= CommandListVoxels.VoxelCache.Count)
            //        {
            //            voxelName = CommandListVoxels.VoxelCache[index - 1].ToString();
            //        }

            //        if (voxelName != null)
            //        {
            //            var voxelMapPath = Path.Combine(MyAPIGateway.Utilities.GamePaths.ContentPath, "VoxelMaps");
            //            var filename = voxelName + ".vx2";

            //            var filepartname = Path.GetFileNameWithoutExtension(filename).ToLower();
            //            var extension = Path.GetExtension(filename).ToLower();
            //            var uniqueName = Support.CreateUniqueStorageName(filepartname);

            //            MyAPIGateway.Utilities.ShowMessage("Not", "Implmented");
            //            return true;

            //            // TODO: is there a way of loading from VoxelMap game folder?
            //            //var storage = MyAPIGateway.Session.VoxelMaps..CreateStorage(size);

            //            var controlledEntity = MyAPIGateway.Session.ControlledObject.Entity;
            //            var pos = new MyPositionAndOrientation(controlledEntity.GetPosition() + controlledEntity.WorldMatrix.Forward * 300f, Vector3.Forward, Vector3.Up);

            //            var voxelMap = new MyObjectBuilder_VoxelMap
            //            {
            //                StorageName = uniqueName,
            //                PositionAndOrientation = pos,
            //                PersistentFlags = MyPersistentEntityFlags2.InScene | MyPersistentEntityFlags2.Enabled | MyPersistentEntityFlags2.CastShadows,
            //                MutableStorage = true
            //            };

            //            var entity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(voxelMap);

            //            MyAPIGateway.Utilities.ShowMessage("Check", Path.Combine(voxelMapPath, filename));

            //            if (entity != null)
            //            {
            //                var asteroid = (IMyVoxelMap)entity;
            //                //asteroid.VoxelFileName = uniqueFilename;
            //                MyAPIGateway.Utilities.ShowMessage("Created", uniqueName);
            //            }
            //            else
            //            {
            //                MyAPIGateway.Utilities.ShowMessage("Failed", uniqueName);
            //            }

            //            return true;
            //        }
            //    }
            //}

            return false;
        }
    }
}
