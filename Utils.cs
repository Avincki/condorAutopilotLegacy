using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;


// GMap.NET.WindowsForms.GMapMarker marker = new GMarkerGoogle(point, RotateImg(bmpMarker,45));
namespace CondorAutoPilot
{

    class Utils
    {
        [DllImport("USER32.DLL")]
        private static extern short GetAsyncKeyState(int vKey);
        public static Bitmap RotateImg(Bitmap bmpimage, float angle)
        {
            int w = bmpimage.Width;
            int h = bmpimage.Height;
            PixelFormat pf;
            pf = bmpimage.PixelFormat;
            Bitmap tempImg = new Bitmap(w, h, pf);
            Graphics g = Graphics.FromImage(tempImg);
            g.DrawImageUnscaled(bmpimage, 1, 1);
            g.Dispose();
            GraphicsPath path = new GraphicsPath();
            path.AddRectangle(new RectangleF(0.0F, 0.0F, w, h));
            Matrix mtrx = new Matrix();

            mtrx.Rotate(angle);
            RectangleF rct = path.GetBounds(mtrx);
            Bitmap newImg = new Bitmap(Convert.ToInt32(rct.Width), Convert.ToInt32(rct.Height), pf);
            g = Graphics.FromImage(newImg);
            g.TranslateTransform(-rct.X, -rct.Y);
            g.RotateTransform(angle);
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.DrawImageUnscaled(tempImg, 0, 0);
            g.Dispose();
            tempImg.Dispose();
            return newImg;
        }

        public static void DrawRotatedTextAt(Graphics gr, float angle,
                string txt, float x, float y, Font the_font, Brush the_brush)
        {
            // Save the graphics state.
            GraphicsState state = gr.Save();
            gr.ResetTransform();

            // Rotate.
            gr.RotateTransform(angle);

            // Translate to desired position. Be sure to append
            // the rotation so it occurs after the rotation.
            gr.TranslateTransform(x, y, MatrixOrder.Append);

            // Draw the text at the origin.
            gr.DrawString(txt, the_font, the_brush, 0, 0);

            // Restore the graphics state.
            gr.Restore(state);
        }

        public static bool KeyPressed(int iKey)
        {
            short keyState = GetAsyncKeyState(iKey);
            return ((keyState >> 15) & 0x0001) == 0x0001;
        }

        public static Bitmap ProcessBMP(Bitmap bmpimage)
        {
            Bitmap newImg = new Bitmap(bmpimage.Width, bmpimage.Height);

            for (int i = 0; i < bmpimage.Width; i++)
            {
                for (int j = 0; j < bmpimage.Height; j++)
                {
                    Color pixelCol = bmpimage.GetPixel(i, j);
                    Color pixelColNew = pixelCol;

                    if (pixelCol.R < 3 && pixelCol.G < 3 && pixelCol.B < 3)
                    {
                        pixelColNew = Color.FromArgb(
                            0,
                           pixelCol.R,
                           pixelCol.G,
                           pixelCol.B
                           );
                    }
                    else if (pixelCol.R > 240 && pixelCol.G > 240 && pixelCol.B > 240)
                    {
                        pixelColNew = Color.FromArgb(
                           255,
                           pixelCol.R,
                           pixelCol.G,
                           pixelCol.B
                           );
                    }
                    else 
                    {
                        pixelColNew = Color.FromArgb(
                           160,
                           pixelCol.R,
                           pixelCol.G,
                           pixelCol.B
                           );
                    }
                    //else if (pixelCol.R > 230 && pixelCol.G < 40 && pixelCol.B < 40)
                    //{
                    //    pixelColNew = Color.FromArgb(
                    //       140,
                    //       pixelCol.R,
                    //       pixelCol.G,
                    //       pixelCol.B
                    //       );
                    //}
                    //else if (pixelCol.R < 40 && pixelCol.G < 40 && pixelCol.B > 230)
                    //{
                    //    pixelColNew = Color.FromArgb(
                    //       140,
                    //       pixelCol.R,
                    //       pixelCol.G,
                    //       pixelCol.B
                    //       );
                    //}
                    newImg.SetPixel(i, j, pixelColNew);
                }
            }

            return newImg;
        }

        public static float rad2deg(float rad)
        {
            return rad * 180 / 3.1415f;
        }
        public static float deg2rad(float deg)
        {
            return deg * 3.1415f / 180;
        }

        public static double rad2deg(double rad)
        {
            return rad * 180 / 3.1415f;
        }
        public static double deg2rad(double deg)
        {
            return deg * 3.1415f / 180;
        }

        public static float roundKmHAirspeed10(float airspeed)
        {
            return Convert.ToSingle(5 * Math.Round(airspeed / 5));
        }
        public static Bitmap ProcessPhotoBMP(Bitmap bmpimage)
        {
            Bitmap newImg = new Bitmap(bmpimage.Width, bmpimage.Height);

            for (int i = 0; i < bmpimage.Width; i++)
            {
                for (int j = 0; j < bmpimage.Height; j++)
                {
                    Color pixelCol = bmpimage.GetPixel(i, j);
                    Color pixelColNew = pixelCol;

                    //if (pixelCol.R < 3 && pixelCol.G < 3 && pixelCol.B < 3)
                    //{
                    //    pixelColNew = Color.FromArgb(
                    //        0,
                    //       pixelCol.R,
                    //       pixelCol.G,
                    //       pixelCol.B
                    //       );
                    //}else 
                    if (pixelCol.R > 220 && pixelCol.G < 20 && pixelCol.B < 20)//RED
                    {
                        pixelColNew = Color.FromArgb(
                           0,
                           pixelCol.R,
                           pixelCol.G,
                           pixelCol.B
                           );
                    }else if (pixelCol.R < 220 || pixelCol.G < 220 || pixelCol.B < 220)//Not White
                    {
                        pixelColNew = Color.FromArgb(
                           140,
                           pixelCol.R,
                           pixelCol.G,
                           pixelCol.B
                           );
                    }
                    newImg.SetPixel(i, j, pixelColNew);
                }
            }

            return newImg;
        }

        public static double interpolate(double x, double x1, double y1, double x2, double y2)
        {
            if (x < x1)
                return y1;

            if (x > x2)
                return y2;

            if (x1 == x2)// in fact bad arguments
                return 0.5 * (y1 + y2);

            double y;

            y = y1 + (x - x1) / (x2 - x1) * (y2 - y1);

            return y;
        }
        public static float InterpolateFloat(double x, double x1, double y1, double x2, double y2)
        {
            double dOut = interpolate(x, x1, y1, x2, y2);

            return Convert.ToSingle(dOut);
        }


    //    private class Deg2UTM
    //    {
    //        double Easting;
    //        double Northing;
    //        int Zone;
    //        char Letter;
    //        private Deg2UTM(double Lat, double Lon)
    //        {
    //            Zone = (int)Math.Floor(Lon / 6 + 31);
    //            if (Lat < -72)
    //                Letter = 'C';
    //            else if (Lat < -64)
    //                Letter = 'D';
    //            else if (Lat < -56)
    //                Letter = 'E';
    //            else if (Lat < -48)
    //                Letter = 'F';
    //            else if (Lat < -40)
    //                Letter = 'G';
    //            else if (Lat < -32)
    //                Letter = 'H';
    //            else if (Lat < -24)
    //                Letter = 'J';
    //            else if (Lat < -16)
    //                Letter = 'K';
    //            else if (Lat < -8)
    //                Letter = 'L';
    //            else if (Lat < 0)
    //                Letter = 'M';
    //            else if (Lat < 8)
    //                Letter = 'N';
    //            else if (Lat < 16)
    //                Letter = 'P';
    //            else if (Lat < 24)
    //                Letter = 'Q';
    //            else if (Lat < 32)
    //                Letter = 'R';
    //            else if (Lat < 40)
    //                Letter = 'S';
    //            else if (Lat < 48)
    //                Letter = 'T';
    //            else if (Lat < 56)
    //                Letter = 'U';
    //            else if (Lat < 64)
    //                Letter = 'V';
    //            else if (Lat < 72)
    //                Letter = 'W';
    //            else
    //                Letter = 'X';
    //            Easting = 0.5 * Math.Log((1 + Math.Cos(Lat * Math.PI / 180) * Math.Sin(Lon * Math.PI / 180 - 
    //                (6 * Zone - 183) * Math.PI / 180)) / (1 - Math.Cos(Lat * Math.PI / 180) * 
    //                Math.Sin(Lon * Math.PI / 180 - (6 * Zone - 183) * Math.PI / 180)))
    //                * 0.9996 * 6399593.62 / Math.Pow((1 + Math.Pow(0.0820944379, 2) * 
    //                Math.Pow(Math.Cos(Lat * Math.PI / 180), 2)), 0.5) * 
    //                (1 + Math.Pow(0.0820944379, 2) / 2 * Math.Pow((0.5 * Math.Log((1 + Math.Cos(Lat * Math.PI / 180) * 
    //                Math.Sin(Lon * Math.PI / 180 - (6 * Zone - 183) * Math.PI / 180)) / (1 - Math.Cos(Lat * Math.PI / 180) * 
    //                Math.Sin(Lon * Math.PI / 180 - (6 * Zone - 183) * Math.PI / 180)))), 2) * 
    //                Math.Pow(Math.Cos(Lat * Math.PI / 180), 2) / 3) + 500000;
    //            Easting = Math.Round(Easting * 100) * 0.01;
    //            Northing = (Math.Atan(Math.Tan(Lat * Math.PI / 180) / Math.Cos((Lon * Math.PI / 180 - 
    //                (6 * Zone - 183) * Math.PI / 180))) - Lat * Math.PI / 180) * 0.9996 * 6399593.625 / 
    //                Math.Sqrt(1 + 0.006739496742 * Math.Pow(Math.Cos(Lat * Math.PI / 180), 2)) * 
    //                (1 + 0.006739496742 / 2 * Math.Pow(0.5 * Math.Log((1 + Math.Cos(Lat * Math.PI / 180) * 
    //                Math.Sin((Lon * Math.PI / 180 - (6 * Zone - 183) * Math.PI / 180))) / 
    //                (1 - Math.Cos(Lat * Math.PI / 180) * Math.Sin((Lon * Math.PI / 180 - 
    //                (6 * Zone - 183) * Math.PI / 180)))), 2) * Math.Pow(Math.Cos(Lat * Math.PI / 180), 2)) + 
    //                0.9996 * 6399593.625 * (Lat * Math.PI / 180 - 0.005054622556 * (Lat * Math.PI / 180 + 
    //                Math.Sin(2 * Lat * Math.PI / 180) / 2) + 4.258201531e-05 * (3 * (Lat * Math.PI / 180 + 
    //                Math.Sin(2 * Lat * Math.PI / 180) / 2) + Math.Sin(2 * Lat * Math.PI / 180) * 
    //                Math.Pow(Math.Cos(Lat * Math.PI / 180), 2)) / 4 - 1.674057895e-07 * 
    //                (5 * (3 * (Lat * Math.PI / 180 + Math.Sin(2 * Lat * Math.PI / 180) / 2) + 
    //                Math.Sin(2 * Lat * Math.PI / 180) * Math.Pow(Math.Cos(Lat * Math.PI / 180), 2)) / 4 + 
    //                Math.Sin(2 * Lat * Math.PI / 180) * Math.Pow(Math.Cos(Lat * Math.PI / 180), 2) * 
    //                Math.Pow(Math.Cos(Lat * Math.PI / 180), 2)) / 3);
    //            if (Letter < 'M')
    //                Northing = Northing + 10000000;
    //            Northing = Math.Round(Northing * 100) * 0.01;
    //        }
    //    }

    //    private class UTM2Deg
    //    {
    //        double latitude;
    //        double longitude;
    //        private UTM2Deg(String UTM)
    //        {
    //            String[] parts = UTM.Split(' ');
    //            int Zone = int.Parse(parts[0]);
    //            char Letter = parts[1].ToUpper().ToCharArray()[0];
    //            double Easting = Double.Parse(parts[2]);
    //            double Northing = Double.Parse(parts[3]);
    //            double Hem;
    //            if (Letter > 'M')
    //                Hem = 'N';
    //            else
    //                Hem = 'S';
    //            double north;
    //            if (Hem == 'S')
    //                north = Northing - 10000000;
    //            else
    //                north = Northing;

    //            double MC1 = Math.Cos(north / 6366197.724 / 0.9996);
    //            double MS1 = Math.Sin(north / 6366197.724 / 0.9996);
    //            double MS2 = Math.Sin(2 * north / 6366197.724 / 0.9996);
    //            double MP1 = Math.Pow(0.006739496742 * 3 / 4, 2);
    //            double MP2 = Math.Pow(0.006739496742 * 3 / 4, 3);
    //            double MP3 = Math.Pow(MC1, 2);
    //            double MSQ1 = Math.Sqrt((1 + 0.006739496742 * MP3));

    //            latitude = (north / 6366197.724 / 0.9996 + (1 + 0.006739496742 * 
    //                MP3 - 0.006739496742 * 
    //                MS1 * MC1 * 
    //                (Math.Atan(Math.Cos(Math.Atan((Math.Exp((Easting - 500000) / 
    //                (0.9996 * 6399593.625 / MSQ1) * 
    //                (1 - 0.006739496742 * Math.Pow((Easting - 500000) / (0.9996 * 6399593.625 / 
    //                Math.Sqrt((1 + 0.006739496742 * Math.Pow(MC1, 
    //                2)))), 2) / 2 * MP3 / 3)) - 
    //                Math.Exp(-(Easting - 500000) / (0.9996 * 6399593.625 / Math.Sqrt((1 + 
    //                0.006739496742 * MP3))) * 
    //                (1 - 0.006739496742 * Math.Pow((Easting - 500000) / (0.9996 * 6399593.625 / 
    //                Math.Sqrt((1 + 0.006739496742 * Math.Pow(MC1,
    //                2)))), 2) / 2 * MP3 / 3))) 
    //                / 2 / Math.Cos((north - 0.9996 * 6399593.625 * (north / 6366197.724 / 0.9996 - 
    //                0.006739496742 * 3 / 4 * (north / 6366197.724 / 0.9996 + MS2
    //                / 2) + MP1 * 5 / 3 * (3 * (north / 6366197.724 / 0.9996 + 
    //                MS2 / 2) + MS2 * 
    //                MP3) / 4 - MP2
    //                * 35 / 27 * (5 * (3 * (north / 6366197.724 / 0.9996 + MS2
    //                / 2) + MS2 * 
    //                MP3) / 4 + 
    //                MS2 * MP3
    //                * MP3) / 3)) / (0.9996 * 6399593.625 / 
    //                MSQ1) * 
    //                (1 - 0.006739496742 * Math.Pow((Easting - 500000) / (0.9996 * 6399593.625 /
    //                MSQ1), 2) /
    //                2 * MP3) + north / 6366197.724 / 0.9996)))
    //                * Math.Tan((north - 0.9996 * 6399593.625 * (north / 6366197.724 / 0.9996 - 
    //                0.006739496742 * 3 / 4 * (north / 6366197.724 / 0.9996 + MS2
    //                / 2) + MP1 * 5 / 3 * (3 * (north / 6366197.724 / 0.9996 
    //                + MS2 / 2) + MS2 
    //                * MP3) / 4 - MP2
    //                * 35 / 27 * (5 * (3 * (north / 6366197.724 / 0.9996 + MS2 / 2)
    //                + MS2 * MP3) /
    //                4 + MS2 * MP3
    //                * MP3) / 3)) / (0.9996 * 6399593.625 / 
    //                MSQ1) * 
    //                (1 - 0.006739496742 * Math.Pow((Easting - 500000) / (0.9996 * 6399593.625 / 
    //                MSQ1), 2) / 2 * 
    //                MP3) + north / 6366197.724 / 0.9996)) - north /
    //                6366197.724 / 0.9996) * 3 / 2) * (Math.Atan(Math.Cos(Math.Atan((Math.Exp((Easting - 500000) /
    //                (0.9996 * 6399593.625 / Math.Sqrt((1 + 0.006739496742 * Math.Pow(Math.Cos(north /
    //                6366197.724 / 0.9996), 2)))) * (1 - 0.006739496742 * Math.Pow((Easting - 500000) /
    //                (0.9996 * 6399593.625 / Math.Sqrt((1 + 0.006739496742 * Math.Pow(MC1,
    //                2)))), 2) / 2 * MP3 / 3)) - 
    //                Math.Exp(-(Easting - 500000) / (0.9996 * 6399593.625 / Math.Sqrt((1 + 0.006739496742 * 
    //                MP3))) * 
    //                (1 - 0.006739496742 * Math.Pow((Easting - 500000) / (0.9996 * 6399593.625 / 
    //                MSQ1), 2) / 2 * 
    //                MP3 / 3))) / 2 / 
    //                Math.Cos((north - 0.9996 * 6399593.625 * (north / 6366197.724 / 0.9996 - 0.006739496742 * 3 / 4 * 
    //                (north / 6366197.724 / 0.9996 + MS2 / 2) + 
    //                MP1 * 5 / 3 * (3 * (north / 6366197.724 / 0.9996 + 
    //                MS2 / 2) + MS2 * 
    //                MP3) / 4 - MP2 * 
    //                35 / 27 * (5 * (3 * (north / 6366197.724 / 0.9996 + MS2 / 2) +
    //                MS2 * MP3) / 4 +
    //                MS2 * MP3 *
    //                MP3) / 3)) / (0.9996 * 6399593.625 / 
    //                MSQ1) * 
    //                (1 - 0.006739496742 * Math.Pow((Easting - 500000) / (0.9996 * 6399593.625 / 
    //                MSQ1), 2) / 
    //                2 * MP3) + north / 6366197.724 / 0.9996))) * 
    //                Math.Tan((north - 0.9996 * 6399593.625 * (north / 6366197.724 / 0.9996 - 0.006739496742 * 3 / 4 * 
    //                (north / 6366197.724 / 0.9996 + MS2 / 2) + 
    //                MP1 * 5 / 3 * (3 * (north / 6366197.724 / 0.9996 + 
    //                MS2 / 2) + MS2 * 
    //                MP3) / 4 - 
    //                MP2 * 35 / 27 * (5 * (3 * (north / 6366197.724 / 0.9996 + 
    //                MS2 / 2) + MS2 * 
    //                MP3) / 4 + MS2 *
    //                MP3 * Math.Pow(MC1,
    //                2)) / 3)) / (0.9996 * 6399593.625 / Math.Sqrt((1 + 0.006739496742 * 
    //                MP3))) * 
    //                (1 - 0.006739496742 * Math.Pow((Easting - 500000) / (0.9996 * 6399593.625 / 
    //                MSQ1), 2) / 2 * 
    //                MP3) + north / 6366197.724 / 0.9996)) - 
    //                north / 6366197.724 / 0.9996)) * 180 / Math.PI;
    //            latitude = Math.Round(latitude * 10000000);
    //            latitude = latitude / 10000000;
    //            longitude = Math.Atan((Math.Exp((Easting - 500000) / (0.9996 * 6399593.625 / Math.Sqrt((1 + 0.006739496742 *
    //                MP3))) * (1 - 0.006739496742 * Math.Pow((Easting - 500000) /
    //                (0.9996 * 6399593.625 / MSQ1),
    //                2) / 2 * MP3 / 3)) - Math.Exp(-(Easting - 500000) / 
    //                (0.9996 * 6399593.625 / MSQ1) * 
    //                (1 - 0.006739496742 * Math.Pow((Easting - 500000) / (0.9996 * 6399593.625 / Math.Sqrt((1 + 0.006739496742 * 
    //                MP3))), 2) / 2 * Math.Pow(MC1,
    //                2) / 3))) / 2 / Math.Cos((north - 0.9996 * 6399593.625 * (north / 6366197.724 / 0.9996 - 0.006739496742 * 3 / 4 * 
    //                (north / 6366197.724 / 0.9996 + MS2 / 2) + 
    //                MP1 * 5 / 3 * (3 * (north / 6366197.724 / 0.9996 + 
    //                MS2 / 2) + MS2 * 
    //                MP3) / 4 - MP2 * 35 / 27 * 
    //                (5 * (3 * (north / 6366197.724 / 0.9996 + MS2 / 2) + 
    //                MS2 * MP3) / 4 + 
    //                MS2 * MP3 * 
    //                MP3) / 3)) / (0.9996 * 6399593.625 / 
    //                MSQ1) * (1 - 
    //                0.006739496742 * Math.Pow((Easting - 500000) / (0.9996 * 6399593.625 / Math.Sqrt((1 + 
    //                0.006739496742 * MP3))), 2) / 2 * 
    //                MP3) + north / 6366197.724 / 0.9996)) * 180 / Math.PI + Zone * 6 - 183;
    //            longitude = Math.Round(longitude * 10000000);
    //            longitude = longitude / 10000000;
    //        }
    //    }
    }
}
