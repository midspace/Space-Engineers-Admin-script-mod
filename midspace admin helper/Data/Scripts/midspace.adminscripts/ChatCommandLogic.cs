namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Timers;

    using Sandbox.Common.Localization;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Definitions;
    using Sandbox.ModAPI;

    /// <summary>
    /// Adds special chat commands, allowing the player to get their position, date, time, change their location on the map.
    /// Author: Midspace. AKA Screaming Angels.
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
        #region fields

        private bool _isInitilized;
        private Timer _timer;
        private bool _1000MsTimerElapsed;
        private static string[] _oreNames;
        private static List<string> _ingotNames;
        private static Dictionary<MyTextsWrapperEnum, string> _resouceLookup;
        private static MyPhysicalItemDefinition[] _physicalItems;

        #endregion

        #region attaching events and wiring up

        public override void UpdateBeforeSimulation()
        {
            // This needs to wait until the MyAPIGateway.Session.Player is created, as running on a Dedicated server can cause issues.
            // It would be nicer to just read a property that indicates this is a dedicated server, and simply return.
            if (!_isInitilized && MyAPIGateway.Session != null && MyAPIGateway.Session.Player != null)
                Init();

            base.UpdateBeforeSimulation();

            if (MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Multiplayer.IsServer)
                return;

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
            base.UnloadData();
        }

        #endregion

        private void Init()
        {
            _isInitilized = true; // Set this first to block any other calls from UpdateBeforeSimulation().
            MyAPIGateway.Utilities.MessageEntered += Utilities_MessageEntered;

            // This will populate the _oreNames, _ingotNames, ready for the ChatCommands.
            BuildResourceLookups();

            // New command classes must be added in here.

            ChatCommandService.Register(new CommandAddPrefab());
            ChatCommandService.Register(new CommandAddWireframe());
            //ChatCommandService.Register(new CommandAddVoxel());  //not working any more
            ChatCommandService.Register(new CommandCountObjects());
            ChatCommandService.Register(new CommandClaim());
            ChatCommandService.Register(new CommandDate());
            ChatCommandService.Register(new CommandEject());
            ChatCommandService.Register(new CommandFactionDemote());
            ChatCommandService.Register(new CommandFactionJoin());
            ChatCommandService.Register(new CommandFactionKick());
            ChatCommandService.Register(new CommandFactionRemove());
            ChatCommandService.Register(new CommandFactionPromote());
            //ChatCommandService.Register(new CommandFindOre(_oreNames));
            ChatCommandService.Register(new CommandFlyTo());
            ChatCommandService.Register(new CommandGameName());
            ChatCommandService.Register(new CommandHeading());
            ChatCommandService.Register(new CommandHelloWorld());
            ChatCommandService.Register(new CommandHelp());
            ChatCommandService.Register(new CommandIdentify());
            ChatCommandService.Register(new CommandInventoryAdd(_oreNames, _ingotNames.ToArray(), _physicalItems, _resouceLookup));
            ChatCommandService.Register(new CommandInventoryDrop(_oreNames, _ingotNames.ToArray(), _physicalItems, _resouceLookup));
            ChatCommandService.Register(new CommandInventoryClear());
            ChatCommandService.Register(new CommandListAsteroids());
            //ChatCommandService.Register(new CommandListBlueprints()); // no API currently.
            ChatCommandService.Register(new CommandListBots());
            ChatCommandService.Register(new CommandListPrefabs());
            ChatCommandService.Register(new CommandListShips());
            ChatCommandService.Register(new CommandListShips2());
            //ChatCommandService.Register(new CommandListVoxels()); //not working any more
            ChatCommandService.Register(new CommandMeteor(_oreNames));
            ChatCommandService.Register(new CommandOff());
            ChatCommandService.Register(new CommandOn());
            ChatCommandService.Register(new CommandPastePrefab());
            ChatCommandService.Register(new CommandPullObjects());
            ChatCommandService.Register(new CommandPosition());
            //ChatCommandService.Register(new CommandRespawn());  //not working any more
            ChatCommandService.Register(new CommandRevoke());
            //ChatCommandService.Register(new CommandRotateAsteroid());  //not working any more
            ChatCommandService.Register(new CommandSaveGame());
            ChatCommandService.Register(new CommandSetVector());
            //ChatCommandService.Register(new CommandShare());  //not working
            ChatCommandService.Register(new CommandSlay());
            ChatCommandService.Register(new CommandSmite(_oreNames));
            //ChatCommandService.Register(new CommandSpreadAsteroids());  //not working
            ChatCommandService.Register(new CommandStatus());
            ChatCommandService.Register(new CommandStop());
            ChatCommandService.Register(new CommandTeleport());
            ChatCommandService.Register(new CommandTeleportDelete());
            ChatCommandService.Register(new CommandTeleportFavorite());
            ChatCommandService.Register(new CommandTeleportJump());
            ChatCommandService.Register(new CommandTeleportOffset());
            ChatCommandService.Register(new CommandTeleportList());
            ChatCommandService.Register(new CommandTeleportSave());
            ChatCommandService.Register(new CommandTeleportToPlayer());
            ChatCommandService.Register(new CommandTeleportToShip());
            ChatCommandService.Register(new CommandTest(_resouceLookup));
            ChatCommandService.Register(new CommandTime());
            ChatCommandService.Register(new CommandVersion());
            ChatCommandService.Register(new CommandVoxelClear());
            ChatCommandService.Register(new CommandVoxelSet());

            // Futher ideas:
            // Delete <ship> <floatingobjects>
            // /svflyto <#> <X> <Y> <Z> <Speed> : Sets ship <#> to vector <X> <Y> <Z> and to fly thare at <Speed>


            _timer = new Timer(1000);
            _timer.Elapsed += TimerOnElapsed;
            _timer.Start();
            // Attach any other events here.

            ChatCommandService.Init();
        }

        #region detaching events

        private void DetachEvents()
        {
            if (MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Multiplayer.IsServer)
                return;

            MyAPIGateway.Utilities.MessageEntered -= Utilities_MessageEntered;

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Elapsed -= TimerOnElapsed;
            }
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            // Run timed events that do not affect Threading in game.
            _1000MsTimerElapsed = true;

            // DO NOT SET ANY IN GAME API CALLS HERE. AT ALL!
        }

        #endregion

        #region message processing

        private void Utilities_MessageEntered(string messageText, ref bool sendToOthers)
        {
            if (ChatCommandService.ProcessMessage(messageText))
            {
                sendToOthers = false;
            }
        }

        #endregion

        #region helpers

        private static void BuildResourceLookups()
        {
            _resouceLookup = new Dictionary<MyTextsWrapperEnum, string>();
            var textEnums = (MyTextsWrapperEnum[])Enum.GetValues(typeof(MyTextsWrapperEnum));

            foreach (var textEnum in textEnums)
            {
                // This will be fixed against the current Localization Sandbox.Common.Localization.MyTextsWrapper.Culture.
                var value = MyTextsWrapper.GetString(textEnum);
                _resouceLookup.Add(textEnum, value);
            }

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
    }
}