namespace midspace.adminscripts.Messages.Sync
{
    using System;
    using System.Collections.Generic;
    using ProtoBuf;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Common.ObjectBuilders.Definitions;
    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using Sandbox.ModAPI.Interfaces;
    using VRage;
    using VRage.Game;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;
    using VRageMath;

    /// <summary>
    /// Ares ; God of war, bloodshed, and violence.
    /// Message Sync for causing mayhem, destruction and death.
    /// </summary>
    [ProtoContract]
    public class MessageSyncAres : MessageBase
    {
        static readonly Random _random = new Random();

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

        public static void Slap(ulong steamId)
        {
            Process(new MessageSyncAres { SyncType = SyncAresType.Slap, SteamId = steamId });
        }

        public static void ThrowBomb(ulong steamId)
        {
            Process(new MessageSyncAres { SyncType = SyncAresType.Bomb, SteamId = steamId });
        }

        public static void ThrowMeteor(ulong steamId, string oreMaterial)
        {
            Process(new MessageSyncAres { SyncType = SyncAresType.Meteor, SteamId = steamId, OreMaterial = oreMaterial });
        }

        public static void Eject(ulong steamId)
        {
            Process(new MessageSyncAres { SyncType = SyncAresType.Eject, SteamId = steamId });
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
            var player = MyAPIGateway.Players.FindPlayerBySteamId(steamId);
            if (player == null)
                return;

            switch (syncType)
            {
                case SyncAresType.Bomb:
                    ThrowBomb(player);
                    break;

                case SyncAresType.Meteor:
                    ThrowMeteor(MyAPIGateway.Players.FindPlayerBySteamId(steamId), oreMaterial);
                    break;

                case SyncAresType.Slay:
                    Slay(player);
                    break;

                case SyncAresType.Smite:
                    Smite(player, OreMaterial);
                    break;

                case SyncAresType.Slap:
                    Slap(player);
                    break;

                case SyncAresType.Eject:
                    Eject(player);
                    break;
            }
        }

        private void Slay(IMyPlayer player)
        {
            if (player.KillPlayer(MyDamageType.Environment))
                MyAPIGateway.Utilities.SendMessage(SenderSteamId, "slaying", player.DisplayName);
            else
                MyAPIGateway.Utilities.SendMessage(SenderSteamId, "Failed", "Could not slay '{0}'. Player may be dead already.", player.DisplayName);
        }

        private void Slap(IMyPlayer player)
        {
            var character = player.GetCharacter();
            var destroyable = character as IMyDestroyableObject;
            if (destroyable == null)
            {
                MyAPIGateway.Utilities.SendMessage(SenderSteamId, "Failed", "Could not slap '{0}'. Player may be dead.", player.DisplayName);
                return;
            }

            MyAPIGateway.Utilities.SendMessage(SenderSteamId, "slapping", player.DisplayName);
            MyAPIGateway.Utilities.SendMessage(player.SteamUserId, "Server", "You were slapped");

            if (destroyable.Integrity > 1f)
                destroyable.DoDamage(1f, MyDamageType.Environment, true);

            // TODO: does not work on the server. Will need to be implmented on Client.
            //var physics = ((IMyEntity)character).Physics;
            //if (physics != null)
            //{
            //    Vector3 random = new Vector3();
            //    random.X = 2.0f * (float)_random.NextDouble() - 1.0f;
            //    random.Y = 2.0f * (float)_random.NextDouble() - 1.0f;
            //    random.Z = 2.0f * (float)_random.NextDouble() - 1.0f;
            //    random.Normalize();
            //    physics.ApplyImpulse(random * 500, Vector3.Zero); // 5m/s.
            //}
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

        private void Eject(IMyPlayer player)
        {
            if (player.Controller.ControlledEntity.Entity.Parent != null)
            {
                MyAPIGateway.Utilities.SendMessage(SenderSteamId, "ejecting", player.DisplayName);

                // this will find the cockpit the player's character is inside of.
                var character = player.GetCharacter();
                if (character != null)
                {
                    var baseController = ((IMyEntity)character).Parent as IMyControllableEntity;

                    if (baseController != null) // this will eject the Character from the cockpit.
                        baseController.Use();
                }

                player.Controller.ControlledEntity.Use();

                //// Enqueue the command a second time, to make sure the player is ejected from a remote controlled ship and a piloted ship.
                //_workQueue.Enqueue(delegate ()
                //{
                //    if (selectedPlayer.Controller.ControlledEntity.Entity.Parent != null)
                //    {
                //        selectedPlayer.Controller.ControlledEntity.Use();
                //    }
                //});

                // Neither of these do what I expect them to. In fact, I'm not sure what they do.
                //MyAPIGateway.Players.RemoveControlledEntity(player.Controller.ControlledEntity.Entity);
                //MyAPIGateway.Players.RemoveControlledEntity(player.Controller.ControlledEntity.Entity.Parent);
            }
            else
            {
                MyAPIGateway.Utilities.SendMessage(SenderSteamId, "player", "{0} is not a pilot", player.DisplayName);
            }
        }

        public enum SyncAresType
        {
            Smite,
            Slay,
            Slap,
            Bomb,
            Meteor,
            Eject
        }
    }
}