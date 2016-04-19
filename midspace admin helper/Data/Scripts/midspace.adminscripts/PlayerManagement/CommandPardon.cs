namespace midspace.adminscripts
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Messages.Communication;
    using Sandbox.ModAPI;

    public class CommandPardon : ChatCommand
    {
        public CommandPardon()
            : base(ChatCommandSecurity.Admin, ChatCommandFlag.Server | ChatCommandFlag.MultiplayerOnly, "pardon", new string[] { "/pardon" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/pardon <#>", "Pardons the specified player <#> if he has been forcebanned.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/pardon\s+(?<Key>.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var playerName = match.Groups["Key"].Value;

                Player bannedPlayer = ChatCommandLogic.Instance.ServerCfg.Config.ForceBannedPlayers.FirstOrDefault(p => p.PlayerName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase));
                if (bannedPlayer.SteamId != 0)
                {
                    ChatCommandLogic.Instance.ServerCfg.Config.ForceBannedPlayers.Remove(bannedPlayer);
                    MessageClientTextMessage.SendMessage(steamId, "Server", string.Format("Pardoned player {0}", bannedPlayer.PlayerName));
                }
                else
                    MessageClientTextMessage.SendMessage(steamId, "Server", string.Format("Can't find a banned player named {0}", playerName));

                return true;
            }

            MyAPIGateway.Utilities.SendMessage(steamId, "Pardoning", "Please supply name to pardon from Ban.");
            return true;
        }
    }
}
