using System;
using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;
using VRage;

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
            using (ExecutionLock.AcquireExclusiveUsing())
            {
                var fileName = !string.IsNullOrEmpty(customSaveName) ? string.Format(Format, customSaveName) : Name;

                TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(fileName, typeof (ServerConfig));
                writer.Write(MyAPIGateway.Utilities.SerializeToXML(PrivateConversations));
                writer.Flush();
                writer.Close();
            }
        }

        public override void Load()
        {
            TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(Name, typeof(ServerConfig));
            var text = reader.ReadToEnd();
            reader.Close();

            try
            {
                PrivateConversations = MyAPIGateway.Utilities.SerializeFromXML<List<PrivateConversation>>(text);
            }
            catch (Exception ex)
            {
                var exception = new Exception(string.Format("An error occuring loading the file '{0}'. Begining with the text \"{1}\".", Name, text.Substring(0, Math.Min(text.Length, 100))), ex);
                AdminNotificator.StoreExceptionAndNotify(exception);
                throw exception;
            }
        }

        public override void Create()
        {
            PrivateConversations = new List<PrivateConversation>();
        }
    }
}