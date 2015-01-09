namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using VRageMath;

    /// <summary>
    /// The IMyPlayer.Respawn() API command was removed, and no longer accessible.
    /// It was good for testing additional player bodies, especially in creative mode.
    /// </summary>
    public class CommandRespawn : ChatCommand
    {
        public CommandRespawn()
            : base(ChatCommandSecurity.Experimental, "respawn", new[] { "/respawn" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/respawn", "Respawns the player");
        }

        public override bool Invoke(string messageText)
        {
            //if (messageText.StartsWith("/respawn", StringComparison.InvariantCultureIgnoreCase))
            //{
            //    string playerName = null;
            //    var match = Regex.Match(messageText, @"/respawn\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
            //    if (match.Success)
            //    {
            //        playerName = match.Groups["Key"].Value;
            //    }

            //    if (playerName == null)
            //    {
            //        MyAPIGateway.Session.Player.PlayerCharacter.Die();
            //        return true;
            //    }
            //    else
            //    {
            //        var players = new List<IMyPlayer>();
            //        MyAPIGateway.Players.GetPlayers(players, p => p != null);

            //        var findPlayer = players.FirstOrDefault(p => p.DisplayName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase));
            //        if (findPlayer != null)
            //        {
            //            MyAPIGateway.Utilities.ShowMessage("slaying", findPlayer.DisplayName);
            //            findPlayer.RequestRespawn();
            //            return true;
            //        }

            //        int index;
            //        if (playerName.Substring(0, 1) == "#" && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= _playerCache.Count)
            //        {
            //            var player = _playerCache[index - 1];
            //            MyAPIGateway.Utilities.ShowMessage("slaying", player.DisplayName);
            //            player.RequestRespawn();
            //            return true;
            //        }
            //    }
            //}
            return false;
        }
    }
}
