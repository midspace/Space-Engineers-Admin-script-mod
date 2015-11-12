namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Common.ObjectBuilders.VRageData;
    using Sandbox.ModAPI;
    using VRage;
    using VRage.ObjectBuilders;
    using VRageMath;

    public class CommandBomb : ChatCommand
    {
        public CommandBomb()
            : base(ChatCommandSecurity.Admin, "bomb", new[] { "/bomb" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/bomb", "Throws a warhead in the direction you face");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            MatrixD worldMatrix;
            Vector3D position;

            if (MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.Parent == null)
            {
                worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.GetHeadMatrix(true, true, false); // dead center of player cross hairs.
                position = worldMatrix.Translation + worldMatrix.Forward * 2.5f; // Spawn item 1.5m in front of player for safety.
            }
            else
            {
                worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.WorldMatrix;
                position = worldMatrix.Translation + worldMatrix.Forward * 2.5f + worldMatrix.Up * 0.5f; // Spawn item 1.5m in front of player in cockpit for safety.
            }

            // TODO: multiply vector against current entity.LinearVelocity.
            Vector3 vector = worldMatrix.Forward * 300;

            var gridObjectBuilder = new MyObjectBuilder_CubeGrid()
            {
                PersistentFlags = MyPersistentEntityFlags2.CastShadows | MyPersistentEntityFlags2.InScene,
                GridSizeEnum = MyCubeSize.Large,
                IsStatic = false,
                LinearVelocity = vector,
                AngularVelocity = new SerializableVector3(0, 0, 0),
                PositionAndOrientation = new MyPositionAndOrientation(position, Vector3.Forward, Vector3.Up),
                DisplayName = "Prepare to die."
            };

            MyObjectBuilder_Warhead cube = new MyObjectBuilder_Warhead()
            {
                Min = new SerializableVector3I(0, 0, 0),
                SubtypeName = "LargeWarhead",
                ColorMaskHSV = new SerializableVector3(0, -1, 0),
                EntityId = 0,
                Owner = 0,
                BlockOrientation = new SerializableBlockOrientation(Base6Directions.Direction.Forward, Base6Directions.Direction.Up),
                ShareMode = MyOwnershipShareModeEnum.All,
                CustomName = "Hello. My name is Inigo Montoya. You killed my father. Prepare to die.",
            };

            gridObjectBuilder.CubeBlocks.Add(cube);
            var tempList = new List<MyObjectBuilder_EntityBase>();
            tempList.Add(gridObjectBuilder);
            tempList.CreateAndSyncEntities();
            return true;
        }
    }
}
