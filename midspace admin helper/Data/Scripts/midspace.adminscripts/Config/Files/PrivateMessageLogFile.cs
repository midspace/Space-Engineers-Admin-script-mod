using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;

namespace midspace.adminscripts.Config.Files
{
    public class PrivateMessageLogFile : FileBase
    {
        private const string Format = "PrivateMessageLog_{0}.xml";

        public List<PrivateConversation> PrivateConversations { get; set; }

        public PrivateMessageLogFile(string fileName)
            : base(fileName, Format) { }

        public override void Save(string customSaveName = null)
        {
            var fileName = !string.IsNullOrEmpty(customSaveName) ? string.Format(Format, customSaveName) : Name;

            TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(fileName, typeof(ServerConfig));
            writer.Write(MyAPIGateway.Utilities.SerializeToXML(PrivateConversations));
            writer.Flush();
            writer.Close();
        }

        public override void Load()
        {
            TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(Name, typeof(ServerConfig));
            var text = reader.ReadToEnd();
            reader.Close();

            PrivateConversations = MyAPIGateway.Utilities.SerializeFromXML<List<PrivateConversation>>(text);
        }

        public override void Create()
        {
            PrivateConversations = new List<PrivateConversation>();
        }
    }
}