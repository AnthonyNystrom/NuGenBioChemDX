using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace NuGenBioChem.Visualization
{
    /// <summary>
    /// Represents control to show rendered content
    /// </summary>
    public class Output : Control
    {
        #region Fields

        // Current render
        Render render = null;
        // Current camera
        Camera camera = null;
        
        // Direct3D surface
        RenderTarget renderTarget = null;
        // D3DImage
        D3DImage d3dImage = new D3DImage();

        // Indicates that it have to be redraw
        bool invalidated = false;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets render
        /// </summary>
        public Render Render
        {
            get { return render; }
            set
            {
                if (render == value) return;
                if (render != null)
                {
                    render.Invalidated -= OnRenderInvalidated;
                }

                render = value;
                
                if (render != null)
                {
                    render.Invalidated += OnRenderInvalidated;
                }
                Invalidate();
            }
        }

        void OnRenderInvalidated(object sender, EventArgs e)
        {
            Invalidate();
        }

        /// <summary>
        /// Gets or sets camera
        /// </summary>
        public Camera Camera
        {
            get { return camera; }
            set
            {
                camera = value;
                Invalidate();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Default constructor
        /// </summary>
        public Output()
        {
            d3dImage.IsFrontBufferAvailableChanged += OnIsFrontBufferAvailableChanged;
        }

        void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Request redrawing when it is available again
            if ((bool)e.NewValue) Invalidate();
            else
            {
                // D3DImage is no so good to handle availability changing, 
                // so make things carefully as possible
                if (renderTarget != null) renderTarget.Dispose();
                renderTarget = null;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Invalidates the content
        /// </summary>
        public void Invalidate()
        {
            invalidated = true;
            InvalidateVisual();
        }

        /// <summary>
        /// Refreshes output content
        /// </summary>
        void Update()
        {
            // Skip if something missing
            if (camera == null || render == null) return;
            
            d3dImage.Lock();

            // Check whether current render target exists and has appropriate size
            if (renderTarget == null || (int)RenderSize.Width != renderTarget.Width || (int)RenderSize.Height != renderTarget.Height)
            {
                if (renderTarget != null) renderTarget.Dispose();
                renderTarget = new RenderTarget((int)RenderSize.Width, (int)RenderSize.Height);
            }
            // Set it as current
            d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, renderTarget.Handle);

            // Render to it & unlock
            render.Draw(renderTarget, camera);
            d3dImage.AddDirtyRect(new Int32Rect(0, 0, renderTarget.Width, renderTarget.Height));
            d3dImage.Unlock();
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Renders content
        /// </summary>
        /// <param name="drawingContext">Drawing context</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            if (invalidated) Update();
            invalidated = false;

            drawingContext.DrawImage(d3dImage, new Rect(RenderSize));
        }

        /// <summary>
        /// Raises the SizeChanged event, using the specified information as part of the eventual event data. 
        /// </summary>
        /// <param name="sizeInfo">Details of the old and new size involved in the change.</param>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            camera.Aspect = sizeInfo.NewSize.Width / sizeInfo.NewSize.Height;
            Invalidate();
            base.OnRenderSizeChanged(sizeInfo);
        }

        #endregion
    }
}
