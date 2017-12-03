namespace midspace.adminscripts.Messages.Permissions
{
    using ProtoBuf;
    using System.Collections.Generic;
    using midspace.adminscripts.Config;

    [ProtoContract]
    public class MessageCommandPermission : MessageBase
    {
        [ProtoMember(201)]
        public List<CommandStruct> Commands;

        [ProtoMember(202)]
        public CommandActions CommandAction;

        [ProtoMember(203)]
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

    public enum CommandActions : byte
    {
        Level = 0,
        List = 1
    }
}
