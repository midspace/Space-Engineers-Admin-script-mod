namespace midspace.adminscripts
{
    using midspace.adminscripts.Messages.Sync;
    using Sandbox.ModAPI;
    using VRage;
    using VRageMath;

    public class CommandMeteor : ChatCommand
    {
        private readonly string _defaultOreName;

        public CommandMeteor(string defaultOreName)
            : base(ChatCommandSecurity.Admin, "throwmeteor", new[] { "/throwmeteor", "/meteor" })
        {
            _defaultOreName = defaultOreName;
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/throwmeteor", "Throws a meteor in the direction you face");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            MatrixD worldMatrix;
            if (MyAPIGateway.Session.CameraController is MySpectator)
            {
                worldMatrix = MyAPIGateway.Session.Camera.WorldMatrix;
            }
            else if (MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.Parent == null)
            {
                worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.GetHeadMatrix(true, true, false); // dead center of player cross hairs.
                worldMatrix.Translation = worldMatrix.Translation + worldMatrix.Forward * 2.5f; // Spawn item 1.5m in front of player for safety.
            }
            else
            {
                worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.WorldMatrix;
                worldMatrix.Translation = worldMatrix.Translation + worldMatrix.Forward * 2.5f + worldMatrix.Up * 0.5f; // Spawn item 1.5m in front of player in cockpit for safety.
            }

            MessageSyncAres.ThrowMeteor(steamId, _defaultOreName, new SerializableMatrix(worldMatrix));
            return true;
        }
    }
}
