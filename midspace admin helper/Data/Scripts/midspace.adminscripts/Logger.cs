namespace midspace.adminscripts
{
    using Sandbox.ModAPI;
    using System;
    using System.IO;

    public static class Logger
    {
        private readonly static string fileName = string.Format("AdminHelperCommands_{0}.log", MyAPIGateway.Session.WorldID);
        private static TextWriter Writer;
        private static bool isInitialized = false;

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
            if (!ChatCommandLogic.Instance.Debug || Writer == null)
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
            }
            isInitialized = false;
        }
    }
}
