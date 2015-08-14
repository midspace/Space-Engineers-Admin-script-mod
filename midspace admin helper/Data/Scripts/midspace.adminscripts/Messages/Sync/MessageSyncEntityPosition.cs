using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace midspace.adminscripts.Messages.Sync
{
    [ProtoContract]
    public class MessageSyncEntityPosition : MessageBase
    {
        [ProtoMember(1)]
        public Vector3D Position;

        [ProtoMember(2)]
        public long EntityId;

        public override void ProcessClient()
        {
            if (!MyAPIGateway.Entities.EntityExists(EntityId))
                return;

            var entity = MyAPIGateway.Entities.GetEntityById(EntityId);

            entity.Stop();

            // This still is not syncing properly. Called on the server, it does not show correctly on the client.
            entity.SetPosition(Position);
        }

        public override void ProcessServer()
        {
            if (!MyAPIGateway.Entities.EntityExists(EntityId))
                return;

            var entity = MyAPIGateway.Entities.GetEntityById(EntityId);

            entity.Stop();

            // This still is not syncing properly. Called on the server, it does not show correctly on the client.
            entity.SetPosition(Position);
        }
    }
}
