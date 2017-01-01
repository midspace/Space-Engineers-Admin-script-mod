namespace midspace.adminscripts.Protection
{
    using System.Collections.Generic;
    using ProtoBuf;
    using Sandbox.ModAPI;
    using VRage;
    using VRage.Game;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;
    using VRageMath;

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
            return Contains(entity.WorldAABB);
        }

        public bool Contains(IMySlimBlock block)
        {
            if (block.CubeGrid == null)
                return false;

            // FatBlock is null for non functional blocks such as armor
            if (block.FatBlock != null)
                return Contains(block.FatBlock);

            BoundingBoxD box;
            block.GetWorldBoundingBox(out box);
            return Contains(box);
        }

        public bool Contains(BoundingBoxD boundingbox)
        {
            switch (Shape)
            {
                case ProtectionAreaShape.Cube:
                    var boundingBox = new BoundingBoxD(Center - Size, Center + Size);
                    return boundingBox.Intersects(boundingbox);
                case ProtectionAreaShape.Sphere:
                    var boundingSphere = new BoundingSphereD(Center, Size);
                    return boundingSphere.Intersects(boundingbox);
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
