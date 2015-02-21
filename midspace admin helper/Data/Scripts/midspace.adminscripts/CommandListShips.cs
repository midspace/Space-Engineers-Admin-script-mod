namespace midspace.adminscripts
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;

    public class CommandListShips : ChatCommand
    {
        /// <summary>
        /// Temporary hotlist cache created when player requests a list of in game ships, populated only by search results.
        /// </summary>
        public readonly static List<IMyEntity> ShipCache = new List<IMyEntity>();

        public CommandListShips()
            : base(ChatCommandSecurity.Admin, "listships", new[] { "/listships" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/listships <filter>", "List in-game ships/stations. Optional <filter> to refine your search by ship name or antenna/beacon name.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.StartsWith("/listships", StringComparison.InvariantCultureIgnoreCase))
            {
                string shipName = null;
                var match = Regex.Match(messageText, @"/listships\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    shipName = match.Groups["Key"].Value;
                }

                var currentShipList = Support.FindShipsByName(shipName);

                ShipCache.Clear();
                MyAPIGateway.Utilities.ShowMessage("Count", currentShipList.Count.ToString());
                var index = 1;
                foreach (var ship in currentShipList.OrderBy(s => s.DisplayName))
                {
                    ShipCache.Add(ship);
                    MyAPIGateway.Utilities.ShowMessage(string.Format("#{0}", index++), ship.DisplayName);
                }

                return true;
            }

            return false;
        }
    }
}
