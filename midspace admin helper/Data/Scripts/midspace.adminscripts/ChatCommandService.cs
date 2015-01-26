namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Sandbox.ModAPI;

    /// <summary>
    /// The Chat command service does most of the heavy work in organising and processing the ChatCommands.
    /// </summary>
    internal static class ChatCommandService
    {
        #region fields and properties

        private static readonly Dictionary<string, ChatCommand> CommandShortcuts = new Dictionary<string, ChatCommand>();
        private static readonly HashSet<ChatCommand> Commands = new HashSet<ChatCommand>();
        private static ChatCommandSecurity _userSecurity;
        private static bool _isInitialized;

        public static ChatCommandSecurity UserSecurity
        {
            get { return _userSecurity; }
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

            if (session.Player.IsAdmin())
                _userSecurity |= ChatCommandSecurity.Admin;
            if (session.Player.IsExperimentalCreator())
                _userSecurity |= ChatCommandSecurity.Experimental;

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

            if (Commands.Any(c => c.GetType() == typeof(T)))
                throw new Exception(string.Format("ChatCommand Type {0} is already registered", typeof(T)));

            Commands.Add(chatCommand);
            foreach (var command in chatCommand.Commands)
            {
                if (CommandShortcuts.Any(c => c.Key.Equals(command, StringComparison.InvariantCultureIgnoreCase)))
                    throw new Exception(string.Format("ChatCommand '{0}' already registered", command));

                CommandShortcuts.Add(command, chatCommand);
            }
        }

        /// <summary>
        /// Returns the list of chat commands that the only a person with standard User security can use.
        /// </summary>
        /// <returns></returns>
        public static string[] GetUserListCommands()
        {
            return Commands.Where(c => (c.Security & ChatCommandSecurity.User) != ChatCommandSecurity.None).Select(c => c.Name).ToArray();
        }

        /// <summary>
        /// Returns the list of chat commands that this user can use.
        /// </summary>
        /// <returns></returns>
        public static string[] GetListCommands()
        {
            return Commands.Where(c => (c.Security & _userSecurity) != ChatCommandSecurity.None).Select(c => c.Name).ToArray();
        }

        public static string[] GetNonUserListCommands()
        {
            return Commands.Where(c => (c.Security ^ _userSecurity) != ChatCommandSecurity.None && (c.Security & _userSecurity) != ChatCommandSecurity.User).Select(c => c.Name).ToArray();
        }

        public static bool Help(string commandName)
        {
            foreach (var command in Commands.Where(command => (command.Security & _userSecurity) != ChatCommandSecurity.None && command.Name.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)))
            {
                command.Help();
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

            var comandList = CommandShortcuts.Where(k => k.Key.Equals(commands[0], StringComparison.InvariantCultureIgnoreCase));
            foreach (var command in comandList.Where(command => (command.Value.Security & _userSecurity) != ChatCommandSecurity.None))
            {
                try
                {
                    if (command.Value.Invoke(messageText))
                        return true;
                }
                catch (Exception ex)
                {
                    // Exception handling to prevent any crash in the ChatCommand's reaching the user.
                    // Additional information for developers
                    if ((ChatCommandSecurity.Experimental & _userSecurity) != ChatCommandSecurity.None)
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

            foreach (var command in Commands.Where(command => (command.Security & _userSecurity) != ChatCommandSecurity.None))
            {
                command.UpdateBeforeSimulation();
            }
        }

        public static void UpdateBeforeSimulation1000()
        {
            if (!_isInitialized)
                return;

            foreach (var command in Commands.Where(command => (command.Security & _userSecurity) != ChatCommandSecurity.None))
            {
                command.UpdateBeforeSimulation1000();
            }
        }

        #endregion
    }
}
