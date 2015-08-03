using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace midspace.adminscripts.Messages
{
    [ProtoContract]
    public class MessageAdminNotification : MessageBase
    {
        [ProtoMember(1)]
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
