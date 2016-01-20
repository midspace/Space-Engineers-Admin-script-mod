using midspace.adminscripts.Messages;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VRage.Collections;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;

namespace midspace.adminscripts
{
    public class CommandPrivateMessage : ChatCommand
    {
        public IMyPlayer WhisperPartner;
        public static ulong LastWhisperId;
        public static bool LogPrivateMessages = false;
        public CommandPrivateMessage()
            : base(ChatCommandSecurity.User, ChatCommandFlag.Client | ChatCommandFlag.MultiplayerOnly, "msg", new[] { "/msg", "@", "/tell", "@@", "@@@", "@@@@", "@@@@@", "@?", "=", "-", "+", "!" })
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
There are several enhancements in this command to make private communication easier. Here is a list of all commands and their funcitons:

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
            //TODO: matching playernames
            var match = Regex.Match(messageText, @"@@@@@\s+(?<Key>.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string message = match.Groups["Key"].Value;
                IMyPlayer lastWhisper = null;
                if (MyAPIGateway.Players.TryGetPlayer(LastWhisperId, out lastWhisper))
                {
                    SendPrivateMessage(lastWhisper, message);
                    return true;
                }
                MyAPIGateway.Utilities.ShowMessage("PM System", "Could not find player. Either no one whispered to you or he is offline now.");
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
                        SendPrivateMessage(lastWhisper, message);

                    WhisperPartner = lastWhisper;
                    MyAPIGateway.Utilities.ShowMessage("PM System", string.Format("Set whisperpartner to {0}", WhisperPartner.DisplayName));
                    return true;
                }
                MyAPIGateway.Utilities.ShowMessage("PM System", "Could not find player. Either no one whispered to you or he is offline now.");
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
                        SendPrivateMessage(receiver, message);

                    WhisperPartner = receiver;
                    MyAPIGateway.Utilities.ShowMessage("PM System", string.Format("Set whisperpartner to {0}", WhisperPartner.DisplayName));
                }
                else if (playerName.Substring(0, 1) == "#" && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= CommandPlayerStatus.IdentityCache.Count)
                {
                    var listplayers = new List<IMyPlayer>();
                    MyAPIGateway.Players.GetPlayers(listplayers, p => p.PlayerID == CommandPlayerStatus.IdentityCache[index - 1].PlayerId);
                    receiver = listplayers.FirstOrDefault();
                    if (!string.IsNullOrEmpty(message))
                        SendPrivateMessage(receiver, message);

                    WhisperPartner = receiver;
                    MyAPIGateway.Utilities.ShowMessage("PM System", string.Format("Set whisperpartner to {0}", WhisperPartner.DisplayName));
                }
                else
                    MyAPIGateway.Utilities.ShowMessage("PM System", string.Format("Player {0} does not exist.", match2.Groups["KeyPlayer"].Value));

                return true;
            }

            var match3 = Regex.Match(messageText, @"@@\s+(?<Key>.+)", RegexOptions.IgnoreCase);
            if (match3.Success)
            {
                if (WhisperPartner == null)
                {
                    MyAPIGateway.Utilities.ShowMessage("PM System", "No whisperpartner set");
                    return true;
                }
                //make sure player is online
                if (MyAPIGateway.Players.TryGetPlayer(WhisperPartner.SteamUserId, out WhisperPartner))
                {
                    SendPrivateMessage(WhisperPartner, match3.Groups["Key"].Value);
                    return true;
                }

                MyAPIGateway.Utilities.ShowMessage("PM System", string.Format("Player {0} is offline.", WhisperPartner.DisplayName));
                return true;
            }

            var match4 = Regex.Match(messageText, @"(@|/msg|/tell)\s+(?<Player>[^\s]+)\s+(?<Message>.+)", RegexOptions.IgnoreCase);
            if (match4.Success)
            {
                var playerName = match4.Groups["Player"].Value;
                IMyPlayer receiver;
                int index;
                if (MyAPIGateway.Players.TryGetPlayer(playerName, out receiver)) 
                    SendPrivateMessage(receiver, match4.Groups["Message"].Value);
                else if (playerName.Substring(0, 1) == "#" && Int32.TryParse(playerName.Substring(1), out index) && index > 0 && index <= CommandPlayerStatus.IdentityCache.Count)
                {
                    var listplayers = new List<IMyPlayer>();
                    MyAPIGateway.Players.GetPlayers(listplayers, p => p.PlayerID == CommandPlayerStatus.IdentityCache[index - 1].PlayerId);
                    receiver = listplayers.FirstOrDefault();
                    SendPrivateMessage(receiver, match4.Groups["Message"].Value);
                }
                else
                    MyAPIGateway.Utilities.ShowMessage("PM System", string.Format("Player {0} does not exist.", match4.Groups["KeyPlayer"].Value));
                return true;
            }

            var match5 = Regex.Match(messageText, @"@\?", RegexOptions.IgnoreCase);
            if (match5.Success)
            {
                if (WhisperPartner != null)
                    MyAPIGateway.Utilities.ShowMessage("PM System", string.Format("Your current whisperpartner is {0}.", WhisperPartner.DisplayName));
                else
                    MyAPIGateway.Utilities.ShowMessage("PM System", "No whisperpartner set.");
                return true;
            }

            string sHelper;
            if (messageText.StartsWith("="))
            {
                if (messageText.Length >= 3)
                {
                    sHelper = messageText.Substring(2);
                    IMyFaction plFaction;
                    IMyPlayer Me = MyAPIGateway.Session.Player;
                    plFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(Me.PlayerID);
                    if (plFaction != null)
                    {
                        SendToFaction(sHelper, plFaction, Me);
                        MyAPIGateway.Utilities.ShowMessage(Me.DisplayName, sHelper);
                    }
                    else MyAPIGateway.Utilities.ShowMessage("Faction chat", "You are not a member of any faction.");
                }
                return true;
            }

            if (messageText.StartsWith("-"))
            {
                if (messageText.Length >= 3)
                {
                    sHelper = messageText.Substring(2);
                    IMyFaction plFaction;
                    IMyFaction currFaction;
                    IMyPlayer Me = MyAPIGateway.Session.Player;
                    plFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(Me.PlayerID);
                    if (plFaction != null)
                    {
                        Sandbox.Common.ObjectBuilders.MyObjectBuilder_FactionCollection factionCollection = MyAPIGateway.Session.Factions.GetObjectBuilder();
                        List<Sandbox.Common.ObjectBuilders.Definitions.MyObjectBuilder_Faction> factionsList = new List<Sandbox.Common.ObjectBuilders.Definitions.MyObjectBuilder_Faction>();
                        factionsList = factionCollection.Factions;
                        foreach (Sandbox.Common.ObjectBuilders.Definitions.MyObjectBuilder_Faction currBuilderFaction in factionsList)
                        {
                            currFaction = MyAPIGateway.Session.Factions.TryGetFactionById(currBuilderFaction.FactionId);
                            if (!MyAPIGateway.Session.Factions.AreFactionsEnemies(plFaction.FactionId, currFaction.FactionId))
                            {
                                SendToFaction(sHelper, currFaction, Me);
                            }
                        }
                        MyAPIGateway.Utilities.ShowMessage(Me.DisplayName, sHelper);
                    }
                    else MyAPIGateway.Utilities.ShowMessage("Faction chat", "You are not a member of any faction.");
                }
                return true;
            }

            if (messageText.StartsWith("+"))
            {
                if (messageText.Length >= 3)
                {
                    sHelper = messageText.Substring(2);
                    IMyFaction chatFaction;
                    IMyFaction currFaction;
                    IMyFaction plFaction;
                    IMyPlayer Me = MyAPIGateway.Session.Player;
                    plFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(Me.PlayerID);
                    if (plFaction != null)
                    {
                        Sandbox.Common.ObjectBuilders.MyObjectBuilder_FactionCollection factionCollection = MyAPIGateway.Session.Factions.GetObjectBuilder();
                        List<Sandbox.Common.ObjectBuilders.Definitions.MyObjectBuilder_Faction> factionsList = new List<Sandbox.Common.ObjectBuilders.Definitions.MyObjectBuilder_Faction>();
                        factionsList = factionCollection.Factions;
                        foreach (Sandbox.Common.ObjectBuilders.Definitions.MyObjectBuilder_Faction currCHTBuilderFaction in factionsList)
                        {
                            currFaction = MyAPIGateway.Session.Factions.TryGetFactionById(currCHTBuilderFaction.FactionId);
                            if (currFaction.Tag == "CHT")
                            {
                                if (!MyAPIGateway.Session.Factions.AreFactionsEnemies(plFaction.FactionId, currFaction.FactionId))
                                {
                                    chatFaction = currFaction;
                                    foreach (Sandbox.Common.ObjectBuilders.Definitions.MyObjectBuilder_Faction currBuilderFaction in factionsList)
                                    {
                                        currFaction = MyAPIGateway.Session.Factions.TryGetFactionById(currBuilderFaction.FactionId);
                                        if (!MyAPIGateway.Session.Factions.AreFactionsEnemies(chatFaction.FactionId, currFaction.FactionId))
                                        {
                                            SendToFaction(sHelper, currFaction, Me);
                                        }
                                    }
                                    MyAPIGateway.Utilities.ShowMessage(Me.DisplayName, sHelper);
                                }
                                else MyAPIGateway.Utilities.ShowMessage("Faction chat", "You are not allied with CHT faction.");
                                return true;
                            }
                        }
                        MyAPIGateway.Utilities.ShowMessage("Faction chat", "There are no CHT faction for broadcasting.");
                    }
                    else MyAPIGateway.Utilities.ShowMessage("Faction chat", "You are not a member of any faction.");
                }
                return true;
            }

            if (messageText.StartsWith("!"))
            {
                if (messageText.Length >= 3)
                {
                    sHelper = messageText.Substring(2);
                    IMyFaction plFaction;
                    IMyPlayer Me = MyAPIGateway.Session.Player;
                    var clients = MyAPIGateway.Session.GetWorld().Checkpoint.Clients;
                    if (clients != null)
                    {
                        var client = clients.FirstOrDefault(c => c.SteamId == MyAPIGateway.Multiplayer.MyId);
                        if (client != null)
                        {
                            if (client.IsAdmin)
                            {
                                List<Sandbox.ModAPI.IMyPlayer> listplayers = new List<Sandbox.ModAPI.IMyPlayer>();
                                MyAPIGateway.Players.GetPlayers(listplayers);
                                foreach (IMyPlayer pl in listplayers)
                                {
                                    IMyPlayer Receiver = pl;
                                    if (Me.DisplayName != Receiver.DisplayName)
                                    {
                                        if (MyAPIGateway.Players.TryGetPlayer(Receiver.DisplayName, out Receiver))
                                        {
                                            SendFactionMessage(Receiver, sHelper);
                                        }
                                    }
                                }
                                MyAPIGateway.Utilities.ShowMessage(Me.DisplayName, sHelper);
                            }
                            else MyAPIGateway.Utilities.ShowMessage("Faction chat", "You do not have administrator privileges for broadcasting to everybody.");
                        }
                    }
                    else MyAPIGateway.Utilities.ShowMessage("Faction chat", "There are no clients on the server.");
                }
                return true;
            }

            return false;
        }

        void SendToFaction(string message, IMyFaction plFaction, IMyPlayer Me)
        {
            var listplayers = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(listplayers);
            IMyPlayer Receiver;
            foreach (IMyPlayer pl in listplayers)
            {
                if (plFaction.IsMember(pl.PlayerID))
                {
                    Receiver = pl;
                    if (Me.DisplayName != Receiver.DisplayName)
                    {
                        if (MyAPIGateway.Players.TryGetPlayer(Receiver.DisplayName, out Receiver))
                        {
                            SendFactionMessage(Receiver, message);
                            break;
                        }
                    }
                }
            }
        }

        void SendFactionMessage(IMyPlayer receiver, string message)
        {
            var privateMessage = new MessagePrivateMessage();
            privateMessage.ChatMessage = new ChatMessage()
            {
                Sender = new Player()
                {
                    SteamId = MyAPIGateway.Session.Player.SteamUserId,
                    PlayerName = MyAPIGateway.Session.Player.DisplayName
                },
                Text = message,
                Date = DateTime.Now
            };

            privateMessage.Receiver = receiver.SteamUserId;
            ConnectionHelper.SendMessageToServer(privateMessage);
        }

        void SendPrivateMessage(IMyPlayer receiver, string message)
        {
            if (string.IsNullOrEmpty(message))
                MyAPIGateway.Utilities.ShowMessage("PM System", "Message too short.");

            var privateMessage = new MessagePrivateMessage();
            privateMessage.ChatMessage = new ChatMessage()
            {
                Sender = new Player()
                {
                    SteamId = MyAPIGateway.Session.Player.SteamUserId,
                    PlayerName = MyAPIGateway.Session.Player.DisplayName
                },
                Text = message,
                Date = DateTime.Now
            };

            privateMessage.Receiver = receiver.SteamUserId;
            ConnectionHelper.SendMessageToServer(privateMessage);

            MyAPIGateway.Utilities.ShowMessage(string.Format("Whispered {0}", receiver.DisplayName), message);
        }
    }
}
