using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NuGenBioChem.Visualization
{
    /// <summary>
    /// Represents render target
    /// </summary>
    public class RenderTarget : IDisposable
    {
        #region Native Methods

        [DllImport("NuGenBioChem.Native.dll")]
        static extern IntPtr CreateRenderTarget(int width, int height);

        [DllImport("NuGenBioChem.Native.dll")]
        static extern void DisposeRenderTarget(IntPtr handle);

        [DllImport("NuGenBioChem.Native.dll")]
        static extern void GetRenderTargetData(IntPtr handle, IntPtr data);
        
        #endregion

        #region Fields

        

        #endregion

        #region Properties

        /// <summary>
        /// Gets width of the render target
        /// </summary>
        public int Width
        {
            get; private set;
        }

        /// <summary>
        /// Gets height of the render target
        /// </summary>
        public int Height
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets unmanaged handle of the render target
        /// </summary>
        public IntPtr Handle
        {
            get;
            private set;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        public RenderTarget(int width, int height)
        {
            Width = width;
            Height = height;
            Handle = CreateRenderTarget(Width, Height);
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~RenderTarget()
        {
            Dispose();
        }

        /// <summary>
        /// Disposes unmanaged resources
        /// </summary>
        public void Dispose()
        {
            if (Handle == IntPtr.Zero) return;
            
            GC.SuppressFinalize(this);
            DisposeRenderTarget(Handle);
            Handle = IntPtr.Zero;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets render target's data
        /// </summary>
        /// <param name="data">Pixels</param>
        public void GetData(byte[] data)
        {
            if (data.Length < 4 * Width * Height) throw new Exception("Unable to get data of a render target to array: array must be 4 * Width * Height bytes or larger");

            GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                GetRenderTargetData(Handle, dataHandle.AddrOfPinnedObject());
            }
            finally
            {
                dataHandle.Free();
            }
        }

        /// <summary>
        /// Converts to bitmap
        /// </summary>
        /// <returns></returns>
        public BitmapSource ToBitmap()
        {
            // Gets render target's data
            byte[] data = new byte[4 * Width * Height];
            GetData(data);

            // Create bitmap image
            return BitmapSource.Create(Width, Height, 96, 96, PixelFormats.Bgra32, null, data, 4 * Width);
        }

        #endregion
    }
}
