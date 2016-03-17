namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using midspace.adminscripts.Messages;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;

    /// <summary>
    /// Organizes the notifications for admins such as errors.
    /// </summary>
    public static class AdminNotificator
    {
        static List<AdminNotification> m_AdminNotifications = new List<AdminNotification>();
        static Dictionary<ulong, List<int>> m_NotificationQueue = new Dictionary<ulong, List<int>>();

        static bool isInitialized = false;


        public static void Init()
        {
            if (isInitialized)
                return;

            if (MyAPIGateway.Utilities.IsDedicated)
            {
                var cfg = MyAPIGateway.Utilities.ConfigDedicated;
                cfg.Load();
                ulong steamId;
                foreach (string id in cfg.Administrators)
                    if (ulong.TryParse(id, out steamId))
                        m_NotificationQueue.Add(steamId, new List<int>());
            }
            else
                m_NotificationQueue.Add(MyAPIGateway.Session.Player.SteamUserId, new List<int>());
            isInitialized = true;
        }

        public static void StoreExceptionAndNotify(Exception ex, string additionalInformation = null)
        {
            if (!isInitialized)
                return;

            // log the exception and create a new notification with it
            Logger.LogException(ex, additionalInformation);
            var notification = new AdminNotification(ex);

            StoreAndNotify(notification);
        }

        public static void StoreAndNotify(AdminNotification notification)
        {
            if (!isInitialized)
                return;

            // if we have a non dedicated server everything is much simpler, we only need to notfy the host...
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                SendNotification(notification, MyAPIGateway.Session.Player);
                return;
            }

            // the complicated stuff begins here

            // first we get all admins who are currently online
            List<IMyPlayer> admins = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(admins, p => p != null && p.IsAdmin());

            // and we create a copy of the admin list to see who is not online in the end
            List<ulong> offlineAdmins = new List<ulong>(m_NotificationQueue.Keys);

            // we send the notification to the admins who are online and remove them from the list of offline admins
            foreach (IMyPlayer admin in admins)
            {
                SendNotification(notification, admin);

                if (offlineAdmins.Contains(admin.SteamUserId))
                    offlineAdmins.Remove(admin.SteamUserId);
            }

            // we only need to save it if we have some admins who aren't online
            if (offlineAdmins.Count > 0)
                m_AdminNotifications.Add(notification);
            else
                return;

            //now we store who must be notified when he is online
            foreach (ulong steamId in offlineAdmins)
            {
                if (m_NotificationQueue.ContainsKey(steamId))
                    m_NotificationQueue[steamId].Add(m_AdminNotifications.IndexOf(notification));
            }
        }
        
        public static void SendNotification(AdminNotification notification, IMyPlayer receiver)
        {
            SendNotification(notification, receiver.SteamUserId);
        }

        public static void SendNotification(AdminNotification notification, ulong receiver)
        {
            if (!isInitialized)
                return;

            var notificationMessage = new MessageAdminNotification() { Notification = notification };

            ConnectionHelper.SendMessageToPlayer(receiver, notificationMessage);
        }

        public static void SendEnqueuedNotifications(ulong steamId)
        {
            if (!isInitialized)
                return;

            if (!m_NotificationQueue.ContainsKey(steamId))
            {
                // if there is someone not yet added, we add him if he is an admin
                if (ServerConfig.IsServerAdmin(steamId))
                    m_NotificationQueue.Add(steamId, new List<int>());
                // if we just added him, we can leave it for now, because there won't be enqueued notifications
                // and if he wasn't really an admin, we stop here anyway...
                return;
            }
            // we get all enqueued notifications
            List<AdminNotification> notifications = m_AdminNotifications.FindAll(n => m_NotificationQueue[steamId].Contains(m_AdminNotifications.IndexOf(n)));

            // depending on how many we got, we handle it differently
            switch (notifications.Count)
            {
                case 0:
                    break;
                case 1:
                    SendNotification(notifications[0], steamId);
                    break;
                default:
                    var notification = new AdminNotification(notifications);
                    SendNotification(notification, steamId);
                    break;
            }

            // finally we clean it up, nobody likes duplication...
            m_NotificationQueue[steamId].Clear();
        }
    }
}
