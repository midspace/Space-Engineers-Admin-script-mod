using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace midspace.adminscripts
{
    public class CommandGodMode : ChatCommand
    {
        private bool RegisteredHandler = false;
        public bool GodModeEnabled = false;


        public CommandGodMode()
            : base(ChatCommandSecurity.Admin, "god", new string[] { "/god" })
        {

        }

        public override void Help(bool brief)
        {
            if (brief)
                MyAPIGateway.Utilities.ShowMessage("/god <on|off>", "Turns god mode on or off");
        }

        public override bool Invoke(string messageText)
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
                        MyAPIGateway.Utilities.ShowMessage("GodMode", string.Format("'{0}' is no valid setting. Use 'On' or 'Off'."));
                        return true;
                    }
                }
                else
                    GodModeEnabled ^= true;

                if (GodModeEnabled && !RegisteredHandler)
                {
                    MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, GodModeDamageHandler);
                    RegisteredHandler = true;
                }

                MyAPIGateway.Utilities.ShowMessage("GodMode", GodModeEnabled ? "On" : "Off");

                return true;
            }

            return false;
        }

        private void GodModeDamageHandler(object target, ref MyDamageInformation info)
        {
            if (GodModeEnabled && target is IMyCharacter && target == MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity)
                info.Amount = 0;
        }
    }
}
