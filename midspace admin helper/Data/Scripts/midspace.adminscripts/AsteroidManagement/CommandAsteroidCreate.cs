namespace midspace.adminscripts
{
    using System.Globalization;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using VRageMath;

    public class CommandAsteroidCreate : ChatCommand
    {
        public CommandAsteroidCreate()
            : base(ChatCommandSecurity.Admin, "createroid", new[] { "/createroid" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/createroid <X> <Y> <Z> <Size> <Name>", "Creates an empty Asteroid space at location <X,Y,Z> of cubic <Size>. The size must be multiple of 64.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/createroid\s+(?<X>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s+(?<Y>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s+(?<Z>[+-]?((\d+(\.\d*)?)|(\.\d+)))\s+(?<Size>(\d+?))\s+(?<Name>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var position = new Vector3D(
                    double.Parse(match.Groups["X"].Value, CultureInfo.InvariantCulture),
                    double.Parse(match.Groups["Y"].Value, CultureInfo.InvariantCulture),
                    double.Parse(match.Groups["Z"].Value, CultureInfo.InvariantCulture));

                var length = int.Parse(match.Groups["Size"].Value, CultureInfo.InvariantCulture);
                if (length < 1 || length % 64 != 0)
                {
                    MyAPIGateway.Utilities.ShowMessage("Invalid", "Size specified.");
                    return true;
                }

                var size = new Vector3I(length, length, length);
                var name = match.Groups["Name"].Value;

                if (position.IsValid() && ((Vector3D)size).IsValid())
                {
                    MyAPIGateway.Utilities.ShowMessage("Size", size.ToString());
                    var newName = Support.CreateUniqueStorageName(name);

                    // MyAPIGateway.Session.VoxelMaps.CreateVoxelMap will always create a square shaped asteroid.
                    // This is by design within the API itself and cannot be altered.
                    var newVoxelMap = Support.CreateNewAsteroid(newName, size, position);
                    return true;
                }
            }

            return false;
        }
    }
}
