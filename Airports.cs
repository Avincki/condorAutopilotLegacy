using GMap.NET.WindowsForms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondorAutoPilot
{
    public class AirportItem
    {
        public string Name { get; set; }
        public float Lat { get; set; }
        public float Lon { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Altitude { get; set; }
        public int Orientation { get; set; }
        public int Length { get; set; }
        public int Width { get; set; }

        public bool GearOut { get; set; }
        public float QX { get; set; }
        public float QY { get; set; }
        public float QZ { get; set; }


        private float dSlope = Convert.ToSingle(Math.Tan(Utils.deg2rad(6)));
        private float dCurvelength = 200;
        private float dA;
        private float dY0 = 0;
        private float dY1 = 0;
        private float dY2 = 0;
        private float dYI = 0;
        private float dZD;// height at curve start
        public bool PreLanding { get; set; }

        public AirportItem()
        {
            GearOut = false;
            PreLanding = true;
        }

        public void Set(AirportItem other)
        {
            Name = other.Name;
            Lat = other.Lat;
            Lon = other.Lon;
            X = other.X;
            Y = other.Y;
            Altitude = other.Altitude;
            Orientation = other.Orientation;
            Length = other.Length;
            Width = other.Width;
            dA = 0.5f * dSlope / dCurvelength;
            dZD = 0.5f * dSlope * dCurvelength + Altitude;// height at curve start
            PreLanding = true;

            QY = 0;
        }

        public bool IsCloseToLanding(Point3d ptPlaneLocation, float delta)
        {
            return (ptPlaneLocation.Z - Altitude < delta);
        }

        public Point3d GetPosition()
        {
            Point3d pt = new Point3d(X, Y, Altitude);
            pt = pt.add(-1.5f*dCurvelength, GetVector());
            return pt;
        }

        public Vector3d GetVector()
        {
            double alfa = Utils.deg2rad(Orientation);
            Vector3d vec = new Vector3d(-Math.Cos(alfa), -Math.Sin(alfa), 0);
            return vec;
        }
        public Vector3d GetPerpVector()
        {
            double alfa = Utils.deg2rad(Orientation);
            Vector3d vec = new Vector3d(-Math.Sin(alfa), Math.Cos(alfa), 0);
            return vec;
        }

        public Point3d GetTarget(Point3d ptPlaneLocation)
        {
            QX = XAtPlaneAltitude(ptPlaneLocation.Z);
            Point3d ptTarget = GetPosition().add(XAtPlaneAltitude(ptPlaneLocation.Z), GetVector());

            if (PreLanding)
            {
                ptTarget = ptTarget.add(0, 500, GetVector());
                if (ptTarget.distanceXYTo(ptPlaneLocation) > 500)
                {
                    ptTarget.Z = ptPlaneLocation.Z + 50;
                    QZ = ptTarget.Z - Altitude;
                    return ptTarget;
                }
            }

            PreLanding = false;

            Vector3d vec = new Vector3d(GetPosition(), ptPlaneLocation);
            vec.z = 0;

            float dX = GetVector().dotProduct(vec);
            float dAltitude = AltitudeAtX(dX);
            ptTarget = GetPosition().add(dX, GetVector());
            float dT = Utils.InterpolateFloat(dX, -2000, 1000, 200, 500);

            dY2 = dY1;
            dY1 = dY0;
            dY0 = GetPerpVector().dotProduct(vec);
            dYI += 0.01f * dY0;
            if (dYI > 30)
                dYI = 30;
            else if (dYI < -30)
                dYI = -30;


            float dA = (dY2 - 2 * dY1 + dY0) / 2;
            float dB = (dY0 - dY2) / 2;
            float dC = dY1;
            float dXD = 4;
            float dYTarget = dA * dXD * dXD + dB * dXD + dC;
            //QY = 0.5f * dYTarget;
            QY = 0.25f * dY0 + dYI + 0.2f * (dY0 - dY2);

            ptTarget = ptTarget.add(dT, QY, GetVector());
            ptTarget.Z = dAltitude;


            QZ = ptTarget.Z - Altitude;


            return ptTarget;
        }

        private float XAtPlaneAltitude(float z)
        {
            double dXAtPlaneAltitude;

            if (z < Altitude + 1)
            {
                dXAtPlaneAltitude = 2500;
            }
            else if (z < dZD)
            {
                dXAtPlaneAltitude = -Math.Sqrt((z - Altitude) / dA);
            }
            else
            {
                dXAtPlaneAltitude = -(z - Altitude) / dSlope - 0.5 * dCurvelength;
            }
            return Convert.ToSingle(dXAtPlaneAltitude);
        }
        private float AltitudeAtX(float x)
        {
            float dAltitude;

            if (x > 0)
            {
                dAltitude = Altitude + 0;// - dSlope * x/8;
            }
            else if (x > -dCurvelength)
            {
                dAltitude = dA * x * x + Altitude + 0;
            }
            else
            {
                dAltitude = - dSlope*( x + 0.5f*dCurvelength) + Altitude + 0;
            }
            return dAltitude;
        }


    }

    public class Airports
    {
        public List<AirportItem> myAirports { get; set; }
        public AirportItem CurrentAirport { get; set; }
        public bool HasCurrentAirport { get; set; }

        public bool Init(FlightPlanData flightPlan)
        {
            CurrentAirport = new AirportItem();
            myAirports = new List<AirportItem>();
            HasCurrentAirport = false;
            string strAPTFile = Properties.Settings.Default.CondorApplicationPath +
                "\\Landscapes\\" +
                flightPlan.StrLandscape + "\\" +
                flightPlan.StrLandscape + ".apt";


            BinaryReader binReader =
                new BinaryReader(File.Open(strAPTFile, FileMode.Open), Encoding.ASCII);
            try
            {
                while (binReader.BaseStream.Position != binReader.BaseStream.Length)
                {
                    AirportItem item = new AirportItem();
                    byte b = binReader.ReadByte();
                    char[] charRead = binReader.ReadChars(35);
                    string str = new string(charRead);
                    item.Name = str.Replace("\0", "");
                    item.Lat = binReader.ReadSingle();
                    item.Lon = binReader.ReadSingle();
                    item.Altitude = binReader.ReadSingle();
                    item.Orientation = 90 + binReader.ReadInt32();
                    item.Length = binReader.ReadInt32();
                    item.Width = binReader.ReadInt32();
                    int iAsphalt = binReader.ReadInt32();
                    float fFrequency = binReader.ReadSingle();
                    b = binReader.ReadByte();
                    b = binReader.ReadByte();
                    b = binReader.ReadByte();
                    b = binReader.ReadByte();

                    float X = 0;
                    float Y = 0;
                    Form1.naviCon64.calcUTMIter(
                        item.Lat,
                        item.Lon,
                        out X,
                        out Y);
                    item.X = X;
                    item.Y = Y;


                    myAirports.Add(item);
                }
                binReader.Close();
            }
            catch (IOException ioexp)
            {
                binReader.Close();
                return false;
            }
            binReader.Close();

            return true;
        }

        public bool SetCurrentAirport(string strAirportName)
        {
            HasCurrentAirport = false;

            foreach (AirportItem item in myAirports)
            {
                if (item.Name.Equals(strAirportName, StringComparison.OrdinalIgnoreCase))
                {
                    CurrentAirport.Set(item);
                    HasCurrentAirport = true;

                }
            }
            return HasCurrentAirport;
        }

        public bool SetCurrentAirport(Point3d ptLocation, Vector3d vecWind)
        {
            HasCurrentAirport = false;

            double distanceCheck = Double.MaxValue;

            foreach (AirportItem item in myAirports)
            {
                double dist = item.GetPosition().distanceXYTo(ptLocation);
                Vector3d vecAirport = new Vector3d(Utils.deg2rad(item.Orientation));
                double dWind = 5000*Math.Abs(vecWind.dotProduct(vecAirport));
                dist -= dWind;

                if (dist < distanceCheck)
                {
                    distanceCheck = dist;
                    CurrentAirport.Set(item);
                    HasCurrentAirport = true;
                }
            }
            return HasCurrentAirport;
        }

    
        public bool SetTarget(Point3d ptLocation)
        {

            return false;
        }

        public void FillMap(GMapControl mapControl)
        {

        }

    }
}
