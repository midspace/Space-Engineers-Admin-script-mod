namespace midspace.adminscripts
{
    using VRage;
    using VRage.Utils;

    public static class Localize
    {
        // TODO: Localize out our resources?


        // Cannot use namespace "Sandbox.Game.Localization", as it's not whitelisted.
        //MyStringId WorldSaved = MySpaceTexts.WorldSaved;

        // MySpaceTexts is not allowed in scripts. Last checked in version 01.100.024.
        //var test = MyTexts.GetString(Sandbox.Game.Localization.MySpaceTexts.WorldSettings_Description);

        // Game resources.
        public const string WorldSaved = "WorldSaved";

        public static string GetResource(string stringId, params object[] args)
        {
            if (args.Length == 0)
                return MyStringId.Get(stringId).GetString();
            else
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
