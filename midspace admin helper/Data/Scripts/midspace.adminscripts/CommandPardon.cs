using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace midspace.adminscripts
{
    public class CommandPardon : ChatCommand
    {

        public CommandPardon()
            : base(ChatCommandSecurity.Admin, "pardon", new string[] { "/pardon" })
        {

        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/pardon <#>", "Pardons the specified player <#> if he has been forcebanned.");
        }

        public override bool Invoke(string messageText)
        {
            if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                return false;

            if (messageText.StartsWith("/pardon", StringComparison.InvariantCultureIgnoreCase))
            {
                string playerName = null;
                var match = Regex.Match(messageText, @"/pardon\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    playerName = match.Groups["Key"].Value;
                }
                ConnectionHelper.CreateAndSendConnectionEntity(ConnectionHelper.ConnectionKeys.Pardon, playerName);
                MyAPIGateway.Utilities.ShowMessage("Pardoning", playerName);
                return true;
            }

            return false;
        }
    }
}
