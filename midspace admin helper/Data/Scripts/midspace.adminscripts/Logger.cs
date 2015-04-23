namespace midspace.adminscripts
{
    using Sandbox.ModAPI;
    using System;
    using System.IO;

    public static class Logger
    {
        private readonly static string fileName;
        private static TextWriter Writer;
        private static bool isInitialized = false;

        static Logger()
        {
            if (MyAPIGateway.Session != null)
                fileName = string.Format("AdminHelperCommands_{0}.log", Path.GetFileNameWithoutExtension(MyAPIGateway.Session.CurrentPath));
            else
                fileName = string.Format("AdminHelperCommands_{0}.log", 0);
        }

        public static void Init()
        {
            if (ChatCommandLogic.Instance.Debug && !isInitialized)
            {
                Writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(fileName, typeof(Logger));
                isInitialized = true;
            }
        }

        public static void Debug(string text)
        {
            if (Writer == null || !isInitialized || !ChatCommandLogic.Instance.Debug)
                return;

            Writer.WriteLine(string.Format("[{0:yyyy-MM-dd HH:mm:ss:fff}] Debug - {1}", DateTime.Now, text));
            Writer.Flush();
        }

        public static void Terminate()
        {
            if (Writer != null)
            {
                Writer.Flush();
                Writer.Close();
                Writer = null;
            }
            isInitialized = false;
        }
    }
}
