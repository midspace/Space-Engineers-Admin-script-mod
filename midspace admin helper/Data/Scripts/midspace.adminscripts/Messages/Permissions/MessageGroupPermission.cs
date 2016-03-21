using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using midspace.adminscripts.Config;

namespace midspace.adminscripts.Messages.Permissions
{
    [ProtoContract]
    public class MessageGroupPermission : MessageBase
    {
        [ProtoMember(1)]
        public string GroupName;

        [ProtoMember(2)]
        public uint GroupLevel;

        [ProtoMember(3)]
        public string Name;

        [ProtoMember(4)]
        public List<PermissionGroup> Groups;

        [ProtoMember(6)]
        public List<string> MemberNames; // TODO that feels unsafe, need to be done properly

        [ProtoMember(7)]
        public PermissionGroupAction Action;

        public override void ProcessClient()
        {
            switch (Action)
            {
                case PermissionGroupAction.List:
                    CommandPermission.ShowGroupList(Groups, MemberNames);
                    break;
            }
        }

        public override void ProcessServer()
        {
            switch (Action)
            {
                case PermissionGroupAction.Level:
                    ChatCommandLogic.Instance.ServerCfg.SetGroupLevel(GroupName, GroupLevel, SenderSteamId);
                    break;
                case PermissionGroupAction.Name:
                    ChatCommandLogic.Instance.ServerCfg.SetGroupName(GroupName, Name, SenderSteamId);
                    break;
                case PermissionGroupAction.Add:
                    ChatCommandLogic.Instance.ServerCfg.AddPlayerToGroup(GroupName, Name, SenderSteamId);
                    break;
                case PermissionGroupAction.Remove:
                    ChatCommandLogic.Instance.ServerCfg.RemovePlayerFromGroup(GroupName, Name, SenderSteamId);
                    break;
                case PermissionGroupAction.Create:
                    ChatCommandLogic.Instance.ServerCfg.CreateGroup(GroupName, GroupLevel, SenderSteamId);
                    break;
                case PermissionGroupAction.Delete:
                    ChatCommandLogic.Instance.ServerCfg.DeleteGroup(GroupName, SenderSteamId);
                    break;
                case PermissionGroupAction.List:
                    ChatCommandLogic.Instance.ServerCfg.CreateGroupHotlist(SenderSteamId, GroupName);
                    break;
            }
        }
    }

    public enum PermissionGroupAction
    {
        Level,
        Name,
        Add,
        Remove,
        Create,
        Delete,
        List
    }
}
