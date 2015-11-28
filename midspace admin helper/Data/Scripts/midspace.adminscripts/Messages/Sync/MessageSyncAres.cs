namespace midspace.adminscripts.Messages.Sync
{
    using System.Collections.Generic;
    using ProtoBuf;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Common.ObjectBuilders.Definitions;
    using Sandbox.Common.ObjectBuilders.VRageData;
    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using VRage;
    using VRage.ObjectBuilders;
    using VRageMath;

    /// <summary>
    /// Ares ; God of war, bloodshed, and violence.
    /// Message Sync for causing mayhem, destruction and death.
    /// </summary>
    [ProtoContract]
    public class MessageSyncAres : MessageBase
    {
        #region fields

        [ProtoMember(1)]
        public SyncAresType SyncType;

        [ProtoMember(2)]
        public ulong SteamId;

        [ProtoMember(3)]
        public string OreMaterial;

        #endregion

        #region Process

        public static void Smite(ulong steamId, string oreMaterial)
        {
            Process(new MessageSyncAres { SyncType = SyncAresType.Smite, SteamId = steamId, OreMaterial = oreMaterial });
        }

        public static void Slay(ulong steamId)
        {
            Process(new MessageSyncAres { SyncType = SyncAresType.Slay, SteamId = steamId });
        }

        public static void ThrowBomb(ulong steamId)
        {
            Process(new MessageSyncAres { SyncType = SyncAresType.Bomb, SteamId = steamId });
        }

        public static void ThrowMeteor(ulong steamId, string oreMaterial)
        {
            Process(new MessageSyncAres { SyncType = SyncAresType.Meteor, SteamId = steamId, OreMaterial = oreMaterial });
        }

        private static void Process(MessageSyncAres syncEntity)
        {
            if (MyAPIGateway.Multiplayer.MultiplayerActive)
                ConnectionHelper.SendMessageToServer(syncEntity);
            else
                syncEntity.CommonProcess(syncEntity.SyncType, syncEntity.SteamId, syncEntity.OreMaterial);
        }

        #endregion


        public override void ProcessClient()
        {
            // processed server only.
        }

        public override void ProcessServer()
        {
            CommonProcess(SyncType, SteamId, OreMaterial);
        }

        private void CommonProcess(SyncAresType syncType, ulong steamId, string oreMaterial)
        {
            var player = MyAPIGateway.Players.FindPlayerBySteamId(SteamId);
            if (player == null)
                return;

            switch (SyncType)
            {
                case SyncAresType.Bomb:
                    ThrowBomb(player);
                    break;

                case SyncAresType.Meteor:
                    ThrowMeteor(MyAPIGateway.Players.FindPlayerBySteamId(SteamId), OreMaterial);
                    break;

                case SyncAresType.Slay:
                    Slay(player);
                    break;

                case SyncAresType.Smite:
                    Smite(player, OreMaterial);
                    break;
            }
        }

        private void Slay(IMyPlayer player)
        {
            if (player.KillPlayer(MyDamageType.Environment))
                MyAPIGateway.Utilities.SendMessage(SenderSteamId, "slaying", player.DisplayName);
            else
                MyAPIGateway.Utilities.SendMessage(SenderSteamId, "could not slay", "{0} as player is Pilot. Use /eject first.", player.DisplayName);
        }

        private void Smite(IMyPlayer player, string oreName)
        {
            var worldMatrix = player.Controller.ControlledEntity.GetHeadMatrix(true, true, true);
            var maxspeed = MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed * 1.25f;

            var meteorBuilder = new MyObjectBuilder_Meteor
            {
                Item = new MyObjectBuilder_InventoryItem { Amount = 1, Content = new MyObjectBuilder_Ore { SubtypeName = oreName } },
                PersistentFlags = MyPersistentEntityFlags2.InScene, // Very important
                PositionAndOrientation = new MyPositionAndOrientation
                {
                    Position = (worldMatrix.Translation + worldMatrix.Up * -0.5f).ToSerializableVector3D(),
                    Forward = worldMatrix.Forward.ToSerializableVector3(),
                    Up = worldMatrix.Up.ToSerializableVector3(),
                },
                LinearVelocity = worldMatrix.Down * -maxspeed, // has to be faster than JetPack speed, otherwise it could be avoided.
                // Update 01.052 seemed to have flipped the direction. It's Up instead of Down???
                Integrity = 1
            };

            meteorBuilder.CreateAndSyncEntity();
        }

        private void ThrowBomb(IMyPlayer player)
        {
            if (player == null)
                return;

            MatrixD worldMatrix;
            Vector3D position;

            if (player.Controller.ControlledEntity.Entity.Parent == null)
            {
                worldMatrix = player.Controller.ControlledEntity.GetHeadMatrix(true, true, false); // dead center of player cross hairs.
                position = worldMatrix.Translation + worldMatrix.Forward * 2.5f; // Spawn item 1.5m in front of player for safety.
            }
            else
            {
                worldMatrix = player.Controller.ControlledEntity.Entity.WorldMatrix;
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
        }

        private void ThrowMeteor(IMyPlayer player, string oreName)
        {
            MatrixD worldMatrix;
            Vector3D position;

            if (player.Controller.ControlledEntity.Entity.Parent == null)
            {
                worldMatrix = player.Controller.ControlledEntity.GetHeadMatrix(true, true, false); // dead center of player cross hairs.
                position = worldMatrix.Translation + worldMatrix.Forward * 1.5f; // Spawn item 1.5m in front of player for safety.
            }
            else
            {
                worldMatrix = player.Controller.ControlledEntity.Entity.WorldMatrix;
                position = worldMatrix.Translation + worldMatrix.Forward * 1.5f + worldMatrix.Up * 0.5f; // Spawn item 1.5m in front of player in cockpit for safety.
            }

            var meteorBuilder = new MyObjectBuilder_Meteor()
            {
                Item = new MyObjectBuilder_InventoryItem()
                {
                    Amount = 10000,
                    Content = new MyObjectBuilder_Ore() { SubtypeName = oreName }
                },
                PersistentFlags = MyPersistentEntityFlags2.InScene, // Very important
                PositionAndOrientation = new MyPositionAndOrientation()
                {
                    Position = position,
                    Forward = (Vector3)worldMatrix.Forward,
                    Up = (Vector3)worldMatrix.Up,
                },
                LinearVelocity = worldMatrix.Forward * 500,
                Integrity = 100,
            };

            meteorBuilder.CreateAndSyncEntity();
        }
    }

    public enum SyncAresType
    {
        Smite,
        Slay,
        Bomb,
        Meteor
    }
}
