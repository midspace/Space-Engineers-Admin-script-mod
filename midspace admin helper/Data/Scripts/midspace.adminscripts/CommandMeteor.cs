namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;

    using Sandbox.Common.ObjectBuilders;
    using Sandbox.ModAPI;
    using VRageMath;

    public class CommandMeteor : ChatCommand
    {
        private readonly string[] _oreNames;

        public CommandMeteor(string[] oreNames)
            : base(ChatCommandSecurity.Admin, "meteor", new[] { "/meteor" })
        {
            _oreNames = oreNames;
        }

        public override void Help(bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/meteor", "Throws a meteor in the direction you face");
        }

        public override bool Invoke(string messageText)
        {
            if (messageText.Equals("/meteor", StringComparison.InvariantCultureIgnoreCase))
            {
                MatrixD worldMatrix;
                Vector3D position;

                if (MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.Parent == null)
                {
                    worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.GetHeadMatrix(true, true, true);
                    position = worldMatrix.Translation + worldMatrix.Forward * 1.5f; // Spawn meteor 1.5m in front of player for safety.
                }
                else
                {
                    worldMatrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.WorldMatrix;
                    position = worldMatrix.Translation + worldMatrix.Forward * 1.5f + worldMatrix.Up * 0.5f; // Spawn ore 1.5m in front of player in cockpit for safety.
                }

                var meteorBuilder = new MyObjectBuilder_Meteor()
                {
                    Item = new MyObjectBuilder_InventoryItem()
                    {
                        Amount = 10000,
                        Content = new MyObjectBuilder_Ore() { SubtypeName = _oreNames[0] }
                    },
                    PersistentFlags = MyPersistentEntityFlags2.InScene, // Very important
                    PositionAndOrientation = new MyPositionAndOrientation()
                    {
                        Position = position,
                        Forward = (Vector3)worldMatrix.Forward,
                        Up = (Vector3)worldMatrix.Up,
                    },
                    LinearVelocity = worldMatrix.Forward * 300,
                    Integrity = 100,
                };

                var tempList = new List<MyObjectBuilder_EntityBase> { meteorBuilder };
                MyAPIGateway.Entities.RemapObjectBuilderCollection(tempList);
                tempList.ForEach(grid => MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(grid));
                MyAPIGateway.Multiplayer.SendEntitiesCreated(tempList);
                return true;
            }

            return false;
        }
    }
}
