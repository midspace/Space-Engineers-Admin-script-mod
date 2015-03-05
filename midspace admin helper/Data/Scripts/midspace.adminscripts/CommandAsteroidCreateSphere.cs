namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using VRageMath;
    using VRage.Common.Voxels;
    using Sandbox.ModAPI.Interfaces;

    public class CommandAsteroidCreateSphere : ChatCommand
    {
        //private Queue<Action> _workQueue = new Queue<Action>();

        public CommandAsteroidCreateSphere()
            : base(ChatCommandSecurity.Experimental, "createroidsphere", new[] { "/createroidsphere" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/createroidsphere <X> <Y> <Z> <Diameter> <Shell> <Material> <Name>", "Creates an Sphere Asteroid space at location <X,Y,Z> of <Diameter>, with a <Shell> thickness. Specify a shell of 0 for no shell.");


            // As Asteroid volumes are cubic octrees, they are sized in 64, 128, 256, 512, 1024, 2048

            // Sample calls...
            //  /createroidsphere 200 200 200 50 0 Gold_01 test
            //  /createroidsphere 200 200 200 500 0 Uranium_01 test
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/createroidsphere\s+(?<X>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s+(?<Y>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s+(?<Z>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s+(?<Diameter>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s+(?<Shell>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s+(?<Material>.+)\s+(?<Name>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var position = new Vector3D(
                    double.Parse(match.Groups["X"].Value, CultureInfo.InvariantCulture),
                    double.Parse(match.Groups["Y"].Value, CultureInfo.InvariantCulture),
                    double.Parse(match.Groups["Z"].Value, CultureInfo.InvariantCulture));

                var diameter = double.Parse(match.Groups["Diameter"].Value, CultureInfo.InvariantCulture);
                if (diameter < 3 || diameter > 5000)
                {
                    MyAPIGateway.Utilities.ShowMessage("Invalid", "Diamater specified. Between 3 and 5000");
                    return true;
                }

                var shellWidth = double.Parse(match.Groups["Shell"].Value, CultureInfo.InvariantCulture);
                if (shellWidth > diameter / 2)
                {
                    MyAPIGateway.Utilities.ShowMessage("Invalid", "Shell specified. Must be less than radius.");
                    return true;
                }

                var checkName = match.Groups["Material"].Value;
                var validMaterials = MyDefinitionManager.Static.GetVoxelMaterialDefinitions().Where(k => k.Id.SubtypeName.IndexOf(checkName, StringComparison.InvariantCultureIgnoreCase) >= 0).Select(k => k.Id.SubtypeName).ToArray();
                if (validMaterials.Length == 0)
                {
                    validMaterials = MyDefinitionManager.Static.GetVoxelMaterialDefinitions().Select(k => k.Id.SubtypeName).ToArray();
                    var materialNames = String.Join(", ", validMaterials);
                    MyAPIGateway.Utilities.ShowMessage("Invalid", "Material specified. Cannot find the material.\r\nTry the following: {0}", materialNames);
                    return true;
                }
                if (validMaterials.Length > 1)
                {
                    var materialNames = String.Join(", ", validMaterials);
                    MyAPIGateway.Utilities.ShowMessage("Invalid", "Material specified. Did you mean {0} ?", materialNames);
                    return true;
                }
                var material = MyDefinitionManager.Static.GetVoxelMaterialDefinition(validMaterials[0]).Index;

                var length = (int)(diameter + 4).RoundUpToNearest(64);
                var size = new Vector3I(length, length, length);
                var name = match.Groups["Name"].Value;

                if (Vector3D.IsValid(position) && Vector3D.IsValid(size))
                {
                    var origin = new Vector3I(size.X / 2, size.Y / 2, size.Z / 2);
                    var asteroidName = Support.CreateUniqueStorageName(name);

                    // Cannot be processed on seperate thread.
                    var voxelMap = Support.CreateNewAsteroid(asteroidName, size, position - (Vector3D)origin);
                    voxelMap.Physics.Enabled = false;

                    MyAPIGateway.Parallel.StartBackground(delegate()
                    {
                        ProcessAsteroid(voxelMap.Storage, position, origin, diameter, shellWidth, material);
                    },
                    delegate()
                    {
                        voxelMap.Physics.Enabled = true;
                    });

                    return true;
                }
            }

            return false;
        }

        private void ProcessAsteroid(IMyStorage storage, Vector3D position, Vector3I origin, double diamater, double shellWidth, byte material)
        {
            var partCount = Math.Pow(storage.Size.X / 64, 3);
            var partCounter = 0;
            Vector3I block;
            var radius = diamater / 2;
            var hollow = shellWidth > 0.5f;

            MyAPIGateway.Utilities.ShowMessage("Asteroid", "Generating {0}m diameter sphere.", diamater);
            //_workQueue.Enqueue(delegate() { MyAPIGateway.Utilities.ShowMessage("Asteroid", "Generating {0}m diameter sphere.", diamater); });

            // read the asteroid in chunks of 64 to avoid the Arithmetic overflow issue.
            for (block.Z = 0; block.Z < storage.Size.Z; block.Z += 64)
                for (block.Y = 0; block.Y < storage.Size.Y; block.Y += 64)
                    for (block.X = 0; block.X < storage.Size.X; block.X += 64)
                    {
                        //MyAPIGateway.Utilities.ShowMessage("Asteroid", "Generating {0}m diameter sphere. {1}/{2}", diamater, partCount, ++partCounter);
                        var message = string.Format("Generating part {0}/{1}", ++partCounter, partCount);
                        MyAPIGateway.Utilities.ShowMessage("Asteroid", message);
                        //_workQueue.Enqueue(delegate() { MyAPIGateway.Utilities.ShowMessage("Asteroid", message); });

                        ProcessVolume(storage, block, origin, radius, hollow, shellWidth, material);
                    }

            //_workQueue.Enqueue(delegate() { MyAPIGateway.Utilities.ShowMessage("Asteroid", "Sphere completed generation."); });
            MyAPIGateway.Utilities.ShowMessage("Asteroid", "{0}m diameter sphere completed generation.", diamater);
        }

        private void ProcessVolume(IMyStorage storage, Vector3I block, Vector3I origin, double radius, bool hollow, double shellWidth, byte material)
        {
            var cacheSize = new Vector3I(64);
            var cache = new MyStorageDataCache();
            cache.Resize(cacheSize);

            Vector3I p;
            for (p.Z = 0; p.Z < cache.Size3D.Z; ++p.Z)
                for (p.Y = 0; p.Y < cache.Size3D.Y; ++p.Y)
                    for (p.X = 0; p.X < cache.Size3D.X; ++p.X)
                    {
                        var coord = p + block;
                        byte volumne = 0;

                        var dist =
                            Math.Sqrt(Math.Abs(Math.Pow(coord.X - origin.X, 2)) +
                            Math.Abs(Math.Pow(coord.Y - origin.Y, 2)) +
                            Math.Abs(Math.Pow(coord.Z - origin.Z, 2)));

                        if (dist >= radius)
                        {
                            volumne = 0x00;
                        }
                        else if (dist > radius - 1)
                        {
                            volumne = (byte)((radius - dist) * 255);
                        }
                        else if (hollow && (radius - shellWidth) < dist)
                        {
                            volumne = 0xFF;
                        }
                        else if (hollow && (radius - shellWidth - 1) < dist)
                        {
                            volumne = (byte)((1 - ((radius - shellWidth) - dist)) * 255);
                        }
                        else if (hollow)
                        {
                            volumne = 0x00;
                        }
                        else //if (!hollow)
                        {
                            volumne = 0xFF;
                        }

                        cache.Set(MyStorageDataTypeEnum.Content, ref p, volumne);
                        cache.Set(MyStorageDataTypeEnum.Material, ref p, material);
                    }

            storage.WriteRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, block, block + cacheSize - 1);
        }

        //public override void UpdateBeforeSimulation()
        //{
        //    if (_workQueue.Count > 0)
        //    {
        //        var action = _workQueue.Dequeue();
        //        action.Invoke();
        //    }
        //}
    }
}
