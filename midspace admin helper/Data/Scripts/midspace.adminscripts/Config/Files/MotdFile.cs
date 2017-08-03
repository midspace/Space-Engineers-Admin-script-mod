using System.IO;
using Sandbox.ModAPI;
using VRage;

namespace midspace.adminscripts.Config.Files
{
    public class MotdFile : FileBase
    {
        private const string Format = "Motd_{0}.txt";

        public string MessageOfTheDay { get; set; }

        public MotdFile(string fileName)
            : base(fileName, Format) { }

        public override void Save(string customSaveName = null)
        {
            using (ExecutionLock.AcquireExclusiveUsing())
            {
                TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(Name, typeof(ChatCommandLogic));
                writer.Write(CommandMessageOfTheDay.Content ?? "");
                writer.Flush();
                writer.Close();
            }
        }

        public override void Load()
        {
            TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(Name, typeof(ChatCommandLogic));
            MessageOfTheDay = reader.ReadToEnd();
            reader.Close();
        }

        public override void Create()
        {
            TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(Name, typeof(ChatCommandLogic));
            writer.Flush();
            writer.Close();
        }
    }
}