namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using VRage.ModAPI;
    using VRage.Voxels;
    using VRageMath;

    /// <summary>
    /// This worked by creating a new asteroid store with the new dimentions required (as the entire space is rotated).
    /// </summary>
    public class CommandAsteroidRotate : ChatCommand
    {
        public CommandAsteroidRotate()
            : base(ChatCommandSecurity.Admin, "rotateroid", new[] { "/rotateroid" })
        {
        }

        public override void Help(bool brief)
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

                    var currentAsteroidList = new List<IMyVoxelBase>();
                    IMyVoxelBase originalAsteroid = null;
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
                    if (searchName.Substring(0, 1) == "#" && Int32.TryParse(searchName.Substring(1), out index) && index > 0 && index <= CommandAsteroidsList.AsteroidCache.Count)
                    {
                        originalAsteroid = CommandAsteroidsList.AsteroidCache[index - 1];
                    }

                    if (originalAsteroid == null)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Cannot find asteroid", string.Format("'{0}'", searchName));
                        return true;
                    }

                    var quaternion = Quaternion.CreateFromYawPitchRoll(rotateVector.X / (180 / MathHelper.Pi), rotateVector.Y / (180 / MathHelper.Pi), rotateVector.Z / (180 / MathHelper.Pi));
                    var oldStorage = originalAsteroid.Storage;

                    var oldCache = new MyStorageDataCache();
                    oldCache.Resize(oldStorage.Size);
                    oldStorage.ReadRange(oldCache, MyStorageDataTypeFlags.ContentAndMaterial, 0, Vector3I.Zero, oldStorage.Size - 1);

                    var transSize = Vector3I.Transform(oldStorage.Size, quaternion);
                    var newSize = Vector3I.Abs(transSize);
                    var newName = Support.CreateUniqueStorageName(originalAsteroid.StorageName);
                    var newVoxelMap = Support.CreateNewAsteroid(newName, newSize, originalAsteroid.PositionLeftBottomCorner);

                    var cache = new MyStorageDataCache();
                    var min = Vector3I.Zero;
                    var max = newSize - 1;
                    cache.Resize(min, max);

                    Vector3I p;
                    for (p.Z = 0; p.Z < oldStorage.Size.Z; ++p.Z)
                        for (p.Y = 0; p.Y < oldStorage.Size.Y; ++p.Y)
                            for (p.X = 0; p.X < oldStorage.Size.X; ++p.X)
                            {
                                var content = oldCache.Content(ref p);
                                var material = oldCache.Material(ref p);

                                var newP = Vector3I.Transform(p, quaternion);
                                // readjust the points, as rotation occurs arround 0,0,0.
                                newP.X = newP.X < 0 ? newP.X - transSize.X - 1 : newP.X;
                                newP.Y = newP.Y < 0 ? newP.Y - transSize.Y - 1 : newP.Y;
                                newP.Z = newP.Z < 0 ? newP.Z - transSize.Z - 1 : newP.Z;

                                cache.Content(ref newP, content);
                                cache.Material(ref newP, material);
                            }

                    newVoxelMap.Storage.WriteRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, min, max);
                    MyAPIGateway.Entities.RemoveEntity((IMyEntity)originalAsteroid);

                    // Invalidate the cache, to force user to select again to prevent possible corruption by using an old cache.
                    CommandAsteroidsList.AsteroidCache.Clear();
                    return true;
                }
            }

            return false;
        }
    }
}
