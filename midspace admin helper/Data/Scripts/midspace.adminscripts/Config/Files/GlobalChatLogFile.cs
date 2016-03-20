using System;
using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;

namespace midspace.adminscripts.Config.Files
{
    public class GlobalChatLogFile : FileBase
    {
        private const string Format = "GlobalChatLog_{0}.xml";

        public List<ChatMessage> ChatMessages { get; set; }

        public GlobalChatLogFile(string fileName)
            : base(fileName, Format) { }

        public override void Save(string customSaveName = null)
        {
            string fileName;

            if (!string.IsNullOrEmpty(customSaveName))
                fileName = String.Format(Format, customSaveName);
            else
                fileName = Name;

            TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(fileName, typeof(ServerConfig));
            writer.Write(MyAPIGateway.Utilities.SerializeToXML(ChatMessages));
            writer.Flush();
            writer.Close();
        }

        public override void Load()
        {
            TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(Name, typeof(ServerConfig));
            var text = reader.ReadToEnd();
            reader.Close();

            if (!string.IsNullOrEmpty(text))
            {
                try
                {
                    ChatMessages = MyAPIGateway.Utilities.SerializeFromXML<List<ChatMessage>>(text);
                }
                catch (Exception ex)
                {
                    var exception = new Exception(string.Format("An error occuring loading the file '{0}'. Begining with the text \"{1}\".", Name, text.Substring(0, Math.Min(text.Length, 100))), ex);
                    AdminNotificator.StoreExceptionAndNotify(exception);
                }
            }

            if (ChatMessages == null)
                ChatMessages = new List<ChatMessage>();
        }

        public override void Create()
        {
            ChatMessages = new List<ChatMessage>();
        }
    }
}