using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace midspace.adminscripts
{

    public class CommandChatHistory : ChatCommand
    {
        static List<ChatMessage> MessageCache = new List<ChatMessage>();

        public CommandChatHistory()
            : base (ChatCommandSecurity.User, "chat", new string[] { "/chat" })
        {
            // clear the static cache.
            MessageCache.Clear();
        }

        public override void Help(bool brief)
        {
            if (brief)
                MyAPIGateway.Utilities.ShowMessage("/chat [entries]", "Shows the chat's history. By default it shows the last 100 entries.");
            else
            {
                MyAPIGateway.Utilities.ShowMissionScreen("Help", null, Name, @"Shows the chat's history. The argument 'entries' is optional, by default the command shows the last 100 entries.

Syntax:
/chat [entries]

Example:
/chat 50
-> Shows the last 50 entries of the chat history.");
            }
        }

        public override bool Invoke(string messageText)
        {
            if (!MyAPIGateway.Multiplayer.MultiplayerActive)
            {
                MyAPIGateway.Utilities.ShowMessage("Chat", "Command disabled in offline mode.");
                return true;
            }

            var match = Regex.Match(messageText, @"/chat(\s+(?<Entries>.+)|)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var entriesStr = match.Groups["Entries"].Value;

                uint entries = 100;

                if (string.IsNullOrEmpty(entriesStr))
                    ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.Chat, entries.ToString());
                else
                    if (uint.TryParse(entriesStr, out entries) && entries != 0)
                        ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.Chat, entries.ToString());
                    else
                        MyAPIGateway.Utilities.ShowMessage("Entries", "The argument entries must be an integer higher than 0");

                return true;
            }

            return false;
        }

        public static void AddMessageToCache(ChatMessage message, bool lastEntry)
        {
            MessageCache.Add(message);

            if (lastEntry)
            {
                StringBuilder content = new StringBuilder();
                foreach (ChatMessage chatMessage in MessageCache.OrderByDescending(m => m.Date))
                    content.AppendLine(string.Format("[{0:yyyy-MM-dd HH:mm:ss}] {1}: {2}", chatMessage.Date, chatMessage.Sender.PlayerName, chatMessage.Message));

                MyAPIGateway.Utilities.ShowMissionScreen("Chat History", "Displayed messages: ", MessageCache.Count.ToString(), content.ToString());

                MessageCache.Clear();
            }
        }
    }
}
