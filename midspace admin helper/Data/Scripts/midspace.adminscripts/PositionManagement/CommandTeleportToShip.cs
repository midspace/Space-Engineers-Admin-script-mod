namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using VRageMath;
    using VRage.ModAPI;

    public class CommandTeleportToShip : ChatCommand
    {
        public CommandTeleportToShip()
            : base(ChatCommandSecurity.Admin, "tps", new[] { "/tps" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/tps <#>", "Teleport player to the specified ship <#>.");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/tps\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var shipName = match.Groups["Key"].Value;

                var currentShipList = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(currentShipList, e => e is IMyCubeGrid && e.DisplayName.Equals(shipName, StringComparison.InvariantCultureIgnoreCase));

                if (currentShipList.Count == 0)
                {
                    int index;
                    if (shipName.Substring(0, 1) == "#" && Int32.TryParse(shipName.Substring(1), out index) && index > 0 && index <= CommandListShips.ShipCache.Count)
                    {
                        currentShipList = new HashSet<IMyEntity> { CommandListShips.ShipCache[index - 1] };
                    }
                }

                var ship = currentShipList.FirstElement();

                if (ship == null)
                {
                    MyAPIGateway.Utilities.ShowMessage("Ship name", "'{0}' not found", shipName);
                    return true;
                }

                if (ship.Closed)
                {
                    MyAPIGateway.Utilities.ShowMessage("Ship", "no longer exists");
                    return true;
                }

                Action<Vector3D> saveTeleportBack = delegate (Vector3D position)
                {
                    // save teleport in history
                    CommandTeleportBack.SaveTeleportInHistory(position);
                };

                Action emptySourceMsg = delegate ()
                {
                    MyAPIGateway.Utilities.ShowMessage("Teleport failed", "Source player no longer exists.");
                };

                Action emptyTargetMsg = delegate ()
                {
                    MyAPIGateway.Utilities.ShowMessage("Teleport failed", "Target ship no longer exists.");
                };

                Action noSafeLocationMsg = delegate ()
                {
                    MyAPIGateway.Utilities.ShowMessage("Failed", "Could not find safe location to transport to.");
                };

                return Support.MoveTo(MyAPIGateway.Session.Player, ship, true,
                           saveTeleportBack, emptySourceMsg, emptyTargetMsg, noSafeLocationMsg);
            }

            return false;
        }
    }
}
