using System;
using System.IO;
using midspace.adminscripts.Messages;
using Sandbox.ModAPI;

namespace midspace.adminscripts.Config.Files
{
    public class ServerConfigFile : FileBase
    {
        private const string Format = "Config_{0}.cfg";

        public ServerConfigurationStruct Config { get; set; }

        public ServerConfigFile(string fileName)
            : base(fileName, Format) { }

        public override void Save(string customSaveName = null)
        {
            string fileName;

            if (!string.IsNullOrEmpty(customSaveName))
                fileName = string.Format(Format, customSaveName);
            else
                fileName = Name;

            TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(fileName, typeof(ServerConfig));
            writer.Write(MyAPIGateway.Utilities.SerializeToXML(Config));
            writer.Flush();
            writer.Close();
        }

        public override void Load()
        {
            Config = new ServerConfigurationStruct();

            TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(Name, typeof(ServerConfig));
            var xmlText = reader.ReadToEnd();
            reader.Close();

            if (string.IsNullOrWhiteSpace(xmlText))
                return;

            try
            {
                Config = MyAPIGateway.Utilities.SerializeFromXML<ServerConfigurationStruct>(xmlText);
            }
            catch (Exception ex)
            {
                AdminNotification notification = new AdminNotification()
                {
                    Date = DateTime.Now,
                    Content = string.Format(@"There is an error in the config file. It couldn't be read. The server was started with default settings.

Message:
{0}

If you can't find the error, simply delete the file. The server will create a new one with default settings on restart.", ex.Message)
                };

                AdminNotificator.StoreAndNotify(notification);
            }

            if (Config == null)
                Config = new ServerConfigurationStruct();

            var sendLogPms = Config.LogPrivateMessages != CommandPrivateMessage.LogPrivateMessages;
            CommandPrivateMessage.LogPrivateMessages = Config.LogPrivateMessages;
            if (sendLogPms)
                ConnectionHelper.SendMessageToAllPlayers(new MessageConfig()
                {
                    Config = new ServerConfigurationStruct()
                    {
                        LogPrivateMessages = CommandPrivateMessage.LogPrivateMessages
                    },
                    Action = ConfigAction.LogPrivateMessages
                });

            Config.MotdFileSuffix = Config.MotdFileSuffix.ReplaceForbiddenChars();
        }

        public override void Create()
        {
            Config = new ServerConfigurationStruct();
            Save();
        }
    }
}