using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sandbox.ModAPI;

namespace midspace.adminscripts.Config.Files
{
    public class PermissionsFile : FileBase
    {
        private const string Format = "Permissions_{0}.xml";
        private readonly List<ChatCommand> _chatCommands; 

        public Permissions Permissions 
        {
            get { return _permissions; }
        }

        private Permissions _permissions;

        public PermissionsFile(string fileName, List<ChatCommand> commands)
        { 
            Name = string.Format(Format, fileName);
            _chatCommands = commands;
            Init();
        }

        public override void Save(string customSaveName = null)
        {
            string fileName;

            if (!string.IsNullOrEmpty(customSaveName))
                fileName = String.Format(Format, customSaveName);
            else
                fileName = Name;

            TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(fileName, typeof(ServerConfig));
            writer.Write(MyAPIGateway.Utilities.SerializeToXML(_permissions));
            writer.Flush();
            writer.Close();
        }

        public override void Load()
        {
            TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(Name, typeof(ServerConfig));
            var text = reader.ReadToEnd();
            reader.Close();

            _permissions = new Permissions
            {
                Commands = new List<CommandStruct>(),
                Groups = new List<PermissionGroup>(),
                Players = new List<PlayerPermission>()
            };

            if (!string.IsNullOrEmpty(text))
            {
                try
                {
                    _permissions = MyAPIGateway.Utilities.SerializeFromXML<Permissions>(text);
                }
                catch (Exception ex)
                {
                    AdminNotification notification = new AdminNotification()
                    {
                        Date = DateTime.Now,
                        Content = string.Format(@"There is an error in the _permissions file. It couldn't be read. The server was started with default _permissions.

Message:
{0}

If you can't find the error, simply delete the file. The server will create a new one with default settings on restart.", ex.Message)
                    };

                    AdminNotificator.StoreAndNotify(notification);
                }
            }

            //create a copy of the commands in the file
            var invalidCommands = new List<CommandStruct>(_permissions.Commands);

            foreach (ChatCommand command in _chatCommands)
            {
                if (!_permissions.Commands.Any(c => c.Name.Equals(command.Name)))
                {
                    //add a command if it does not exist
                    _permissions.Commands.Add(new CommandStruct()
                    {
                        Name = command.Name,
                        NeededLevel = command.Security
                    });
                }
                else
                {
                    //remove all commands from the list, that are valid
                    invalidCommands.Remove(_permissions.Commands.First(c => c.Name.Equals(command.Name)));
                }
            }

            foreach (CommandStruct cmdStruct in invalidCommands)
            {
                // remove all invalid commands
                _permissions.Commands.Remove(cmdStruct);

                // clean up the player permissions
                var extentions = new List<PlayerPermission>(_permissions.Players.Where(p => p.Extensions.Any(c => c.Equals(cmdStruct.Name))));
                var restrictions = new List<PlayerPermission>(_permissions.Players.Where(p => p.Restrictions.Any(c => c.Equals(cmdStruct.Name))));

                foreach (PlayerPermission playerPermission in extentions)
                {
                    var i = _permissions.Players.IndexOf(playerPermission);
                    var player = _permissions.Players[i];
                    _permissions.Players.RemoveAt(i);
                    player.Extensions.Remove(cmdStruct.Name);
                    _permissions.Players.Insert(i, playerPermission);
                }

                foreach (PlayerPermission playerPermission in restrictions)
                {
                    var i = _permissions.Players.IndexOf(playerPermission);
                    var player = _permissions.Players[i];
                    _permissions.Players.RemoveAt(i);
                    player.Restrictions.Remove(cmdStruct.Name);
                    _permissions.Players.Insert(i, player);
                }

                // if the struct used an alias, we add it again properly while keeping the previous level
                // this might be because we changed the name of an command and keep the old as an alias to not confuse the users
                if (_chatCommands.Any(c => c.Commands.Any(s => s.Substring(1).Equals(cmdStruct.Name))))
                {
                    var command = _chatCommands.First(c => c.Commands.Any(s => s.Substring(1).Equals(cmdStruct.Name)));

                    // remove all commands with the same name as we might have added it already asuming it is new
                    _permissions.Commands.RemoveAll(c => c.Name.Equals(command.Name));

                    _permissions.Commands.Add(new CommandStruct()
                    {
                        Name = command.Name,
                        NeededLevel = cmdStruct.NeededLevel
                    });


                    foreach (PlayerPermission playerPermission in extentions)
                    {
                        var i = _permissions.Players.IndexOf(_permissions.Players.First(p => p.Player.SteamId == playerPermission.Player.SteamId));
                        var player = _permissions.Players[i];
                        _permissions.Players.RemoveAt(i);
                        player.Extensions.Add(command.Name);
                        _permissions.Players.Insert(i, player);
                    }

                    foreach (PlayerPermission playerPermission in restrictions)
                    {
                        var i = _permissions.Players.IndexOf(_permissions.Players.First(p => p.Player.SteamId == playerPermission.Player.SteamId));
                        var player = _permissions.Players[i];
                        _permissions.Players.RemoveAt(i);
                        player.Restrictions.Add(command.Name);
                        _permissions.Players.Insert(i, player);
                    }
                }
            }

            Logger.Debug("Permission File loaded {0} commands.", _permissions.Commands.Count);

            // for better readability we sort it, first by level then by name
            _permissions.Commands = new List<CommandStruct>(_permissions.Commands.OrderByDescending(c => c.NeededLevel).ThenBy(c => c.Name));
        }

        public override void Create()
        {
            _permissions = new Permissions
            {
                Commands = new List<CommandStruct>(),
                Groups = new List<PermissionGroup>(),
                Players = new List<PlayerPermission>()
            };

            foreach (ChatCommand command in _chatCommands)
            {
                _permissions.Commands.Add(new CommandStruct()
                {
                    Name = command.Name,
                    NeededLevel = command.Security
                });
            }
        }
    }
}