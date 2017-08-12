namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;
    using VRageMath;

    public class CommandTeleportToPlayer : ChatCommand
    {
        /// <summary>
        /// Still working on this one.
        /// Need to make it safer to teleport when either player is a pilot.
        /// </summary>
        public CommandTeleportToPlayer()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Server, "tpp", new[] { "/tpp" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/tpp <#>", "Teleport you to the specified player <#>.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
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
                List<IMyIdentity> cacheList = CommandPlayerStatus.GetIdentityCache(steamId);
                if (playerName.Substring(0, 1) == "#" && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= cacheList.Count)
                {
                    selectedPlayer = cacheList[index - 1];
                }

                if (selectedPlayer == null)
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "Player name", string.Format("'{0}' not found", playerName));
                    return true;
                }

                var listplayers = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(listplayers, p => p.IdentityId == selectedPlayer.IdentityId);
                var player = listplayers.FirstOrDefault();

                if (player == null)
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "Player", "no longer exists");
                    return true;
                }

                Action<Vector3D> saveTeleportBack = delegate (Vector3D position)
                {
                    // save teleport in history
                    CommandTeleportBack.SaveTeleportInHistory(playerId, position);
                };

                Action<Support.MoveResponseMessage> responseMsg = delegate (Support.MoveResponseMessage message)
                {
                    switch (message)
                    {
                        case Support.MoveResponseMessage.SourceEntityNotFound:
                            MyAPIGateway.Utilities.SendMessage(steamId, "Teleport failed", "Source entity no longer exists.");
                            break;
                        case Support.MoveResponseMessage.TargetEntityNotFound:
                            MyAPIGateway.Utilities.SendMessage(steamId, "Teleport failed", "Target entity no longer exists.");
                            break;
                        case Support.MoveResponseMessage.NoSafeLocation:
                            MyAPIGateway.Utilities.SendMessage(steamId, "Failed", "Could not find safe location to transport to.");
                            break;
                        case Support.MoveResponseMessage.CannotTeleportStatic:
                            MyAPIGateway.Utilities.SendMessage(steamId, "Failed", "Cannot teleport station.");
                            break;
                    }
                };

                IMyPlayer thisPlayer;
                if (!MyAPIGateway.Players.TryGetPlayer(steamId, out thisPlayer))
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "Teleport failed", "Source entity no longer exists.");
                    return true;
                }

                var ret = Support.MoveTo(thisPlayer, player, true, true, saveTeleportBack, responseMsg, steamId);
                return true;
            }

            return false;
        }
    }
}
