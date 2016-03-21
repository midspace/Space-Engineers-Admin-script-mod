using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using midspace.adminscripts.Config;

namespace midspace.adminscripts.Messages.Permissions
{
    [ProtoContract]
    public class MessageCommandPermission : MessageBase
    {
        [ProtoMember(1)]
        public List<CommandStruct> Commands;

        [ProtoMember(2)]
        public CommandActions CommandAction;

        [ProtoMember(3)]
        public string ListParameter;

        public override void ProcessClient()
        {
                switch (CommandAction)
                {
                    case CommandActions.Level:
                        foreach (CommandStruct command in Commands)
                            ChatCommandService.UpdateCommandSecurity(command);
                        break;
                    case CommandActions.List:
                        CommandPermission.ShowCommandList(Commands);
                        break;
                }
        }

        public override void ProcessServer()
        {
            switch (CommandAction)
            {
                case CommandActions.Level:
                    foreach (CommandStruct command in Commands)
                        ChatCommandLogic.Instance.ServerCfg.UpdateCommandSecurity(command, SenderSteamId);
                    break;
                case CommandActions.List:
                    ChatCommandLogic.Instance.ServerCfg.CreateCommandHotlist(SenderSteamId, ListParameter);
                    break;
            }
        }
    }

    public enum CommandActions
    {
        Level,
        List
    }
}
