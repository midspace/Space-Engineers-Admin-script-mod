namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Timers;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Definitions;
    using Sandbox.ModAPI;

    /// <summary>
    /// Adds special chat commands, allowing the player to get their position, date, time, change their location on the map.
    /// Authors: Midspace. AKA Screaming Angels. & Sp[a]cemarine.
    /// 
    /// The main Steam workshop link to this mod is:
    /// http://steamcommunity.com/sharedfiles/filedetails/?id=316190120
    /// 
    /// My other Steam workshop items:
    /// http://steamcommunity.com/id/ScreamingAngels/myworkshopfiles/?appid=244850
    /// </summary>
    /// <example>
    /// To use, simply open the chat window, and enter "/command", where command is one of the specified.
    /// Enter "/help" or "/help command" for more detail on individual commands.
    /// Chat commands do not have to start with "/". This model allows practically any text to become a command.
    /// Each ChatCommand can determine what it's own allowable command is.
    /// </example>
    [Sandbox.Common.MySessionComponentDescriptor(Sandbox.Common.MyUpdateOrder.BeforeSimulation)]
    public class ChatCommandLogic : Sandbox.Common.MySessionComponentBase
    {
        #region fields and constants

        public static ChatCommandLogic Instance;
        public ServerConfig ServerCfg;
        public string AdminNotification;
        public bool BlockCommandExecution = false;

        private bool _isInitialized;
        private Timer _timer100;
        private bool _100MsTimerElapsed;
        private bool _1000MsTimerElapsed;
        private int _timerCounter = 0;
        private static string[] _oreNames;
        private static List<string> _ingotNames;
        private static MyPhysicalItemDefinition[] _physicalItems;

        private Action<byte[]> MessageHandler_Client = new Action<byte[]>(HandleMessage_Client);
        private Action<byte[]> MessageHandler_Server = new Action<byte[]>(HandleMessage_Server);

        /// <summary>
        /// Set manually to true for testing purposes. No need for this function in general.
        /// </summary>
        public bool Debug = true;

        #endregion

        #region attaching events and wiring up

        public override void UpdateBeforeSimulation()
        {
            Instance = this;
            // This needs to wait until the MyAPIGateway.Session.Player is created, as running on a Dedicated server can cause issues.
            // It would be nicer to just read a property that indicates this is a dedicated server, and simply return.
            if (!_isInitialized && MyAPIGateway.Session != null && MyAPIGateway.Session.Player != null)
            {
                if (!MyAPIGateway.Session.OnlineMode.Equals(MyOnlineModeEnum.OFFLINE) && MyAPIGateway.Multiplayer.IsServer && !MyAPIGateway.Utilities.IsDedicated)
                    InitServer();
                Init();
            }
            if (!_isInitialized && MyAPIGateway.Utilities != null && MyAPIGateway.Multiplayer != null
                && MyAPIGateway.Session != null && MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Multiplayer.IsServer)
            {
                InitServer();
                return;
            }

            base.UpdateBeforeSimulation();

            if (_100MsTimerElapsed)
            {
                _100MsTimerElapsed = false;
                ChatCommandService.UpdateBeforeSimulation100();
            }

            if (_1000MsTimerElapsed)
            {
                _1000MsTimerElapsed = false;
                ChatCommandService.UpdateBeforeSimulation1000();
            }

            ChatCommandService.UpdateBeforeSimulation();
        }

        protected override void UnloadData()
        {
            DetachEvents();
            Logger.Debug("Closing...");
            Logger.Terminate();
            base.UnloadData();
        }

        #endregion

        private void Init()
        {
            _isInitialized = true; // Set this first to block any other calls from UpdateBeforeSimulation().
            Logger.Init();
            MyAPIGateway.Utilities.MessageEntered += Utilities_MessageEntered;
            Logger.Debug("Attach MessageEntered");

            foreach (ChatCommand command in GetAllChatCommands())
                ChatCommandService.Register(command);

            _timer100 = new Timer(100);
            _timer100.Elapsed += TimerOnElapsed100;
            _timer100.Start();
            // Attach any other events here.

            ChatCommandService.Init();

            //MultiplayerActive is false when initializing host... extreamly weird
            if (MyAPIGateway.Multiplayer.MultiplayerActive || ServerCfg != null) //only need this in mp
            {
                MyAPIGateway.Entities.OnEntityAdd += Entities_OnEntityAdd_Client;
                MyAPIGateway.Multiplayer.RegisterMessageHandler(ConnectionHelper.StandardClientId, MessageHandler_Client);
                Logger.Debug("Registered ProcessMessage_Client");
                var data = new Dictionary<string, string>();
                data.Add(ConnectionHelper.ConnectionKeys.ConnectionRequest, MyAPIGateway.Session.Player.SteamUserId.ToString());
                //let the server know we are ready for connections
                CommandMessageOfTheDay.ShowMotdOnSpawn = true;
                BlockCommandExecution = true;
                ConnectionHelper.SendMessageToServer(data);
            }
        }

        /// <summary>
        /// Server side initialization.
        /// </summary>
        private void InitServer()
        {
            _isInitialized = true; // Set this first to block any other calls from UpdateBeforeSimulation().
            Logger.Init();
            MyAPIGateway.Multiplayer.RegisterMessageHandler(ConnectionHelper.StandardServerId, MessageHandler_Server);
            Logger.Debug("Registered ProcessMessage_Server");

            ServerCfg = new ServerConfig(GetAllChatCommands());
        }
        private List<ChatCommand> GetAllChatCommands()
        {
            // This will populate the _oreNames, _ingotNames, ready for the ChatCommands.
            BuildResourceLookups();

            List<ChatCommand> commands = new List<ChatCommand>();
            // New command classes must be added in here.

            //commands.Add(new CommandAsteroidFindOre(_oreNames));
            //commands.Add(new CommandAsteroidEditClear());
            //commands.Add(new CommandAsteroidEditSet());
            commands.Add(new CommandAsteroidCreate());
            commands.Add(new CommandAsteroidCreateSphere());
            commands.Add(new CommandAsteroidsList());
            commands.Add(new CommandAsteroidRotate());
            //commands.Add(new CommandAsteroidSpread());  //not working
            commands.Add(new CommandConfig());
            commands.Add(new CommandDate());
            commands.Add(new CommandFactionDemote());
            commands.Add(new CommandFactionJoin());
            commands.Add(new CommandFactionKick());
            commands.Add(new CommandFactionPromote());
            commands.Add(new CommandFactionRemove());
            commands.Add(new CommandForceBan());
            commands.Add(new CommandForceKick());
            commands.Add(new CommandFlyTo());
            commands.Add(new CommandGameName());
            commands.Add(new CommandHeading());
            commands.Add(new CommandHelloWorld());
            commands.Add(new CommandHelp());
            commands.Add(new CommandIdentify());
            commands.Add(new CommandInventoryAdd(_oreNames, _ingotNames.ToArray(), _physicalItems));
            commands.Add(new CommandInventoryClear());
            commands.Add(new CommandInventoryDrop(_oreNames, _ingotNames.ToArray(), _physicalItems));
            commands.Add(new CommandListBots());
            //commands.Add(new CommandListBlueprints()); // no API currently.
            commands.Add(new CommandListPrefabs());
            commands.Add(new CommandListShips());
            commands.Add(new CommandListShips2());
            commands.Add(new CommandMessageOfTheDay());
            commands.Add(new CommandMeteor(_oreNames[0]));
            commands.Add(new CommandObjectsCollect());
            commands.Add(new CommandObjectsCount());
            commands.Add(new CommandObjectsPull());
            commands.Add(new CommandPardon());
            commands.Add(new CommandPermission());
            commands.Add(new CommandPlayerEject());
            commands.Add(new CommandPlayerSlay());
            commands.Add(new CommandPlayerSmite(_oreNames[0]));
            //commands.Add(new CommandPlayerRespawn());  //not working any more
            commands.Add(new CommandPlayerStatus());
            commands.Add(new CommandPosition());
            commands.Add(new CommandPrefabAdd());
            commands.Add(new CommandPrefabAddWireframe());
            commands.Add(new CommandPrefabPaste());
            commands.Add(new CommandPrivateMessage());
            commands.Add(new CommandSaveGame());
            commands.Add(new CommandSessionCargoShips());
            commands.Add(new CommandSessionCopyPaste());
            commands.Add(new CommandSessionCreative());
            commands.Add(new CommandSessionSpectator());
            commands.Add(new CommandSessionWeapons());
            commands.Add(new CommandSetVector());
            commands.Add(new CommandShipOff());
            commands.Add(new CommandShipOn());
            commands.Add(new CommandShipOwnerClaim());
            commands.Add(new CommandShipOwnerRevoke());
            commands.Add(new CommandShipDelete());
            commands.Add(new CommandShipScaleDown());
            commands.Add(new CommandShipScaleUp());
            //commands.Add(new CommandShipOwnerShare());  //not working
            commands.Add(new CommandStop());
            commands.Add(new CommandStopAll());
            commands.Add(new CommandTeleport());
            commands.Add(new CommandTeleportBack());
            commands.Add(new CommandTeleportDelete());
            commands.Add(new CommandTeleportFavorite());
            commands.Add(new CommandTeleportJump());
            commands.Add(new CommandTeleportList());
            commands.Add(new CommandTeleportOffset());
            commands.Add(new CommandTeleportSave());
            commands.Add(new CommandTeleportToPlayer());
            commands.Add(new CommandTeleportToShip());
            commands.Add(new CommandTest());
            commands.Add(new CommandTime());
            commands.Add(new CommandVersion());
            //commands.Add(new CommandVoxelAdd());  //not working any more
            //commands.Add(new CommandVoxelsList()); //not working any more

            return commands;
        }

        #region detaching events

        private void DetachEvents()
        {
            if (_timer100 != null)
            {
                _timer100.Stop();
                _timer100.Elapsed -= TimerOnElapsed100;
            }

            if (ServerCfg != null)
            { //only for clients it is null
                ServerCfg.Save();
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(ConnectionHelper.StandardServerId, MessageHandler_Server);
                Logger.Debug("Uregistered MessageHandler Server");
            }

            if (MyAPIGateway.Utilities != null && MyAPIGateway.Multiplayer != null && MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Utilities.IsDedicated)
                return;

            if (MyAPIGateway.Multiplayer != null &&  MyAPIGateway.Multiplayer.MultiplayerActive || (ServerCfg != null && ServerCfg.ServerIsClient))
            {
                MyAPIGateway.Entities.OnEntityAdd -= Entities_OnEntityAdd_Client;
                Logger.Debug("Detached Entities_OnEntityAdd_Client");
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(ConnectionHelper.StandardClientId, MessageHandler_Client);
                Logger.Debug("Uregistered MessageHandler Client");
            }
            
            if (MyAPIGateway.Utilities != null) {
                MyAPIGateway.Utilities.MessageEntered -= Utilities_MessageEntered;
                Logger.Debug("Detached MessageEntered");
            }
        }

        private void TimerOnElapsed100(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            _timerCounter++;

            // Run timed events that do not affect Threading in game.
            _100MsTimerElapsed = true;

            if (_timerCounter % 10 == 0)
                _1000MsTimerElapsed = true;

            // DO NOT SET ANY IN GAME API CALLS HERE. AT ALL!

            if (_timerCounter == 100)
                _timerCounter = 0;
        }

        #endregion

        #region message processing

        private void Utilities_MessageEntered(string messageText, ref bool sendToOthers)
        {
            if (ChatCommandService.ProcessMessage(messageText))
            {
                sendToOthers = false;
            }

            if (sendToOthers)
                ConnectionHelper.SendMessageToServer(ConnectionHelper.ConnectionKeys.GlobalMessage, messageText);
        }

        #endregion

        #region helpers

        private static void BuildResourceLookups()
        {
            MyDefinitionManager.Static.GetOreTypeNames(out _oreNames);
            var physicalItems = MyDefinitionManager.Static.GetPhysicalItemDefinitions();
            _physicalItems = physicalItems.Where(item => item.Public).ToArray();  // Limit to public items.  This will remove the CubePlacer. :)
            _ingotNames = new List<string>();

            foreach (var physicalItem in _physicalItems)
            {
                if (physicalItem.Id.TypeId == typeof(MyObjectBuilder_Ingot))
                {
                    _ingotNames.Add(physicalItem.Id.SubtypeName);
                }
            }
        }

        #endregion

        void Entities_OnEntityAdd_Client(IMyEntity entity)
        {
            if (entity is IMyCharacter && CommandMessageOfTheDay.ShowMotdOnSpawn && entity.DisplayName.Equals(MyAPIGateway.Session.Player.DisplayName, StringComparison.InvariantCultureIgnoreCase))
            {
                if (CommandMessageOfTheDay.Received)
                {
                    CommandMessageOfTheDay.ShowMotd();
                    if (!string.IsNullOrEmpty(AdminNotification))
                        MyAPIGateway.Utilities.ShowMissionScreen("Admin Notification System", "Error", null, ChatCommandLogic.Instance.AdminNotification, null, null);
                }
                else
                    CommandMessageOfTheDay.ShowOnReceive = true;
                CommandMessageOfTheDay.ShowMotdOnSpawn = false;
            }
        }

        #region connection handling

        private static void HandleMessage_Client(byte[] message)
        {
            Logger.Debug(string.Format("HandleMessage - {0}", System.Text.Encoding.Unicode.GetString(message)));
            ConnectionHelper.ProcessClientData(message);
        }

        private static void HandleMessage_Server(byte[] message)
        {
            ConnectionHelper.ProcessServerData(message);
        }

        #endregion
    }
}