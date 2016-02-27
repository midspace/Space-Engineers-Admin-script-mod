using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using midspace.adminscripts.Messages.Sync;

namespace midspace.adminscripts
{
    public class CommandGodMode : ChatCommand
    {
        private static List<IMyPlayer> Players = new List<IMyPlayer>();

        private static bool RegisteredHandler = false;
        public bool GodModeEnabled = false;

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
                        MyAPIGateway.Utilities.ShowMessage("GodMode", string.Format("'{0}' is no valid setting. Use 'On' or 'Off'.", state));
                        return true;
                    }
                }
                else
                    GodModeEnabled ^= true;

                if (GodModeEnabled && !RegisteredHandler)
                {
                    MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, GodModeDamageHandler_Client);
                    RegisteredHandler = true;
                }

                if (MyAPIGateway.Multiplayer.MultiplayerActive)
                    ConnectionHelper.SendMessageToServer(new MessageSyncGod() {Enable = GodModeEnabled});

                MyAPIGateway.Utilities.ShowMessage("GodMode", GodModeEnabled ? "On" : "Off");

                return true;
            }

            return false;
        }

        private void GodModeDamageHandler_Client(object target, ref MyDamageInformation info)
        {
            if (GodModeEnabled && target is IMyCharacter && target == MyAPIGateway.Session.Player.GetCharacter())
                info.Amount = 0;
        }

        /// <summary>
        /// Used to sync god mode with server.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="enable"></param>
        public static void ChangeGodMode(ulong steamId, bool enable)
        {
            IMyPlayer player;

            if (!MyAPIGateway.Players.TryGetPlayer(steamId, out player))
                return;

            if (enable)
            {
                if (Players.Contains(player))
                    return;

                Players.Add(player);

                if (!RegisteredHandler)
                {
                    MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, GodModeDamageHandler_Server);
                    RegisteredHandler = true;
                }
            }
            else if (Players.Contains(player))
                Players.Remove(player);
        }

        private static void GodModeDamageHandler_Server(object target, ref MyDamageInformation info)
        {
            if (target is IMyCharacter && Players.Any(p => target == p.GetCharacter()))
                info.Amount = 0;
        }
    }
}
