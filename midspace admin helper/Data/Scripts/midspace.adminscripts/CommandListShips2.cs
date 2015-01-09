namespace midspace.adminscripts
{
    using System;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;

    public class CommandListShips2 : ChatCommand
    {
        public CommandListShips2()
            : base(ChatCommandSecurity.Admin, "listships2", new[] { "/listships2" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/listships2 <filter>", "List in-game ships/stations, including postion and distance. Optional <filter> to refine your search by ship name or antenna/beacon name.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.StartsWith("/listships2", StringComparison.InvariantCultureIgnoreCase))
            {
                string shipName = null;
                var match = Regex.Match(messageText, @"/listships2\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    shipName = match.Groups["Key"].Value;
                }

                var currentShipList = Support.FindShipsByName(shipName);
                var position = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetPosition();

                CommandListShips.ShipCache.Clear();
                MyAPIGateway.Utilities.ShowMessage("Count", currentShipList.Count.ToString());
                var index = 1;
                foreach (var ship in currentShipList)
                {
                    CommandListShips.ShipCache.Add(ship);

                    var pos = ship.WorldAABB.Center;
                    var distance = Math.Sqrt((position - pos).LengthSquared());
                    MyAPIGateway.Utilities.ShowMessage(string.Format("#{0}", index++), string.Format("{0} ({1:N}|{2:N}|{3:N}) {4:N}m", ship.DisplayName, pos.X, pos.Y, pos.Z, distance));
                }

                return true;
            }

            return false;
        }
    }
}
