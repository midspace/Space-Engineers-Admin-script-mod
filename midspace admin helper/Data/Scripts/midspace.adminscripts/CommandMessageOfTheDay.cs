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
        public static string MessageOfTheDay;

        /// <summary>
        /// The header in the mission screen (currentObjective)
        /// </summary>
        public static string HeadLine;

        /// <summary>
        /// If true, on the next character spawn, the motd will be shown. False by default.
        /// </summary>
        public static bool ShowMotdOnSpawn = false;

        /// <summary>
        /// If true the motd could not be shown on the character spawn, so that it will be shown when the client receives the data. Mostly used in creative sessions. False by default.
        /// </summary>
        public static bool ShowMotdOnReceive = false;

        /// <summary>
        /// True if the motd was received.
        /// </summary>
        public static bool Received =  false;

        public CommandMessageOfTheDay()
            : base(ChatCommandSecurity.User, "motd", new[] { "/motd" })
        {

        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("Motd", "Displays the message of the day.");
        }

        public override bool Invoke(string messageText)
        {
            //TODO set the motd
            if (!string.IsNullOrEmpty(MessageOfTheDay))
                ShowMotd();
            else
                MyAPIGateway.Utilities.ShowMessage("Motd", "Message of the day not available.");
            return true;
        }

        public static void ShowMotd()
        {
            string headLine = HeadLine;
            MyAPIGateway.Utilities.ShowMissionScreen("Message Of The Day", "", headLine, MessageOfTheDay, null, "Close");
        }
    }
}
