namespace System.Windows.Media.Media3D
{
    /// <summary>
    /// Extension methods for MeshGeometry3D
    /// </summary>
    public static class Point3DExtensions
    {
        /// <summary>
        /// Adds the data of the given mesh
        /// </summary>
        /// <param name="v">This point</param>
        /// <param name="u">Another point</param>
        /// <param name="tolerance">Tolerance</param>
        public static bool ApproxEqual(this Point3D v, Point3D u, double tolerance)
        {
            return  (Math.Abs(v.X - u.X) <= tolerance) &&
                    (Math.Abs(v.Y - u.Y) <= tolerance) &&
                    (Math.Abs(v.Z - u.Z) <= tolerance);
        }


        /// <summary>
        /// Converts to Vector3D
        /// </summary>
        /// <param name="v">This point</param>
        public static Vector3D ToVector3D(this Point3D v)
        {
            return new Vector3D(v.X, v.Y, v.Z);
        }
    }
}
