using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using System.Drawing.Imaging;
using System.Drawing;
using System.Windows.Forms;

namespace CondorAutoPilot
{
    class ReadLocation
    {
        private string[] strCharSet = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "E", "W", "S", "N", "dot" };
        private string strGuid = "Guid";

        private Bitmap[] bmpCharaters = new Bitmap[] {
            global::CondorAutoPilot.Properties.Resources._0,
            global::CondorAutoPilot.Properties.Resources._1,
            global::CondorAutoPilot.Properties.Resources._2,
            global::CondorAutoPilot.Properties.Resources._3,
            global::CondorAutoPilot.Properties.Resources._4,
            global::CondorAutoPilot.Properties.Resources._5,
            global::CondorAutoPilot.Properties.Resources._6,
            global::CondorAutoPilot.Properties.Resources._7,
            global::CondorAutoPilot.Properties.Resources._8,
            global::CondorAutoPilot.Properties.Resources._9,
            global::CondorAutoPilot.Properties.Resources.e,
            global::CondorAutoPilot.Properties.Resources.w,
            global::CondorAutoPilot.Properties.Resources.s,
            global::CondorAutoPilot.Properties.Resources.n,
            global::CondorAutoPilot.Properties.Resources.dot

        };

        public Bitmap captureLocationBitmap;
        public Bitmap captureLocationGrayBitmap;
        public Bitmap captureAltitudeBitmap;
        public Bitmap captureAltitudeGrayBitmap;
        public string my_strResult;

        private Rectangle myCaptureRectangle;
        private int captLocationWidth, captAltitudeWidth, captHeight, captLocationX, captAltitudeX, captY;

        public bool Init()
        {



            myCaptureRectangle = Screen.AllScreens[0].Bounds;
            if (myCaptureRectangle.Width == 2560)
            {
                captLocationWidth = 530;
                captAltitudeWidth = 80;

                captHeight = 27;
                captLocationX = 2560 - captLocationWidth;
                captAltitudeX = 1120;

                captY = 1440 - captHeight - 55;
            }
            else if (myCaptureRectangle.Width == 1920)
            {
                captLocationWidth = 395;
                captAltitudeWidth = 60;
                captHeight = 22;
                captLocationX = 1525;
                captAltitudeX = 840;
                captY = 1008;
            }
            else
                return false;

            captureLocationBitmap = new Bitmap(captLocationWidth, captHeight, PixelFormat.Format32bppArgb);
            captureLocationGrayBitmap = new Bitmap(captLocationWidth, captHeight, PixelFormat.Format32bppArgb);

            captureAltitudeBitmap = new Bitmap(captAltitudeWidth, captHeight, PixelFormat.Format32bppArgb);
            captureAltitudeGrayBitmap = new Bitmap(captAltitudeWidth, captHeight, PixelFormat.Format32bppArgb);

            return true;

        }
        public bool GetAltitude(out float dAltitude)
        {

            Graphics captureGraphics = Graphics.FromImage(captureAltitudeBitmap);
            Rectangle captureRectangle = Screen.AllScreens[0].Bounds;

            captureGraphics.CopyFromScreen(captAltitudeX, captY,
                0, 0,
                captureRectangle.Size);

            dAltitude = 0;

           // SaveBWImage(captureAltitudeBitmap);

            if (!MakeBWImage(captureAltitudeBitmap, captureAltitudeGrayBitmap))
                return false;


            if (ExtractCharacters(captureAltitudeGrayBitmap, out List<string> strResults, out List<int> iResults) < 1)
                return false;

            for (int i = 0; i < iResults.Count(); i++)
            {
                dAltitude += Convert.ToSingle(iResults[i] * Math.Pow(10, iResults.Count() - i - 1));
            }

            return true;
        }
        public bool GetLocation(out float dLat, out float dLon)
        {

            Graphics captureGraphics = Graphics.FromImage(captureLocationBitmap);
            Rectangle captureRectangle = Screen.AllScreens[0].Bounds;

            captureGraphics.CopyFromScreen(captLocationX, captY,
                0, 0,
                captureRectangle.Size);

            dLat = 0;
            dLon = 0;

            if (!MakeBWImage(captureLocationBitmap, captureLocationGrayBitmap))
                return false;
            if (false)
                SaveBWImage(captureLocationBitmap);
           if (ExtractCharacters(captureLocationGrayBitmap, out List<string> strResults, out List<int> iResults) != 21)
                return false;

            dLat = 10000 * iResults[3] +
                1000 * iResults[4] +
                100 * iResults[6] +
                10 * iResults[7] +
                iResults[8];
            dLat = dLat / 60000;

            dLat += 10 * iResults[0] + iResults[1];

            if (strResults[9] == "S")
                dLat = -dLat;


            dLon = 10000 * iResults[14] +
                1000 * iResults[15] +
                100 * iResults[17] +
                10 * iResults[18] +
                iResults[19];
            dLon = dLon / 60000;

            dLon += 100 * iResults[10] + 10 * iResults[11] + iResults[12];

            if (strResults[20] == "W")
                dLon = -dLon;


            return true;
        }

        public bool SetBWImage(ref System.Windows.Forms.PictureBox pictureBox)
        {
            pictureBox.Image = captureLocationBitmap;
            return true;
        }

        private bool ColorIsCloseTo(Color color, int iR, int iG, int iB)
        {
            int iDiff;

            iDiff = color.R - iR;
            if (iDiff > 12 || iDiff < -12)
                return false;

            iDiff = color.G - iG;
            if (iDiff > 12 || iDiff < -12)
                return false;

            iDiff = color.B - iB;
            if (iDiff > 12 || iDiff < -12)
                return false;

            return true;
        }

        private bool MakeBWImage(Bitmap cBitmap, Bitmap cGrayBitmap)
        {
            // Graphics captureGrayGraphics = Graphics.FromImage(captureGrayBitmap);
            for (int i = 0; i < cBitmap.Width; i++)
            {
                for (int j = 0; j < cBitmap.Height; j++)
                {
                    Color px = cBitmap.GetPixel(i, j);
                    if (ColorIsCloseTo(px, 250, 250, 10))
                    {
                        cGrayBitmap.SetPixel(i, j, Color.Black);
                    }
                    else if (ColorIsCloseTo(px, 215, 225, 155))
                    {
                        cGrayBitmap.SetPixel(i, j, Color.Black);
                    }
                    else
                    {
                        cGrayBitmap.SetPixel(i, j, Color.White);
                    }
                }
            }

            for (int i = 1; i < cGrayBitmap.Width - 1; i++)
            {
                for (int j = 1; j < cGrayBitmap.Height - 1; j++)
                {
                    Color px = cGrayBitmap.GetPixel(i, j);
                    if (px.R == 255)
                    {
                        if (
                             cGrayBitmap.GetPixel(i - 1, j).R == 0 ||
                             cGrayBitmap.GetPixel(i - 1, j - 1).R == 0 ||
                             cGrayBitmap.GetPixel(i - 1, j + 1).R == 0 ||
                             cGrayBitmap.GetPixel(i + 1, j).R == 0 ||
                             cGrayBitmap.GetPixel(i + 1, j - 1).R == 0 ||
                             cGrayBitmap.GetPixel(i + 1, j + 1).R == 0 ||
                             cGrayBitmap.GetPixel(i, j + 1).R == 0 ||
                             cGrayBitmap.GetPixel(i, j - 1).R == 0
                             )
                        {
                            cGrayBitmap.SetPixel(i, j, Color.Gray);
                        }
                    }
                }
            }
            return true;
        }

        public bool SaveBWImage(Bitmap cBitmap)
        {
            strGuid = System.Guid.NewGuid().ToString();

            string strBMPFile = Properties.Settings.Default.CondorApplicationPath + @"\ScreenReader\captureBW_" + strGuid + ".bmp";
            cBitmap.Save(strBMPFile, ImageFormat.Bmp);

            strGuid = System.Guid.NewGuid().ToString();

            strBMPFile = Properties.Settings.Default.CondorApplicationPath + @"\ScreenReader\capture_" + strGuid + ".bmp";
            cBitmap.Save(strBMPFile, ImageFormat.Bmp);

            return true;
        }

        public int ExtractCharacters(Bitmap cGrayBitmap, out List<string> strResults, out List<int> iResults)
        {
            strResults = new List<string>();
            iResults = new List<int>();

            int iStart = 0;
            int iCount = 0;

            for (int i = 0; i < cGrayBitmap.Width; i++)
            {
                bool bIsBlocked = false;
                for (int j = 0; j < cGrayBitmap.Height; j++)
                {
                    Color px = cGrayBitmap.GetPixel(i, j);
                    if (px.R < 25)
                    {
                        bIsBlocked = true;
                        break;
                    }
                }

                if (!bIsBlocked)
                {
                    if (i - iStart > 2)
                    {
                        int iWidth = i - iStart + 1;
                        Bitmap charBitmap = new Bitmap(iWidth, captHeight, PixelFormat.Format32bppArgb);
                        for (int ic = 0; ic < charBitmap.Width; ic++)
                        {
                            for (int j = 0; j < charBitmap.Height; j++)
                            {
                                charBitmap.SetPixel(ic, j, cGrayBitmap.GetPixel(iStart + ic, j));
                            }
                        }
                        iCount++;

                        if (WhichCharacter(ref charBitmap, out string strChar, out int iFound))
                        {
                            iResults.Add(iFound);
                            if (strChar == "dot")
                            {
                                strResults.Add(".");
                            }
                            else
                            {
                                strResults.Add(strChar);
                            }
                        }
                        else
                            return -1;

                    }
                    iStart = i;
                }
            }

            my_strResult = "";
            for (int i = 0; i < strResults.Count(); i++)
            {
                my_strResult = my_strResult + strResults[i];
            }

            return strResults.Count();
        }

        private bool WhichCharacter(ref Bitmap bmp, out string strChar, out int iFound)
        {
            strChar = "";

            iFound = -1;
            double dQ = 0;
            for (int i = 0; i < strCharSet.Count(); i++)
            {
//                string strBMPFile = Properties.Settings.Default.CondorApplicationPath + @"\ScreenReader\" + strCharSet[i] + ".bmp";
//                if (!File.Exists(strBMPFile))
//                    continue;

//                Bitmap bitmapChar = new Bitmap(strBMPFile);

                if (CompareBitMaps(ref bmp, ref bmpCharaters[i], out double dQuality))
                {
                    if (dQuality > 0.9)
                    {
                        if (dQuality > dQ)
                        {
                            iFound = i;
                            dQ = dQuality;
                        }
                    }
                }
           }
            if (iFound == -1)
            {
                string strBMPFile = Properties.Settings.Default.CondorApplicationPath + @"\ScreenReader\char_" + System.Guid.NewGuid().ToString() + ".bmp";
                bmp.Save(strBMPFile, ImageFormat.Bmp);
                return false;
            }
            strChar = strCharSet[iFound];
            return true;
        }

        private bool CompareBitMaps(ref Bitmap bmp0, ref Bitmap bmp1, out double dQuality)
        {
            int nWidth;
            int scaleW0, scaleW1;
            int stepW, stepH;

            if (bmp0.Width == bmp1.Width)
            {
                nWidth = bmp0.Width;
                scaleW0 = 1;
                scaleW1 = 1;
                stepW = 1;
            }
            else
            {
                nWidth = bmp0.Width * bmp1.Width;
                scaleW0 = bmp1.Width;
                scaleW1 = bmp0.Width;
                stepW = Math.Min(scaleW0, scaleW1);
            }
            int nHeight;
            int scaleH0, scaleH1;

            if (bmp0.Height == bmp1.Height)
            {
                nHeight = bmp0.Height;
                scaleH0 = 1;
                scaleH1 = 1;
                stepH = 1;
            }
            else
            {
                nHeight = bmp0.Height * bmp1.Height;
                scaleH0 = bmp1.Height;
                scaleH1 = bmp0.Height;
                stepH = Math.Min(scaleH0, scaleH1);
            }

            int iIdentical = 0;
            int iDifferent = 0;

            int rg = Color.Gray.R;

            for (int i = 0; i < nWidth-1; i += stepW)
            {
                int i0 = i / scaleW0;
                int i1 = i / scaleW1;
                for (int j = 1; j < nHeight - 1; j += stepH)
                {
                    int j0 = j / scaleH0;
                    int j1 = j / scaleH1;
                    int r0, r1;
                    r0 = bmp0.GetPixel(i0, j0).R;
                    r1 = bmp1.GetPixel(i1, j1).R;

                    if (r1 == rg) continue;
                    if (r0 == rg) continue;
                    if (r1 == r0)
                        iIdentical++;
                    else
                        iDifferent++;
                }
            }

            dQuality = 1.0 -  iDifferent / Convert.ToDouble(iIdentical + iDifferent);

            return true;
        }
    }
}
