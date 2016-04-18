namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRageMath;

    public class CommandExtendedListShips : ChatCommand
    {
        public CommandExtendedListShips()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Server, "elistships", new[] { "/elistships", "/extendedlistships", "/listships2" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/elistships <filter>", "List in-game ships/stations, including postion and distance. Optional <filter> to refine your search by ship name or antenna/beacon name.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/(elistships|extendedlistships|listships2)(\s{1,}(?<Key>.+)|)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string shipName = match.Groups["Key"].Value;

                var currentShipList = Support.FindShipsByName(shipName);

                IMyPlayer player = MyAPIGateway.Players.GetPlayer(steamId);
                Vector3D position = Vector3D.Zero;
                if (player != null)
                    position = player.Controller.ControlledEntity.Entity.GetPosition();

                List<IMyEntity> shipCache = CommandListShips.GetShipCache(steamId);
                shipCache.Clear();

                var description = new StringBuilder();
                var prefix = string.Format("Count: {0}", currentShipList.Count);
                var index = 1;
                foreach (var ship in currentShipList.OrderBy(s => s.DisplayName))
                {
                    shipCache.Add(ship);
                    var pos = ship.WorldAABB.Center;
                    var distance = Math.Sqrt((position - pos).LengthSquared());
                    description.AppendFormat("#{0} {1}\r\n  ({2:N}|{3:N}|{4:N}) {5:N}m\r\n", index++, ship.DisplayName, pos.X, pos.Y, pos.Z, distance);
                }

                MyAPIGateway.Utilities.SendMissionScreen(steamId, "List Ships", prefix, " ", description.ToString(), null, "OK");
                return true;
            }

            return false;
        }
    }
}
