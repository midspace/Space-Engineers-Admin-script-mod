using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.VRageData;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using VRage;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace midspace.adminscripts.Protection
{
    [ProtoContract]
    public class ProtectionArea
    {
        [ProtoMember(1)] 
        public string Name;

        [ProtoMember(2)]
        public Vector3D Center;

        [ProtoMember(3)]
        public double Size;

        [ProtoMember(4)]
        public ProtectionAreaShape Shape;

        // parameterless ctor for xml serialization
        public ProtectionArea() { }

        public ProtectionArea(string name, Vector3D center, double size, ProtectionAreaShape shape)
        {
            Name = name;
            Center = center;
            Size = size;
            Shape = shape;
        }

        public bool Contains(IMyEntity entity)
        {
            switch (Shape)
            {
                case ProtectionAreaShape.Cube:
                    var boundingBox = new BoundingBoxD(new Vector3D(Center.X - Size, Center.Y - Size, Center.Z - Size), new Vector3D(Center.X + Size, Center.Y + Size, Center.Z + Size));
                    return boundingBox.Intersects(entity.WorldAABB);
                case ProtectionAreaShape.Sphere:
                    var boundingSphere = new BoundingSphereD(Center, Size);
                    return entity.GetIntersectionWithSphere(ref boundingSphere); //return boundingSphere.Intersects(entity.WorldAABB);
            }

            return false;
        }

        public bool Contains(IMySlimBlock block)
        {
            if (block.CubeGrid == null)
                return false;

            // FatBlock is null for non functional blocks such as armor
            if (block.FatBlock != null)
                return Contains(block.FatBlock);

            // since some blocks aren't an entity we have to deal with them otherwise
            // we'll create an grid that acts as substitute for the block to get an entity that has a collision
            // TODO find a better suited way to get the boundings of an armor block
            var worldPosition = block.CubeGrid.GetPosition() + block.Position;

            // creating grid
            var gridBuilder = new MyObjectBuilder_CubeGrid()
            {
                PersistentFlags = MyPersistentEntityFlags2.None,
                PositionAndOrientation = new MyPositionAndOrientation(worldPosition, Vector3.Forward, Vector3.Up),
                DisplayName = "substitute",
                GridSizeEnum = block.CubeGrid.GridSizeEnum
            };

            // creating cube block
            MyObjectBuilder_CubeBlock cube = new MyObjectBuilder_CubeBlock();
            cube.Min = block.Position;
            cube.SubtypeName = block.CubeGrid.GridSizeEnum == MyCubeSize.Large ? "LargeBlockArmorBlock" : "SmallBlockArmorBlock";
            cube.BuildPercent = 0;
            gridBuilder.CubeBlocks.Add(cube);

            // add grid
            MyAPIGateway.Entities.RemapObjectBuilder(gridBuilder);
            var entity = MyAPIGateway.Entities.CreateFromObjectBuilder(gridBuilder);
            var grid = entity as IMyCubeGrid;
            var blocks = new List<IMySlimBlock>();

            // get blocks
            grid.GetBlocks(blocks, b => b != null);

            if (blocks.Count > 0)
            {
                // found block
                var cubeBlock = blocks[0].FatBlock;
                bool isInside = Contains(cubeBlock);
                grid.Close();
                return isInside;
            }

            return false;
        }
    }

    public enum ProtectionAreaShape
    {
        Sphere,
        Cube
    }
}
