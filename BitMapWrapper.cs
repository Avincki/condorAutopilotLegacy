using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondorAutoPilot
{
    unsafe public class BitMapWrapper
    {
        public Bitmap myBitmap { get; set; }
        private BitmapData bitmapData;
        private int bytesPerPixel;
        private int heightInPixels;
        private int widthInBytes;
        private byte* ptrFirstPixel;

        public BitMapWrapper(Bitmap bitmap)
        {
           myBitmap = bitmap;
           Init();
        }

        public BitMapWrapper(int width, int height, PixelFormat format)
        {
            myBitmap = new Bitmap(width, height, format);
            Init();
        }

        private void Init()
        {
            bitmapData = myBitmap.LockBits(new Rectangle(0, 0, myBitmap.Width, myBitmap.Height), ImageLockMode.ReadWrite, myBitmap.PixelFormat);
            bytesPerPixel = System.Drawing.Bitmap.GetPixelFormatSize(myBitmap.PixelFormat) / 8;
            heightInPixels = bitmapData.Height;
            widthInBytes = bitmapData.Width * bytesPerPixel;
            ptrFirstPixel = (byte*)bitmapData.Scan0;
        }

        private void Gray(int i, int j)
        {
            byte* currentLine = ptrFirstPixel + (j * bitmapData.Stride);
            int x = i * bytesPerPixel;
            var gray = (currentLine[x + 2] + currentLine[x + 1] + currentLine[x])/3;

            currentLine[x] = (byte)gray;
            currentLine[x + 1] = (byte)gray;
            currentLine[x + 2] = (byte)gray;
        }

        public void Gray()
        {
            //Parallel.For(0, myBitmap.Height, (j) =>
            for (int j = 0; j < myBitmap.Height; j++)
            {
                for (int i = 0; i < myBitmap.Width; i++)
                {
                    Gray(i, j);
                }
            };//);
        }
        public Color GetPixel(int i, int j)
        {
            Color colorOut;
            byte* currentLine = ptrFirstPixel + (j * bitmapData.Stride);
            int x = i * bytesPerPixel;
            colorOut = Color.FromArgb(currentLine[x + 3], currentLine[x + 2], currentLine[x + 1], currentLine[x]);

            return colorOut;
        }
        public void SetPixel(int i, int j, Color color)
        {
            byte* currentLine = ptrFirstPixel + (j * bitmapData.Stride);
            int x = i * bytesPerPixel;
            currentLine[x] = (byte)color.B;
            currentLine[x+1] = (byte)color.G;
            currentLine[x + 2] = (byte)color.R;
            currentLine[x + 3] = (byte)color.A;
        }
        public void SetPixelUnLocked(int i, int j, Color color)
        {
            myBitmap.SetPixel(i, j, color);
        }

        public void UnLock()
        {
            myBitmap.UnlockBits(bitmapData);
        }
    }
}
