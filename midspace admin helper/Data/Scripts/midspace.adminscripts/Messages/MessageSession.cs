namespace midspace.adminscripts.Messages
{
    using ProtoBuf;
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRage.Library.Utils;

    [ProtoContract]
    public class MessageSession : MessageBase
    {
        [ProtoMember(201)]
        public bool State;

        [ProtoMember(202)]
        public SessionSetting Setting;

        [ProtoMember(203)]
        public int StateValue;

        public override void ProcessClient()
        {
            if (!MyAPIGateway.Session.Player.IsHost())
                SetSetting();

            if (MyAPIGateway.Session.Player.IsAdmin())
                MyAPIGateway.Utilities.ShowMessage(string.Format("Server {0}", Setting), State ? "On" : "Off");
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
                case SessionSetting.Meteors:
                    MyAPIGateway.Session.SessionSettings.EnvironmentHostility = (MyEnvironmentHostilityEnum)StateValue;
                    break;
            }
        }
    }

    public enum SessionSetting : byte
    {
        CargoShips = 0,
        CopyPaste = 1,
        Creative = 2,
        Spectator = 3,
        Weapons = 4,
        Wolves = 5,
        Spiders = 6,
        Meteors = 7,
    }
}
