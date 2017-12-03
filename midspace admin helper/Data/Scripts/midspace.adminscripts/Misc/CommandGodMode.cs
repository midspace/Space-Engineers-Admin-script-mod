namespace midspace.adminscripts
{
    using midspace.adminscripts.Messages.Sync;
    using Sandbox.ModAPI;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VRage.Game.ModAPI;

    public class CommandGodMode : ChatCommand
    {
        private static List<IMyPlayer> _players = new List<IMyPlayer>();

        private static bool _registeredHandler;

        public bool GodModeEnabled;

        public CommandGodMode()
            : base(ChatCommandSecurity.Admin, "god", new string[] {"/god"})
        {

        }

        public override void Help(ulong steamId, bool brief)
        {
            if (brief)
                MyAPIGateway.Utilities.ShowMessage("/god <on|off>", "Turns god mode on or off");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/god(\s{1,}(?<Key>.+)|)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var state = match.Groups["Key"].Value;

                if (!string.IsNullOrEmpty(state))
                {
                    if (state.Equals("On", StringComparison.InvariantCultureIgnoreCase))
                        GodModeEnabled = true;
                    else if (state.Equals("Off", StringComparison.InvariantCultureIgnoreCase))
                        GodModeEnabled = false;
                    else
                    {
                        MyAPIGateway.Utilities.ShowMessage("GodMode", $"'{state}' is no valid setting. Use 'On' or 'Off'.");
                        return true;
                    }
                }
                else
                    GodModeEnabled ^= true;

                if (GodModeEnabled && !_registeredHandler)
                {
                    MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, GodModeDamageHandler_Client);
                    _registeredHandler = true;
                }

                if (MyAPIGateway.Multiplayer.MultiplayerActive)
                    ConnectionHelper.SendMessageToServer(new MessageSyncGod { Enable = GodModeEnabled });

                MyAPIGateway.Utilities.ShowMessage("GodMode", GodModeEnabled ? "On" : "Off");

                return true;
            }

            return false;
        }

        private void GodModeDamageHandler_Client(object target, ref MyDamageInformation info)
        {
            if (GodModeEnabled && target is IMyCharacter && target == MyAPIGateway.Session.Player.Character)
                info.Amount = 0;
        }

        /// <summary>
        /// Used to sync god mode with server.
        /// </summary>
        /// <param name="steamId"></param>
        /// <param name="enable"></param>
        public static void ChangeGodMode(ulong steamId, bool enable)
        {
            IMyPlayer player;

            if (!MyAPIGateway.Players.TryGetPlayer(steamId, out player))
                return;

            if (enable)
            {
                if (_players.Contains(player))
                    return;

                _players.Add(player);

                if (!_registeredHandler)
                {
                    MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, GodModeDamageHandler_Server);
                    _registeredHandler = true;
                }
            }
            else if (_players.Contains(player))
                _players.Remove(player);
        }

        private static void GodModeDamageHandler_Server(object target, ref MyDamageInformation info)
        {
            if (target is IMyCharacter && _players.Any(p => target == p.Character))
                info.Amount = 0;
        }
    }
}
