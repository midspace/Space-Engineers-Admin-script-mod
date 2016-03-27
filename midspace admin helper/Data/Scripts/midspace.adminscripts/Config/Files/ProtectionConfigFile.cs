using System;
using System.IO;
using System.Security.Policy;
using midspace.adminscripts.Protection;
using Sandbox.ModAPI;

namespace midspace.adminscripts.Config.Files
{
    public class ProtectionConfigFile : FileBase
    {
        public ProtectionConfig Config { get; set; }

        private const string Format = "Areas_{0}.xml";

        public ProtectionConfigFile(string fileName) 
          : base(fileName, Format) { }

        public override void Save(string customSaveName = null)
        {
            string fileName;

            if (!string.IsNullOrEmpty(customSaveName))
                fileName = String.Format(Format, customSaveName);
            else
                fileName = Name;

            TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(fileName, typeof(ServerConfig));
            writer.Write(MyAPIGateway.Utilities.SerializeToXML(Config));
            writer.Flush();
            writer.Close();
            Logger.Debug("Saved protection.");
        }

        public override void Load()
        {
            TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(Name, typeof(ServerConfig));
            var text = reader.ReadToEnd();
            reader.Close();

            Config = new ProtectionConfig();
            
            try
            {
                Config = MyAPIGateway.Utilities.SerializeFromXML<ProtectionConfig>(text);
            }
            catch
            {
                Logger.Debug("Protection was corrupt and will not be loaded.");
                return;
            }

            Logger.Debug("Protection loaded.");
        }

        public override void Create()
        {
            Config = new ProtectionConfig();
        }
    }
}