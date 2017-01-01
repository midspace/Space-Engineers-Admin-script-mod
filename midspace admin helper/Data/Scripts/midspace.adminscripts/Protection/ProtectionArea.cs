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
            switch (Shape)
            {
                case ProtectionAreaShape.Cube:
                    var boundingBox = new BoundingBoxD(new Vector3D(Center.X - Size, Center.Y - Size, Center.Z - Size), new Vector3D(Center.X + Size, Center.Y + Size, Center.Z + Size));
                    return boundingBox.Intersects(entity.WorldAABB);
                case ProtectionAreaShape.Sphere:
                    var boundingSphere = new BoundingSphereD(Center, Size);
                    return boundingSphere.Intersects(entity.WorldAABB);
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

            BoundingBoxD box;
            block.GetWorldBoundingBox(out box);

            switch (Shape)
            {
                case ProtectionAreaShape.Cube:
                    var boundingBox = new BoundingBoxD(new Vector3D(Center.X - Size, Center.Y - Size, Center.Z - Size), new Vector3D(Center.X + Size, Center.Y + Size, Center.Z + Size));
                    return boundingBox.Intersects(box);
                case ProtectionAreaShape.Sphere:
                    var boundingSphere = new BoundingSphereD(Center, Size);
                    return boundingSphere.Intersects(box);
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
