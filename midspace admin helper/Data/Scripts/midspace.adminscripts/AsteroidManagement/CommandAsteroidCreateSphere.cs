namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRageMath;

    public class CommandAsteroidCreateSphere : ChatCommand
    {
        private bool _displayMessage;
        private DateTime _startTime;
        private string _message;
        private string _asteroidName;

        public CommandAsteroidCreateSphere()
            : base(ChatCommandSecurity.Admin, "createroidsphere", new[] { "/createroidsphere" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            if (brief)
            {
                MyAPIGateway.Utilities.ShowMessage("/createroidsphere <Name> <X> <Y> <Z> <Parts> <Material> <Diameter>", "Creates an Sphere Asteroid in <Parts> at location <X,Y,Z> with the <Material> of <Diameter>.");
            }
            else
            {
                var validMaterials = MyDefinitionManager.Static.GetVoxelMaterialDefinitions().Select(k => k.Id.SubtypeName).OrderBy(s => s).ToArray();
                var materialNames = String.Join(", ", validMaterials);

                var description = new StringBuilder();
                description.AppendFormat(@"This command is used to generate a sphere asteroid at the exact center of the specified co-ordinates, with multiple layers.
/createroidsphere <Name> <X> <Y> <Z> <Parts> <Material1> <Diameter1> <Material2> <Diameter2> <Material3> <Diameter3> ....

  <Name> - the base name of the asteroid file. A number will be added if it already exists.
  <X> <Y> <Z> - the center coordinate of where to place the asteroid.
  <Parts> - specify to break the sphere down into smaller chunks. Either 1=whole sphere, 2=hemispheres, 4 or 8 parts.
  <Material> - the material of the layer. An empty layer can be specified with 'none'. The following materials are available: {0}
  <Diameter> - the diameter of the layer.
  ... - Additional material and diameters can be specified for additional layers.

Note:
The larger the asteroid, the longer it will take to generate. More than 2000m can as much as an hour on some computers.
The flat faces on the inside of the multi part asteroids will seem to become invisible at a distance.

Examples:
  /createroidsphere sphere_solid_stone 1000 1000 1000 1 Stone_01 100

  /createroidsphere sphere_hollow_stone 2000 2000 2000 8 Stone_01 200 none 180

  /createroidsphere sphere_3_tricky_layers 3000 3000 3000 2 Stone_01 200 none 180 Stone_01 160 none 140 Stone_01 120 none 100 

  /createroidsphere sphere_layers 8000 8000 8000 2 Stone_01 200 Iron_01 180 Nickel_01 100 Cobalt_01 90 Magnesium_01 80 Silicon_01 70 

", materialNames);
                MyAPIGateway.Utilities.ShowMissionScreen("Create Asteroid Sphere:", null, " ", description.ToString(), null, "OK");
            }


            // As Asteroid volumes are cubic octrees, they are sized in 64, 128, 256, 512, 1024, 2048

            /*
            Sample calls...
            /createroidsphere test 200 200 200 1 Gold_01 200
            
            /createroidsphere sphere_solid_xx_stone_01a 2000 2000 2000 2 Stone_01 200 none 100
            /createroidsphere sphere_solid_xx_stone_01a 2000 2000 2000 2 Stone_01 200 Iron_01 180 Nickel_01 100 Cobalt_01 90 Magnesium_01 80 Silicon_01 70 Silver_01 60 Gold_01 50 Platinum_01 40 Uraninite_01 30
            /createroidsphere sphere_solid_xx_stone_01a 2000 2000 2000 4 Stone_01 200 Iron_02 190 Nickel_01 145 Cobalt_01 130 Magnesium_01 115 Silicon_01 100 Silver_01 85 Gold_01 70 Platinum_01 55 Uraninite_01 40
             
            344m, 38s.
            This call takes 58 seconds, for a 344 diameter sphere, with no freeze.
            /createroidsphere 200 200 200 344 0 1 Nickel_01 test 
             
            http://steamcommunity.com/sharedfiles/filedetails/?id=399791753
             
            This call takes, with a frozen game, and wont work if floating items occupy space.
            344m, 13s.
            400m, 20s.
            500m, 40s.
            600m, 1:06s.
            1000m, 4:53s.
            2000m, 40-50min.
            2700m, 1:36:50s.
            */
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (MyAPIGateway.Session.OnlineMode != MyOnlineModeEnum.OFFLINE)
            {
                MyAPIGateway.Utilities.ShowMessage("Warning", "This command must be carried out in an 'Offline' game, as it is too processor intensive to run in a multiplayer game.");
                return true;
            }

            var match = Regex.Match(messageText, @"/createroidsphere\s+(?<Name>[^\s]+)\s+(?<X>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s+(?<Y>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s+(?<Z>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s+(?<Grid>(\d+?))(?:\s+(?<Material>[^\s]+)\s+(?<Diameter>[+-]?((\d+(\.\d*)?)|(\.\d+))))+", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                StringBuilder description;

                var position = new Vector3D(
                    double.Parse(match.Groups["X"].Value, CultureInfo.InvariantCulture),
                    double.Parse(match.Groups["Y"].Value, CultureInfo.InvariantCulture),
                    double.Parse(match.Groups["Z"].Value, CultureInfo.InvariantCulture));

                var grid = int.Parse(match.Groups["Grid"].Value, CultureInfo.InvariantCulture);
                if (grid <= 1)
                    grid = 1;
                else if (grid <= 2)
                    grid = 2;
                else if (grid <= 4)
                    grid = 4;
                else
                    grid = 8;

                var layers = new List<AsteroidSphereLayer>();
                double maxDiameter = 0;

                for (var i = 0; i < match.Groups["Material"].Captures.Count; i++)
                {
                    byte material = 0;
                    var checkName = match.Groups["Material"].Captures[i].Value;
                    var materialName = checkName;

                    if (checkName.Equals("none", StringComparison.InvariantCultureIgnoreCase))
                    {
                        material = 255;
                    }
                    else
                    {
                        MyVoxelMaterialDefinition materialDef;
                        string suggestedMaterials = "";
                        if (!Support.FindMaterial(checkName, out materialDef, ref suggestedMaterials))
                        {
                            MyAPIGateway.Utilities.ShowMessage("Invalid Material1 specified.", "Cannot find the material '{0}'.\r\nTry the following: {1}", checkName, suggestedMaterials);
                            return true;
                        }

                        materialName = materialDef.Id.SubtypeName;
                        material = materialDef.Index;
                    }

                    var diameter = double.Parse(match.Groups["Diameter"].Captures[i].Value, CultureInfo.InvariantCulture);
                    if (diameter < 4 || diameter > 5000)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Invalid", "Diamater specified. Between 4 and 5000");
                        return true;
                    }

                    maxDiameter = Math.Max(maxDiameter, diameter);
                    layers.Add(new AsteroidSphereLayer() { Diameter = diameter, Material = material, MaterialName = materialName });
                }

                var length = (int)(maxDiameter + 4).RoundUpToNearest(64);
                var size = new Vector3I(length, length, length);
                var name = match.Groups["Name"].Value;

                if (position.IsValid() && ((Vector3D)size).IsValid())
                {
                    var boundingSphere = new BoundingSphereD(position, (maxDiameter + 2) / 2);
                    var floatingList = MyAPIGateway.Entities.GetEntitiesInSphere(ref boundingSphere);
                    floatingList = floatingList.Where(e =>
                        (e is Sandbox.ModAPI.IMyCubeGrid && ((Sandbox.ModAPI.IMyCubeGrid)e).IsStatic == false)
                        || (e is Sandbox.ModAPI.IMyCubeBlock && ((Sandbox.ModAPI.IMyCubeGrid)((Sandbox.ModAPI.IMyCubeBlock)e).Parent).IsStatic == false)
                        || (e is Sandbox.ModAPI.IMyCharacter)).ToList();

                    if (floatingList.Count > 0)
                    {
                        description = new StringBuilder();
                        description.AppendFormat(@"{0} items were found floating in the specified location.
The asteroid cannot be generated until this area is cleared of ships and players.", floatingList.Count);
                        MyAPIGateway.Utilities.ShowMissionScreen("Cannot generate Asteroid:", null, " ", description.ToString(), null, "OK");
                        return true;
                    }

                    var origin = new Vector3I(size.X / 2, size.Y / 2, size.Z / 2);
                    _startTime = DateTime.Now;

                    layers = layers.OrderByDescending(e => e.Diameter).ToList();

                    switch (grid)
                    {
                        case 1:
                            ProcessAsteroid(name, size, position, Vector3D.Zero, origin, layers);
                            break;
                        case 2:
                            ProcessAsteroid(name, size, position, new Vector3D(origin.X - 2, 0, 0), origin, layers);
                            ProcessAsteroid(name, size, position, new Vector3D(-origin.X + 2, 0, 0), origin, layers);
                            break;
                        case 4:
                            ProcessAsteroid(name, size, position, new Vector3D(origin.X - 2, origin.Y - 2, 0), origin, layers);
                            ProcessAsteroid(name, size, position, new Vector3D(-origin.X + 2, origin.Y - 2, 0), origin, layers);
                            ProcessAsteroid(name, size, position, new Vector3D(origin.X - 2, -origin.Y + 2, 0), origin, layers);
                            ProcessAsteroid(name, size, position, new Vector3D(-origin.X + 2, -origin.Y + 2, 0), origin, layers);
                            break;
                        case 8:
                            // downsize the Asteroid store.
                            length = (int)((maxDiameter / 2) + 4).RoundUpToNearest(64);
                            size = new Vector3I(length, length, length);
                            origin = new Vector3I(size.X / 2, size.Y / 2, size.Z / 2);
                            ProcessAsteroid(name, size, position, new Vector3D(origin.X - 2, origin.Y - 2, origin.Z - 2), origin, layers);
                            ProcessAsteroid(name, size, position, new Vector3D(-origin.X + 2, origin.Y - 2, origin.Z - 2), origin, layers);
                            ProcessAsteroid(name, size, position, new Vector3D(origin.X - 2, -origin.Y + 2, origin.Z - 2), origin, layers);
                            ProcessAsteroid(name, size, position, new Vector3D(-origin.X + 2, -origin.Y + 2, origin.Z - 2), origin, layers);
                            ProcessAsteroid(name, size, position, new Vector3D(origin.X - 2, origin.Y - 2, -origin.Z + 2), origin, layers);
                            ProcessAsteroid(name, size, position, new Vector3D(-origin.X + 2, origin.Y - 2, -origin.Z + 2), origin, layers);
                            ProcessAsteroid(name, size, position, new Vector3D(origin.X - 2, -origin.Y + 2, -origin.Z + 2), origin, layers);
                            ProcessAsteroid(name, size, position, new Vector3D(-origin.X + 2, -origin.Y + 2, -origin.Z + 2), origin, layers);
                            break;
                    }

                    var end = DateTime.Now;
                    description = new StringBuilder();
                    description.AppendFormat("{0} asteroids generated.\r\n\r\nLayers: {1}\r\n", grid, layers.Count);
                    foreach (var layer in layers)
                    {
                        description.AppendFormat("{0}: {1}m\r\n", layer.MaterialName, layer.Diameter);
                    }

                    _asteroidName = name;
                    _message = description.ToString();
                    _displayMessage = true;
                    return true;
                }
            }

            Help(steamId, true);
            return true;
        }

        private string ProcessAsteroid(string asteroidName, Vector3I size, Vector3D position, Vector3D offset, Vector3I origin, List<AsteroidSphereLayer> layers)
        {
            var storeName = Support.CreateUniqueStorageName(asteroidName);
            var storage = MyAPIGateway.Session.VoxelMaps.CreateStorage(size);
            var voxelMap = MyAPIGateway.Session.VoxelMaps.CreateVoxelMap(storeName, storage, position - (Vector3D)origin - offset, 0);
            
            if (layers.Count > 0)
                voxelMap.Storage.OverwriteAllMaterials(layers[0].Material);

            bool isEmpty = true;

            foreach (var layer in layers)
            {
                var radius = (float)(layer.Diameter - 2) / 2f;
                IMyVoxelShapeSphere sphereShape = MyAPIGateway.Session.VoxelMaps.GetSphereVoxelHand();
                sphereShape.Center = position;
                sphereShape.Radius = radius;

                if (layer.Material == 255)
                {
                    MyAPIGateway.Session.VoxelMaps.CutOutShape(voxelMap, sphereShape);
                    isEmpty = true;
                }
                else if (isEmpty)
                {
                    MyAPIGateway.Session.VoxelMaps.FillInShape(voxelMap, sphereShape, layer.Material);
                    isEmpty = false;
                }
                else
                {
                    MyAPIGateway.Session.VoxelMaps.PaintInShape(voxelMap, sphereShape, layer.Material);
                }
            }

            return storeName;
        }

        class AsteroidSphereLayer
        {
            public int Index { get; set; }
            public string MaterialName { get; set; }
            public byte Material { get; set; }
            public double Diameter { get; set; }
        }

        public override void UpdateBeforeSimulation100()
        {
            // TODO: extra loop to make sure 100ms has past.
            if (_displayMessage)
            {
                // Process the end message several frames later, as asteroid process can be instantanious, but the game engine takes MUCH longer to create the asteroid and render it. 
                _displayMessage = false;
                var timeProcessing = DateTime.Now - _startTime;
                MyAPIGateway.Utilities.ShowMissionScreen("Asteroid generated:", _asteroidName, " ", string.Format("Time taken: {0}\r\n", timeProcessing) + _message, null, "OK");
            }
        }
    }
}
