namespace midspace.adminscripts
{
    using midspace.adminscripts.Messages.Sync;
    using Sandbox.ModAPI;
    using System;
    using System.Text.RegularExpressions;
    using VRage.Game.ModAPI;
    using VRageMath;

    public class CommandShipMirror : ChatCommand
    {
        public CommandShipMirror()
            : base(ChatCommandSecurity.Admin, "mirror", new[] { "/mirror" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/mirror <red> <green> <blue>", "Mirror the targeted grid. The mirroring can be restricted to a pre-defined symmetry color.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var entity = Support.FindLookAtEntity_New(MyAPIGateway.Session.ControlledObject, true, true, false, false, false, false);
            if (entity == null)
            {
                MyAPIGateway.Utilities.ShowMessage("Mirror", "No ship targeted.");
                return true;
            }

            IMyCubeGrid shipEntity = entity as IMyCubeGrid;
            Vector3I blockPosition = Vector3I.MinValue;

            IMySlimBlock slimEntity = entity as IMySlimBlock;
            if (slimEntity != null)
            {
                blockPosition = slimEntity.Position;
                shipEntity = slimEntity.CubeGrid;
            }
            IMyCubeBlock blockEntity = entity as IMyCubeBlock;
            if (blockEntity != null)
            {
                blockPosition = blockEntity.Position;
                shipEntity = blockEntity.CubeGrid;
            }

            if (messageText.Equals("/mirror", StringComparison.OrdinalIgnoreCase))
            {
                MessageSyncMirror.AddMirror(shipEntity.EntityId, true, true, true, shipEntity.XSymmetryOdd, shipEntity.XSymmetryPlane, shipEntity.YSymmetryOdd, shipEntity.YSymmetryPlane, shipEntity.ZSymmetryOdd, shipEntity.ZSymmetryPlane, blockPosition, false);
                return true;
            }

            var match = Regex.Match(messageText, @"/mirror(\s{1,}(?<Key>.+))", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string[] codes = match.Groups["Key"].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                bool redAxis = false;
                bool greenAxis = false;
                bool blueAxis = false;
                bool oneWay = false;

                foreach (string code in codes)
                {
                    string shortCode = code.ToLower().Substring(0, 1);
                    if (shortCode == "r")
                        redAxis = true;
                    else if (shortCode == "g")
                        greenAxis = true;
                    else if (shortCode == "b")
                        blueAxis = true;
                    else if (shortCode == "1" || shortCode == "o")
                        oneWay = true;
                }

                if (!redAxis && !greenAxis && !blueAxis)
                {
                    redAxis = true;
                    greenAxis = true;
                    blueAxis = true;
                }

                if (!shipEntity.XSymmetryPlane.HasValue &&
                    !shipEntity.YSymmetryPlane.HasValue &&
                    !shipEntity.ZSymmetryPlane.HasValue)
                {
                    MyAPIGateway.Utilities.ShowMessage("Mirror", "No symmetry plane defined.");
                    return true;
                }

                if (redAxis && !shipEntity.XSymmetryPlane.HasValue)
                {
                    MyAPIGateway.Utilities.ShowMessage("Mirror", "Red(X) symmetry plane is not defined.");
                    return true;
                }

                if (greenAxis && !shipEntity.YSymmetryPlane.HasValue)
                {
                    MyAPIGateway.Utilities.ShowMessage("Mirror", "Green(Y) symmetry plane is not defined.");
                    return true;
                }

                if (blueAxis && !shipEntity.ZSymmetryPlane.HasValue)
                {
                    MyAPIGateway.Utilities.ShowMessage("Mirror", "Blue(Z) symmetry plane is not defined.");
                    return true;
                }

                if (oneWay && blockEntity == null)
                {
                    MyAPIGateway.Utilities.ShowMessage("Mirror", "Unable to determine block for one-way mirroring.");
                    return true;
                }

                MessageSyncMirror.AddMirror(shipEntity.EntityId, redAxis, greenAxis, blueAxis, shipEntity.XSymmetryOdd, shipEntity.XSymmetryPlane, shipEntity.YSymmetryOdd, shipEntity.YSymmetryPlane, shipEntity.ZSymmetryOdd, shipEntity.ZSymmetryPlane, blockPosition, oneWay);
                return true;
            }

            return false;
        }
    }
}
