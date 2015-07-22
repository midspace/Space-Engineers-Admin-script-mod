using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace midspace.adminscripts.Messages
{
    [ProtoContract]
    public class MessageCommandPermissions : MessageBase
    {
        [ProtoMember]
        public List<CommandStruct> Commands;

        [ProtoMember]
        public CommandActions CommandAction;

        [ProtoMember]
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
