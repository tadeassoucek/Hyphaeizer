using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DrawingColor = System.Drawing.Color;

namespace Hyphaeizer
{
    public unsafe class FastWriteableBitmap
    {
        /// <summary>
        ///     The pixel format used. This is <em>de facto</em> a constant — if you wish to change it, make sure to change
        ///     <see cref="BYTES_PER_PIXEL"/> as well. Also, if you use a format where a pixel takes up N bits, where N is not
        ///     a power of 8, you will need to change the internal logic of how pixels are accessed.
        /// </summary>
        readonly PixelFormat pixelFormat = PixelFormats.Rgb24;
        /// <summary>
        ///     How many bytes each pixel takes up.
        /// </summary>
        const int BYTES_PER_PIXEL = 3;

        /// <summary>
        ///     The width of the bitmap in pixels.
        /// </summary>
        public int Width { get; private set; }
        /// <summary>
        ///     The height of the bitmap in pixels.
        /// </summary>
        public int Height { get; private set; }

        public readonly WriteableBitmap bitmap;
        /// <summary>
        ///     If <c>false</c>, it is not safe to write to the bitmap. If you want to write to it, you need to call
        ///     <see cref="LockDrawingContext()"/> first, which sets <see cref="locked"/> to <c>true</c>. Only <em>then</em>
        ///     can you make changes to the bitmap. Don't forget to call <see cref="UnlockDrawingContext()"/> after you're
        ///     finished changing the bitmap so that the bitmap can be displayed.
        /// </summary>
        bool locked = false;
        byte* backBuffer;

        public FastWriteableBitmap(int width, int height, Image imageControl) : this(width, height) => Attach(imageControl);

        public FastWriteableBitmap(int width, int height)
        {
            Width = width;
            Height = height;
            bitmap = new WriteableBitmap(width, height, 96, 96, pixelFormat, palette: null);
        }

        /// <summary>
        ///     Locks the bitmap by calling <see cref="WriteableBitmap.Lock"/>, thus making it safe to use its
        ///     back buffer and make changes to it.
        /// </summary>
        public void LockDrawingContext()
        {
            bitmap.Lock();
            locked = true;
            backBuffer = (byte*)bitmap.BackBuffer;
        }

        /// <summary>
        ///     Unlocks the bitmap by calling <see cref="WriteableBitmap.Unlock"/>, thus allowing the bitmap to be
        ///     redrawn on screen.
        /// </summary>
        public void UnlockDrawingContext()
        {
            bitmap.Unlock();
            locked = false;
            backBuffer = null;
        }

        /// <summary>
        ///     Displays the bitmap in an image control.
        /// </summary>
        public void Attach(Image imageControl) => imageControl.Source = bitmap;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static void SetPixel(byte* buffer, int index, DrawingColor color)
        {
            buffer[index] = color.R;
            buffer[index + 1] = color.G;
            buffer[index + 2] = color.B;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        int PointToIndex(int x, int y) => (y * bitmap.BackBufferStride) + x * BYTES_PER_PIXEL;

        public void SetPixel(int x, int y, DrawingColor color) => SetPixel(backBuffer, PointToIndex(x, y), color);

        public DrawingColor GetPixel(int x, int y)
        {
            var index = PointToIndex(x, y);
            return DrawingColor.FromArgb(backBuffer[index], backBuffer[index + 1], backBuffer[index + 2]);
        }

        public void Fill(Int32Rect rect, DrawingColor color)
        {
#if DEBUG
            if (!locked)
                throw new InvalidOperationException("Image unlocked!");

            if (rect.X < 0 || rect.Y < 0 || rect.X > Width || rect.Y > Height)
                throw new Exception("Rect out of bounds.");
            if (rect.X + rect.Width > Width || rect.Y + rect.Height > Height)
                throw new Exception("Rect too big.");
#endif

            for (int j = 0; j < rect.Height; j++)
            {
                int bufferY = (rect.Y + j) * bitmap.BackBufferStride;

                for (int i = 0; i < rect.Width; i++)
                {
                    int bufferX = rect.X + i;
                    SetPixel(backBuffer, bufferY + bufferX, color);
                }
            }

            bitmap.AddDirtyRect(rect);
        }

        public void UpdateWhole() => bitmap.AddDirtyRect(new Int32Rect(0, 0, Width, Height));
    }
}
