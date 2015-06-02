namespace midspace.adminscripts
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using VRage;
    using VRageMath;

    public class CommandPrefabPaste : ChatCommand
    {
        public CommandPrefabPaste()
            : base(ChatCommandSecurity.Admin, "pasteprefab", new[] { "/pasteprefab" })
        {
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/pasteprefab <#>", "Pastes the specified <#> prefab from clipboard (Works only in Creative mode, ignores disabled Copy/Paste.)");
        }

        public override bool Invoke(string messageText)
        {
            var match = Regex.Match(messageText, @"/pasteprefab\s{1,}(?<Key>.+)", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var prefabName = match.Groups["Key"].Value;
                var prefabKvp = MyDefinitionManager.Static.GetPrefabDefinitions().FirstOrDefault(kvp => kvp.Key.Equals(prefabName, StringComparison.InvariantCultureIgnoreCase));
                MyPrefabDefinition prefab = null;

                if (prefabKvp.Value != null)
                {
                    prefab = prefabKvp.Value;
                }

                int index;
                if (prefabName.Substring(0, 1) == "#" && Int32.TryParse(prefabName.Substring(1), out index) && index > 0 && index <= CommandListPrefabs.PrefabCache.Count)
                {
                    prefab = CommandListPrefabs.PrefabCache[index - 1];
                }

                if (prefab != null)
                {
                    if (prefab.CubeGrids == null)
                    {
                        MyDefinitionManager.Static.ReloadPrefabsFromFile(prefab.PrefabPath);
                        prefab = MyDefinitionManager.Static.GetPrefabDefinition(prefab.Id.SubtypeName);
                    }

                    if (prefab.CubeGrids.Count() == 0)
                        return true;

                    var worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.WorldMatrix;
                    
                    // Use the cubeGrid BoundingBox to determine distance to place.
                    Vector3I min = Vector3I.MaxValue;
                    Vector3I max = Vector3I.MinValue;
                    foreach (var b in prefab.CubeGrids[0].CubeBlocks)
                    {
                        min = Vector3I.Min(b.Min, min);
                        max = Vector3I.Max(b.Min, max);
                    }
                    var size = new Vector3(max - min);
                    var distance = (float)(Math.Sqrt(size.LengthSquared()) * prefab.CubeGrids[0].GridSizeEnum.ToGridLength() / 2) + 5;
                    var position = worldMatrix.Translation;
                    var offset = position - prefab.CubeGrids[0].PositionAndOrientation.Value.Position;

                    MyAPIGateway.Entities.RemapObjectBuilderCollection(prefab.CubeGrids);
                    foreach (var grid in prefab.CubeGrids)
                    {
                        grid.PositionAndOrientation = new MyPositionAndOrientation(grid.PositionAndOrientation.Value.Position + offset, grid.PositionAndOrientation.Value.Forward, grid.PositionAndOrientation.Value.Up);
                    }

                    // only works in Creative mode, both Single and Server (even with paste disabled).
                    MyAPIGateway.CubeBuilder.ActivateShipCreationClipboard(prefab.CubeGrids, worldMatrix.Forward, distance);

                    return true;
                }
            }

            return false;
        }
    }
}
