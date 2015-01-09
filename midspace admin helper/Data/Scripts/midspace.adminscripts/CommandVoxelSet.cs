namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;

    using Sandbox.Common;
    using Sandbox.ModAPI;
    using VRageMath;
    using VRage.Common.Voxels;

    public class CommandVoxelSet: ChatCommand
    {
        public static bool ActiveVoxelSetter { get; private set; }
        public static Vector3? ActiveVoxelSetterPosition { get; set; }
        private static Vector3? _activeVoxelSetterPositionA;
        private static Vector3? _activeVoxelSetterPositionB;
        private static string _activeVoxelSetterNameA;
        private static string _activeVoxelSetterNameB;

        public CommandVoxelSet()
            : base(ChatCommandSecurity.Admin, "voxelset", new[] { "/voxelset" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/voxelset [on|off|A|B|Clear|Fill]", "Voxel editing. Will clear/fill blocks of voxel cells. Select hand drill. [on] to turn on. [A] to set point A. [B]. to set point B. [C] to clear between points A and B. [F] to fill between points A and B. [off] to turn off.");
        }

        public override bool Invoke(string messageText)
        {
            // voxelset [0/off] [1/on] [A] [B] [C/Clear]
            if (messageText.StartsWith("/voxelset ", StringComparison.InvariantCultureIgnoreCase))
            {
                var strings = messageText.Split(' ');
                if (strings.Length > 1)
                {
                    if (strings[1].Equals("off", StringComparison.InvariantCultureIgnoreCase) || strings[1].Equals("0", StringComparison.InvariantCultureIgnoreCase))
                    {
                        ActiveVoxelSetterPosition = null;
                        _activeVoxelSetterPositionA = null;
                        _activeVoxelSetterPositionB = null;
                        _activeVoxelSetterNameA = null;
                        _activeVoxelSetterNameB = null;
                        ActiveVoxelSetter = false;
                        MyAPIGateway.Utilities.ShowNotification("Voxel setter off", 1000, MyFontEnum.Green);
                        return true;
                    }
                    if (strings[1].Equals("on", StringComparison.InvariantCultureIgnoreCase) || strings[1].Equals("1", StringComparison.InvariantCultureIgnoreCase))
                    {
                        ActiveVoxelSetterPosition = null;
                        _activeVoxelSetterPositionA = null;
                        _activeVoxelSetterPositionB = null;
                        _activeVoxelSetterNameA = null;
                        _activeVoxelSetterNameB = null;
                        ActiveVoxelSetter = true;
                        MyAPIGateway.Utilities.ShowNotification("Voxel setter activated", 1000, MyFontEnum.Green);
                        return true;
                    }
                    if (!ActiveVoxelSetter)
                    {
                        MyAPIGateway.Utilities.ShowNotification("Voxel setter hasn't been actived.", 2000, MyFontEnum.Red);
                        return true;
                    }
                    if (strings[1].Equals("A", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var tmp = ActiveVoxelSetterPosition;
                        if (tmp.HasValue)
                        {
                            var currentAsteroidList = new List<IMyVoxelMap>();
                            var bb = new BoundingBoxD(tmp.Value - 0.2f, tmp.Value + 0.2f);
                            MyAPIGateway.Session.VoxelMaps.GetInstances(currentAsteroidList, v => v.IsBoxIntersectingBoundingBoxOfThisVoxelMap(ref bb));

                            if (currentAsteroidList.Count > 0)
                            {
                                _activeVoxelSetterNameA = currentAsteroidList[0].StorageName;
                                _activeVoxelSetterPositionA = tmp;
                            }
                        }

                        if (_activeVoxelSetterPositionA.HasValue)
                            MyAPIGateway.Utilities.ShowMessage(_activeVoxelSetterNameA, "Voxel setter point A: active");
                        //MyAPIGateway.Utilities.ShowNotification("Voxel setter A: active.", 1000, MyFontEnum.Green);
                        else
                            MyAPIGateway.Utilities.ShowNotification("Voxel setter A: invalid.", 1000, MyFontEnum.Red);
                        return true;
                    }
                    if (strings[1].Equals("B", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var tmp = ActiveVoxelSetterPosition;
                        if (tmp.HasValue)
                        {
                            var currentAsteroidList = new List<IMyVoxelMap>();
                            var bb = new BoundingBoxD(tmp.Value - 0.2f, tmp.Value + 0.2f);
                            MyAPIGateway.Session.VoxelMaps.GetInstances(currentAsteroidList, v => v.IsBoxIntersectingBoundingBoxOfThisVoxelMap(ref bb));

                            if (currentAsteroidList.Count > 0)
                            {
                                _activeVoxelSetterNameB = currentAsteroidList[0].StorageName;
                                _activeVoxelSetterPositionB = tmp;
                            }
                        }

                        if (_activeVoxelSetterPositionB.HasValue)
                            MyAPIGateway.Utilities.ShowMessage(_activeVoxelSetterNameB, "Voxel setter point B: active");
                        //MyAPIGateway.Utilities.ShowNotification("Voxel setter B: active.", 1000, MyFontEnum.Green);
                        else
                            MyAPIGateway.Utilities.ShowNotification("Voxel setter B: invalid.", 1000, MyFontEnum.Red);
                        return true;
                    }
                    if (strings[1].Equals("C", StringComparison.InvariantCultureIgnoreCase) || strings[1].Equals("Clear", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (_activeVoxelSetterPositionA.HasValue && _activeVoxelSetterPositionB.HasValue && _activeVoxelSetterNameA == _activeVoxelSetterNameB)
                        {
                            var currentAsteroidList = new List<IMyVoxelMap>();
                            MyAPIGateway.Session.VoxelMaps.GetInstances(currentAsteroidList, v => v.StorageName.Equals(_activeVoxelSetterNameA, StringComparison.InvariantCultureIgnoreCase));
                            if (currentAsteroidList.Count > 0)
                            {
                                var storage = currentAsteroidList[0].Storage;
                                var point1 = new Vector3I(_activeVoxelSetterPositionA.Value - currentAsteroidList[0].PositionLeftBottomCorner);
                                var point2 = new Vector3I(_activeVoxelSetterPositionB.Value - currentAsteroidList[0].PositionLeftBottomCorner);

                                var cache = new MyStorageDataCache();
                                var size = storage.Size;
                                cache.Resize(size);
                                storage.ReadRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, (int)VRageRender.MyLodTypeEnum.LOD0, Vector3I.Zero, size - 1);

                                var min = Vector3I.Min(point1, point2);
                                var max = Vector3I.Max(point1, point2);

                                MyAPIGateway.Utilities.ShowMessage("Cutting", min.ToString() + " " + max.ToString());
                                //MyAPIGateway.Utilities.ShowNotification("cutting:" + min.ToString() + " " + max.ToString(), 5000, MyFontEnum.Blue);

                                Vector3I p;
                                for (p.Z = min.Z; p.Z <= max.Z; ++p.Z)
                                    for (p.Y = min.Y; p.Y <= max.Y; ++p.Y)
                                        for (p.X = min.X; p.X <= max.X; ++p.X)
                                            cache.Content(ref p, 0);
                                storage.WriteRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, Vector3I.Zero, size - 1);
                            }
                        }
                        else if (_activeVoxelSetterNameA != _activeVoxelSetterNameB)
                        {
                            MyAPIGateway.Utilities.ShowNotification("Voxel setter Cut: different asteroids.", 2000, MyFontEnum.Red);
                        }
                        else
                        {
                            MyAPIGateway.Utilities.ShowNotification("Voxel setter Cut: invalid points.", 2000, MyFontEnum.Red);
                        }
                        return true;
                    }
                    if (strings[1].Equals("F", StringComparison.InvariantCultureIgnoreCase) || strings[1].Equals("Fill", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (_activeVoxelSetterPositionA.HasValue && _activeVoxelSetterPositionB.HasValue && _activeVoxelSetterNameA == _activeVoxelSetterNameB)
                        {
                            var currentAsteroidList = new List<IMyVoxelMap>();
                            MyAPIGateway.Session.VoxelMaps.GetInstances(currentAsteroidList, v => v.StorageName.Equals(_activeVoxelSetterNameA, StringComparison.InvariantCultureIgnoreCase));
                            if (currentAsteroidList.Count > 0)
                            {
                                var storage = currentAsteroidList[0].Storage;
                                var point1 = new Vector3I(_activeVoxelSetterPositionA.Value - currentAsteroidList[0].PositionLeftBottomCorner);
                                var point2 = new Vector3I(_activeVoxelSetterPositionB.Value - currentAsteroidList[0].PositionLeftBottomCorner);

                                var cache = new MyStorageDataCache();
                                var size = storage.Size;
                                cache.Resize(size);
                                storage.ReadRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, (int)VRageRender.MyLodTypeEnum.LOD0, Vector3I.Zero, size - 1);

                                var min = Vector3I.Min(point1, point2);
                                var max = Vector3I.Max(point1, point2);

                                MyAPIGateway.Utilities.ShowMessage("Filling", min.ToString() + " " + max.ToString());
                                //MyAPIGateway.Utilities.ShowNotification("filling:" + min.ToString() + " " + max.ToString(), 5000, MyFontEnum.Blue);

                                Vector3I p;
                                for (p.Z = min.Z; p.Z <= max.Z; ++p.Z)
                                    for (p.Y = min.Y; p.Y <= max.Y; ++p.Y)
                                        for (p.X = min.X; p.X <= max.X; ++p.X)
                                            cache.Content(ref p, 0xff);
                                storage.WriteRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, Vector3I.Zero, size - 1);
                            }
                        }
                        else if (_activeVoxelSetterNameA != _activeVoxelSetterNameB)
                        {
                            MyAPIGateway.Utilities.ShowNotification("Voxel setter Cut: different asteroids.", 2000, MyFontEnum.Red);
                        }
                        else
                        {
                            MyAPIGateway.Utilities.ShowNotification("Voxel setter Cut: invalid points.", 2000, MyFontEnum.Red);
                        }
                        return true;
                    }
                }
            } 

            return false;
        }
    }
}
