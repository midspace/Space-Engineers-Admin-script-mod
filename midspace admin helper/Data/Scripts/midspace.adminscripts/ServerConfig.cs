using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace midspace.adminscripts
{
    public class ServerConfig
    {
        /// <summary>
        /// The format of the config file name.
        /// </summary>
        private const string FileNameFormat = "Config_{0}.cfg";

        private string FileName;
        private StringBuilder Content = new StringBuilder();

        /// <summary>
        /// The suffix for the motd file. For a better identification.
        /// </summary>
        public string MotdFileSuffix = MyAPIGateway.Session.Name;
        public string MotdHeadLine;
        
        /// <summary>
        /// The permission string. No need to initialize the permissions on the server since it is transmitted as a string anyway.
        /// </summary>
        public string CommandPermissions;

        public ServerConfig()
        {
            LoadOrCreateConfig();
        }

        private void LoadOrCreateConfig()
        {
            FileName = string.Format(FileNameFormat, MyAPIGateway.Session.WorldID);

            if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(FileName, typeof(ServerConfig)))
                CreateConfig();

            TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(FileName, typeof(ServerConfig));
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                Content.Append(line);
                //comment function, empty or lines without key-value pair
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//") || !line.Contains('=')) 
                    continue;

                Evaluate(line);
            }
        }

        private void CreateConfig()
        {
            TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(FileName, typeof(ServerConfig));
            //general description
            writer.WriteLine(string.Format("//This config file originally refers to the savegame located in '{0}'", MyAPIGateway.Utilities.ConfigDedicated.LoadWorld));
            writer.WriteLine("//This file contains settings for the Admin helper commands. There are several keys and values below. You recognize them by the '=' after a certain keyword.");
            writer.WriteLine("//Example: There is 'motdheadline='. 'motdheadline' is the key and whatever you write after the '=' will be the value as long as it is in the same line.");
            writer.WriteLine("//The end of the line represents the end of the value. You also can write comments by putting '//' in front of the line. It only works for the whole line.");
            writer.WriteLine("//Example: 'motdheadline=Welcome!//this is not a comment'. In this case the value is 'Welcome!//this is not a comment' and not 'Welcome!'. Be careful!");
            writer.WriteLine("//The comment function is for you to have a better overview anyway. There won't be any malfunctions if you just type text in this file, unless you use certain key words.");
            writer.WriteLine("//It is better to use comments. Believe me.");
            writer.WriteLine("");
            //message of the day suffix
            writer.WriteLine("//The setting below refers to the 'message of the day' file that will be loaded for this server. It will be named 'Motd_<Suffix>.cfg' while '<Suffix>' is the name you have set.");
            writer.WriteLine("//By default it is the name of your world. If you change it and not file can be found with that suffix, a new file will be created.");
            writer.WriteLine(string.Format("motdsuffix={0}", MyAPIGateway.Utilities.ConfigDedicated.WorldName));
            writer.WriteLine("");
            //message of the day headline
            writer.WriteLine("//With this setting you can specify what the header of your message of the day says.");
            writer.WriteLine("//I don't recommand unsing more than about 40 characters.");
            writer.WriteLine("//Since the textfield is not unlimited, depending on the size and number of the characters, they will overflow left and right if you use too many.");
            writer.WriteLine("motdheadline=");
            writer.WriteLine("");
            //command permissions
            /*writer.WriteLine("//The following setting can change the needed permission to use a command. Simply use the following form: commandname:permissionlevel. Example: 'tp:user' (without the 's).");
            writer.WriteLine("//To set the permissions for more than one command, simply separate them with a comma (,). Example: 'tp:user, help:admin' (without the 's).");
            writer.WriteLine("//The names of the commands are the same as those appearing in the help list.");
            writer.WriteLine("//There are two permission levels available by now: 'user' and 'admin'. 'User' will allow anyone to use this command, 'Admin' will allow nobody but the admins to use it.");
            writer.WriteLine("// The order is irrelevant unless you set the permission for a command twice. If you do, the last one will be valid. ");
            writer.WriteLine("cmdperm=");*/
            writer.Flush();
            writer.Close();
        }

        private void Evaluate(string pair)
        {
            //we do not use '=' in keys, values can contain anything
            var key = pair.Substring(0, pair.IndexOf('='));
            var value = pair.Substring(pair.IndexOf('=') + 1);

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                return;

            switch (key)
            {
                case "motdsuffix":
                    MotdFileSuffix = value;
                    break;
                case "motdheadline":
                    CommandMessageOfTheDay.HeadLine = value;
                    break;
                case "cmdperm":
                    //CommandPermissions = value.Trim();
                    break;
            }
        }
    }
}
