using ProtoBuf;
using VRageMath;

namespace midspace.adminscripts
{
    /// <summary>
    /// Serializable wrapper for the VRageMath.MatrixD as Keen have not provided 
    /// one, so we can serialize with ProtoBuf to a binary stream.
    /// </summary>
    [ProtoContract]
    public class SerializableMatrix
    {
        #region fields

        /// <summary>
        /// Value at row 1 column 1 of the matrix.
        /// </summary>
        [ProtoMember(1)]
        public double M11;

        /// <summary>
        /// Value at row 1 column 2 of the matrix.
        /// </summary>
        [ProtoMember(2)]
        public double M12;

        /// <summary>
        /// Value at row 1 column 3 of the matrix.
        /// </summary>
        [ProtoMember(3)]
        public double M13;

        /// <summary>
        /// Value at row 1 column 4 of the matrix.
        /// </summary>
        [ProtoMember(4)]
        public double M14;

        /// <summary>
        /// Value at row 2 column 1 of the matrix.
        /// </summary>
        [ProtoMember(5)]
        public double M21;

        /// <summary>
        /// Value at row 2 column 2 of the matrix.
        /// </summary>
        [ProtoMember(6)]
        public double M22;

        /// <summary>
        /// Value at row 2 column 3 of the matrix.
        /// </summary>
        [ProtoMember(7)]
        public double M23;

        /// <summary>
        /// Value at row 2 column 4 of the matrix.
        /// </summary>
        [ProtoMember(8)]
        public double M24;

        /// <summary>
        /// Value at row 3 column 1 of the matrix.
        /// </summary>
        [ProtoMember(9)]
        public double M31;

        /// <summary>
        /// Value at row 3 column 2 of the matrix.
        /// </summary>
        [ProtoMember(10)]
        public double M32;

        /// <summary>
        /// Value at row 3 column 3 of the matrix.
        /// </summary>
        [ProtoMember(11)]
        public double M33;

        /// <summary>
        /// Value at row 3 column 4 of the matrix.
        /// </summary>
        [ProtoMember(12)]
        public double M34;

        /// <summary>
        /// Value at row 4 column 1 of the matrix.
        /// </summary>
        [ProtoMember(13)]
        public double M41;

        /// <summary>
        /// Value at row 4 column 2 of the matrix.
        /// </summary>
        [ProtoMember(14)]
        public double M42;

        /// <summary>
        /// Value at row 4 column 3 of the matrix.
        /// </summary>
        [ProtoMember(15)]
        public double M43;

        /// <summary>
        /// Value at row 4 column 4 of the matrix.
        /// </summary>
        [ProtoMember(16)]
        public double M44; 

        #endregion

        public SerializableMatrix()
        {
        }

        public SerializableMatrix(MatrixD matrix)
        {
            M11 = matrix.M11;
            M12 = matrix.M12;
            M13 = matrix.M13;
            M14 = matrix.M14;
            M21 = matrix.M21;
            M22 = matrix.M22;
            M23 = matrix.M23;
            M24 = matrix.M24;
            M31 = matrix.M31;
            M32 = matrix.M32;
            M33 = matrix.M33;
            M34 = matrix.M34;
            M41 = matrix.M41;
            M42 = matrix.M42;
            M43 = matrix.M43;
            M44 = matrix.M44;
        }

        public static implicit operator SerializableMatrix(MatrixD matrix)
        {
            return new SerializableMatrix(matrix);
        }

        public static implicit operator MatrixD(SerializableMatrix v)
        {
            if (v == null)
                return new MatrixD();
            return new MatrixD(v.M11, v.M12, v.M13, v.M14, v.M21, v.M22, v.M23, v.M24, v.M31, v.M32, v.M33, v.M34, v.M41, v.M42, v.M43, v.M44);
        }
    }
}
