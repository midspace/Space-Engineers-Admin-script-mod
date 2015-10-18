using System;
using System.Text;
using System.Text.RegularExpressions;
using midspace.adminscripts.Messages.Protection;
using Sandbox.ModAPI;
using VRageMath;

namespace midspace.adminscripts.Protection.Commands
{
    public class CommandProtectionArea : ChatCommand
    {
        public CommandProtectionArea()
            : base(
                ChatCommandSecurity.Admin, "protectionarea", new string[] { "/protectionarea", "/pa" }) { }

        public override void Help(bool brief)
        {
            if (brief)
            {
                MyAPIGateway.Utilities.ShowMessage("/protectionarea <action> [options]", "Can add, remove, modify and list protection areas");
                return;
            }

            StringBuilder builder = new StringBuilder();
            builder.Append(@"NOTE: This feature is considered as work in progress. We won't be responsible for any damage that might occur even if a ship is inside a protection area.

This command is used to administrate protection areas. They can be added, removed and listed. Inside of protection areas grids cannot be damaged and only be modified by players who own one or more blocks on the ship.

/protectionarea add <name> <x> <y> <z> <size> <shape>
Creates a protection area at the given coordinates with the given size and shape. The name is a unique identifier, so choose it wisely!
Example: /pa add Safezone 0 0 0 5000 sphere
-> Creates a sphere with the radius 5000 at the position X: 0, Y: 0, Z: 0. Inside the sphere nothing can be destroyed.

Shapes:
- cube, cubic
- sphere, spherical

/protectionarea remove <name>
Removes the protection area with the given name.
Example: /pa remove Safezone
-> Removes the protection area named Safezone if it exists.

/protectionarea list
Lists all protection areas.

Alias: /pa
We know that '/protectionarea' is a bit long. Just use '/pa' instead and be happy!
");
            MyAPIGateway.Utilities.ShowMissionScreen(Name, "/protectionarea <action> [options]", null, builder.ToString());
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/(pa|protectionarea)\s+(?<CommandParts>.*)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var commandParts = match.Groups["CommandParts"].Value.Split(' ');
                if (commandParts.Length < 1)
                {
                    MyAPIGateway.Utilities.ShowMessage("ProtectionArea", "Not enough parameters.");
                    Help(true);
                    return true;
                }

                var action = commandParts[0];

                switch (action.ToLowerInvariant())
                {
                    case "add":
                    {
                        if (commandParts.Length == 7)
                        {
                            string name = commandParts[1];
                            string xS = commandParts[2];
                            string yS = commandParts[3];
                            string zS = commandParts[4];
                            string sizeS = commandParts[5];
                            string shapeS = commandParts[6];

                            double x, y, z, size;
                            ProtectionAreaShape shape;

                            if (!double.TryParse(xS, out x))
                            {
                                MyAPIGateway.Utilities.ShowMessage("ProtectionArea", "Cannot parse x.");
                                return true;
                            }

                            if (!double.TryParse(yS, out y))
                            {
                                MyAPIGateway.Utilities.ShowMessage("ProtectionArea", "Cannot parse y.");
                                return true;
                            }

                            if (!double.TryParse(zS, out z))
                            {
                                MyAPIGateway.Utilities.ShowMessage("ProtectionArea", "Cannot parse z.");
                                return true;
                            }

                            if (!double.TryParse(sizeS, out size))
                            {
                                MyAPIGateway.Utilities.ShowMessage("ProtectionArea", "Cannot parse size.");
                                return true;
                            }

                            if (!TryParseShape(shapeS, out shape))
                            {
                                MyAPIGateway.Utilities.ShowMessage("ProtectionArea",
                                    "Cannot parse shape. Shapes: cube, cubic, sphere, spherical");
                                // TODO display help
                                return true;
                            }

                            ProtectionArea area = new ProtectionArea(name, new Vector3D(x, y, z), size, shape);
                            var message = new MessageProtectionArea()
                            {
                                ProtectionArea = area,
                                Type = ProtectionAreaMessageType.Add
                            };
                            ConnectionHelper.SendMessageToServer(message);
                            return true;
                        }

                        MyAPIGateway.Utilities.ShowMessage("ProtectionArea",
                            "Wrong parameters. /protectionarea add <name> <x> <y> <z> <size> <shape>");
                        break;
                    }
                    case "remove":
                    {
                        if (commandParts.Length == 2)
                        {
                            string name = commandParts[1];
                            ProtectionArea area = new ProtectionArea(name, new Vector3D(), 0, ProtectionAreaShape.Cube);
                            var message = new MessageProtectionArea()
                            {
                                ProtectionArea = area,
                                Type = ProtectionAreaMessageType.Remove
                            };
                            ConnectionHelper.SendMessageToServer(message);
                            return true;
                        }

                        MyAPIGateway.Utilities.ShowMessage("ProtectionArea",
                            "Wrong parameters. /protectionarea remove <name>");
                        break;
                    }
                    case "list":
                        if (ProtectionHandler.Config == null || ProtectionHandler.Config.Areas == null)
                        {
                            MyAPIGateway.Utilities.ShowMessage("ProtectionArea",
                                "Areas not loaded yet. Please try again later.");
                            ConnectionHelper.SendMessageToServer(new MessageSyncProtection());
                            return true;
                        }

                        StringBuilder areaList = new StringBuilder();
                        int index = 1;

                        foreach (ProtectionArea protectionArea in ProtectionHandler.Config.Areas)
                        {
                            areaList.AppendLine(String.Format("#{0}, {1}:", index++, protectionArea.Name));
                            areaList.AppendLine(String.Format("X: {0:#0.0##}, Y: {1:#0.0##}, Z: {2:#0.0##}",
                                protectionArea.Center.X, protectionArea.Center.Y, protectionArea.Center.Z));
                            areaList.AppendLine(String.Format("Size: {0}, Shape: {1}", protectionArea.Size,
                                protectionArea.Shape == ProtectionAreaShape.Cube ? "cube" : "sphere"));
                            areaList.AppendLine("");
                        }

                        MyAPIGateway.Utilities.ShowMissionScreen("Protection Areas",
                            String.Format("Count: {0}", ProtectionHandler.Config.Areas.Count), null, areaList.ToString());
                        break;
                    default:
                        MyAPIGateway.Utilities.ShowMessage("ProtectionArea",
                            String.Format("{0} is no valid action. Actions: add, remove, list", action));
                        break;
                }
            }
            else
                Help(true);

            return true;
        }

        private bool TryParseShape(string shapeString, out ProtectionAreaShape shape)
        {
            switch (shapeString.ToLowerInvariant())
            {
                case "sphere":
                case "spherical":
                    shape = ProtectionAreaShape.Sphere;
                    return true;
                case "cube":
                case "cubic":
                    shape = ProtectionAreaShape.Cube;
                    return true;
            }

            shape = ProtectionAreaShape.Cube;
            return false;
        }
    }
}