namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using Sandbox.ModAPI.Interfaces;
    using VRageMath;
    using VRage.Common.Voxels;

    /// <summary>
    /// This worked by creating a new asteroid store with the new dimentions required (as the entire space is rotated).
    /// Currently it is not working, as the API for creating new a asteroid Store has been removed.
    /// </summary>
    public class CommandRotateAsteroid : ChatCommand
    {
        public CommandRotateAsteroid()
            : base(ChatCommandSecurity.Experimental, "rotateroid", new[] { "/rotateroid" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/rotateroid <name> <yaw> <pitch> <roll>", "Rotates the specified Asteroid <name> about <yaw> <pitch> <roll>. ie, \"rotateroid baseasteroid1 -90 180 0\"");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.StartsWith("/rotateroid ", StringComparison.InvariantCultureIgnoreCase))
            {
                var match = Regex.Match(messageText, @"/rotateroid\s{1,}(?<Key>.+){1,}\s{1,}(?<X>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s{1,}(?<Y>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s{1,}(?<Z>[+-]?((\d+(\.\d*)?)|(\.\d+)))", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var rotateVector = new Vector3(
                        double.Parse(match.Groups["X"].Value, CultureInfo.InvariantCulture),
                        double.Parse(match.Groups["Y"].Value, CultureInfo.InvariantCulture),
                        double.Parse(match.Groups["Z"].Value, CultureInfo.InvariantCulture));
                    var searchName = match.Groups["Key"].Value;

                    var currentAsteroidList = new List<IMyVoxelMap>();
                    IMyVoxelMap originalAsteroid = null;
                    MyAPIGateway.Session.VoxelMaps.GetInstances(currentAsteroidList, v => v.StorageName.Equals(searchName, StringComparison.InvariantCultureIgnoreCase));
                    if (currentAsteroidList.Count == 1)
                    {
                        originalAsteroid = currentAsteroidList[0];
                    }
                    else
                    {
                        MyAPIGateway.Session.VoxelMaps.GetInstances(currentAsteroidList, v => v.StorageName.IndexOf(searchName, StringComparison.InvariantCultureIgnoreCase) >= 0);

                        if (currentAsteroidList.Count == 1)
                        {
                            originalAsteroid = currentAsteroidList[0];
                        }
                    }

                    int index;
                    if (searchName.Substring(0, 1) == "#" && Int32.TryParse(searchName.Substring(1), out index) && index > 0 && index <= CommandListAsteroids.AsteroidCache.Count)
                    {
                        originalAsteroid = CommandListAsteroids.AsteroidCache[index - 1];
                    }

                    if (originalAsteroid != null)
                    {
                        //MyAPIGateway.Utilities.ShowMessage("check", string.Format("{0} {1},{2},{3}", asteroidName, rotateVector.X, rotateVector.Y, rotateVector.Z));
                        var quaternion = Quaternion.CreateFromYawPitchRoll(rotateVector.X / (180 / MathHelper.Pi), rotateVector.Y / (180 / MathHelper.Pi), rotateVector.Z / (180 / MathHelper.Pi));

                        //currentAsteroidList.Clear();
                        //MyAPIGateway.Session.VoxelMaps.GetInstances(currentAsteroidList, v => v.StorageName.Equals(asteroidName, StringComparison.InvariantCultureIgnoreCase));
                        //var originalAsteroid = currentAsteroidList[0];

                        //var storages = new List<IMyStorage>();
                        //MyAPIGateway.Session.VoxelMaps.GetStorages(storages, s => s.Name == asteroidName);
                        //var oldStorage = storages[0];
                        var oldStorage = originalAsteroid.Storage;

                        //MyAPIGateway.Utilities.ShowMessage("Test1", "check");

                        var oldCache = new MyStorageDataCache();
                        oldCache.Resize(oldStorage.Size);
                        oldStorage.ReadRange(oldCache, MyStorageDataTypeFlags.ContentAndMaterial, (int)VRageRender.MyLodTypeEnum.LOD0, Vector3I.Zero, oldStorage.Size - 1);

                        //MyAPIGateway.Utilities.ShowMessage("Test2", "check");

                        var transSize = Vector3I.Transform(oldStorage.Size, quaternion);
                        var size = Vector3I.Abs(transSize);

                        //MyAPIGateway.Utilities.ShowMessage("Test3", "check");

                        var newName = Support.CreateUniqueStorageName(originalAsteroid.StorageName);

                        var newStorage = Support.CreateNewAsteroid(newName, size, originalAsteroid.PositionLeftBottomCorner);

                        // Names are screwed around with bad.
                        //MyAPIGateway.Utilities.ShowMessage("name", string.Format("{0} {1}", newName, newStorage));

                        var cache = new MyStorageDataCache();
                        var min = Vector3I.Zero;
                        var max = size - 1;
                        cache.Resize(min, max);

                        MyAPIGateway.Utilities.ShowMessage("Test6", "check");

                        Vector3I p;
                        for (p.Z = 0; p.Z < oldStorage.Size.Z; ++p.Z)
                            for (p.Y = 0; p.Y < oldStorage.Size.Y; ++p.Y)
                                for (p.X = 0; p.X < oldStorage.Size.X; ++p.X)
                                {
                                    var content = oldCache.Content(ref p);
                                    var material = oldCache.Material(ref p);

                                    var newP = Vector3I.Transform(p, quaternion);
                                    // readjust the points, as rotation occurs arround 0,0,0.
                                    newP.X = newP.X < 0 ? newP.X - transSize.X : newP.X;
                                    newP.Y = newP.Y < 0 ? newP.Y - transSize.Y : newP.Y;
                                    newP.Z = newP.Z < 0 ? newP.Z - transSize.Z : newP.Z;

                                    cache.Content(ref newP, content);
                                    cache.Material(ref newP, material);
                                }

                        newStorage.WriteRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, min, max);
                        MyAPIGateway.Entities.RemoveEntity((IMyEntity)originalAsteroid);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
