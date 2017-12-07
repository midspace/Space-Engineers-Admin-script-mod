namespace midspace.adminscripts.Protection
{
    using ProtoBuf;
    using System.Collections.Generic;

    [ProtoContract(UseProtoMembersOnly = true)]
    public class ProtectionConfig
    {
        /// <remarks>ProtoBuf treats empty collections as null, so they need to be constructed by default,
        /// otherwise an empty collection will not deserialize.</remarks>
        [ProtoMember(1)]
        public List<ProtectionArea> Areas;

        [ProtoMember(2)]
        public bool ProtectionEnabled;

        [ProtoMember(3)]
        public bool ProtectionInverted;

        [ProtoMember(4)]
        public bool ProtectionAllowLandingGear;

        public ProtectionConfig()
        {
            Areas = new List<ProtectionArea>();
        }
    }
}