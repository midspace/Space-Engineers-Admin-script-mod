using System.Linq;
using ProtoBuf;
using Sandbox.ModAPI;

namespace midspace.adminscripts.Messages.Sync
{
    [ProtoContract]
    public class MessageSyncFaction : MessageBase
    {
        #region fields

        [ProtoMember(1)]
        public SyncFactionType SyncType;

        [ProtoMember(2)]
        public long FactionId;

        [ProtoMember(3)]
        public long PlayerId;

        #endregion

        #region Process

        public static void JoinFaction(long factionId, long playerId)
        {
            Process(new MessageSyncFaction { SyncType = SyncFactionType.Join, FactionId = factionId, PlayerId = playerId });
        }

        public static void CancelJoinFaction(long factionId, long playerId)
        {
            Process(new MessageSyncFaction { SyncType = SyncFactionType.CancelJoin, FactionId = factionId, PlayerId = playerId });
        }

        public static void KickFaction(long factionId, long playerId)
        {
            Process(new MessageSyncFaction { SyncType = SyncFactionType.Kick, FactionId = factionId, PlayerId = playerId });
        }

        public static void DemotePlayer(long factionId, long playerId)
        {
            Process(new MessageSyncFaction { SyncType = SyncFactionType.Demote, FactionId = factionId, PlayerId = playerId });
        }

        public static void PromotePlayer(long factionId, long playerId)
        {
            Process(new MessageSyncFaction { SyncType = SyncFactionType.Promote, FactionId = factionId, PlayerId = playerId });
        }

        public static void RemoveFaction(long factionId)
        {
            Process(new MessageSyncFaction { SyncType = SyncFactionType.Remove, FactionId = factionId });
        }

        public static void AcceptPeace(long factionId)
        {
            Process(new MessageSyncFaction { SyncType = SyncFactionType.AcceptPeace, FactionId = factionId });
        }

        private static void Process(MessageSyncFaction syncEntity)
        {
            if (MyAPIGateway.Multiplayer.MultiplayerActive)
                ConnectionHelper.SendMessageToServer(syncEntity);
            else
                syncEntity.CommonProcess(syncEntity.SyncType, syncEntity.FactionId, syncEntity.PlayerId);
        }

        #endregion

        public override void ProcessClient()
        {
            CommonProcess(SyncType, FactionId, PlayerId);
        }

        public override void ProcessServer()
        {
            CommonProcess(SyncType, FactionId, PlayerId);
        }

        private void CommonProcess(SyncFactionType syncType, long factionId, long playerId)
        {
            switch (syncType)
            {
                case SyncFactionType.Join:
                    {
                        var fc = MyAPIGateway.Session.Factions.GetObjectBuilder();
                        //var faction = MyAPIGateway.Session.Factions.TryGetFactionById(factionId);

                        // AddPlayerToFaction() Doesn't work right on dedicated servers. To be removed by Keen in future. Is Depriated.
                        //MyAPIGateway.Session.Factions.AddPlayerToFaction(selectedPlayer.PlayerId, factionCollectionBuilder.FactionId);
                        //MyAPIGateway.Session.Factions.AddPlayerToFaction(PlayerId, FactionId);

                        var request = fc.Factions.FirstOrDefault(f => f.JoinRequests.Any(r => r.PlayerId == playerId));

                        if (request != null && request.FactionId != factionId)
                        {
                            // Cancel join request to other faction.
                            MyAPIGateway.Session.Factions.CancelJoinRequest(request.FactionId, playerId);
                        }
                        else if (request != null && request.FactionId == factionId)
                        {
                            MyAPIGateway.Session.Factions.AcceptJoin(factionId, playerId);
                            //MyAPIGateway.Utilities.ShowMessage("join", string.Format("{0} has been addded to faction.", selectedPlayer.DisplayName));
                            return;
                        }

                        // The SendJoinRequest and AcceptJoin cannot be called consecutively as the second call fails to work, so they must be run on individual game frames.
                        MyAPIGateway.Session.Factions.SendJoinRequest(factionId, playerId);
                        MyAPIGateway.Session.Factions.AcceptJoin(factionId, playerId);

                        //// The SendJoinRequest and AcceptJoin cannot be called consecutively as the second call fails to work, so they must be run on individual game frames.
                        //_workQueue.Enqueue(delegate () { MyAPIGateway.Session.Factions.SendJoinRequest(factionId, playerIdd); });
                        //_workQueue.Enqueue(delegate () { MyAPIGateway.Session.Factions.AcceptJoin(factionId, playerId); });
                    }
                    break;

                case SyncFactionType.Kick:
                    MyAPIGateway.Session.Factions.KickMember(factionId, playerId);
                    break;

                case SyncFactionType.Promote:
                    MyAPIGateway.Session.Factions.PromoteMember(factionId, playerId);
                    break;

                case SyncFactionType.Demote:
                    MyAPIGateway.Session.Factions.DemoteMember(factionId, playerId);
                    break;

                case SyncFactionType.Remove:
                    MyAPIGateway.Session.Factions.RemoveFaction(factionId);
                    break;

                case SyncFactionType.CancelJoin:
                    MyAPIGateway.Session.Factions.CancelJoinRequest(factionId, playerId);
                    break;

                case SyncFactionType.AcceptPeace:
                    {
                        var fc = MyAPIGateway.Session.Factions.GetObjectBuilder();

                        foreach (var faction in fc.Factions)
                        {
                            if (factionId == faction.FactionId)
                                continue;
                            if (MyAPIGateway.Session.Factions.IsPeaceRequestStatePending(factionId, faction.FactionId))
                                MyAPIGateway.Session.Factions.AcceptPeace(factionId, faction.FactionId);
                        }
                    }
                    break;
            }
        }
    }

    public enum SyncFactionType
    {
        Join = 1,
        Kick = 2,
        Promote = 3,
        Demote = 4,
        Remove = 5,
        CancelJoin = 6,
        AcceptPeace = 7
    }
}