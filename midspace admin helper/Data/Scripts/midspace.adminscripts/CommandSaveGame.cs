namespace midspace.adminscripts
{
    using System;

    using Sandbox.ModAPI;
    using VRage.Library.Utils;

    /// <summary>
    /// Instructs the game to save to the local computer.
    /// This can save a Dedicated server to the local computer, ignoring the 'save game' setting.
    /// Its only use is to make a second backup copy of a online game in progress without affecting the server.
    /// Untested on hosted server.
    /// </summary>
    public class CommandSaveGame : ChatCommand
    {
        public CommandSaveGame()
            : base(ChatCommandSecurity.Admin, "savegame", new[] { "/savegame" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/savegame", "Saves the active game to the local computer.");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.Equals("/savegame", StringComparison.InvariantCultureIgnoreCase))
            {
                MyAPIGateway.Session.Save();
                var msg = MyStringId.Get("WorldSaved").GetStringFormat(MyAPIGateway.Session.Name);
                MyAPIGateway.Utilities.ShowNotification(msg, 2500, Sandbox.Common.MyFontEnum.White);
                return true;
            }
            return false;
        }
    }
}
