namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using midspace.adminscripts.Messages;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;

    public class CommandPrivateMessage : ChatCommand
    {
        public IMyPlayer WhisperPartner;
        public static ulong LastWhisperId;
        public static bool LogPrivateMessages = false;

        public CommandPrivateMessage()
            : base(ChatCommandSecurity.User, ChatCommandFlag.Server | ChatCommandFlag.MultiplayerOnly, "msg", new[] { "/msg", "@", "/tell", "@@", "@@@", "@@@@", "@@@@@", "@?" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            string pmLogging = LogPrivateMessages ? "on" : "off";
            if (brief)
                MyAPIGateway.Utilities.ShowMessage("/msg <player> <message>", "Sends a private <message> to the specified <player>. Beware that it can be logged on the server.");
            else
            {
                StringBuilder description = new StringBuilder();
                description.AppendFormat(
@"This command is used for private communication. Beware that it can be logged on the server. It can only be used in multiplayer.
If you can't type a name, e.g. because it contains symbols, use /status to create a player hotlist. 
There are several enhancements in this command to make private communication easier. Here is a list of all commands and their functions:

  /msg <player> <message>
Aliases: /tell, @
To use an alias just replace the command name with the alias. '/tell {0} Hello' and '@ {0} Hello' is the same as '/msg {0} Hello'.
Function: Sends the specified <player> a private <message>.

  @@ <message>
Function: Sends a <message> to your 'whisperparnter'.

  @@@ <player> [message]
Function: Sets the specified <player> to your 'whisperpartner' and optionally sends him a private [message].

  @@@@ [message]
Function: Sets your 'whisperpartner' to the last player who whispered to you and optionally sends him a private [message].

  @@@@@ <message>
Function: Sends a private <message> to the last player who whispered to you.

  @?
Function: Shows you the name of your 'whisperpartner'.

The logging of private messages is {1}.
", MyAPIGateway.Session.Player.DisplayName, pmLogging); // add empty line at the end -> looks better.
                MyAPIGateway.Utilities.ShowMissionScreen("Help", null, Name, description.ToString(), null, null);
            }
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            List<IMyIdentity> cacheList = CommandPlayerStatus.GetIdentityCache(steamId);

            //TODO: matching playernames
            var match = Regex.Match(messageText, @"@@@@@\s+(?<Key>.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string message = match.Groups["Key"].Value;
                IMyPlayer lastWhisper = null;
                if (MyAPIGateway.Players.TryGetPlayer(LastWhisperId, out lastWhisper))
                {
                    SendPrivateMessage(steamId, lastWhisper, message);
                    return true;
                }
                MyAPIGateway.Utilities.SendMessage(steamId, "PM System", "Could not find player. Either no one whispered to you or he is offline now.");
                return true;
            }

            var match1 = Regex.Match(messageText, @"@@@@(\s+(?<Key>.+)|)", RegexOptions.IgnoreCase);
            if (match1.Success)
            {
                string message = match1.Groups["Key"].Value;
                IMyPlayer lastWhisper = null;
                if (MyAPIGateway.Players.TryGetPlayer(LastWhisperId, out lastWhisper))
                {
                    if (!string.IsNullOrEmpty(message))
                        SendPrivateMessage(steamId, lastWhisper, message);

                    WhisperPartner = lastWhisper;
                    MyAPIGateway.Utilities.SendMessage(steamId, "PM System", string.Format("Set whisperpartner to {0}", WhisperPartner.DisplayName));
                    return true;
                }
                MyAPIGateway.Utilities.SendMessage(steamId, "PM System", "Could not find player. Either no one whispered to you or he is offline now.");
                return true;
            }

            var match2 = Regex.Match(messageText, @"@@@\s+((?<Player>[^\s]+)\s+(?<Message>.*)|(?<Player>.+))", RegexOptions.IgnoreCase);
            if (match2.Success)
            {
                var playerName = match2.Groups["Player"].Value;
                var message = match2.Groups["Message"].Value;
                IMyPlayer receiver;
                int index;
                if (MyAPIGateway.Players.TryGetPlayer(playerName, out receiver))
                {
                    if (!string.IsNullOrEmpty(message))
                        SendPrivateMessage(steamId, receiver, message);

                    WhisperPartner = receiver;
                    MyAPIGateway.Utilities.SendMessage(steamId, "PM System", string.Format("Set whisperpartner to {0}", WhisperPartner.DisplayName));
                }
                else if (playerName.Substring(0, 1) == "#" && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= cacheList.Count)
                {
                    var listplayers = new List<IMyPlayer>();
                    MyAPIGateway.Players.GetPlayers(listplayers, p => p.PlayerID == cacheList[index - 1].PlayerId);
                    receiver = listplayers.FirstOrDefault();
                    if (!string.IsNullOrEmpty(message))
                        SendPrivateMessage(steamId, receiver, message);

                    WhisperPartner = receiver;
                    MyAPIGateway.Utilities.SendMessage(steamId, "PM System", string.Format("Set whisperpartner to {0}", WhisperPartner.DisplayName));
                }
                else
                    MyAPIGateway.Utilities.SendMessage(steamId, "PM System", string.Format("Player {0} does not exist.", match2.Groups["KeyPlayer"].Value));

                return true;
            }

            var match3 = Regex.Match(messageText, @"@@\s+(?<Key>.+)", RegexOptions.IgnoreCase);
            if (match3.Success)
            {
                if (WhisperPartner == null)
                {
                    MyAPIGateway.Utilities.SendMessage(steamId, "PM System", "No whisperpartner set");
                    return true;
                }
                //make sure player is online
                if (MyAPIGateway.Players.TryGetPlayer(WhisperPartner.SteamUserId, out WhisperPartner))
                {
                    SendPrivateMessage(steamId, WhisperPartner, match3.Groups["Key"].Value);
                    return true;
                }

                MyAPIGateway.Utilities.SendMessage(steamId, "PM System", string.Format("Player {0} is offline.", WhisperPartner.DisplayName));
                return true;
            }

            var match4 = Regex.Match(messageText, @"(@|/msg|/tell)\s+(?<Player>[^\s]+)\s+(?<Message>.+)", RegexOptions.IgnoreCase);
            if (match4.Success)
            {
                var playerName = match4.Groups["Player"].Value;
                IMyPlayer receiver;
                int index;
             
                if (MyAPIGateway.Players.TryGetPlayer(playerName, out receiver)) 
                    SendPrivateMessage(steamId, receiver, match4.Groups["Message"].Value);
                else if (playerName.Substring(0, 1) == "#" && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= cacheList.Count)
                {
                    var listplayers = new List<IMyPlayer>();
                    MyAPIGateway.Players.GetPlayers(listplayers, p => p.PlayerID == cacheList[index - 1].PlayerId);
                    receiver = listplayers.FirstOrDefault();
                    SendPrivateMessage(steamId, receiver, match4.Groups["Message"].Value);
                }
                else
                    MyAPIGateway.Utilities.SendMessage(steamId, "PM System", string.Format("Player {0} does not exist.", match4.Groups["KeyPlayer"].Value));
                return true;
            }

            var match5 = Regex.Match(messageText, @"@\?", RegexOptions.IgnoreCase);
            if (match5.Success)
            {
                if (WhisperPartner != null)
                    MyAPIGateway.Utilities.SendMessage(steamId, "PM System", string.Format("Your current whisperpartner is {0}.", WhisperPartner.DisplayName));
                else
                    MyAPIGateway.Utilities.SendMessage(steamId, "PM System", "No whisperpartner set.");
                return true;
            }
            

            return false;
        }

        private void SendPrivateMessage(ulong steamId, IMyPlayer receiver, string message)
        {
            if (string.IsNullOrEmpty(message))
                MyAPIGateway.Utilities.SendMessage(steamId, "PM System", "Message too short.");

            var privateMessage = new MessagePrivateMessage();

            IMyPlayer player = MyAPIGateway.Players.GetPlayer(steamId);
            string name = player == null ? string.Empty : player.DisplayName;

            privateMessage.ChatMessage = new ChatMessage
            {
                Sender = new Player
                {

                    SteamId = steamId,
                    PlayerName = name
                },
                Text = message,
                Date = DateTime.Now
            };

            privateMessage.Receiver = receiver.SteamUserId;
            ConnectionHelper.SendMessageToServer(privateMessage);

            MyAPIGateway.Utilities.SendMessage(steamId, string.Format("Whispered {0}", receiver.DisplayName), message);
        }
    }
}
