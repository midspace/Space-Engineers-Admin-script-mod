using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace midspace.adminscripts.Messages.Permissions
{
    [ProtoContract]
    public class MessagePlayerPermission : MessageBase
    {
        [ProtoMember(1)]
        public string PlayerName;

        [ProtoMember(2)]
        public uint PlayerLevel;

        [ProtoMember(3)]
        public string CommandName;

        [ProtoMember(4)]
        public bool UsePlayerLevel;

        [ProtoMember(5)]
        public List<PlayerPermission> PlayerPermissions;

        [ProtoMember(6)]
        public PlayerPermissionAction Action;

        public override void ProcessClient()
        {
            switch (Action)
            {
                case PlayerPermissionAction.Level:
                    ChatCommandService.UserSecurity = PlayerLevel;

                    //allow cmds now
                    ChatCommandLogic.Instance.BlockCommandExecution = false;

                    //and stop further requests
                    if (ChatCommandLogic.Instance.PermissionRequestTimer != null)
                    {
                        ChatCommandLogic.Instance.PermissionRequestTimer.Stop();
                        ChatCommandLogic.Instance.PermissionRequestTimer.Close();
                    }
                    break;
                case PlayerPermissionAction.List:
                    CommandPermission.ShowPlayerList(PlayerPermissions);
                    break;
            }
        }

        public override void ProcessServer()
        {
            switch (Action)
            {
                case PlayerPermissionAction.Level:
                    ChatCommandLogic.Instance.ServerCfg.SetPlayerLevel(PlayerName, PlayerLevel, SenderSteamId);
                    break;
                case PlayerPermissionAction.Extend:
                    ChatCommandLogic.Instance.ServerCfg.ExtendRights(PlayerName, CommandName, SenderSteamId);
                    break;
                case PlayerPermissionAction.Restrict:
                    ChatCommandLogic.Instance.ServerCfg.RestrictRights(PlayerName, CommandName, SenderSteamId);
                    break;
                case PlayerPermissionAction.UsePlayerLevel:
                    ChatCommandLogic.Instance.ServerCfg.UsePlayerLevel(PlayerName, UsePlayerLevel, SenderSteamId);
                    break;
                case PlayerPermissionAction.List:
                    ChatCommandLogic.Instance.ServerCfg.CreatePlayerHotlist(SenderSteamId, PlayerName);
                    break;
            }
        }
    }

    public enum PlayerPermissionAction
    {
        Level,
        Extend,
        Restrict,
        UsePlayerLevel,
        List
    }
}
