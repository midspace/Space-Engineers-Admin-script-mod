namespace midspace.adminscripts.Messages
{
    using ProtoBuf;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRage.Library.Utils;

    [ProtoContract]
    public class MessageSession : MessageBase
    {
        [ProtoMember(1)]
        public bool State;

        [ProtoMember(2)]
        public SessionSetting Setting;

        public override void ProcessClient()
        {
            if (!MyAPIGateway.Session.Player.IsHost())
                SetSetting();

            if (MyAPIGateway.Session.Player.IsAdmin())
                MyAPIGateway.Utilities.ShowMessage(string.Format("Server {0}", Setting.ToString()), State ? "On" : "Off");
        }

        public override void ProcessServer()
        {
            SetSetting();

            ConnectionHelper.SendMessageToAllPlayers(this);
        }

        void SetSetting()
        {
            switch (Setting)
            {
                case SessionSetting.CargoShips:
                    MyAPIGateway.Session.SessionSettings.CargoShipsEnabled = State;
                    break;
                case SessionSetting.CopyPaste:
                    MyAPIGateway.Session.SessionSettings.EnableCopyPaste = State;
                    break;
                case SessionSetting.Creative:
                    MyGameModeEnum gameMode = State ? MyGameModeEnum.Creative : MyGameModeEnum.Survival;
                    MyAPIGateway.Session.SessionSettings.GameMode = gameMode;
                    break;
                case SessionSetting.Spectator:
                    MyAPIGateway.Session.SessionSettings.EnableSpectator = State;
                    break;
                case SessionSetting.Weapons:
                    MyAPIGateway.Session.SessionSettings.WeaponsEnabled = State;
                    break;
                case SessionSetting.Wolves:
                    MyAPIGateway.Session.SessionSettings.EnableWolfs = State;
                    break;
                case SessionSetting.Spiders:
                    MyAPIGateway.Session.SessionSettings.EnableSpiders = State;
                    break;
            }
        }
    }

    public enum SessionSetting
    {
        CargoShips,
        CopyPaste,
        Creative,
        Spectator,
        Weapons,
        Wolves,
        Spiders
    }
}
