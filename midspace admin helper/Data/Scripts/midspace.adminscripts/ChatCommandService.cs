namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Sandbox.ModAPI;
    using Sandbox.Common.ObjectBuilders;

    /// <summary>
    /// The Chat command service does most of the heavy work in organising and processing the ChatCommands.
    /// </summary>
    internal static class ChatCommandService
    {
        #region fields and properties

        private static readonly Dictionary<string, ChatCommand> Commands = new Dictionary<string, ChatCommand>();
        private static uint _userSecurity;
        private static bool _isInitialized;

        public static uint UserSecurity
        {
            get { return _userSecurity; }
            set { _userSecurity = value; }
        }

        #endregion

        #region methods

        /// <summary>
        /// Initilizes the Service, fetching the security level of the user, and 
        /// instructing the ChatCommandLogic that it is ready to process chat commands.
        /// </summary>
        public static void Init()
        {
            var session = MyAPIGateway.Session;
            _userSecurity = ChatCommandSecurity.User;

            //only set this in sp, in mp we need to wait until the server sends us our level. On LS this will be read in during the creation of the server cfg.
            if (session.Player.IsAdmin() && session.SessionSettings.OnlineMode.Equals(MyOnlineModeEnum.OFFLINE))
                _userSecurity = ChatCommandSecurity.Admin;

            _isInitialized = true;
        }

        /// <summary>
        /// Register the specified ChatCommand.
        /// Commands can only be registered once.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="chatCommand"></param>
        public static void Register<T>(T chatCommand) where T : ChatCommand
        {
            // An exception is thrown in here at this time on purpose.
            // As this will occur during the loading of the World, instead of during gameplay.
            // This is to prevent coders to adding duplicate ChatCommands.

            if (Commands.Any(c => c.GetType() == typeof(T)) || Commands.ContainsKey(chatCommand.Name))
                throw new Exception(string.Format("ChatCommand Type {0} is already registered", typeof(T)));

            foreach (string command in chatCommand.Commands)
                if (Commands.Any(pair => pair.Value.Commands.Any(s => s.Equals(command))))
                    throw new Exception(string.Format("ChatCommand '{0}' already registered", command));

            Commands.Add(chatCommand.Name, chatCommand);

        }

        /// <summary>
        /// Returns the list of chat commands that the only a person with standard User security can use.
        /// </summary>
        /// <returns></returns>
        public static string[] GetUserListCommands()
        {
            return Commands.Where(c => HasRight(c.Value) && c.Value.Security == ChatCommandSecurity.User).Select(c => c.Key).ToArray();
        }

        /// <summary>
        /// Returns the list of chat commands that this user can use.
        /// </summary>
        /// <returns></returns>
        public static string[] GetListCommands()
        {
            return Commands.Where(c => HasRight(c.Value)).Select(c => c.Key).ToArray();
        }

        public static string[] GetNonUserListCommands()
        {
            return Commands.Where(c => HasRight(c.Value) && c.Value.Security > ChatCommandSecurity.User).Select(c => c.Key).ToArray();
        }

        public static bool Help(string commandName, bool brief)
        {
            foreach (var command in Commands.Where(command => HasRight(command.Value) && command.Key.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)))
            {
                command.Value.Help(brief);
                return true;
            }

            return false;
        }

        /// <summary>
        /// This will use _commandShortcuts dictionary to only run the specific ChatCommands that has the specified command text registered.
        /// </summary>
        /// <param name="messageText"></param>
        /// <returns>Returns true if a valid command was found and successfuly invoked.</returns>
        public static bool ProcessMessage(string messageText)
        {
            if (!_isInitialized || string.IsNullOrEmpty(messageText))
                return false;

            var commands = messageText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (commands.Length == 0)
                return false;


            var comandList = Commands.Where(k => k.Value.Commands.Any(a => a.Equals(commands[0], StringComparison.InvariantCultureIgnoreCase)));
            foreach (var command in comandList)
            {
                if (ChatCommandLogic.Instance.BlockCommandExecution)
                {
                    MyAPIGateway.Utilities.ShowMessage("Permission", "Loading permissions... Please try again later.");
                    return true;
                }

                if (!HasRight(command.Value))
                {
                    MyAPIGateway.Utilities.ShowMessage("Permission", "You do not have the permission to use this command.");
                    return true;
                }

                try
                {
                    if (command.Value.Invoke(messageText))
                        return true;
                    else
                    {
                        MyAPIGateway.Utilities.ShowMessage("Command failed", string.Format("Execution of command {0} failed. Use '/help {0}' for receiving a detailed instruction.", command.Value.Name));
                        command.Value.Help(true);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // Exception handling to prevent any crash in the ChatCommand's reaching the user.
                    // Additional information for developers
                    if (MyAPIGateway.Session.Player.IsExperimentalCreator())
                    {
                        MyAPIGateway.Utilities.ShowMissionScreen(string.Format("Error in {0}", command.Value.Name), "Input: ", messageText, ex.ToString(), null, null);
                        continue;
                    }

                    var message = ex.Message.Replace("\r", " ").Replace("\n", " ");
                    message = message.Substring(0, Math.Min(message.Length, 50));
                    MyAPIGateway.Utilities.ShowMessage("Error", String.Format("Occured attempting to run {0}.\r\n{1}", command, message));
                }
            }

            return false;
        }

        public static void UpdateBeforeSimulation()
        {
            if (!_isInitialized)
                return;

            foreach (var command in Commands.Where(command => HasRight(command.Value)))
            {
                command.Value.UpdateBeforeSimulation();
            }
        }

        public static void UpdateBeforeSimulation100()
        {
            if (!_isInitialized)
                return;

            foreach (var command in Commands.Where(command => HasRight(command.Value)))
            {
                command.Value.UpdateBeforeSimulation100();
            }
        }

        public static void UpdateBeforeSimulation1000()
        {
            if (!_isInitialized)
                return;

            foreach (var command in Commands.Where(command => HasRight(command.Value)))
            {
                command.Value.UpdateBeforeSimulation1000();
            }
        }

        public static bool HasRight(ChatCommand command)
        {
            if (command.HasFlag(ChatCommandFlag.Experimental))
                return MyAPIGateway.Session.Player.IsExperimentalCreator() && command.Security <= _userSecurity;

            return command.Security <= _userSecurity;
        }

        public static bool UpdateCommandSecurity(CommandStruct command)
        {
            if (!Commands.ContainsKey(command.Name))
                return false;

            Commands[command.Name].Security = command.NeededLevel;

            return true;
        }

        public static bool IsCommandRegistered(string commandName)
        {
            return Commands.Any(pair => pair.Value.Name.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));
        }

        #endregion
    }
}
