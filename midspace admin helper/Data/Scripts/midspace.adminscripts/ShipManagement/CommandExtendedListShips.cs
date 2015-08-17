namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;

    public class CommandExtendedListShips : ChatCommand
    {
        public CommandExtendedListShips()
            : base(ChatCommandSecurity.Admin, "elistships", new[] { "/elistships", "/extendedlistships", "/listships2" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/elistships <filter>", "List in-game ships/stations, including postion and distance. Optional <filter> to refine your search by ship name or antenna/beacon name.");
        }

        public override bool Invoke(string messageText)
        {
            string shipName = null;
            var match = Regex.Match(messageText, @"/(elistships|extendedlistships|listships2)(\s{1,}(?<Key>.+)|)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                shipName = match.Groups["Key"].Value;

                var currentShipList = Support.FindShipsByName(shipName);
                var position = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetPosition();

                CommandListShips.ShipCache.Clear();
                var description = new StringBuilder();
                var prefix = string.Format("Count: {0}", currentShipList.Count);
                var index = 1;
                foreach (var ship in currentShipList.OrderBy(s => s.DisplayName))
                {
                    CommandListShips.ShipCache.Add(ship);
                    var pos = ship.WorldAABB.Center;
                    var distance = Math.Sqrt((position - pos).LengthSquared());
                    description.AppendFormat("#{0} {1}\r\n  ({2:N}|{3:N}|{4:N}) {5:N}m\r\n", index++, ship.DisplayName, pos.X, pos.Y, pos.Z, distance);
                }

                MyAPIGateway.Utilities.ShowMissionScreen("List Ships", prefix, " ", description.ToString(), null, "OK");
                return true;
            }

            return false;
        }
    }
}
