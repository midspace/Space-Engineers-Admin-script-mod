using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace midspace.adminscripts
{
    public static class Logger
    {
        const string FileName = "AdminHelperCommands.log";
        static TextWriter Writer;
        static bool isInitialized = false;

        public static void Init()
        {
            if (!isInitialized)
            {
                Writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(FileName, typeof(Logger));
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
            Writer.Flush();
            Writer.Close();
            isInitialized = false;
        }
    }
}
