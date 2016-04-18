namespace midspace.adminscripts.Messages
{
    using System.Collections.Generic;
    using ProtoBuf;
    using Sandbox.ModAPI;
    using VRage.ModAPI;

    /// <summary>
    /// This stores the last item a player has used the /id command on, 
    /// so it can be used server side for other commands.
    /// </summary>
    [ProtoContract]
    public class MessageTaggedEntityStore : MessageBase
    {
        [ProtoMember(1)]
        public long PlayerId;

        [ProtoMember(2)]
        public long EntityId;

        public readonly static Dictionary<long, long> EntityList = new Dictionary<long, long>();

        public static void RegisterIdentity(long playerId, long entityId)
        {
            ConnectionHelper.SendMessageToServer(new MessageTaggedEntityStore { PlayerId = playerId, EntityId = entityId });
        }

        public static IMyEntity GetEntity(long playerId)
        {
            if (!EntityList.ContainsKey(playerId))
                return null;

            IMyEntity entity;
            return MyAPIGateway.Entities.TryGetEntityById(EntityList[playerId], out entity) ? entity : null;
        }

        public override void ProcessClient()
        {
            // never processed here
        }

        public override void ProcessServer()
        {
            EntityList[PlayerId] = EntityId;
        }
    }
}
