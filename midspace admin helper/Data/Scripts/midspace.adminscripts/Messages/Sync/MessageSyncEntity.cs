using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace midspace.adminscripts.Messages.Sync
{
    [ProtoContract]
    public class MessageSyncEntity : MessageBase
    {
        [ProtoMember(1)]
        public long EntityId;

        [ProtoMember(2)]
        public SyncEntityType Type;

        public override void ProcessClient()
        {
            // never processed
        }

        public override void ProcessServer()
        {
            if (!MyAPIGateway.Entities.EntityExists(EntityId))
                return;

            var entity = MyAPIGateway.Entities.GetEntityById(EntityId);

            switch (Type)
            {
                case SyncEntityType.Stop:
                    entity.Stop();
                    break;
            }
        }
    }

    public enum SyncEntityType
    {
        Stop,
    }
}
