namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using midspace.adminscripts.Messages;
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRage.Game.ModAPI;

    public class CommandFactionChat : ChatCommand
    {
        public CommandFactionChat()
            : base(ChatCommandSecurity.User, ChatCommandFlag.Client | ChatCommandFlag.MultiplayerOnly, "factionchat", new[] { "/factionchat", "/fch", "=", "-", "+", "!" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            string logging = "off"; //CommandPrivateMessage.LogPrivateMessages ? "on" : "off";
            if (brief)
                MyAPIGateway.Utilities.ShowMessage("'fch', '=', '-', '+', '!' <message>", "Sends a <message> to the specified recepients group. Beware that it can be logged on the server.");
            else
            {
                StringBuilder description = new StringBuilder();
                description.AppendFormat(
@"This command is used for faction communication and broadcasting. There are prefixes to indicate how the message was sent. You don't need to type in the prefixes as they will be automatically be generated. Beware that the messages can be logged on the server. The commands can only be used in multiplayer.
Here is a list of all commands and their functions:

  = <message>
Aliases: /fch
Function: Sends a <message> to your faction.
Prefix: [F]

  - <message>
Function: Sends a <message> to your and all allied facions.
Prefix: [A]

  + <message>
Function: Sends a <message> to all factions wich are in peace with faction with tag CHT (this faction is like a hub).
Note: This only makes sense if the global chat is disabled.
Prefix: [H]

  ! <message>
Function: Broadcast to whole server (usefull when global chat is disabled, can only be used by admins).
Prefix: [B]

The logging of private messages is {0}.
", logging); // add empty line at the end -> looks better.
                MyAPIGateway.Utilities.ShowMissionScreen("Help", null, Name, description.ToString());
            }
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            var matchFactionChat = Regex.Match(messageText, @"(=|/fch|/factionchat)\s+(?<Message>.+)", RegexOptions.IgnoreCase);
            if (matchFactionChat.Success)
            {
                IMyPlayer player = MyAPIGateway.Session.Player;
                var plFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(player.IdentityId);
                if (plFaction != null)
                {
                    SendToFaction(matchFactionChat.Groups["Message"].Value, plFaction, FactionMessageType.OwnFaction);
                    MyAPIGateway.Utilities.ShowMessage(String.Format("[F] {0}", player.DisplayName), matchFactionChat.Groups["Message"].Value);
                }
                else
                    MyAPIGateway.Utilities.ShowMessage("Faction chat", "You are not a member of any faction.");
                return true;
            }

            var matchAlliedFactionChat = Regex.Match(messageText, @"-\s+(?<Message>.+)", RegexOptions.IgnoreCase);
            if (matchAlliedFactionChat.Success)
            {
                IMyPlayer player = MyAPIGateway.Session.Player;
                var plFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(player.IdentityId);
                if (plFaction != null)
                {
                    MyObjectBuilder_FactionCollection factionCollection = MyAPIGateway.Session.Factions.GetObjectBuilder();
                    var factionsList = factionCollection.Factions;
                    foreach (MyObjectBuilder_Faction currBuilderFaction in factionsList)
                    {
                        var currFaction = MyAPIGateway.Session.Factions.TryGetFactionById(currBuilderFaction.FactionId);
                        if (!MyAPIGateway.Session.Factions.AreFactionsEnemies(plFaction.FactionId, currFaction.FactionId))
                        {
                            SendToFaction(matchAlliedFactionChat.Groups["Message"].Value, currFaction, FactionMessageType.AlliedFacitons);
                        }
                    }
                    MyAPIGateway.Utilities.ShowMessage(String.Format("[A] {0}", player.DisplayName), matchAlliedFactionChat.Groups["Message"].Value);
                }
                else
                    MyAPIGateway.Utilities.ShowMessage("Faction chat", "You are not a member of any faction.");
                return true;
            }

            var matchHubChat = Regex.Match(messageText, @"\+\s+(?<Message>.+)", RegexOptions.IgnoreCase);
            if (matchHubChat.Success)
            {
                IMyPlayer player = MyAPIGateway.Session.Player;
                var plFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(player.IdentityId);
                if (plFaction != null)
                {
                    MyObjectBuilder_FactionCollection factionCollection = MyAPIGateway.Session.Factions.GetObjectBuilder();
                    var factionsList = factionCollection.Factions;

                    var chatFactionBuilder = factionsList.FirstOrDefault(f => f.Tag.Equals("CHT"));
                    if (chatFactionBuilder == null)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Faction chat", "There is no CHT faction for broadcasting.");
                        return true;
                    }

                    var chatFaction = MyAPIGateway.Session.Factions.TryGetFactionById(chatFactionBuilder.FactionId);
                    if (!MyAPIGateway.Session.Factions.AreFactionsEnemies(plFaction.FactionId, chatFaction.FactionId))
                    {
                        foreach (MyObjectBuilder_Faction currBuilderFaction in factionsList)
                        {
                            var currFaction = MyAPIGateway.Session.Factions.TryGetFactionById(currBuilderFaction.FactionId);
                            if (!MyAPIGateway.Session.Factions.AreFactionsEnemies(chatFaction.FactionId, currFaction.FactionId))
                            {
                                SendToFaction(matchHubChat.Groups["Message"].Value, currFaction, FactionMessageType.AlliedWithHub);
                            }
                        }
                        MyAPIGateway.Utilities.ShowMessage(String.Format("[H] {0}", player.DisplayName), matchHubChat.Groups["Message"].Value);
                    }
                    else
                        MyAPIGateway.Utilities.ShowMessage("Faction chat", "You are not allied with CHT.");
                    return true;
                }
                
                MyAPIGateway.Utilities.ShowMessage("Faction chat", "You are not a member of any faction.");
                return true;
            }

            var matchBroadcast = Regex.Match(messageText, @"!\s+(?<Message>.+)", RegexOptions.IgnoreCase);
            if (matchBroadcast.Success)
            {
                IMyPlayer player = MyAPIGateway.Session.Player;
                if (player.IsAdmin())
                {
                    List<IMyPlayer> listplayers = new List<IMyPlayer>();
                    MyAPIGateway.Players.GetPlayers(listplayers);
                    foreach (IMyPlayer receiver in listplayers.Where(p => p != player))
                    {
                        SendFactionMessage(receiver, matchBroadcast.Groups["Message"].Value, FactionMessageType.Broadcast);
                    }
                    MyAPIGateway.Utilities.ShowMessage(String.Format("[B] {0}", player.DisplayName), matchBroadcast.Groups["Message"].Value);
                }
                else
                    MyAPIGateway.Utilities.ShowMessage("Faction chat", "You do not have administrator privileges for broadcasting to everybody.");
                return true;
            }

            return false;
        }

        private void SendToFaction(string message, IMyFaction faction, FactionMessageType type)
        {
            var listplayers = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(listplayers);
            foreach (IMyPlayer receiver in listplayers.Where(p => faction.IsMember(p.IdentityId) && p != MyAPIGateway.Session.Player))
            {
                SendFactionMessage(receiver, message, type);
            }
        }

        private void SendFactionMessage(IMyPlayer receiver, string message, FactionMessageType type)
        {
            var factionMessage = new MessageFactionMessage();
            factionMessage.ChatMessage = new ChatMessage
            {
                Sender = new Player
                {
                    SteamId = MyAPIGateway.Session.Player.SteamUserId,
                    PlayerName = MyAPIGateway.Session.Player.DisplayName
                },
                Text = message,
                Date = DateTime.Now
            };

            factionMessage.Receiver = receiver.SteamUserId;
            factionMessage.Type = type;
            ConnectionHelper.SendMessageToServer(factionMessage);
        }
    }
}