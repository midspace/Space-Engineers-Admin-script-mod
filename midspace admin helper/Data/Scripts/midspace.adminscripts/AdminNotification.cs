using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace midspace.adminscripts
{
    [ProtoContract]
    public class AdminNotification
    {
        [ProtoMember(1)]
        public string Content;
        
        [ProtoMember(2)]
        public DateTime Date;

        /// <summary>
        /// Creates a new admin notification that describes the given exception
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="additionalInformation"></param>
        public AdminNotification(Exception exception, string additionalInformation = null)
        {
            // build the message's content
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(string.Format("An exception occurred in Midspace's admin helper commands. Please inform the mod's creators about it. You will find a file named \"{0}\" at the local storage of the mod.", Logger.ErrorFileName));
            builder.AppendLine("To share the file on the steam workshop, just paste the file's content at pastebin.com and provide the link to it. Thanks a lot!");
            builder.AppendLine("");
            builder.AppendLine("-----");
            builder.AppendLine("");
            builder.AppendLine(exception.ToString());
            builder.AppendLine("");
            if (!string.IsNullOrEmpty(additionalInformation))
            {
                builder.AppendLine("-----");
                builder.AppendLine("");
                builder.AppendLine(additionalInformation);
                builder.AppendLine("");
            }

            // apply values
            Content = builder.ToString();
            Date = DateTime.Now;
        }

        /// <summary>
        /// Creates a new admin notification that sums up the given notifications.
        /// </summary>
        /// <param name="notifications"></param>
        public AdminNotification(List<AdminNotification> notifications)
        {
            // build the message's content
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(string.Format("You have {0} new Notifications:", notifications.Count));
            builder.AppendLine("");

            foreach (AdminNotification notification in notifications) {
                builder.AppendLine("----------");
                builder.AppendLine("");
                builder.AppendLine(string.Format("Notification created at {0:yyyy-MM-dd HH:mm:ss}", notification.Date));
                builder.AppendLine("");
                builder.AppendLine("-----");
                builder.AppendLine("");
                builder.AppendLine(notification.Content);
            }

            // apply values
            Content = builder.ToString();
            Date = DateTime.Now;
        }

        /// <summary>
        /// Creates an empty admin notification without timestamp
        /// </summary>
        public AdminNotification() { }

        public void Show()
        {
            if (MyAPIGateway.Session.Player != null)
                MyAPIGateway.Utilities.ShowMissionScreen("Admin Notification", string.Format("Created at {0:yyyy-MM-dd HH:mm:ss}", Date), null, Content);
        }
    }
}
