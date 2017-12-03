namespace midspace.adminscripts.Messages.Sync
{
    using ProtoBuf;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using VRage.ModAPI;

    [ProtoContract]
    public class MessageSyncVoxelChange : MessageBase
    {
        [ProtoMember(201)]
        public SyncVoxelChangeType SyncType;

        [ProtoMember(202)]
        public long EntityId;

        [ProtoMember(203)]
        public string SearchEntity;

        [ProtoMember(204)]
        public bool Planet;

        public static void SendMessage(SyncVoxelChangeType syncType, long entityId, string searchEntity, bool planet)
        {
            Process(new MessageSyncVoxelChange { SyncType = syncType, EntityId = entityId, SearchEntity = searchEntity, Planet = planet });
        }

        private static void Process(MessageSyncVoxelChange syncEntity)
        {
            if (MyAPIGateway.Multiplayer.MultiplayerActive && !MyAPIGateway.Multiplayer.IsServer)
                ConnectionHelper.SendMessageToServer(syncEntity);
            else
                syncEntity.CommonProcess(syncEntity.SenderSteamId, syncEntity.SyncType, syncEntity.EntityId, syncEntity.SearchEntity, syncEntity.Planet);
        }

        public override void ProcessClient()
        {
            // never called on client
        }

        public override void ProcessServer()
        {
            CommonProcess(SenderSteamId, SyncType, EntityId, SearchEntity, Planet);
        }

        private void CommonProcess(ulong steamId, SyncVoxelChangeType syncType, long entityId, string searchEntity, bool planet)
        {
            List<IMyVoxelBase> selectedVoxels = new List<IMyVoxelBase>();

            if (entityId != 0)
            {
                MyVoxelBase selectedVoxel;
                if (planet)
                    selectedVoxel = MyAPIGateway.Entities.GetEntityById(entityId) as MyPlanet;
                else
                    selectedVoxel = MyAPIGateway.Entities.GetEntityById(entityId) as MyVoxelMap;

                if (selectedVoxel != null)
                    selectedVoxels.Add(selectedVoxel);
            }
            else if (!string.IsNullOrEmpty(searchEntity))
            {
                var currentPlanetList = new List<IMyVoxelBase>();
                MyAPIGateway.Session.VoxelMaps.GetInstances(currentPlanetList, v => v is Sandbox.Game.Entities.MyPlanet && v.StorageName.IndexOf(searchEntity, StringComparison.InvariantCultureIgnoreCase) >= 0);

                if (currentPlanetList.Count == 1)
                {
                    selectedVoxels.Add(currentPlanetList.First());
                }
                else  if (currentPlanetList.Count == 0)
                {
                    List<IMyVoxelBase> planetCache = CommandPlanetsList.GetPlanetCache(steamId);
                    int index;
                    if (searchEntity.Substring(0, 1) == "#" && int.TryParse(searchEntity.Substring(1), out index) && index > 0 && index <= planetCache.Count && planetCache[index - 1] != null)
                    {
                        selectedVoxels.Add(planetCache[index - 1]);
                    }
                }
            }

            if (selectedVoxels.Count == 0)
            {
                MyAPIGateway.Utilities.SendMessage(steamId, "Server", "No planet found.");
                return;
            }

            switch (syncType)
            {
                case SyncVoxelChangeType.DeletePlanet:
                    {
                        if (selectedVoxels.Count == 1)
                            DeletePlanet(steamId, selectedVoxels.First());
                        else if (selectedVoxels.Count > 1)
                            MyAPIGateway.Utilities.SendMessage(steamId, "deleteplanet", "{0} Planets match that name.", selectedVoxels.Count);
                    }
                    break;
            }
        }

        private void DeletePlanet(ulong steamId, IMyVoxelBase planetEntity)
        {
            if (planetEntity == null || planetEntity.Closed)
            {
                MyAPIGateway.Utilities.SendMessage(steamId, "planet", "already deleted.");
                return;
            }
            var name = planetEntity.StorageName;
            planetEntity.SyncObject.SendCloseRequest();
            MyAPIGateway.Utilities.SendMessage(steamId, "planet", "'{0}' deleted.", name);
        }
    }

    public enum SyncVoxelChangeType : byte
    {
        DeletePlanet = 0
    }
}
