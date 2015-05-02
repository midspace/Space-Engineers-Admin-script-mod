namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;

    public class CommandShipScaleDown : ChatCommand
    {
        private const MyCubeSize scale = MyCubeSize.Small;

        public CommandShipScaleDown()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Experimental, "scaledown", new[] { "/scaledown" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/scaledown <#>", "---");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.Equals("/scaledown", StringComparison.InvariantCultureIgnoreCase))
            {
                var entity = Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, false);
                if (entity != null)
                {
                    if (CommandShipScaleUp.ScaleShip(entity as IMyCubeGrid, scale))
                        return true;
                }

                MyAPIGateway.Utilities.ShowMessage("scaledown", "No ship targeted.");
                return true;
            }

            var match = Regex.Match(messageText, @"/scaledown\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var shipName = match.Groups["Key"].Value;

                var currentShipList = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(currentShipList, e => e is Sandbox.ModAPI.IMyCubeGrid && e.DisplayName.Equals(shipName, StringComparison.InvariantCultureIgnoreCase));

                if (currentShipList.Count == 1)
                {
                    if (CommandShipScaleUp.ScaleShip(currentShipList.First() as IMyCubeGrid, scale))
                        return true;
                }
                else if (currentShipList.Count == 0)
                {
                    int index;
                    if (shipName.Substring(0, 1) == "#" && Int32.TryParse(shipName.Substring(1), out index) && index > 0 && index <= CommandListShips.ShipCache.Count && CommandListShips.ShipCache[index - 1] != null)
                    {
                        if (CommandShipScaleUp.ScaleShip(CommandListShips.ShipCache[index - 1] as IMyCubeGrid, scale))
                        {
                            CommandListShips.ShipCache[index - 1] = null;
                            return true;
                        }
                    }
                }
                else if (currentShipList.Count > 1)
                {
                    MyAPIGateway.Utilities.ShowMessage("scaledown", "{0} Ships match that name.", currentShipList.Count);
                    return true;
                }

                MyAPIGateway.Utilities.ShowMessage("scaledown", "Ship name not found.");
                return true;
            }

            return false;
        }
    }
}
