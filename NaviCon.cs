using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System.IO;

namespace CondorAutoPilot
{

    class NaviCon
    {
//        [DllImport("NaviCon.dll", CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, EntryPoint = "NaviConInit")]
//        private static extern bool NaviConInit([MarshalAs(UnmanagedType.LPStr)] string TRNFile);

// //       [DllImport("NaviCon.dll", EntryPoint = "NaviConInit")]

////        private static extern bool NaviConInit(ref string TRNFile);


//        [DllImport("NaviCon.dll", EntryPoint = "XYToLon")]
//        private static extern float XYToLon(float X, float Y);
//        [DllImport("NaviCon.dll", EntryPoint = "XYToLat")]
//        private static extern float XYToLat(float X, float Y);

//        [DllImport("NaviCon.dll", EntryPoint = "GetMaxX")]
//        private static extern float GetMaxX();

//        [DllImport("NaviCon.dll", EntryPoint = "GetMaxY")]
//        private static extern float GetMaxY();


        static double[] densities = new double[] { 1.225, 1.112, 1.007, 0.9093, 0.8194, 0.7364, 0.6601, 0.5900, 0.5258, 0.4671, 0.4135, 0.1948 };

        private static double Density(float dAltitude)
        {
            int iAltitude = Convert.ToInt32(Math.Floor(dAltitude / 1000));
            if (iAltitude < 0)
                iAltitude = 0;
            if (iAltitude > 10)
                iAltitude = 10;

            double dRhoAltitude = Utils.interpolate(dAltitude, iAltitude * 1000, densities[iAltitude], iAltitude * 1000 + iAltitude < 10 ? 1000: 5000, densities[iAltitude + 1]);
            return dRhoAltitude;
        }
        public static float IAS2TAS(float dIAS, float dAltitude)
        {
            double dRhoAltitude = Density(dAltitude);
            double dFact = Math.Sqrt(1.225 / dRhoAltitude);
            return Convert.ToSingle(dIAS * dFact);
        }
        public static float TAS2IAS(float dTAS, float dAltitude)
        {
            double dRhoAltitude = Density(dAltitude);
            double dFact = Math.Sqrt(dRhoAltitude / 1.225);
            return Convert.ToSingle(dTAS * dFact);
        }
    //    public static bool initLandscape(string strLandscape)
    //    {
    //        string strTRN = Properties.Settings.Default.CondorApplicationPath + 
    //            @"\Landscapes\" + 
    //            strLandscape + 
    //            @"\" + 
    //            strLandscape + 
    //            @".trn";
    //        if (!File.Exists(strTRN))
    //            return false;

    //        float xMax = GetMaxX();
    //        float yMax = GetMaxY();

    //        return NaviConInit(strTRN);
    //    }
    //    public static void getLatLonFromXY(float X, float Y, out float Lat, out float Lon)
    //    {
    //        Lat = XYToLat(X, Y);
    //        Lon = XYToLon(X, Y);
    //    }

    //    public static void getXYFromLatLon(float targetLat, float targetLon, ref float X, ref float Y)
    //    {
    //        float dO = 100f;
    //        float dPrecision = 10;
    //        int iter = 0;
    //        while (dPrecision > 1f && iter < 20)
    //        {
    //            dPrecision = 0;
    //            iter++;
    //            //do X
    //            {
    //                float lat = XYToLat(X, Y);
    //                float lat1 = XYToLat(X, Y + dO);

    //                if (lat1 != lat)
    //                {
    //                    float fact = dO * (targetLat - lat) / (lat1 - lat);
    //                    Y += 0.5f * fact;
    //                    dPrecision = Math.Abs(fact);
    //                }
    //            }
    //            //do Y
    //            {
    //                float lon = XYToLon(X, Y);
    //                float lon1 = XYToLon(X + dO, Y);

    //                if (lon1 != lon)
    //                {
    //                    float fact = dO * (targetLon - lon) / (lon1 - lon);
    //                    X += 0.5f * fact;
    //                    dPrecision += Math.Abs(fact);
    //                }
    //            }
    //        }
    //        iter = 0;
    //    }
    }
}
