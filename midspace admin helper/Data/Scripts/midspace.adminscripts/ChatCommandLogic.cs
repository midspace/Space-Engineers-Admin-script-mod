namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Timers;
    using midspace.adminscripts.Messages;
    using midspace.adminscripts.Protection;
    using midspace.adminscripts.Protection.Commands;
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
        public AdminNotification AdminNotification;
        public bool ShowDialogsOnReceive = false;

        public Timer PermissionRequestTimer;
        public bool BlockCommandExecution = false;
        public bool AllowBuilding = false;

        private bool _permissionRequest;
        private bool _isInitialized;
        private bool _commandsRegistered;
        private Timer _timer100;
        private bool _100MsTimerElapsed;
        private bool _1000MsTimerElapsed;
        private int _timerCounter = 0;
        private static string[] _oreNames;
        private static List<string> _ingotNames;
        private static MyPhysicalItemDefinition[] _physicalItems;

        private Action<byte[]> MessageHandler = new Action<byte[]>(HandleMessage);

        /// <summary>
        /// Set manually to true for testing purposes. No need for this function in general.
        /// </summary>
        public bool Debug = false;

        #endregion

        #region attaching events and wiring up

        public override void UpdateBeforeSimulation()
        {
            Instance = this;
            // This needs to wait until the MyAPIGateway.Session.Player is created, as running on a Dedicated server can cause issues.
            // It would be nicer to just read a property that indicates this is a dedicated server, and simply return.
            if (!_isInitialized && MyAPIGateway.Session != null && MyAPIGateway.Session.Player != null)
            {
                Debug = MyAPIGateway.Session.Player.IsExperimentalCreator();

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

            if (_permissionRequest)
            {
                _permissionRequest = false;
                ConnectionHelper.SendMessageToServer(new MessagePermissionRequest());
            }

            ChatCommandService.UpdateBeforeSimulation();
            ProtectionHandler.UpdateBeforeSimulation();
        }

        protected override void UnloadData()
        {
            DetachEvents();
            Logger.Debug("Closing...");
            Logger.Terminate();
            base.UnloadData();
        }

        public override void SaveData()
        {
            base.SaveData();

            if (ServerCfg != null)
                ServerCfg.Save();
        }

        #endregion

        private void Init()
        {
            _isInitialized = true; // Set this first to block any other calls from UpdateBeforeSimulation().
            Logger.Init();
            MyAPIGateway.Utilities.MessageEntered += Utilities_MessageEntered;
            Logger.Debug("Attach MessageEntered");


            _timer100 = new Timer(100);
            _timer100.Elapsed += TimerOnElapsed100;
            _timer100.Start();
            // Attach any other events here.

            if (!_commandsRegistered)
            {
                foreach (ChatCommand command in GetAllChatCommands())
                    ChatCommandService.Register(command);
                _commandsRegistered = true;

                ChatCommandService.Init();
            }

            //MultiplayerActive is false when initializing host... extreamly weird
            if (MyAPIGateway.Multiplayer.MultiplayerActive || ServerCfg != null) //only need this in mp
            {
                MyAPIGateway.Session.OnSessionReady += Session_OnSessionReady;
                Logger.Debug("Attach Session_OnSessionReady");
                if (ServerCfg == null) // if the config is already present, the messagehandler is also already registered
                {
                    MyAPIGateway.Multiplayer.RegisterMessageHandler(ConnectionHelper.ConnectionId, MessageHandler);
                    Logger.Debug("Registered ProcessMessage");
                }
                ConnectionHelper.Client_MessageCache.Clear();
                BlockCommandExecution = true;
                PermissionRequestTimer = new Timer(10000);
                PermissionRequestTimer.Elapsed += PermissionRequestTimer_Elapsed;
                PermissionRequestTimer.Start();
                // tell the server that we need everything now, permissions, protection, etc.
                ConnectionHelper.SendMessageToServer(new MessageConnectionRequest());
            }
        }

        /// <summary>
        /// Server side initialization.
        /// </summary>
        private void InitServer()
        {
            //Debug = true;
            _isInitialized = true; // Set this first to block any other calls from UpdateBeforeSimulation().
            Logger.Init();
            Logger.Debug("Server Logger started");

            // TODO: restructure the ChatCommandLogic to encapsulate ChatCommandService on the server side.
            // Required to check security for user calls on the Server side, and call the UpdateBeforeSimulation...() methods for each command.
            if (!_commandsRegistered)
            {
                foreach (ChatCommand command in GetAllChatCommands())
                    ChatCommandService.Register(command);
                _commandsRegistered = true;

                ChatCommandService.Init();
            }

            AdminNotificator.Init();
            ProtectionHandler.Init();
            MyAPIGateway.Multiplayer.RegisterMessageHandler(ConnectionHelper.ConnectionId, MessageHandler);
            Logger.Debug("Registered ProcessMessage");

            ConnectionHelper.Server_MessageCache.Clear();

            ServerCfg = new ServerConfig(GetAllChatCommands());
        }

        private List<ChatCommand> GetAllChatCommands()
        {
            // This will populate the _oreNames, _ingotNames, ready for the ChatCommands.
            BuildResourceLookups();

            List<ChatCommand> commands = new List<ChatCommand>();
            // New command classes must be added in here.

            //commands.Add(new CommandAsteroidFindOre(_oreNames));
            commands.Add(new CommandAsteroidScanOre(_oreNames));
            //commands.Add(new CommandAsteroidEditClear());
            //commands.Add(new CommandAsteroidEditSet());
            commands.Add(new CommandAsteroidFill());
            commands.Add(new CommandAsteroidReplace());
            commands.Add(new CommandAsteroidCreate());
            commands.Add(new CommandAsteroidCreateSphere());
            commands.Add(new CommandAsteroidsList());
            commands.Add(new CommandPlanetsList());
            commands.Add(new CommandAsteroidRotate());
            //commands.Add(new CommandAsteroidSpread()); //not working
            commands.Add(new CommandChatHistory());
            commands.Add(new CommandVoxelAdd());
            commands.Add(new CommandVoxelsList());
            commands.Add(new CommandConfig());
            commands.Add(new CommandDate());
            commands.Add(new CommandExtendedListShips());
            commands.Add(new CommandFactionDemote());
            commands.Add(new CommandFactionJoin());
            commands.Add(new CommandFactionKick());
            commands.Add(new CommandFactionPromote());
            commands.Add(new CommandFactionRemove());
            commands.Add(new CommandForceBan());
            commands.Add(new CommandForceKick());
            commands.Add(new CommandFlyTo());
            commands.Add(new CommandGameName());
            commands.Add(new CommandGodMode());
            commands.Add(new CommandHeading());
            commands.Add(new CommandHelloWorld());
            commands.Add(new CommandLaserRangefinder());
            commands.Add(new CommandSettings());
            commands.Add(new CommandHelp());
            commands.Add(new CommandIdentify());
            commands.Add(new CommandDetail());
            commands.Add(new CommandInventoryAdd(_oreNames, _ingotNames.ToArray(), _physicalItems));
            commands.Add(new CommandInventoryInsert(_oreNames, _ingotNames.ToArray(), _physicalItems));
            commands.Add(new CommandInventoryClear());
            commands.Add(new CommandInventoryDrop(_oreNames, _ingotNames.ToArray(), _physicalItems));
            commands.Add(new CommandListBots());
            //commands.Add(new CommandListBlueprints()); // no API currently.
            commands.Add(new CommandListPrefabs());
            commands.Add(new CommandListShips());
            commands.Add(new CommandMessageOfTheDay());
            commands.Add(new CommandBomb());
            commands.Add(new CommandInvisible());
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
            commands.Add(new CommandProtectionArea());
            commands.Add(new CommandSaveGame());
            commands.Add(new CommandSessionCargoShips());
            commands.Add(new CommandSessionCopyPaste());
            commands.Add(new CommandSessionCreative());
            commands.Add(new CommandSessionSpectator());
            commands.Add(new CommandSessionWeapons());
            commands.Add(new CommandSetVector());
            commands.Add(new CommandSpeed());
            commands.Add(new CommandShipOff());
            commands.Add(new CommandShipOn());
            commands.Add(new CommandShipSwitch());
            commands.Add(new CommandShipOwnerClaim());
            commands.Add(new CommandShipOwnerRevoke());
            commands.Add(new CommandShipDelete());
            commands.Add(new CommandShipRepair());
            commands.Add(new CommandShipDestructible());
            commands.Add(new CommandShipScaleDown());
            commands.Add(new CommandShipScaleUp());
            commands.Add(new CommandShipOwnerShare());
            commands.Add(new CommandShipCubeRename());
            commands.Add(new CommandShipCubeRenumber());
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

            // servers: listen and dedicated, MP
            if (ServerCfg != null)
            {
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(ConnectionHelper.ConnectionId, MessageHandler);
                ProtectionHandler.Close();
                Logger.Debug("Unregistered MessageHandler");
            }

            if (MyAPIGateway.Utilities != null && MyAPIGateway.Multiplayer != null && MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Utilities.IsDedicated)
                return;

            if (MyAPIGateway.Multiplayer != null &&  MyAPIGateway.Multiplayer.MultiplayerActive || (ServerCfg != null && ServerConfig.ServerIsClient))
            {
                // all clients, including hosts, MP
                if (PermissionRequestTimer != null)
                {
                    PermissionRequestTimer.Stop();
                    PermissionRequestTimer.Close();
                }
                MyAPIGateway.Session.OnSessionReady -= Session_OnSessionReady;
                Logger.Debug("Detached Session_OnSessionReady");
                
                // only clients, not the host
                if (ServerCfg == null)
                {
                    MyAPIGateway.Multiplayer.UnregisterMessageHandler(ConnectionHelper.ConnectionId, MessageHandler);
                    Logger.Debug("Unregistered MessageHandler");
                }
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

        void PermissionRequestTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _permissionRequest = true;
        }

        #endregion

        #region message processing

        private void Utilities_MessageEntered(string messageText, ref bool sendToOthers)
        {
            if (ChatCommandService.ProcessClientMessage(messageText))
                sendToOthers = false;
            else
            {
                var globalMessage = new MessageGlobalMessage() { 
                    ChatMessage = new ChatMessage() { 
                        Text = messageText,
                        Sender = new Player() {
                            SteamId = MyAPIGateway.Session.Player.SteamUserId,
                            PlayerName = MyAPIGateway.Session.Player.DisplayName
                        },
                        Date = DateTime.Now
                    }
                };
                ConnectionHelper.SendMessageToServer(globalMessage);
            }
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

        void Session_OnSessionReady()
        {
            if (CommandMessageOfTheDay.Received && !String.IsNullOrEmpty(CommandMessageOfTheDay.Content))
                CommandMessageOfTheDay.ShowMotd();

            if (AdminNotification != null)
                AdminNotification.Show();

            ShowDialogsOnReceive = true;
        }

        #region connection handling

        private static void HandleMessage(byte[] message)
        {
            Logger.Debug("-- HandleMessage: --");
            Logger.Debug("--------------------");
            Logger.Debug(string.Format("{0}", System.Text.Encoding.Unicode.GetString(message)));
            Logger.Debug("--------------------");
            ConnectionHelper.ProcessData(message);
        }

        #endregion
    }
}