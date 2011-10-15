using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using NuGenBioChem.Data.Commands;

namespace NuGenBioChem.Visualization
{
    /// <summary>
    /// Represents the main visualizer
    /// </summary>
    public partial class Visualizer1 : UserControl
    {
        #region Fields

        Render render = null;
        Camera camera = new Camera();

        // Timer to perform animations (CompositionTarget.Rendering lags)
        DispatcherTimer timer = null;

        #endregion

        #region Commands

        #region Show All

        // Command
        RelayCommand showAllComand;

        /// <summary>
        /// Gets show all command
        /// </summary>
        public RelayCommand ShowAllComand
        {
            get
            {
                if (showAllComand == null)
                {
                    showAllComand = new RelayCommand(x => ShowAll());
                }
                return showAllComand;
            }
        }

        // Is it required to show all?
        bool doShowAll = false;

        /// <summary>
        /// Centers and zooms to show all content
        /// </summary>
        public void ShowAll()
        {
            doShowAll = true;
        }
        
        #endregion

        #region Look At

        // Command
        RelayCommand lookAtComand;

        /// <summary>
        /// Gets look at command
        /// </summary>
        public RelayCommand LookAtComand
        {
            get
            {
                if (lookAtComand == null)
                {
                    lookAtComand = new RelayCommand(x => OnLookAtCommand(x));
                }
                return lookAtComand;
            }
        }

        void OnLookAtCommand(object parameter)
        {
            string plane = parameter as string;
            switch (plane)
            {
                case "+X":
                    camera.HorizontalRotation = Math.PI;
                    camera.VerticalRotation = 0;
                    break;
                case "+Y":
                    camera.HorizontalRotation = 2.5 * Math.PI;
                    camera.VerticalRotation = -0.5 * Math.PI;
                    break;
                case "+Z":
                    camera.HorizontalRotation = 1.5 * Math.PI;
                    camera.VerticalRotation = 0;
                    break;
                case "-X":
                    camera.HorizontalRotation = 2.0 * Math.PI;
                    camera.VerticalRotation = 0;
                    break;
                case "-Y":
                    camera.HorizontalRotation = 2.5 * Math.PI;
                    camera.VerticalRotation = 0.5 * Math.PI;
                    break;
                case "-Z":
                    camera.HorizontalRotation = 0.5 * Math.PI;
                    camera.VerticalRotation = 0;
                    break;

                case "Bias":
                    camera.HorizontalRotation = 1.86;
                    camera.VerticalRotation = 0.56;
                    break;
            }
        }

        #endregion

        #region Navigate To

        // Command
        RelayCommand navigateToComand;

        /// <summary>
        /// Gets show all command
        /// </summary>
        public RelayCommand NavigateToComand
        {
            get
            {
                if (navigateToComand == null)
                {
                    // TODO: implement NavigateTo command
                    //navigateToComand = new RelayCommand(x => NavigateTo((Data.Molecule)x));
                }
                return navigateToComand;
            }
        }

        #endregion

        #endregion

        #region Properties

        #region Render

        /// <summary>
        /// Gets or sets render
        /// </summary>
        public Render Render
        {
            get { return render; }
            set
            {
                if (render == value) return;

                render = value;
                output.Render = value;
                ShowAll();
            }
        }
        
        #endregion

        #region Zoom

        /// <summary>
        /// Gets or sets zoom
        /// </summary>
        public double Zoom
        {
            get { return (double)GetValue(ZoomProperty); }
            set { SetValue(ZoomProperty, value); }
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for Zoom.  
        /// This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register("Zoom", typeof(double), typeof(Visualizer1),
            new UIPropertyMetadata(1.0d, OnZoomChanged));

        // Zoom changing handler
        static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // TODO: implement zoom
        }

        double GetShowAllDistance()
        {
            if (render == null) return 1;

            Rect3D boundingBox = render.Bounds;

            double boundingSphereRadius = new Vector3D(
                boundingBox.Size.X / 2.0,
                boundingBox.Size.Y / 2.0,
                boundingBox.Size.Z / 2.0).Length;

            return  (boundingSphereRadius / Math.Tan(camera.FieldOfView / 2.0));
        }

        #endregion

        #region Field of View

        /// <summary>
        /// Gets or sets field of view of the camera
        /// </summary>
        public double FieldOfView
        {
            get { return (double)GetValue(FieldOfViewProperty); }
            set { SetValue(FieldOfViewProperty, value); }
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for FieldOfView.  
        /// This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty FieldOfViewProperty =
            DependencyProperty.Register("FieldOfView", typeof(double), typeof(Visualizer1),
            new UIPropertyMetadata(60.0d, OnFieldOfViewChanged));

        static void OnFieldOfViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Visualizer1 visualizer = (Visualizer1)d;
            visualizer.camera.FieldOfView = ((double) e.NewValue) * Math.PI / 180.0;
        }

        #endregion

        #region CameraProjectionMode

        /// <summary>
        /// Gets or sets projection mode of the camera
        /// </summary>
        public CameraProjectionMode CameraProjectionMode
        {
            get { return (CameraProjectionMode)GetValue(CameraProjectionModeProperty); }
            set { SetValue(CameraProjectionModeProperty, value); }
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for CameraProjectionMode.  
        /// This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty CameraProjectionModeProperty =
            DependencyProperty.Register("CameraProjectionMode", typeof(CameraProjectionMode), typeof(Visualizer1),
            new UIPropertyMetadata(CameraProjectionMode.Perspective, OnCameraProjectionModeChanged));

        static void OnCameraProjectionModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Visualizer1 visualizer = (Visualizer1)d;
            CameraProjectionMode cameraProjectionMode = (CameraProjectionMode)e.NewValue;
            visualizer.camera.Mode = visualizer.output.Camera.Mode = cameraProjectionMode;
            visualizer.output.Invalidate();
        }

        #endregion


        #endregion

        #region Initilization
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public Visualizer1()
        {
            InitializeComponent();

            // Tune camera
            output.Camera = new Camera();
            camera.Target = output.Camera.Target = new Point3D(0,0,0);
            camera.Distance = output.Camera .Distance = 3;
            
            // Subscribe to events
            IsVisibleChanged += OnIsVisibleChanged;
        }

        #endregion

        #region Event Handlers

        

        void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                if (timer == null) timer = new DispatcherTimer(TimeSpan.FromMilliseconds(15), DispatcherPriority.Background, OnCompositionTargetRendering, Dispatcher);
                timer.Start();
            }
            else
            {
                // Unsubscribe to prevent memory leaks
                if (timer != null) timer.Stop();
            }
        }

        #region Smooth Changing

        void OnCompositionTargetRendering(object sender, EventArgs e)
        {
            // Is it required to do show all?
            if (doShowAll)
            {
                Zoom = 1;
                camera.Target = render.Bounds.GetCenter();
                doShowAll = false;
            }

            // Update distance
            double showAllDistance = GetShowAllDistance();
            camera.Distance = showAllDistance / Zoom;

            // Flag to track changes
            bool invalidateRequired = false;

            // Animate the current camera in the output
            output.Camera.Distance = AnimateValue(camera.Distance, output.Camera.Distance, ref invalidateRequired, 0.15);
            output.Camera.Target = AnimateValue(camera.Target, output.Camera.Target, ref invalidateRequired, 0.1);
            output.Camera.HorizontalRotation = AnimateValue(camera.HorizontalRotation, output.Camera.HorizontalRotation, ref invalidateRequired, 0.18);
            output.Camera.VerticalRotation = AnimateValue(camera.VerticalRotation, output.Camera.VerticalRotation, ref invalidateRequired, 0.18);
            output.Camera.FieldOfView = AnimateValue(camera.FieldOfView, output.Camera.FieldOfView, ref invalidateRequired, 0.18);
            

            // Invoke invalidate here only if it is required to do
            if (invalidateRequired) output.Invalidate();
        }

        static double AnimateValue(double targetValue, double currentValue, ref bool updateRequired, double velocity)
        {
            if (Double.IsNaN(currentValue) || Double.IsInfinity(currentValue) || IsApproxEquals(currentValue, targetValue))
            {
                if (currentValue != targetValue) updateRequired = true;
                return targetValue;
            }
            else
            {
                updateRequired = true;
                return currentValue * (1.0 - velocity) + targetValue * velocity;
            }
        }

        static Point3D AnimateValue(Point3D targetValue, Point3D currentValue, ref bool updateRequired, double velocity)
        {
            if (Double.IsNaN(currentValue.X) || IsApproxEquals(currentValue, targetValue))
            {
                if (currentValue != targetValue) updateRequired = true;
                return targetValue;
            }
            else
            {
                updateRequired = true;
                return new Point3D(currentValue.X * (1.0 - velocity) + targetValue.X * velocity,
                                   currentValue.Y * (1.0 - velocity) + targetValue.Y * velocity,
                                   currentValue.Z * (1.0 - velocity) + targetValue.Z * velocity);
                
            }
        }

        static bool IsApproxEquals(double a, double b)
        {
            return Math.Abs(a - b) < 0.001;
        }

        static bool IsApproxEquals(Point3D a, Point3D b)
        {
            return IsApproxEquals(a.X, b.X) && IsApproxEquals(a.Y, b.Y) && IsApproxEquals(a.Z, b.Z);
        }

        #endregion

       

        #endregion

        #region Mouse Event Handlers

        // Position where mouse were down
        Point mouseDownPosition;
        MouseButton mouseDownButton;
        // It's for dragging
        double mouseX = Double.NaN;
        double mouseY = Double.NaN;
        

        // Mouse down handling
        void OnMouseDown(object sender, MouseEventArgs e)
        {
            mouseDownPosition = e.GetPosition(this);
            if (e.MouseDevice.MiddleButton == MouseButtonState.Pressed) mouseDownButton = MouseButton.Middle;
            if (e.MouseDevice.RightButton == MouseButtonState.Pressed) mouseDownButton = MouseButton.Right;
            if (e.MouseDevice.LeftButton == MouseButtonState.Pressed) mouseDownButton = MouseButton.Left;
            mouseX = mouseDownPosition.X;
            mouseY = mouseDownPosition.Y;
            if (e.MiddleButton == MouseButtonState.Pressed) Cursor = Cursors.SizeAll;
            Mouse.Capture(this);
        }

        // Mouse up handling
        void OnMouseUp(object sender, MouseEventArgs e)
        {
            Cursor = Cursors.Arrow;
            Mouse.Capture(null);

            Point point = e.GetPosition(this);
            // If mouse were moved only a little
            if (Math.Abs(mouseDownPosition.X - point.X) < 2 && Math.Abs(mouseDownPosition.Y - point.Y) < 2)
            {
                Atom atom = HitTestAtom(e.GetPosition(this));
                bool isCtrlPressed = Keyboard.Modifiers == ModifierKeys.Control;

                if (mouseDownButton == MouseButton.Left)
                {
                    if (atom != null)
                    {
                        // Select atoms
                        if (isCtrlPressed)
                        {
                            if (render.Substance.SelectedAtoms.Contains(atom.Data)) render.Substance.SelectedAtoms.Remove(atom.Data);
                            else render.Substance.SelectedAtoms.Add(atom.Data);
                        }
                        else
                        {
                            for (int i = render.Substance.SelectedAtoms.Count - 1; i >= 1; i--) render.Substance.SelectedAtoms.RemoveAt(i);
                            if (render.Substance.SelectedAtoms.Count == 0) render.Substance.SelectedAtoms.Add(atom.Data);
                            else render.Substance.SelectedAtoms[0] = atom.Data;
                        }
                    }
                    else
                    {
                        if (!isCtrlPressed) render.Substance.SelectedAtoms.Clear();
                    }
                }
            }
        }

        // Mouse move handling
        void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.Captured != this) return;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point point = e.GetPosition(this);

                if (!Double.IsNaN(mouseX))
                {
                    camera.HorizontalRotation += (float)((point.X - mouseX) / 100);
                    camera.VerticalRotation += (float)((-mouseY + point.Y) / 100);
                }

                mouseX = point.X;
                mouseY = point.Y;
            }
            else if (e.MiddleButton == MouseButtonState.Pressed)
            {
                Point point = e.GetPosition(this);

                if (!Double.IsNaN(mouseX) && ActualWidth != 0 && ActualHeight != 0)
                {
                    Vector3D upVector = camera.Up;
                    Vector3D sideVector = Vector3D.CrossProduct(camera.Up, camera.LookDirection);

                    double proportion = camera.Distance / camera.Near;
                    double aspectRatio = ActualHeight / ActualWidth;
                    double mousedx = 0.25 * (point.X - mouseX) / ActualWidth;
                    double mousedy = 0.25 * (point.Y - mouseY) / ActualHeight;

                    camera.Target = camera.Target + upVector * proportion * aspectRatio * mousedy + sideVector * proportion * mousedx;
                }
                mouseX = point.X;
                mouseY = point.Y;
            }
        }

        // Mouse wheel handling
        void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            Zoom *= e.Delta > 0 ? 1.05 : 0.95;
        }

        #endregion

        #region Hit Test

        // Hit test for atoms
        Atom HitTestAtom(Point mousePoint)
        {
            // TODO: implement atom hit test
            return null;
        }

        #endregion
    }
}
