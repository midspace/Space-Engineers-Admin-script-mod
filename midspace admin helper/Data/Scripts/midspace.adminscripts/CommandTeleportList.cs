namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.ModAPI;
    using VRageMath;
    using System.IO;

    public class CommandTeleportList : ChatCommand
    {
        /// <summary>
        /// Temporary hotlist cache created of positons.
        /// </summary>
        public readonly static Dictionary<string, Vector3D> PositionCache = new Dictionary<string, Vector3D>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Filename where the position details will be stored. It uses the WorldID, so even 
        /// if the game name changes, your positions will remain intact and not be confused with another game of the same name.
        /// </summary>
        public readonly static string saveFile = string.Format("Position_{0}.bin", Path.GetFileNameWithoutExtension(MyAPIGateway.Session.CurrentPath));

        public CommandTeleportList()
            : base(ChatCommandSecurity.Admin, "tplist", new[] { "/tplist" })
        {
            LoadPoints();
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/tplist <filter>", "List the current favorite save points. Optional <filter> to refine your search name.");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/tplist\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);
            string saveName = null;

            if (match.Success)
            {
                saveName = match.Groups["Key"].Value;
            }

            var position = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetPosition();
            var list = PositionCache.Where(k => saveName == null || k.Key.IndexOf(saveName, StringComparison.InvariantCultureIgnoreCase) >= 0).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            MyAPIGateway.Utilities.ShowMessage("Count", list.Count.ToString());

            foreach (var kvp in list)
            {
                var distance = Math.Sqrt((position - kvp.Value).LengthSquared());
                MyAPIGateway.Utilities.ShowMessage(kvp.Key, string.Format("({0:N}| {1:N}| {2:N}) {3:N}m", kvp.Value.X, kvp.Value.Y, kvp.Value.Z, distance));
            }

            return true;
        }

        public static bool LoadPoints()
        {
            PositionCache.Clear();

            if (MyAPIGateway.Utilities.FileExistsInLocalStorage(saveFile, typeof(CommandTeleportList)))
            {
                using (var reader = MyAPIGateway.Utilities.ReadBinaryFileInLocalStorage(saveFile, typeof(CommandTeleportList)))
                {
                    var count = reader.ReadInt32();
                    for (var i = 0; i < count; i++)
                    {
                        var name = reader.ReadString();
                        var x = reader.ReadDouble();
                        var y = reader.ReadDouble();
                        var z = reader.ReadDouble();
                        PositionCache.Add(name, new Vector3D(x, y, z));
                    }
                }
            }

            return true;
        }

        public static bool SavePoint(string saveName, Vector3D position)
        {
            CommandTeleportList.PositionCache.Update(saveName, position);
            MyAPIGateway.Utilities.ShowMessage("Position", string.Format("'{0}' saved.", saveName));

            return SaveList();
        }

        public static bool DeletePoint(string saveName)
        {
            if (CommandTeleportList.PositionCache.ContainsKey(saveName))
            {
                CommandTeleportList.PositionCache.Remove(saveName);
                MyAPIGateway.Utilities.ShowMessage("Location removed", saveName);
                return SaveList();
            }

            return false;
        }

        public static bool SaveList()
        {
            using (var writer = MyAPIGateway.Utilities.WriteBinaryFileInLocalStorage(saveFile, typeof(CommandTeleportList)))
            {
                writer.Write(PositionCache.Count);
                foreach (var kvp in PositionCache)
                {
                    writer.Write(kvp.Key);
                    writer.Write(kvp.Value.X);
                    writer.Write(kvp.Value.Y);
                    writer.Write(kvp.Value.Z);
                }
                writer.Flush();
                writer.Close();
            }

            return true;
        }
    }
}
