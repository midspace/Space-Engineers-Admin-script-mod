namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using VRageMath;
    using Sandbox.Definitions;

    public class CommandTeleportToPlayer : ChatCommand
    {
        /// <summary>
        /// Still working on this one.
        /// Need to make it safer to teleport when either player is a pilot.
        /// </summary>
        public CommandTeleportToPlayer()
            : base(ChatCommandSecurity.Admin, "tpp", new[] { "/tpp" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/tpp <#>", "Teleport you to the specified player <#>.");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/tpp\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var playerName = match.Groups["Key"].Value;
                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, p => p != null);
                IMyIdentity selectedPlayer = null;

                var identities = new List<IMyIdentity>();
                MyAPIGateway.Players.GetAllIdentites(identities, delegate(IMyIdentity i) { return i.DisplayName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase); });
                selectedPlayer = identities.FirstOrDefault();

                int index;
                if (playerName.Substring(0, 1) == "#" && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= CommandPlayerStatus.IdentityCache.Count)
                {
                    selectedPlayer = CommandPlayerStatus.IdentityCache[index - 1];
                }

                if (selectedPlayer == null)
                {
                    MyAPIGateway.Utilities.ShowMessage("Player name", string.Format("'{0}' not found", playerName));
                    return true;
                }

                var listplayers = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(listplayers, p => p.PlayerID == selectedPlayer.PlayerId);
                var player = listplayers.FirstOrDefault();

                if (player == null)
                {
                    MyAPIGateway.Utilities.ShowMessage("Player", "no longer exists");
                    return true;
                }

                Action<Vector3D> saveTeleportBack = delegate (Vector3D position)
                {
                    // save teleport in history
                    CommandTeleportBack.SaveTeleportInHistory(position);
                };

                Action emptySourceMsg = delegate ()
                {
                    MyAPIGateway.Utilities.ShowMessage("Teleport failed", "Source entity no longer exists.");
                };

                Action emptyTargetMsg = delegate ()
                {
                    MyAPIGateway.Utilities.ShowMessage("Teleport failed", "Target entity no longer exists.");
                };

                Action noSafeLocationMsg = delegate ()
                {
                    MyAPIGateway.Utilities.ShowMessage("Failed", "Could not find safe location to transport to.");
                };

                Support.MoveTo(MyAPIGateway.Session.Player, player, true, true, saveTeleportBack, emptySourceMsg, emptyTargetMsg, noSafeLocationMsg);
                return true;
            }

            return false;
        }
    }
}
