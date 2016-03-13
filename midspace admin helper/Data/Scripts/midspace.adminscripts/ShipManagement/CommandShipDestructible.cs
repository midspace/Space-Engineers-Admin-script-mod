namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using midspace.adminscripts.Messages.Sync;
    using Sandbox.ModAPI;
    using VRage.ModAPI;

    public class CommandShipDestructible : ChatCommand
    {
        public CommandShipDestructible()
            : base(ChatCommandSecurity.Admin, "destructible", new[] { "/destructible", "/destruct" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/destructible On|Off <#>", "Set the specified <#> ship as destructible. Ship will be removed and regenerated.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/((destructible)|(destruct))\s+(?<switch>(on)|(off)|1|0)(\s+|$)(?<name>.*)|$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                bool switchOn = false;
                var switchString = match.Groups["switch"].Value;
                var shipName = match.Groups["name"].Value;

                if (switchString == "")
                    return false;

                if (switchString.Equals("on", StringComparison.InvariantCultureIgnoreCase) || switchString.Equals("1", StringComparison.InvariantCultureIgnoreCase))
                    switchOn = true;

                if (switchString.Equals("off", StringComparison.InvariantCultureIgnoreCase) || switchString.Equals("0", StringComparison.InvariantCultureIgnoreCase))
                    switchOn = false;


                // set destructible on the ship in the crosshairs.
                if (string.IsNullOrEmpty(shipName))
                {
                    var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false, false, false, false);
                    var shipEntity = entity as IMyCubeGrid;
                    if (shipEntity != null)
                    {
                        if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                            MessageSyncSetDestructable.SetDestructible(shipEntity, switchOn);
                        else
                            ConnectionHelper.SendMessageToServer(new MessageSyncSetDestructable()
                            {
                                EntityId = shipEntity.EntityId,
                                Destructable = switchOn
                            });
                        
                        return true;
                    }

                    MyAPIGateway.Utilities.ShowMessage("destructible", "No ship targeted.");
                    return true;
                }

                // Find the selected ship.
                var currentShipList = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(currentShipList, e => e is Sandbox.ModAPI.IMyCubeGrid && e.DisplayName.Equals(shipName, StringComparison.InvariantCultureIgnoreCase));

                if (currentShipList.Count == 1)
                {
                    if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                        MessageSyncSetDestructable.SetDestructible(currentShipList.First(), switchOn);
                    else
                        ConnectionHelper.SendMessageToServer(new MessageSyncSetDestructable()
                        {
                            EntityId = currentShipList.First().EntityId,
                            Destructable = switchOn
                        });

                    return true;
                }
                else if (currentShipList.Count == 0)
                {
                    int index;
                    if (shipName.Substring(0, 1) == "#" && Int32.TryParse(shipName.Substring(1), out index) && index > 0 && index <= CommandListShips.ShipCache.Count && CommandListShips.ShipCache[index - 1] != null)
                    {
                        if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                            MessageSyncSetDestructable.SetDestructible(CommandListShips.ShipCache[index - 1], switchOn);
                        else
                            ConnectionHelper.SendMessageToServer(new MessageSyncSetDestructable()
                            {
                                EntityId = CommandListShips.ShipCache[index - 1].EntityId,
                                Destructable = switchOn
                            });


                        CommandListShips.ShipCache[index - 1] = null;
                        return true;
                    }
                }
                else if (currentShipList.Count > 1)
                {
                    MyAPIGateway.Utilities.ShowMessage("destructible", "{0} Ships match that name.", currentShipList.Count);
                    return true;
                }

                MyAPIGateway.Utilities.ShowMessage("destructible", "Ship name not found.");
                return true;
            }

            return false;
        }
    }
}
