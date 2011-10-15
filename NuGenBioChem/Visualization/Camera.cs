using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace NuGenBioChem.Visualization
{
    /// <summary>
    /// This is the implementation of camera 
    /// which allows us to rotate the observer 
    /// around a target like on an orb
    /// </summary>
    public class Camera
    {
        #region Cache

        // View matrix
        bool viewMatrixCached = false;
        Matrix3D viewMatrix;

        // Projection matrix
        bool projectionMatrixCached = false;
        Matrix3D projectionMatrix;
        

        #endregion

        #region Fields

        // Distance to the object
        double radius = 1.0f;

        // Angles, in radians
        double verticalRotation = 0.0f;
        double horisontalRotation = 0.0f;
        
        /// <summary>
        /// Field of view
        /// </summary>
        double fieldOfView = Math.PI / 3.0f;
        /// <summary>
        /// A ratio of width to height of the frame
        /// </summary>
        double aspect = 4.0f / 3.0f;
        /// <summary>
        /// The distance to near clipping plane
        /// </summary> 
        double near = 0.1f;
        /// <summary>
        /// The distance to far clipping plane
        /// </summary> 
        double far = 50000.0f;
        /// <summary>
        /// Camera target
        /// </summary>
        Point3D target = new Point3D(10, 0, 0);
        /// <summary>
        /// Defines up of the camera
        /// </summary>
        Vector3D up = new Vector3D(0, 10, 0);
        /// <summary>
        /// Mode of camera projection 
        /// </summary>
        CameraProjectionMode mode = CameraProjectionMode.Perspective;


        #endregion

        #region Properties

        /// <summary>
        /// Gets or set mode of projection
        /// </summary>
        public CameraProjectionMode Mode
        {
            get { return mode; }
            set
            {
                if (value == mode) return;
                mode = value;

                Invalidate();
            }
        }

        /// <summary>
        /// Field of view
        /// </summary>
        public double FieldOfView
        {
            get
            {
                return fieldOfView;
            }
            set
            {
                fieldOfView = value;
                Invalidate();
            }
        }

        /// <summary>
        /// A ratio of width to height of the frame
        /// </summary>
        public double Aspect
        {
            get
            {
                return aspect;
            }
            set
            {
                aspect = value;
                Invalidate();
            }
        }

        /// <summary>
        /// The distance to near clipping plane
        /// </summary>
        public double Near
        {
            get
            {
                return near;
            }
            set
            {
                near = value;
                Invalidate();
            }
        }

        /// <summary>
        /// The distance to far clipping plane
        /// </summary>
        public double Far
        {
            get
            {
                return far;
            }
            set
            {
                far = value;
                Invalidate();
            }
        }
        
        /// <summary>
        /// Gets or sets the camera target
        /// </summary>
        public virtual Point3D Target
        {
            get { return target; }
            set
            {
                target = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets up vector which defines up orientation of the camera
        /// </summary>
        public Vector3D Up
        {
            get { return up; }
            set
            {
                up = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Gets direction of the look
        /// </summary>
        public Vector3D LookDirection
        {
            get
            {
                Vector3D direction = Target - Position;
                direction.Normalize();
                return direction;
            }
        }

        /// <summary>
        /// The final matrix of view
        /// </summary>
        public Matrix3D View
        {
            get
            {
                if (!viewMatrixCached)
                {
                    Vector3D up = verticalRotation > Math.PI / 2 && verticalRotation < Math.PI / 2 * 3 ?
                        new Vector3D(0, -1, 0) :
                        new Vector3D(0, 1, 0);
                    
                    viewMatrix = CreateLookAtMatrix(Position, target, up);
                    viewMatrixCached = true;
                }
                return viewMatrix;
            }
        }

        /// <summary>
        /// Angle around target, in radians
        /// </summary>
        public double HorizontalRotation
        {
            get { return horisontalRotation; }
            set
            {
                horisontalRotation = value;

                // Keeps value between 0 and 2*PI 
                //horisontalRotation = horisontalRotation > Math.PI * 2.0 ? horisontalRotation - Math.PI * 2.0 : horisontalRotation;
                //horisontalRotation = horisontalRotation < 0 ? horisontalRotation + Math.PI * 2.0 : horisontalRotation;
                Invalidate();
            }
        }

        /// <summary>
        /// Angle up/down, in radians
        /// </summary>
        public double VerticalRotation
        {
            get { return verticalRotation; }
            set
            {
                verticalRotation = value;

                // Keeps value between 0 and 2*PI 
                /*verticalRotation = verticalRotation > Math.PI * 2 ? verticalRotation - Math.PI * 2 : verticalRotation;
                verticalRotation = verticalRotation < 0 ? verticalRotation + Math.PI * 2 : verticalRotation;*/
                // Keeps value between 0 and 2*PI 
                verticalRotation = verticalRotation > (Math.PI / 2.0 - 0.01) ? (Math.PI / 2.0 - 0.01) : verticalRotation;
                verticalRotation = verticalRotation < (-Math.PI / 2.0 + 0.01) ? (-Math.PI / 2.0 + 0.01) : verticalRotation;
                Invalidate();
            }
        }

        /// <summary>
        /// The distance to the target
        /// </summary>
        public double Distance
        {
            get { return radius; }
            set
            {
                radius = value;
                if (radius < 0.01f) radius = 0.01f;
                Invalidate();
            }
        }

        /// <summary>
        /// The observer's position
        /// </summary>
        public Point3D Position
        {
            get
            {
                Point3D position = new Point3D();
                position.X = (radius * Math.Cos(verticalRotation) * Math.Cos(horisontalRotation));
                position.Y = (radius * Math.Sin(verticalRotation));
                position.Z = (radius * Math.Cos(verticalRotation) * Math.Sin(horisontalRotation));
                position.Offset(target.X, target.Y, target.Z);
                return position;
            }
        }

        /// <summary>
        /// The final projection matrix
        /// </summary>
        public Matrix3D Projection
        {
            get
            {
                if (!projectionMatrixCached)
                {
                    // TODO: add support for orthogonal projection
                    double width = 2.0 * Distance * Math.Tan(FieldOfView / 2.0);

                    projectionMatrix = mode == CameraProjectionMode.Perspective ?
                        CreatePerspectiveProjection(fieldOfView, aspect, near, far) :
                        CreateOrthogonalProjection(width, width / aspect, near, far);
                    
                    projectionMatrixCached = true;
                }
                return projectionMatrix;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructot
        /// </summary>
        public Camera()
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Invalidates object
        /// </summary>
        public void Invalidate()
        {
            // Reset cache
            projectionMatrixCached = false;
            viewMatrixCached = false;

            // ...
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates orthogonal projection
        /// </summary>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        /// <param name="near">Distance to the near plane</param>
        /// <param name="far">Distance to the far plane (can be Double.PositiveInfinity)</param>
        /// <returns>Projection</returns>
        public static Matrix3D CreateOrthogonalProjection(double width, double height, double near, double far)
        {
            return new Matrix3D(2 / width, 0, 0, 0,
                                0, 2 / height, 0, 0,
                                0, 0, 1 / (near - far), 0,
                                0, 0, near / (near - far), 1);
        }

       
        /// <summary>
        /// Creates perspective projection
        /// </summary>
        /// <param name="fovY">Field of view</param>
        /// <param name="ratio">Width / Height</param>
        /// <param name="near">Distance to the near plane</param>
        /// <param name="far">Distance to the far plane (can be Double.PositiveInfinity)</param>
        /// <returns>Perspective projection</returns>
        public static Matrix3D CreatePerspectiveProjection(double fovY, double ratio, double near, double far)
        {
            double h = 1.0f / (float)Math.Tan(fovY / 2.0f);
            double w = h / ratio;

            if (far == Double.PositiveInfinity)
                return new Matrix3D(
                  w, 0, 0, 0,
                  0, h, 0, 0,
                  0, 0, 1, -near,
                  0, 0, 1, 0);

            return new Matrix3D(w, 0, 0, 0,
                               0, h, 0, 0,
                               0, 0, -far / (far - near), -1,
                               0, 0, -near * far / (far - near), 0);


        }


        /// <summary>
        /// Creates view matrix
        /// </summary>
        /// <param name="eye">Eye position</param>
        /// <param name="at">Target</param>
        /// <param name="up">Up vector</param>
        /// <returns>View matrix</returns>
        public static Matrix3D CreateLookAtMatrix(Point3D eye, Point3D at, Vector3D up)
        {
            Vector3D zAxis = eye - at;
            Vector3D xAxis = Vector3D.CrossProduct(up, zAxis);
            Vector3D yAxis = Vector3D.CrossProduct(zAxis, xAxis);

            zAxis.Normalize();
            yAxis.Normalize();
            xAxis.Normalize();

            Vector3D eyeVector = eye.ToVector3D();

            return new Matrix3D(xAxis.X, yAxis.X, zAxis.X, 0,
                                xAxis.Y, yAxis.Y, zAxis.Y, 0,
                                xAxis.Z, yAxis.Z, zAxis.Z, 0,
                                -Vector3D.DotProduct(xAxis, eyeVector),
                                -Vector3D.DotProduct(yAxis, eyeVector),
                                -Vector3D.DotProduct(zAxis, eyeVector), 1);
        }

        #endregion
    }
}
