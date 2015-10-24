namespace midspace.adminscripts.Messages.Sync
{
    using System;
    using System.Collections.Generic;
    using midspace.adminscripts.Messages.Communication;
    using ProtoBuf;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using VRage;
    using VRage.ObjectBuilders;
    using VRageMath;

    [ProtoContract]
    public class MessageSyncCreatePrefab : MessageBase
    {
        [ProtoMember(1)]
        public SyncCreatePrefabType Type;

        [ProtoMember(2)]
        public string PrefabName;

        [ProtoMember(3)]
        public long PositionEntityId;

        public override void ProcessClient()
        {
            // never processed
        }

        public override void ProcessServer()
        {
            if (AddPrefab(PrefabName, PositionEntityId, Type == SyncCreatePrefabType.Wireframe))
                MessageClientTextMessage.SendMessage(SenderSteamId, "Success", "The Prefab {0} is spawning.", PrefabName);
            else
                MessageClientTextMessage.SendMessage(SenderSteamId, "Failed", "Could not create the specified prefab.");
        }

        public static bool AddPrefab(string prefabName, long positionEntityId, bool wireframe, ulong messageId = 0)
        {
            if (!MyAPIGateway.Entities.EntityExists(positionEntityId))
                return false;

            var entity = MyAPIGateway.Entities.GetEntityById(positionEntityId);

            var prefab = MyDefinitionManager.Static.GetPrefabDefinition(prefabName);
            if (prefab.CubeGrids == null)
            {
                MyDefinitionManager.Static.ReloadPrefabsFromFile(prefab.PrefabPath);
                prefab = MyDefinitionManager.Static.GetPrefabDefinition(prefab.Id.SubtypeName);
            }

            if (prefab.CubeGrids.Length == 0)
                return false;

            var worldMatrix = entity.WorldMatrix;
            // Use the cubeGrid BoundingBox to determine distance to place.
            Vector3I min = Vector3I.MaxValue;
            Vector3I max = Vector3I.MinValue;
            foreach (var b in prefab.CubeGrids[0].CubeBlocks)
            {
                min = Vector3I.Min(b.Min, min);
                max = Vector3I.Max(b.Min, max);
            }
            var size = new Vector3(max - min);
            var distance = (Math.Sqrt(size.LengthSquared()) * prefab.CubeGrids[0].GridSizeEnum.ToGridLength() / 2) + 2;
            var position = worldMatrix.Translation + worldMatrix.Forward * distance; // offset the position out in front of player by 2m.
            var offset = position - prefab.CubeGrids[0].PositionAndOrientation.Value.Position;
            var tempList = new List<MyObjectBuilder_EntityBase>();

            // We SHOULD NOT make any changes directly to the prefab, we need to make a Value copy using Clone(), and modify that instead.
            foreach (var grid in prefab.CubeGrids)
            {
                var gridBuilder = (MyObjectBuilder_CubeGrid)grid.Clone();
                gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(grid.PositionAndOrientation.Value.Position + offset, grid.PositionAndOrientation.Value.Forward, grid.PositionAndOrientation.Value.Up);

                if (wireframe)
                    foreach (var cube in gridBuilder.CubeBlocks)
                    {
                        cube.IntegrityPercent = 0.01f;
                        cube.BuildPercent = 0.01f;
                    }

                tempList.Add(gridBuilder);
            }

            tempList.CreateAndSyncEntities();
            return true;
        }
    }

    public enum SyncCreatePrefabType
    {
        Stock,
        Wireframe
    }
}
