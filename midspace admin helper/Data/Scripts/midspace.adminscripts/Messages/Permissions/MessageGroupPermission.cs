namespace midspace.adminscripts.Messages.Permissions
{
    using ProtoBuf;
    using System.Collections.Generic;
    using midspace.adminscripts.Config;

    [ProtoContract]
    public class MessageGroupPermission : MessageBase
    {
        [ProtoMember(201)]
        public string GroupName;

        [ProtoMember(202)]
        public uint GroupLevel;

        [ProtoMember(203)]
        public string Name;

        [ProtoMember(204)]
        public List<PermissionGroup> Groups;

        [ProtoMember(205)]
        public List<string> MemberNames; // TODO that feels unsafe, need to be done properly

        [ProtoMember(206)]
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

    public enum PermissionGroupAction : byte
    {
        Level = 0,
        Name = 1,
        Add = 2,
        Remove = 3,
        Create = 4,
        Delete = 5,
        List = 6
    }
}
