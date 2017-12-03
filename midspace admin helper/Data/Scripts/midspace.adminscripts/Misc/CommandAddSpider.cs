namespace midspace.adminscripts
{
    using midspace.adminscripts.Messages.Sync;
    using Sandbox.ModAPI;
    using VRage;
    using VRageMath;

    public class CommandAddSpider : ChatCommand
    {
        public CommandAddSpider()
            : base(ChatCommandSecurity.Admin, "addspider", new[] { "/addspider" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/addspider", "Spawns a space spider in front of you");
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

            MessageSyncAres.SpawnBot(steamId, "SpaceSpider", new SerializableMatrix(worldMatrix));
            return true;
        }
    }
}
