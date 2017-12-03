namespace midspace.adminscripts.Messages
{
    using ProtoBuf;

    [ProtoContract]
    public class MessageAdminNotification : MessageBase
    {
        [ProtoMember(201)]
        public AdminNotification Notification;

        public override void ProcessClient()
        {
            if (ChatCommandLogic.Instance.ShowDialogsOnReceive)
                Notification.Show();
            else
                ChatCommandLogic.Instance.AdminNotification = Notification;
        }

        public override void ProcessServer()
        {
            // never processed on server
        }
    }
}
