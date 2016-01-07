namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using midspace.adminscripts.Messages.Sync;
    using Sandbox.ModAPI;
    using VRage.ModAPI;

    public class CommandObjectsCount : ChatCommand
    {
        public CommandObjectsCount()
            : base(ChatCommandSecurity.Admin, "countobjects", new[] {"/countobjects"})
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/countobjects", "Counts number of in game floating objects.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/countobjects", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                    CountObjects(0);
                else
                    ConnectionHelper.SendMessageToServer(new MessageSyncFloatingObjects { Type = SyncFloatingObject.Count });
                return true;
            }

            return false;
        }

        public static void CountObjects(ulong steamId)
        {
            var floatingList = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(floatingList, e => (e is Sandbox.ModAPI.IMyFloatingObject));
            var replicableList = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(replicableList, e => (e is Sandbox.Game.Entities.MyInventoryBagEntity));

            MyAPIGateway.Utilities.SendMessage(steamId, "Floating objects", "{0}/{1}", floatingList.Count, MyAPIGateway.Session.SessionSettings.MaxFloatingObjects);

            if (replicableList.Count > 0)
                MyAPIGateway.Utilities.SendMessage(steamId, "Floating backpacks", "{0}", replicableList.Count);
        }
    }
}
