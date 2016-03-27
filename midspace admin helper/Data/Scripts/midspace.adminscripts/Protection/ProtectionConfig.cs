using System;
using System.Collections.Generic;
using System.IO;
using ProtoBuf;

namespace midspace.adminscripts.Protection
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class ProtectionConfig
    {
        [ProtoMember(1)]
        public List<ProtectionArea> Areas;

        [ProtoMember(2)]
        public bool ProtectionEnabled;

        [ProtoMember(3)]
        public bool ProtectionInverted;

        public ProtectionConfig()
        {
            Areas = new List<ProtectionArea>();
        }
    }
}