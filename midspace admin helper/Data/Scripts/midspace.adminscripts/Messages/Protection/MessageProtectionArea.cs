namespace midspace.adminscripts.Messages.Protection
{
    using midspace.adminscripts.Messages.Communication;
    using midspace.adminscripts.Protection;
    using ProtoBuf;

    [ProtoContract]
    public class MessageProtectionArea : MessageBase
    {
        [ProtoMember(201)]
        public ProtectionArea ProtectionArea;

        [ProtoMember(202)] 
        public ProtectionAreaMessageType Type;

        public override void ProcessClient()
        {
            // never processed
        }

        public override void ProcessServer()
        {
            switch (Type)
            {
                case ProtectionAreaMessageType.Add:
                    if (ProtectionHandler.AddArea(ProtectionArea))
                    {
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Server",  "Successfully created area.");
                        ConnectionHelper.SendMessageToAllPlayers(new MessageSyncProtection()
                        {
                            Config = ProtectionHandler.Config
                        });
                        ProtectionHandler.Save();
                    }
                    else
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Server", "An area with that name already exists.");
                    break;
                case ProtectionAreaMessageType.Remove:
                    if (ProtectionHandler.RemoveArea(ProtectionArea))
                    {
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Server", "Successfully removed area.");
                        ConnectionHelper.SendMessageToAllPlayers(new MessageSyncProtection()
                        {
                            Config = ProtectionHandler.Config
                        });
                        ProtectionHandler.Save();
                    }
                    else
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Server", "An area with that name could not be found.");
                    break;
            }
        }
    }

    public enum ProtectionAreaMessageType : byte
    {
        Add = 0,
        Remove = 1
    }
}