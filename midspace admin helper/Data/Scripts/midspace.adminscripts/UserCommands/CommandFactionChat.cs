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
    public class CommandFactionChat : ChatCommand
    {
        public static bool LogPrivateMessages = false;
        public CommandFactionChat()
            : base(ChatCommandSecurity.User, ChatCommandFlag.Client | ChatCommandFlag.MultiplayerOnly, "fch", new[] { "/fch", "=", "-", "+", "!" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            string pmLogging = LogPrivateMessages ? "on" : "off";
            if (brief)
                MyAPIGateway.Utilities.ShowMessage("'fch', '=', '-', '+', '!' <message>", "Sends a <message> to the specified recepients group. Beware that it can be logged on the server.");
            else
            {
                StringBuilder description = new StringBuilder();
                description.AppendFormat(
@"This command is used for faction communication and broadcasting. Beware that it can be logged on the server. It can only be used in multiplayer.
Here is a list of all commands and their funcitons:

  = <message>
Aliases: /fch
Function: Sends a <message> to your faction.

  - <message>
Function: Sends a <message> to your and all allied facions.

  + <message>
Function: Sends a <message> to all factions wich is in peace with faction with tag CHT (this faction is like a hub).

  ! <message>
Function: broadcast to whole server by admin (usefull when global chat is disabled).

The logging of private messages is {1}.
", MyAPIGateway.Session.Player.DisplayName, pmLogging); // add empty line at the end -> looks better.
                MyAPIGateway.Utilities.ShowMissionScreen("Help", null, Name, description.ToString(), null, null);
            }
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            string sHelper;
            if (messageText.Length >= 3)
            {
                if (messageText.StartsWith("="))
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
                    return true;
                }

                if (messageText.StartsWith("-"))
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
                    return true;
                }

                if (messageText.StartsWith("+"))
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
                    return true;
                }

                if (messageText.StartsWith("!"))
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
                    return true;
                }
            }
            if (messageText.Length >= 5)
            {
                if (messageText.StartsWith("/fch"))
                {
                    sHelper = messageText.Substring(5);
                    IMyFaction plFaction;
                    IMyPlayer Me = MyAPIGateway.Session.Player;
                    plFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(Me.PlayerID);
                    if (plFaction != null)
                    {
                        SendToFaction(sHelper, plFaction, Me);
                        MyAPIGateway.Utilities.ShowMessage(Me.DisplayName, sHelper);
                    }
                    else MyAPIGateway.Utilities.ShowMessage("Faction chat", "You are not a member of any faction.");
                    return true;
                }
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
            var privateMessage = new MessageFactionMessage();
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
    }
}