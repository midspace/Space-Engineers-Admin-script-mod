namespace midspace.adminscripts.Messages.Sync
{
    using ProtoBuf;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Definitions;
    using Sandbox.Game.Entities;
    using Sandbox.Game.Entities.Cube;
    using Sandbox.ModAPI;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using VRage;
    using VRage.Game;
    using VRage.Game.Entity;
    using VRage.Game.ModAPI;
    using VRageMath;

    [ProtoContract]
    public class MessageSyncMirror : MessageBase
    {
        [ProtoMember(201)]
        public long ShipEntityId;

        [ProtoMember(202)]
        public bool RedAxis;

        [ProtoMember(203)]
        public bool GreenAxis;

        [ProtoMember(204)]
        public bool BlueAxis;

        [ProtoMember(205)]
        public bool XSymmetryOdd;

        // The SymmetryPlane and SymmetryOdd need to be serialized, because they don't exist on the Server side.
        // Each Client apppears to keep their own symmetry setting.

        [ProtoMember(206)]
        public SerializableVector3I? XSymmetryPlane;   

        [ProtoMember(207)]
        public bool YSymmetryOdd;

        [ProtoMember(208)]
        public SerializableVector3I? YSymmetryPlane;

        [ProtoMember(209)]
        public bool ZSymmetryOdd;

        [ProtoMember(210)]
        public SerializableVector3I? ZSymmetryPlane;

        [ProtoMember(211)]
        public SerializableVector3I CubePosition;

        [ProtoMember(212)]
        public bool OneWay;

        public static bool AddMirror(long entityId, bool redAxis, bool greenAxis, bool blueAxis, bool xSymmetryOdd, SerializableVector3I? xSymmetryPlane, bool ySymmetryOdd, SerializableVector3I? ySymmetryPlane, bool zSymmetryOdd, SerializableVector3I? zSymmetryPlane, Vector3I cubePosition, bool oneWay)
        {
            MessageSyncMirror syncEntity = new MessageSyncMirror
            {
                ShipEntityId = entityId,
                RedAxis = redAxis,
                GreenAxis = greenAxis,
                BlueAxis = blueAxis,
                XSymmetryOdd = xSymmetryOdd,
                XSymmetryPlane = xSymmetryPlane,
                YSymmetryOdd = ySymmetryOdd,
                YSymmetryPlane = ySymmetryPlane,
                ZSymmetryOdd = zSymmetryOdd,
                ZSymmetryPlane = zSymmetryPlane,
                CubePosition = cubePosition,
                OneWay = oneWay
            };

            if (MyAPIGateway.Multiplayer.MultiplayerActive && !MyAPIGateway.Multiplayer.IsServer)
                ConnectionHelper.SendMessageToServer(syncEntity);
            else
                syncEntity.CommonProcess(syncEntity.SenderSteamId, syncEntity.ShipEntityId, syncEntity.RedAxis, syncEntity.GreenAxis, syncEntity.BlueAxis, syncEntity.XSymmetryOdd, syncEntity.XSymmetryPlane, syncEntity.YSymmetryOdd, syncEntity.YSymmetryPlane, syncEntity.ZSymmetryOdd, syncEntity.ZSymmetryPlane, syncEntity.CubePosition, syncEntity.OneWay);
            return true;
        }

        public override void ProcessClient()
        {
            // never processed
        }

        public override void ProcessServer()
        {
            CommonProcess(SenderSteamId, ShipEntityId, RedAxis, GreenAxis, BlueAxis, XSymmetryOdd, XSymmetryPlane, YSymmetryOdd, YSymmetryPlane, ZSymmetryOdd, ZSymmetryPlane, CubePosition, OneWay);
        }

        private bool CommonProcess(ulong steamId, long entityId, bool redAxis, bool greenAxis, bool blueAxis, bool xSymmetryOdd, SerializableVector3I? xSymmetryPlane, bool ySymmetryOdd, SerializableVector3I? ySymmetryPlane, bool zSymmetryOdd, SerializableVector3I? zSymmetryPlane, Vector3I cubePosition, bool oneWay)
        {
            if (!MyAPIGateway.Entities.EntityExists(entityId))
                return false;

            IMyCubeGrid shipEntity = (IMyCubeGrid)MyAPIGateway.Entities.GetEntityById(entityId);

            int count = 0;

            //MyCubeGrid shipGrid = (MyCubeGrid)shipEntity;

            // TODO: use cubePosition with oneWay.

            if (redAxis && xSymmetryPlane.HasValue)
            {
                MirrorDirection xMirror = xSymmetryOdd ? MirrorDirection.EvenDown : MirrorDirection.Odd;
                int xAxis = xSymmetryPlane.Value.X;
                var cubes = MirrorCubes(shipEntity, true, xMirror, xAxis, MirrorDirection.None, 0, MirrorDirection.None, 0).ToArray();
                foreach (MyObjectBuilder_CubeBlock cube in cubes)
                {
                    IMySlimBlock block = shipEntity.AddBlock(cube, false);
                    // TODO: WTF do I need to do to get this to Sync from Server to Clients?  ***KEEN!!!***
                }
                count += cubes.Length;

                // TODO: mirror BlockGroups
            }
            if (greenAxis && ySymmetryPlane.HasValue)
            {
                MirrorDirection yMirror = ySymmetryOdd ? MirrorDirection.EvenDown : MirrorDirection.Odd;
                int yAxis = ySymmetryPlane.Value.Y;
                var cubes = MirrorCubes(shipEntity, true, MirrorDirection.None, 0, yMirror, yAxis, MirrorDirection.None, 0).ToArray();
                foreach (var cube in cubes)
                {
                    IMySlimBlock block = shipEntity.AddBlock(cube, false);
                    // TODO: WTF do I need to do to get this to Sync from Server to Clients?  ***KEEN!!!***
                }
                count += cubes.Length;

                // TODO: mirror BlockGroups
            }
            if (blueAxis && zSymmetryPlane.HasValue)
            {
                MirrorDirection zMirror = zSymmetryOdd ? MirrorDirection.EvenUp : MirrorDirection.Odd;
                int zAxis = zSymmetryPlane.Value.Z;
                var cubes = MirrorCubes(shipEntity, true, MirrorDirection.None, 0, MirrorDirection.None, 0, zMirror, zAxis).ToArray();
                foreach (var cube in cubes)
                {
                    IMySlimBlock block = shipEntity.AddBlock(cube, false);
                    // TODO: WTF do I need to do to get this to Sync from Server to Clients?  ***KEEN!!!***
                }
                count += cubes.Length;

                // TODO: mirror BlockGroups
            }

            //shipEntity.Synchronized = false;
            //shipGrid.IsReadyForReplication = true;
            //Sandbox.Game.Entities.MyEntities.RaiseEntityAdd((MyEntity)shipEntity);
            //Sandbox.Game.Entities.MyEntities.RaiseEntityCreated((MyEntity)shipEntity);

            //shipGrid.SyncFlag = true;
            ////shipGrid.rep

            MyAPIGateway.Utilities.SendMessage(steamId, "Server", $"Mirror has added {count} new blocks.");

            return true;
        }

        private static IEnumerable<MyObjectBuilder_CubeBlock> MirrorCubes(IMyCubeGrid shipEntity, bool integrate, MirrorDirection xMirror, int xAxis, MirrorDirection yMirror, int yAxis, MirrorDirection zMirror, int zAxis)
        {
            var blocks = new List<MyObjectBuilder_CubeBlock>();

            if (xMirror == MirrorDirection.None && yMirror == MirrorDirection.None && zMirror == MirrorDirection.None)
                return blocks;

            List<IMySlimBlock> cubeBlocks = new List<IMySlimBlock>();
            shipEntity.GetBlocks(cubeBlocks);
            foreach (var block in cubeBlocks)
            {
                MyObjectBuilder_CubeBlock newBlock = (MyObjectBuilder_CubeBlock)block.GetObjectBuilder().Clone();

                // Need to use the Min from the GetObjectBuilder(), as block.Position is NOT representative.
                Vector3I oldMin = newBlock.Min;

                newBlock.EntityId = 0;

                MyObjectBuilder_MotorBase motorBase = newBlock as MyObjectBuilder_MotorBase;
                if (motorBase != null)
                    motorBase.RotorEntityId = 0;

                MyObjectBuilder_PistonBase pistonBase = newBlock as MyObjectBuilder_PistonBase;
                if (pistonBase != null)
                    pistonBase.TopBlockId = 0;

                MyCubeBlockDefinition definition = MyDefinitionManager.Static.GetCubeBlockDefinition(newBlock);

                MyCubeBlockDefinition mirrorDefinition;
                MirrorCubeOrientation(definition, block.Orientation, xMirror, yMirror, zMirror, out mirrorDefinition, out newBlock.BlockOrientation);

                newBlock.SubtypeName = mirrorDefinition.Id.SubtypeName;


                if (definition.Size.X == 1 && definition.Size.Y == 1 && definition.Size.Z == 1)
                {
                    newBlock.Min = oldMin.Mirror(xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);
                }
                else
                {
                    // resolve size of component, and transform to original orientation.
                    var orientSize = definition.Size.Add(-1).Transform(block.Orientation).Abs();

                    var min = oldMin.Mirror(xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);
                    var blockMax = new SerializableVector3I(oldMin.X + orientSize.X, oldMin.Y + orientSize.Y, oldMin.Z + orientSize.Z);
                    var max = ((Vector3I)blockMax).Mirror(xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);

                    if (xMirror != MirrorDirection.None)
                        newBlock.Min = new SerializableVector3I(max.X, min.Y, min.Z);
                    if (yMirror != MirrorDirection.None)
                        newBlock.Min = new SerializableVector3I(min.X, max.Y, min.Z);
                    if (zMirror != MirrorDirection.None)
                        newBlock.Min = new SerializableVector3I(min.X, min.Y, max.Z);
                }

                Vector3I newPosition = ComputePositionInGrid(new MatrixI(newBlock.BlockOrientation), mirrorDefinition, newBlock.Min);

                // Don't place a block if one already exists there in the mirror.
                if (integrate && cubeBlocks.Any(b => b.Position.X == newPosition.X && b.Position.Y == newPosition.Y && b.Position.Z == newPosition.Z))
                    continue;

                //VRage.Utils.MyLog.Default.WriteLineAndConsole($"## CHECK ## new block {newBlock.SubtypeName}");

                blocks.Add(newBlock);

                // Alternate to using AddBlock().
                //Quaternion q;
                //((MyBlockOrientation)newBlock.BlockOrientation).GetQuaternion(out q);
                //((MyCubeGrid)shipEntity).BuildGeneratedBlock(new MyCubeGrid.MyBlockLocation(mirrorDefinition.Id, newBlock.Min, newBlock.Min, newBlock.Min, q, 0, 0) , newBlock.ColorMaskHSV);
            }
            return blocks;
        }

        private static Vector3I ComputePositionInGrid(MatrixI localMatrix, MyCubeBlockDefinition blockDefinition, Vector3I min)
        {
            Vector3I center = blockDefinition.Center;
            Vector3I vector3I = blockDefinition.Size - 1;
            Vector3I value;
            Vector3I.TransformNormal(ref vector3I, ref localMatrix, out value);
            Vector3I a;
            Vector3I.TransformNormal(ref center, ref localMatrix, out a);
            Vector3I vector3I2 = Vector3I.Abs(value);
            Vector3I result = a + min;

            if (value.X != vector3I2.X) result.X += vector3I2.X;
            if (value.Y != vector3I2.Y) result.Y += vector3I2.Y;
            if (value.Z != vector3I2.Z) result.Z += vector3I2.Z;

            return result;
        }

        private static void MirrorCubeOrientation(MyCubeBlockDefinition definition, SerializableBlockOrientation orientation, MirrorDirection xMirror, MirrorDirection yMirror, MirrorDirection zMirror, out MyCubeBlockDefinition mirrorDefinition, out SerializableBlockOrientation mirrorOrientation)
        {
            if (string.IsNullOrEmpty(definition.MirroringBlock))
                mirrorDefinition = definition;
            else
            {
                var definitionId = new MyDefinitionId(definition.Id.TypeId, definition.MirroringBlock);
                mirrorDefinition = MyDefinitionManager.Static.GetCubeBlockDefinition(definitionId);
            }

            Matrix sourceMatrix = Matrix.CreateFromDir(Base6Directions.GetVector(orientation.Forward), Base6Directions.GetVector(orientation.Up));
            Matrix targetMatrix;

            Vector3 mirrorNormal = Vector3.Zero;
            if (xMirror != MirrorDirection.None)
                mirrorNormal = Vector3.Right;
            else if (yMirror != MirrorDirection.None)
                mirrorNormal = Vector3.Up;
            else if (zMirror != MirrorDirection.None)
                mirrorNormal = Vector3.Forward;

            MySymmetryAxisEnum blockMirrorAxis = MySymmetryAxisEnum.None;

            if (MathHelper.IsZero(Math.Abs(Vector3.Dot(sourceMatrix.Right, mirrorNormal)) - 1.0f))
                blockMirrorAxis = MySymmetryAxisEnum.X;
            else if (MathHelper.IsZero(Math.Abs(Vector3.Dot(sourceMatrix.Up, mirrorNormal)) - 1.0f))
                blockMirrorAxis = MySymmetryAxisEnum.Y;
            else if (MathHelper.IsZero(Math.Abs(Vector3.Dot(sourceMatrix.Forward, mirrorNormal)) - 1.0f))
                blockMirrorAxis = MySymmetryAxisEnum.Z;

            MySymmetryAxisEnum blockMirrorOption = MySymmetryAxisEnum.None;
            switch (blockMirrorAxis)
            {
                case MySymmetryAxisEnum.X: blockMirrorOption = definition.SymmetryX; break;
                case MySymmetryAxisEnum.Y: blockMirrorOption = definition.SymmetryY; break;
                case MySymmetryAxisEnum.Z: blockMirrorOption = definition.SymmetryZ; break;
                default:
                    throw new Exception("Invalid mirror option");
            }

            switch (blockMirrorOption)
            {
                case MySymmetryAxisEnum.X:
                    targetMatrix = Matrix.CreateRotationX(MathHelper.Pi) * sourceMatrix;
                    break;
                case MySymmetryAxisEnum.Y:
                case MySymmetryAxisEnum.YThenOffsetX:
                    targetMatrix = Matrix.CreateRotationY(MathHelper.Pi) * sourceMatrix;
                    break;
                case MySymmetryAxisEnum.Z:
                case MySymmetryAxisEnum.ZThenOffsetX:
                    targetMatrix = Matrix.CreateRotationZ(MathHelper.Pi) * sourceMatrix;
                    break;
                case MySymmetryAxisEnum.HalfX:
                    targetMatrix = Matrix.CreateRotationX(-MathHelper.PiOver2) * sourceMatrix;
                    break;
                case MySymmetryAxisEnum.HalfY:
                    targetMatrix = Matrix.CreateRotationY(-MathHelper.PiOver2) * sourceMatrix;
                    break;
                case MySymmetryAxisEnum.HalfZ:
                    targetMatrix = Matrix.CreateRotationZ(-MathHelper.PiOver2) * sourceMatrix;
                    break;
                case MySymmetryAxisEnum.XHalfY:
                    targetMatrix = Matrix.CreateRotationX(MathHelper.Pi) * sourceMatrix;
                    targetMatrix = Matrix.CreateRotationY(MathHelper.PiOver2) * targetMatrix;
                    break;
                case MySymmetryAxisEnum.YHalfY:
                    targetMatrix = Matrix.CreateRotationY(MathHelper.Pi) * sourceMatrix;
                    targetMatrix = Matrix.CreateRotationY(MathHelper.PiOver2) * targetMatrix;
                    break;
                case MySymmetryAxisEnum.ZHalfY:
                    targetMatrix = Matrix.CreateRotationZ(MathHelper.Pi) * sourceMatrix;
                    targetMatrix = Matrix.CreateRotationY(MathHelper.PiOver2) * targetMatrix;
                    break;
                case MySymmetryAxisEnum.XHalfX:
                    targetMatrix = Matrix.CreateRotationX(MathHelper.Pi) * sourceMatrix;
                    targetMatrix = Matrix.CreateRotationX(-MathHelper.PiOver2) * targetMatrix;
                    break;
                case MySymmetryAxisEnum.YHalfX:
                    targetMatrix = Matrix.CreateRotationY(MathHelper.Pi) * sourceMatrix;
                    targetMatrix = Matrix.CreateRotationX(-MathHelper.PiOver2) * targetMatrix;
                    break;
                case MySymmetryAxisEnum.ZHalfX:
                    targetMatrix = Matrix.CreateRotationZ(MathHelper.Pi) * sourceMatrix;
                    targetMatrix = Matrix.CreateRotationX(-MathHelper.PiOver2) * targetMatrix;
                    break;
                case MySymmetryAxisEnum.XHalfZ:
                    targetMatrix = Matrix.CreateRotationX(MathHelper.Pi) * sourceMatrix;
                    targetMatrix = Matrix.CreateRotationZ(-MathHelper.PiOver2) * targetMatrix;
                    break;
                case MySymmetryAxisEnum.YHalfZ:
                    targetMatrix = Matrix.CreateRotationY(MathHelper.Pi) * sourceMatrix;
                    targetMatrix = Matrix.CreateRotationZ(-MathHelper.PiOver2) * targetMatrix;
                    break;
                case MySymmetryAxisEnum.ZHalfZ:
                    targetMatrix = Matrix.CreateRotationZ(MathHelper.Pi) * sourceMatrix;
                    targetMatrix = Matrix.CreateRotationZ(-MathHelper.PiOver2) * targetMatrix;
                    break;
                case MySymmetryAxisEnum.XMinusHalfZ:
                    targetMatrix = Matrix.CreateRotationX(MathHelper.Pi) * sourceMatrix;
                    targetMatrix = Matrix.CreateRotationZ(MathHelper.PiOver2) * targetMatrix;
                    break;
                case MySymmetryAxisEnum.YMinusHalfZ:
                    targetMatrix = Matrix.CreateRotationY(MathHelper.Pi) * sourceMatrix;
                    targetMatrix = Matrix.CreateRotationZ(MathHelper.PiOver2) * targetMatrix;
                    break;
                case MySymmetryAxisEnum.ZMinusHalfZ:
                    targetMatrix = Matrix.CreateRotationZ(MathHelper.Pi) * sourceMatrix;
                    targetMatrix = Matrix.CreateRotationZ(MathHelper.PiOver2) * targetMatrix;
                    break;
                case MySymmetryAxisEnum.XMinusHalfX:
                    targetMatrix = Matrix.CreateRotationX(MathHelper.Pi) * sourceMatrix;
                    targetMatrix = Matrix.CreateRotationX(MathHelper.PiOver2) * targetMatrix;
                    break;
                case MySymmetryAxisEnum.YMinusHalfX:
                    targetMatrix = Matrix.CreateRotationY(MathHelper.Pi) * sourceMatrix;
                    targetMatrix = Matrix.CreateRotationX(MathHelper.PiOver2) * targetMatrix;
                    break;
                case MySymmetryAxisEnum.ZMinusHalfX:
                    targetMatrix = Matrix.CreateRotationZ(MathHelper.Pi) * sourceMatrix;
                    targetMatrix = Matrix.CreateRotationX(MathHelper.PiOver2) * targetMatrix;
                    break;
                case MySymmetryAxisEnum.MinusHalfX:
                    targetMatrix = Matrix.CreateRotationX(MathHelper.PiOver2) * sourceMatrix;
                    break;
                case MySymmetryAxisEnum.MinusHalfY:
                    targetMatrix = Matrix.CreateRotationY(MathHelper.PiOver2) * sourceMatrix;
                    break;
                case MySymmetryAxisEnum.MinusHalfZ:
                    targetMatrix = Matrix.CreateRotationZ(MathHelper.PiOver2) * sourceMatrix;
                    break;
                default: // or MySymmetryAxisEnum.None
                    targetMatrix = sourceMatrix;
                    break;
            }

            // Note, the Base6Directions methods call GetDirection(), which rounds off the vector, which should prevent floating point errors creeping in.
            mirrorOrientation = new SerializableBlockOrientation(Base6Directions.GetForward(ref targetMatrix), Base6Directions.GetUp(ref targetMatrix));
        }
    }
}
