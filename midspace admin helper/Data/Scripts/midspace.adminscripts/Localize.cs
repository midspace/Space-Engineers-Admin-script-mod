namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using VRage;
    using VRage.Library.Utils; // old
    using VRage.Utils; // new

    public static class Localize
    {
        // TODO: Localize out our resources?


        // Cannot use namespace "Sandbox.Game.Localization", as it's not whitelisted.
        //MyStringId WorldSaved = MySpaceTexts.WorldSaved;

        // Game resources.
        public const string WorldSaved = "WorldSaved";

        public static string GetResource(string stringId, params object[] args)
        {
            return MyStringId.Get(stringId).GetStringFormat(args);
        }

        // kind of pointless without the Sandbox.Game.Localization namespace, unless we define our own.
        //public static string GetResource(MyStringId stringId, params object[] args)
        //{
        //    return stringId.GetStringFormat(args);
        //}

        public static string GetString(this MyStringId stringId)
        {
            return MyTexts.GetString(stringId);
        }

        public static string GetStringFormat(this MyStringId stringId, params object[] args)
        {
            return string.Format(MyTexts.GetString(stringId), args);
        }
    }
}
