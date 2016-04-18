namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;
    using VRageMath;

    public class CommandTeleportBack : ChatCommand
    {
        public static readonly Dictionary<long, List<Vector3D>> TeleportHistory = new Dictionary<long, List<Vector3D>>();
        private static readonly Dictionary<long, int> CurrentIndex = new Dictionary<long, int>();

        public CommandTeleportBack()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Server, "back", new string[] { "/back" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/back", "Teleports you back to your previous location.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/back", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!TeleportHistory.ContainsKey(playerId) || TeleportHistory[playerId].Count == 0)
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "CommandBack", "No entry in history, perform a teleport first.");
                    return true;
                }

                //if we havn't initialized the index yet or we reached the bottom of the history, we start again from the top
                if (!CurrentIndex.ContainsKey(playerId) || CurrentIndex[playerId] == 0)
                    CurrentIndex[playerId] = TeleportHistory[playerId].Count - 1;
                else
                    CurrentIndex[playerId] -= 1;

                var position = TeleportHistory[playerId][CurrentIndex[playerId]];

                IMyPlayer player = MyAPIGateway.Players.GetPlayer(steamId);

                Action noSafeLocationMsg = delegate
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "Failed", "Could not find safe location to transport to.");
                };

                //basically we perform a normal teleport without adding it to the history
                Support.MoveTo(player, position, true, null, noSafeLocationMsg);
                return true;
            }
            return false;
        }

        public static void SaveTeleportInHistory(long playerId, Vector3D position)
        {
            // add the position to the history and update the index so we start again from the top

            if (!TeleportHistory.ContainsKey(playerId))
                TeleportHistory[playerId] = new List<Vector3D>();
            TeleportHistory[playerId].Add(position);
            CurrentIndex[playerId] = TeleportHistory.Count - 1;
        }
    }
}
