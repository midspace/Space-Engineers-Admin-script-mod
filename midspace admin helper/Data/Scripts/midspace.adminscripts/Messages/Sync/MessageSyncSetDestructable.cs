namespace midspace.adminscripts.Messages.Sync
{
    using System.Collections.Generic;
    using midspace.adminscripts.Messages.Communication;
    using ProtoBuf;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;

    [ProtoContract]
    public class MessageSyncSetDestructable : MessageBase
    {
        [ProtoMember(1)]
        public long EntityId;

        [ProtoMember(2)]
        public bool Destructable;

        public override void ProcessClient()
        {
            // never processed
        }

        public override void ProcessServer()
        {
            // TODO: permission checks.

            if (!MyAPIGateway.Entities.EntityExists(EntityId))
                return;

            var entity = MyAPIGateway.Entities.GetEntityById(EntityId);
            SetDestructible(entity, Destructable, SenderSteamId);
        }

        public static void SetDestructible(IMyEntity shipEntity, bool destructible, ulong steamId = 0)
        {
            var gridObjectBuilder = shipEntity.GetObjectBuilder(true) as MyObjectBuilder_CubeGrid;
            if (gridObjectBuilder.DestructibleBlocks == destructible)
            {
                // TODO: Should make a better wrapper, to send message local or remote.
                if (steamId == 0)
                    MyAPIGateway.Utilities.ShowMessage("destructible", "Ship '{0}' destructible is already set to {1}.", shipEntity.DisplayName, destructible ? "On" : "Off");
                else
                    MessageClientTextMessage.SendMessage(steamId, "destructible", "Ship '{0}' destructible is already set to {1}.", shipEntity.DisplayName, destructible ? "On" : "Off");
                return;
            }

            gridObjectBuilder.EntityId = 0;
            gridObjectBuilder.DestructibleBlocks = destructible;

            // This will Delete the entity and sync to all.
            // Using this, also works with player ejection in the same Tick.
            shipEntity.SyncObject.SendCloseRequest();

            var tempList = new List<MyObjectBuilder_EntityBase>();
            tempList.Add(gridObjectBuilder);
            tempList.CreateAndSyncEntities();

            // TODO: Should make a better wrapper, to send message local or remote.
            if (steamId == 0)
                MyAPIGateway.Utilities.ShowMessage("destructible", "Ship '{0}' destructible has been set to {1}.", shipEntity.DisplayName, destructible ? "On" : "Off");
            else
                MessageClientTextMessage.SendMessage(steamId, "destructible", "Ship '{0}' destructible has been set to {1}.", shipEntity.DisplayName, destructible ? "On" : "Off");
        }
    }
}
