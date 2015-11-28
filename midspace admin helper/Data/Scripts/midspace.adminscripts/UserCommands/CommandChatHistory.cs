using midspace.adminscripts.Messages;
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

        public CommandChatHistory()
            : base(ChatCommandSecurity.User, ChatCommandFlag.Client | ChatCommandFlag.MultiplayerOnly, "chat", new string[] { "/chat" })
        {

        }

        public override void Help(ulong steamId, bool brief)
        {
            if (brief)
                MyAPIGateway.Utilities.ShowMessage("/chat [entries]", "Shows the chat's history. By default it shows the last 100 entries.");
            else
            {
                MyAPIGateway.Utilities.ShowMissionScreen("Admin Helper Commands", "Help : ", Name, @"Shows the chat's history. The argument 'entries' is optional, by default the command shows the last 100 entries.

Syntax:
/chat [entries]

Example:
/chat 50
-> Shows the last 50 entries of the chat history.");
            }
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var match = Regex.Match(messageText, @"/chat(\s+(?<Entries>.+)|)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var entriesStr = match.Groups["Entries"].Value;

                uint entries = 100;

                if (string.IsNullOrEmpty(entriesStr))
                    SendRequest(entries);
                else
                    if (uint.TryParse(entriesStr, out entries) && entries != 0)
                        SendRequest(entries);
                    else
                        MyAPIGateway.Utilities.ShowMessage("Entries", "The argument entries must be an integer higher than 0");

                return true;
            }

            return false;
        }

        void SendRequest(uint entryCount)
        {
            ConnectionHelper.SendMessageToServer(new MessageChatHistory() { EntryCount = entryCount });
        }

        public static void DisplayChatHistory(List<ChatMessage> chatMessages)
        {
            StringBuilder content = new StringBuilder();

            if (chatMessages.Count == 0)
                content.Append("There are no messages in the chat history.");
            else
                foreach (ChatMessage chatMessage in chatMessages.OrderBy(m => m.Date))
                    content.AppendLine(string.Format("[{0:yyyy-MM-dd HH:mm:ss}] {1}: {2}", chatMessage.Date, chatMessage.Sender.PlayerName, chatMessage.Text));

            MyAPIGateway.Utilities.ShowMissionScreen("Chat History", "Displayed messages: ", chatMessages.Count.ToString(), content.ToString());
        }
    }
}
