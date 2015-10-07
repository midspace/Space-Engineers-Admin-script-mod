using System.Collections.Generic;
using midspace.adminscripts.Protection;
using ProtoBuf;

namespace midspace.adminscripts.Messages.Protection
{
    [ProtoContract]
    public class MessageProtectionArea : MessageBase
    {
        [ProtoMember(1)] 
        public ProtectionArea ProtectionArea;

        [ProtoMember(2)] 
        public ProtectionAreaMessageType Type;

        public override void ProcessClient()
        {
            // nothing to do here yet
        }

        public override void ProcessServer()
        {
            switch (Type)
            {
                case ProtectionAreaMessageType.Add:
                    if (ProtectionHandler.AddArea(ProtectionArea))
                        ConnectionHelper.SendChatMessage(SenderSteamId, "Successfully created area.");
                    else
                        ConnectionHelper.SendChatMessage(SenderSteamId, "An area with that name already exists.");
                    break;
                case ProtectionAreaMessageType.Remove:
                    if (ProtectionHandler.RemoveArea(ProtectionArea))
                        ConnectionHelper.SendChatMessage(SenderSteamId, "Successfully removed area.");
                    else
                        ConnectionHelper.SendChatMessage(SenderSteamId, "An area with that name could not be found.");
                    break;
            }
        }
    }

    public enum ProtectionAreaMessageType
    {
        Add,
        Remove,
    }
}