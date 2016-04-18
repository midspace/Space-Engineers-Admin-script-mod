namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRageMath;

    public class CommandTeleportToShip : ChatCommand
    {
        public CommandTeleportToShip()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Server, "tps", new[] { "/tps" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/tps <#>", "Teleport player to the specified ship <#>.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
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
                    List<IMyEntity> shipCache = CommandListShips.GetShipCache(steamId);
                    if (shipName.Substring(0, 1) == "#" && Int32.TryParse(shipName.Substring(1), out index) && index > 0 && index <= shipCache.Count)
                    {
                        currentShipList = new HashSet<IMyEntity> { shipCache[index - 1] };
                    }
                }

                var ship = currentShipList.FirstElement();

                if (ship == null)
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "Ship name", "'{0}' not found", shipName);
                    return true;
                }

                if (ship.Closed)
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "Ship", "no longer exists");
                    return true;
                }

                Action<Vector3D> saveTeleportBack = delegate (Vector3D position)
                {
                    // save teleport in history
                    CommandTeleportBack.SaveTeleportInHistory(playerId, position);
                };

                Action emptySourceMsg = delegate ()
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "Teleport failed", "Source player no longer exists.");
                };

                Action emptyTargetMsg = delegate ()
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "Teleport failed", "Target ship no longer exists.");
                };

                Action noSafeLocationMsg = delegate ()
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "Failed", "Could not find safe location to transport to.");
                };

                IMyPlayer player = MyAPIGateway.Players.GetPlayer(steamId);
                return Support.MoveTo(player, ship, true,
                           saveTeleportBack, emptySourceMsg, emptyTargetMsg, noSafeLocationMsg);
            }

            return false;
        }
    }
}
