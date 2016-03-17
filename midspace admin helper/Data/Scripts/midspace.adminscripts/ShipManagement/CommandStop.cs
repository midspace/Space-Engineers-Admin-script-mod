namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using midspace.adminscripts.Messages.Sync;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;

    public class CommandStop : ChatCommand
    {
        public CommandStop()
            : base(ChatCommandSecurity.Admin, "stop", new[] { "/stop" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/stop <#>", "Stops all motion of the specified <#> ship. Turns on dampeners, and initiates thrusters. Unpowered ships will also stop.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/stop", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false);
                var shipEntity = entity as IMyCubeGrid;
                if (shipEntity != null)
                {
                    if (MyAPIGateway.Multiplayer.MultiplayerActive)
                    {
                        MessageSyncEntity.Process(shipEntity, SyncEntityType.Stop);
                        MyAPIGateway.Utilities.ShowMessage(shipEntity.DisplayName, "Is stopping.");
                        return true;
                    }

                    var ret = entity.StopShip();
                    MyAPIGateway.Utilities.ShowMessage(shipEntity.DisplayName, ret ? "Is stopping." : "Cannot be stopped.");
                    return ret;
                }
                MyAPIGateway.Utilities.ShowMessage("Stop", "No ship targeted.");
                return true;
            }

            if (messageText.StartsWith("/stop ", StringComparison.InvariantCultureIgnoreCase))
            {
                var match = Regex.Match(messageText, @"/stop\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
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

                    if (MyAPIGateway.Multiplayer.MultiplayerActive)
                    {
                        foreach (var selectedShip in currentShipList)
                            MessageSyncEntity.Process(selectedShip, SyncEntityType.Stop);
                        MyAPIGateway.Utilities.ShowMessage(currentShipList.First().DisplayName, "Is stopping.");
                        return true;
                    }

                    var ret = StopShips(currentShipList);
                    MyAPIGateway.Utilities.ShowMessage(currentShipList.First().DisplayName, ret ? "Is stopping." : "Cannot be stopped.");
                    return ret;
                }
            }

            return false;
        }

        private bool StopShips(IEnumerable<IMyEntity> shipList)
        {
            var ret = false;
            foreach (var selectedShip in shipList)
            {
                ret |= selectedShip.StopShip();
            }
            return ret;
        }
    }
}
