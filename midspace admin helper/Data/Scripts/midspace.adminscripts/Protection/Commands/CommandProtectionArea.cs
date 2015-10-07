using System.Text.RegularExpressions;
using midspace.adminscripts.Messages.Protection;
using Sandbox.Engine.Physics;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.ModAPI;
using VRageMath;

namespace midspace.adminscripts.Protection.Commands
{
    public class CommandProtectionArea : ChatCommand
    {
        public CommandProtectionArea()
            : base(
                ChatCommandSecurity.Admin, ChatCommandFlag.Experimental, "protectionarea",
                new string[] { "/protectionarea", "/pa" }) { }

        public override void Help(bool brief)
        {
            if (brief)
            {
                MyAPIGateway.Utilities.ShowMessage("/protectionarea <action> [options]", "Can add, remove, modify and list protection areas");
                return;
            }

            // TODO create help
            MyAPIGateway.Utilities.ShowMessage("/protectionarea <action> [options]", "Can add, remove, modify and list protection areas");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/(pa|protectionarea)\s+(?<CommandParts>.*)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var commandParts = match.Groups["CommandParts"].Value.Split(' ');
                // TODO improve error messages
                if (commandParts.Length < 2)
                {
                    MyAPIGateway.Utilities.ShowMessage("ProtectionArea", "Not enough options.");
                    return true;
                }

                var action = commandParts[0];
                string name = commandParts[1];

                switch (action.ToLowerInvariant())
                {
                    case "add":
                    {
                        if (commandParts.Length == 7)
                        {
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
                                MyAPIGateway.Utilities.ShowMessage("ProtectionArea", "Cannot parse shape.");
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
                        }
                        break;
                    }
                    case "remove":
                    {
                        ProtectionArea area = new ProtectionArea(name, new Vector3D(), 0, ProtectionAreaShape.Cube);
                        var message = new MessageProtectionArea()
                        {
                            ProtectionArea = area,
                            Type = ProtectionAreaMessageType.Remove
                        };
                        ConnectionHelper.SendMessageToServer(message);
                        break;
                    }
                    case "list":
                        break;
                    default:
                        // TODO display help
                        break;
                }
            }

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