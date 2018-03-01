using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace midspace.adminscripts
{
    class CommandMessageOfTheDay : ChatCommand
    {
        /// <summary>
        /// The motd
        /// </summary>
        public static string Content;

        /// <summary>
        /// The header in the mission screen (currentObjective)
        /// </summary>
        public static string HeadLine;

        /// <summary>
        /// True if the motd was received.
        /// </summary>
        public static bool Received =  false;

        /// <summary>
        /// If set to true the motd will show in chat instead of a mission screen.
        /// </summary>
        public static bool ShowInChat = false;

        public CommandMessageOfTheDay()
            : base(ChatCommandSecurity.User, ChatCommandFlag.Client | ChatCommandFlag.MultiplayerOnly, "motd", new[] { "/motd" })
        {

        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("Motd", "Displays the message of the day.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (!string.IsNullOrEmpty(Content))
                ShowMotd();
            else
                MyAPIGateway.Utilities.ShowMessage("Motd", "Message of the day not available.");
            return true;
        }

        public static void ShowMotd()
        {
            string headLine = HeadLine;
            if (!ShowInChat)
                MyAPIGateway.Utilities.ShowMissionScreen("Message Of The Day", "", headLine, Content, null, "Close");
            else
                MyAPIGateway.Utilities.ShowMessage("Motd", Content);
        }
        
        public static void ReplaceUserVariables()
        {
            Content = Content.Replace("%USER_NAME%", MyAPIGateway.Session.Player.DisplayName);
            HeadLine = HeadLine.Replace("%USER_NAME%", MyAPIGateway.Session.Player.DisplayName);
        }
        
    }
}
