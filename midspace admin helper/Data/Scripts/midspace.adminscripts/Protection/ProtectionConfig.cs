using System;
using System.Collections.Generic;
using System.IO;
using ProtoBuf;
using Sandbox.ModAPI;

namespace midspace.adminscripts.Protection
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class ProtectionConfig
    {
        [ProtoMember(1)] public List<ProtectionArea> Areas = new List<ProtectionArea>();
        [ProtoMember(2)] public bool ProtectionEnabled;
        [ProtoMember(3)] public bool ProtectionInverted;

        private static string _fileName;

        private const string FileNameFormat = "Areas_{0}.xml";

        public ProtectionConfig()
        {
            _fileName = String.Format(FileNameFormat, Path.GetFileNameWithoutExtension(MyAPIGateway.Session.CurrentPath));
        }

        public void Save(string customSaveName = null)
        {
            string fileName;

            if (!string.IsNullOrEmpty(customSaveName))
                fileName = String.Format(FileNameFormat, customSaveName);
            else
                fileName = _fileName;

            TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(fileName, typeof (ServerConfig));
            writer.Write(MyAPIGateway.Utilities.SerializeToXML(this));
            writer.Flush();
            writer.Close();
            Logger.Debug("Saved protection.");
        }

        public void Load()
        {
            if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(_fileName, typeof(ServerConfig)))
                return;

            TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(_fileName, typeof(ServerConfig));
            var text = reader.ReadToEnd();
            reader.Close();

            var cfg = MyAPIGateway.Utilities.SerializeFromXML<ProtectionConfig>(text);

            Areas = cfg.Areas;
            ProtectionEnabled = cfg.ProtectionEnabled;
            ProtectionInverted = cfg.ProtectionInverted;

            Logger.Debug("Protection loaded.");
        }
    }
}