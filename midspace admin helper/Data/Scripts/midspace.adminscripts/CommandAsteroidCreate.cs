namespace midspace.adminscripts
{
    using System;
    using System.Globalization;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using VRageMath;

    public class CommandAsteroidCreate : ChatCommand
    {
        public CommandAsteroidCreate()
            : base(ChatCommandSecurity.Admin, "createasteroid", new[] { "/createasteroid" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/createasteroid <X> <Y> <Z> <Sx> <Sy> <Sz> <Name>", "Creates an empty Asteroid space at location <X,Y,Z> of size <Sx,Sy,Sz>. The size must be multiple of 64.");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/createasteroid\s{1,}(?<X>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s{1,}(?<Y>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s{1,}(?<Z>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s{1,}(?<SX>(\d+?))\s{1,}(?<SY>(\d+?))\s{1,}(?<SZ>(\d+?))\s{1,}(?<Name>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var position = new Vector3D(
                    double.Parse(match.Groups["X"].Value, CultureInfo.InvariantCulture),
                    double.Parse(match.Groups["Y"].Value, CultureInfo.InvariantCulture),
                    double.Parse(match.Groups["Z"].Value, CultureInfo.InvariantCulture));

                var size = new Vector3I(
                    int.Parse(match.Groups["SX"].Value, CultureInfo.InvariantCulture),
                    int.Parse(match.Groups["SY"].Value, CultureInfo.InvariantCulture),
                    int.Parse(match.Groups["SZ"].Value, CultureInfo.InvariantCulture));

                var name = match.Groups["Name"].Value;

                if (Vector3D.IsValid(position) && Vector3D.IsValid(size))
                {

                    if (size.X < 1 || size.Y < 1 || size.Z < 1 ||
                        size.X % 64 != 0 || size.Y % 64 != 0 || size.Z % 64 != 0)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Invalid", "Size specified.");
                        return true;
                    }

                    MyAPIGateway.Utilities.ShowMessage("Size", size.ToString());

                    var newName = Support.CreateUniqueStorageName(name);

                    // It appears that MyAPIGateway.Session.VoxelMaps.CreateVoxelMap will always create a square shaped asteroid.
                    // Need to confirm with KeenSWH if this is a bug or by design. The interface can be simplified if this is by design.
                    var newVoxelMap = Support.CreateNewAsteroid(newName, size, position);
                    return true;
                }
            }

            return false;
        }
    }
}
