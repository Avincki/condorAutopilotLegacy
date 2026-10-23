using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Security.Policy;


using GMap.NET.WindowsForms;
using GMap.NET;
using GMap.NET.MapProviders;
using System.Drawing;
using GMap.NET.WindowsForms.Markers;
using System.Data.Entity;
using System.Windows.Forms;

namespace CondorAutoPilot
{
    public class FlightPlanMarker
    {
        public string strMarker { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float safeX { get; set; }
        public float safeY { get; set; }
        public float safeZ { get; set; }
        public float lat { get; set; }
        public float lon { get; set; }

        public float radius { get; set; }

        public List<Point3d> dijkstraPath { get; set; }

        public int dijkstraPos { get; set; }

        public int calDijkstraPath = 0; // 0 = not ready, 1 = calculating, 2 = ready


        public void set(string strName, float fX, float fY, float fZ, float r)
        {
            strMarker = strName;
            X = fX;
            Y = fY;
            Z = fZ;
            radius = r;
            safeX = fX;
            safeY = fY;
            safeZ = fZ;
            //float dlat64, dlon64;
            //Form1.naviCon64.calcLatLon(X, Y, out dlat64, out dlon64);
            //Form1.naviCon64.calcUTMIter(dlat64, dlon64, out double X64, out double Y64);
            float dlat, dlon;
            Form1.naviCon64.calcLatLon(X, Y, out dlat, out dlon);
            lat = dlat;
            lon = dlon;
            dijkstraPath = new List<Point3d>();
            dijkstraPos = 0;
        }

        public void set(string strName)
        {
            strMarker = strName;
            float dlat, dlon;
            Form1.naviCon64.calcLatLon(X, Y, out dlat, out dlon);
            lat = dlat;
            lon = dlon;
        }
        public Point3d getPoint(bool bSafe = false)
        {
            if (bSafe)
            {
                Point3d pt = new Point3d(safeX, safeY, safeZ);
                return pt;
            }
            else
            {
                Point3d pt = new Point3d(X, Y, Z);
                return pt;
            }
        }

        public bool SetDijkstraPath(List <Point3d> path)
        {
            dijkstraPath.Clear();
            foreach (Point3d pt in path)
            {
                dijkstraPath.Add(new Point3d(pt));
            }
            return true;
        }

        public bool GetTarget(Point3d pt, double dist, out Point3d ptTarget)
        {
            dijkstraPos = 0;
            ptTarget = getPoint();
            if (dijkstraPath.Count() == 0)
                return false;

            for (int i = dijkstraPath.Count() - 1; i >= 0; i--)
            {
                if (pt.distanceXYTo(dijkstraPath[i]) < dist)
                {
                    ptTarget = dijkstraPath[i];
                    dijkstraPos = i;
                    return true;
                }
            }
            return false;
        }
        
        public bool AppendToDijkstraPath(List <Point3d> path)
        {
            if (path.Count() == 0 || dijkstraPos == 0)
                return false;

            if (path.Last().distanceXYTo(dijkstraPath[dijkstraPos]) > 2000)
                return false;

            path.RemoveAt(path.Count() - 1);

            for (int i = dijkstraPos; i < dijkstraPath.Count(); i++)
            {
                path.Add(new Point3d(dijkstraPath[i]));
            }
            return true;
        }

        public bool GetMapRoute(List<PointLatLng> pointsWayPlanner)
        {
            for (int i = dijkstraPos; i < dijkstraPath.Count(); i++)
            {
                float dlat, dlon;
                Form1.naviCon64.calcLatLon(dijkstraPath[i].X, dijkstraPath[i].Y, out dlat, out dlon);

                pointsWayPlanner.Add(new PointLatLng(dlat, dlon));
            }
            return pointsWayPlanner.Count() > 0;
        }

    }

public class FlightPlanPointData
    {
        public Point3d ptOnPath { get; set; }
        public Vector3d vecDir { get; set; }
        public Point3d ptMaxFunction { get; set; }
        public double dMaxFunction { get; set; }
        public double dMaxPos { get; set; }
        public double dIntegration { get; set; }

        public List<short> trnIndexList { get; set; }

        public FlightPlanPointData(Point3d pt)
        {
            ptOnPath = pt;
            dIntegration = 0;
        }
        public FlightPlanPointData(Point3d pt, Vector3d vec)
        {
            ptOnPath = pt;
            vecDir = vec;
            dIntegration = 0;
        }

    }
    public class FlightPlanData
    {
        public string StrLandscape { get; set; }
        public double RemainingDistance { get; set; }
        public double RemainingDistanceMinimum { get; set; }
        public double RemainingIntegrationLiftPotential { get; set; }

        public bool HasElectronics { get; set; }
        public double ReferenceSpeed { get; set; }

        public float ThermalsInversionheight { get; set; }

        public List<FlightPlanMarker> Markers { get; set; }

        //public List<FlightPlanPointData> Path100mInterval { get; set; }

        private string myFlightPlanFileName;
        private string myFlightPlanName;
        public FlightPlanMarker markerCenter = new FlightPlanMarker();
        public FlightPlanMarker markerLeftBottom = new FlightPlanMarker();
        public FlightPlanMarker markerRightTop = new FlightPlanMarker();
        public int nextWayPoint { get; set; }

        public Vector3d vecWind { get; set; }
        public float windSpeed { get; set; }
        public float RaceStartTime { get; set; }
        public int StartType { get; set; }

        public FlightPlanData()
        {
            nextWayPoint = 0;
            Markers = new List<FlightPlanMarker>();
            RemainingDistanceMinimum = -100;// to initiate
        }

        public FlightPathPoint getNextMarkerPosition(bool bSafe = false)
        {
            int i = nextWayPoint;
            if (nextWayPoint >= Markers.Count)
                i = Markers.Count-1;

            FlightPathPoint pt = new FlightPathPoint(Markers[i].getPoint(bSafe));
            return pt;
        }


        public FlightPlanMarker CurrentMarker()
        {
            if (nextWayPoint == 0 || nextWayPoint == Markers.Count)
                return null;
            else
                return Markers[nextWayPoint];
        }


        public bool SetNextWayPoint(int iChange)
        {
            nextWayPoint += iChange;
            if (nextWayPoint >= Markers.Count)
                nextWayPoint = Markers.Count - 1;

            if (nextWayPoint < 0)
                nextWayPoint = 0;
            return true;
        }

        public bool SetNextWayPoint(Point3d ptPlane, float dCurrentTime)
        {
            if (nextWayPoint >= Markers.Count)
                return false;

            if (RaceStartTime > dCurrentTime && nextWayPoint == 1)
                return false;

            if (nextWayPoint == 0)
            {
                double dd = ptPlane.distanceXYTo(Markers[nextWayPoint].getPoint());
                if (dd < 500)
                {
                    nextWayPoint++;
                    return true;
                }
            }
            else if (Markers[nextWayPoint].radius == 0)
            {
                double dd = ptPlane.distanceXYTo(Markers[nextWayPoint].getPoint(true));
                if (dd < 275)
                {
                    nextWayPoint++;
                    return true;
                }
            }
            else
            {
                double dd = ptPlane.distanceXYTo(Markers[nextWayPoint].getPoint());
                if (dd < 0.8*Markers[nextWayPoint].radius)
                {
                    nextWayPoint++;
                    return true;
                }
            }
            return false;
        }


        public FlightPlanMarker GetNextSafeWayPoint()
        {
            if (nextWayPoint >= Markers.Count)
                return Markers.Last();
            else
                return Markers[nextWayPoint];

        }
        public double ParallelDistanceToNextWayPoint(FlightPathPoint pt)
        {
            return ParallelDistanceToNextWayPoint(pt.x, pt.y);
        }

        public double ParallelDistanceToNextWayPoint(float X, float Y)
        {
            if (nextWayPoint == 0)
                return 0;
            if (nextWayPoint >= Markers.Count)
                return 0;

            Vector3d vecDir = new Vector3d(Markers[nextWayPoint-1].getPoint(), Markers[nextWayPoint].getPoint());
            vecDir.z = 0;

            vecDir.normalize();

            Vector3d vecMarker = new Vector3d(new Point3d(X, Y, 0), Markers[nextWayPoint].getPoint() );

            double dd;
            dd = vecDir.dotProduct(vecMarker);

            return dd;
        }

        private bool getToken(ref string strContent, string strToken, out string strOut)
        {
            int iPos;
            strOut = "";
            try
            {
                if ((iPos = strContent.IndexOf(strToken + "=")) >= 0)
                {
                    string str = strContent.Substring(iPos + 1 + strToken.Length);
                    str = str.Substring(0, str.IndexOf("\n"));
                    if (str.IndexOf("\r")>0)
                        str = str.Substring(0, str.IndexOf("\r"));
                    strOut = str;
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        private bool getToken(ref string strContent, string strToken, out float fOut)
        {
            fOut = 0;
            int iPos;
            try
            {
                if ((iPos = strContent.IndexOf(strToken + "=")) >= 0)
                {
                    string str = strContent.Substring(iPos + 1 + strToken.Length);
                    str = str.Substring(0, str.IndexOf("\n"));
                    if (str.IndexOf("\r") > 0)
                        str = str.Substring(0, str.IndexOf("\r"));
                    fOut = float.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        private bool getToken(ref string strContent, string strToken, out int iOut)
        {
            iOut = 0;
            int iPos;
            try
            {
                if ((iPos = strContent.IndexOf(strToken + "=")) >= 0)
                {
                    string str = strContent.Substring(iPos + 1 + strToken.Length);
                    str = str.Substring(0, str.IndexOf("\n"));
                    if (str.IndexOf("\r") > 0)
                        str = str.Substring(0, str.IndexOf("\r"));
                    iOut = int.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool setArea()
        {
            bool bStart = true;
            for (int i = 0; i < Markers.Count; i++)
            {
                if (bStart)
                {
                    markerLeftBottom.X = markerRightTop.X = Markers[i].X;
                    markerLeftBottom.Y = markerRightTop.Y = Markers[i].Y;
                    markerLeftBottom.X = markerRightTop.X = Markers[i].X;
                    markerLeftBottom.Y = markerRightTop.Y = Markers[i].Y;
                    bStart = false;
                }
                else
                {
                    markerLeftBottom.X = Math.Min(Markers[i].X, markerLeftBottom.X);
                    markerLeftBottom.Y = Math.Min(Markers[i].Y, markerLeftBottom.Y);

                    markerRightTop.X = Math.Max(Markers[i].X, markerRightTop.X);
                    markerRightTop.Y = Math.Max(Markers[i].Y, markerRightTop.Y);

                }
            }
            markerCenter.set("Center", (markerLeftBottom.X + markerRightTop.X) / 2, (markerLeftBottom.Y + markerRightTop.Y) / 2, 0, 0);
            markerLeftBottom.set("markerLeftBottom");
            markerRightTop.set("markerRightTop");

            return true;
        }

        public bool getMapPolygon (ref GMapOverlay mapOverlay)
        {
            List<PointLatLng> points = new List<PointLatLng>();
            for (int i = 0; i < Markers.Count; i++)
            {
                points.Add(new PointLatLng(Markers[i].lat, Markers[i].lon));
            }
            GMapPolygon polygon = new GMapPolygon(points, myFlightPlanName);
            polygon.Stroke = new Pen(Color.Red, 3);

            mapOverlay.Polygons.Add(polygon);
            return true;
        }

        public bool getMapRoute(ref GMapOverlay mapOverlay)
        {
            List<PointLatLng> points = new List<PointLatLng>();
            for (int i = 0; i < Markers.Count; i++)
            {
                points.Add(new PointLatLng(Markers[i].lat, Markers[i].lon));
            }
            GMapRoute route = new GMapRoute(points, myFlightPlanName);
            route.Stroke = new Pen(Color.Red, 3);

            mapOverlay.Routes.Add(route);
            return true;
        }

        public bool getMapMarkers(ref GMapOverlay mapOverlay)
        {
            for (int i = 0; i < Markers.Count; i++)
            {
                GMarkerGoogle marker = new GMarkerGoogle(new PointLatLng(Markers[i].lat, Markers[i].lon), GMarkerGoogleType.green);
                marker.ToolTipText = Markers[i].strMarker;
                marker.ToolTipMode = MarkerTooltipMode.Never;
                //marker.IsVisible = false;

                mapOverlay.Markers.Add(marker);
            }
            setArea();
            GMarkerGoogle markerLB = new GMarkerGoogle(new PointLatLng(markerLeftBottom.lat - 0.05, markerLeftBottom.lon - 0.05), GMarkerGoogleType.green);
            //markerLB.IsVisible = false;
            mapOverlay.Markers.Add(markerLB);
            GMarkerGoogle markerRT = new GMarkerGoogle(new PointLatLng(markerRightTop.lat + 0.05, markerRightTop.lon + 0.05), GMarkerGoogleType.green);
            //markerRT.IsVisible = false;
            mapOverlay.Markers.Add(markerRT);


            return true;
        }

        public bool GetMarkerLocation(int i, out float dLat, out float dLon)
        {
            if (i >= Markers.Count || i < 0)
            {
                dLat = 0;
                dLon = 0;
                return false;
            }
            dLat = Markers[i].lat;
            dLon = Markers[i].lon;
            return true;
        }

        public bool GetMarkerLocationXYZ(int i, out float dX, out float dY, out float dZ)
        {
            if (i >= Markers.Count || i < 0)
            {
                dX = 0;
                dY = 0;
                dZ = 0;
                return false;
            }
            dX = Markers[i].X;
            dY = Markers[i].Y;
            dZ = Markers[i].Z;
            return true;
        }

        public bool GetSafeWayPointLocation(int i, float X, float Y, out float dX, out float dY, out float dZ)
        {

            if (i >= Markers.Count || i < 0)
            {
                dX = 0;
                dY = 0;
                dZ = 0;
                return false;
            }
            dX = X - Markers[i].safeX;
            dY = Y - Markers[i].safeY;
            double dist = Math.Sqrt(dX * dX + dY * dY);
            if (dist < 2500)
            {
                dX = Markers[i].safeX;
                dY = Markers[i].safeY;
                dZ = Markers[i].safeZ;
            }
            else
            {
                dX = Markers[i].X;
                dY = Markers[i].Y;
                dZ = Markers[i].Z;

            }
            return true;
        }
        public bool CalcSafeLocations(float dOffset)
        {

            for (int i = 0; i < Markers.Count; i++)
            {

                double dxpre = 0;
                double dypre = 0;
                double dlpre = 0;
                double dxpost = 0;
                double dypost = 0;
                double dlpost = 0;

                if (i > 0)
                {
                    dxpre = Markers[i].X - Markers[i - 1].X;
                    dypre = Markers[i].Y - Markers[i - 1].Y;
                    dlpre = Math.Sqrt(dxpre * dxpre + dypre * dypre);
                    if (dlpre > 0)
                    {
                        dxpre /= dlpre;
                        dypre /= dlpre;
                    }
                }
                if (i < Markers.Count - 1)
                {
                    dxpost = Markers[i + 1].X - Markers[i].X;
                    dypost = Markers[i + 1].Y - Markers[i].Y;
                    dlpost = Math.Sqrt(dxpost * dxpost + dypost * dypost);
                    if (dlpost > 0)
                    {
                        dxpost /= dlpost;
                        dypost /= dlpost;
                    }
                }
                if (i < 2)
                {
                    Markers[i].safeX = Markers[i].X - 1.5f * Convert.ToSingle(dxpost * dOffset);
                    Markers[i].safeY = Markers[i].Y - 1.5f * Convert.ToSingle(dypost * dOffset);
                    Markers[i].safeZ = Markers[i].Z;
                    continue;
                }
                if (i == Markers.Count - 1)
                {
                    Markers[i].safeX = Markers[i].X + 1.5f * Convert.ToSingle(dxpre * dOffset);
                    Markers[i].safeY = Markers[i].Y + 1.5f * Convert.ToSingle(dypre * dOffset);
                    Markers[i].safeZ = Markers[i].Z;
                    continue;
                }


                double dxoff = dxpre - dxpost;
                double dyoff = dypre - dypost;
                double dloff = Math.Sqrt(dxoff * dxoff + dyoff * dyoff);
                if (dloff == 0)
                {
                    Markers[i].safeX = Markers[i].X;
                    Markers[i].safeY = Markers[i].Y;
                    Markers[i].safeZ = Markers[i].Z;
                    continue;
                }
                Markers[i].safeX = Markers[i].X + Convert.ToSingle(dxoff * dOffset / dloff/* - 0.5 * dxpre * dOffset*/) ;
                Markers[i].safeY = Markers[i].Y + Convert.ToSingle(dyoff * dOffset / dloff/* - 0.5 * dypre * dOffset*/);
                Markers[i].safeZ = Markers[i].Z;
            }
            return true;
        }

        public bool GetTargetLocationXYZ(int i, double dOffset, out float dX, out float dY, out float dZ)
        {
            if (i >= Markers.Count || i < 0)
            {
                dX = 0;
                dY = 0;
                dZ = 0;
                return false;
            }
            double dxpre = 0;
            double dypre = 0;
            double dlpre = 0;
            double dxpost = 0;
            double dypost = 0;
            double dlpost = 0;

            if (i > 0)
            {
                dxpre = Markers[i].X - Markers[i - 1].X;
                dypre = Markers[i].Y - Markers[i - 1].Y;
                dlpre = Math.Sqrt(dxpre * dxpre + dypre * dypre);
                if (dlpre > 0)
                {
                    dxpre /= dlpre;
                    dypre /= dlpre;
                }
            }
            if (i < Markers.Count - 1)
            {
                dxpost = Markers[i + 1].X - Markers[i].X;
                dypost = Markers[i + 1].Y - Markers[i].Y;
                dlpost = Math.Sqrt(dxpost * dxpost + dypost * dypost);
                if (dlpost > 0)
                {
                    dxpost /= dlpost;
                    dypost /= dlpost;
                }
            }
            if (i == 0)
            {
                dX = Markers[i].X - Convert.ToSingle(dxpost * dOffset);
                dY = Markers[i].Y - Convert.ToSingle(dypost * dOffset);
                dZ = Markers[i].Z;
                return true;
            }
            if (i == Markers.Count - 1)
            {
                dX = Markers[i].X + Convert.ToSingle(dxpre * dOffset);
                dY = Markers[i].Y + Convert.ToSingle(dypre * dOffset);
                dZ = Markers[i].Z;
                return true;
            }


            double dxoff = dxpre - dxpost;
            double dyoff = dypre - dypost;
            double dloff = Math.Sqrt(dxoff * dxoff + dyoff * dyoff);
            if (dloff == 0)
            {
                dX = Markers[i].X;
                dY = Markers[i].Y;
                dZ = Markers[i].Z;
                return true;
            }
            dX = Markers[i].X + Convert.ToSingle(dxoff * dOffset / dloff);
            dY = Markers[i].Y + Convert.ToSingle(dyoff * dOffset / dloff);
            dZ = Markers[i].Z;
            return true;
        }
        public float CalcRemainingDistance(float X, float Y)
        {
            if (nextWayPoint >= Markers.Count)
                return -1;

            RemainingDistance = Math.Sqrt((Markers[nextWayPoint].X - X) * (Markers[nextWayPoint].X - X) +
                (Markers[nextWayPoint].Y - Y) * (Markers[nextWayPoint].Y - Y));

            for (int i = nextWayPoint; i < Markers.Count() - 1; i++)
            {
                RemainingDistance += Math.Sqrt(
                    (Markers[i].X - Markers[i + 1].X) * (Markers[i].X - Markers[i + 1].X) +
                    (Markers[i].Y - Markers[i + 1].Y) * (Markers[i].Y - Markers[i + 1].Y));
            }

            if (RemainingDistanceMinimum < 0)
                RemainingDistanceMinimum = RemainingDistance;
            return Convert.ToSingle(RemainingDistance);
        }

        public double GetDistanceToNextWayPoint(FlightPathPoint ptPlane)
        {
            double dxpre = ptPlane.x - Markers[nextWayPoint].X;
            double dypre = ptPlane.y - Markers[nextWayPoint].Y;
            double dlpre = Math.Sqrt(dxpre * dxpre + dypre * dypre);
            return dlpre;
        }

        public bool GetClosestPositionOnPath(FlightPathPoint ptPlane, out FlightPathPoint ptOnPath)
        {
            ptOnPath = new FlightPathPoint();
            double dxpre, dypre, dlpre;
            if (nextWayPoint >= Markers.Count || nextWayPoint < 1)
            {
                return false;
            }

            dxpre = Markers[nextWayPoint].X - Markers[nextWayPoint - 1].X;
            dypre = Markers[nextWayPoint].Y - Markers[nextWayPoint - 1].Y;
            dlpre = Math.Sqrt(dxpre * dxpre + dypre * dypre);
            if (dlpre == 0)
                return false;
            dxpre /= dlpre;
            dypre /= dlpre;
            double dxplane = ptPlane.x - Markers[nextWayPoint - 1].X;
            double dyplane = ptPlane.y - Markers[nextWayPoint - 1].Y;
            double cosDist = dxpre * dxplane + dypre * dyplane;
            if (cosDist < 0)
                cosDist = 0;
            else if (cosDist > dlpre)
                cosDist = dlpre;


            ptOnPath.x = Convert.ToSingle(Markers[nextWayPoint - 1].X + dxpre * cosDist);
            ptOnPath.y = Convert.ToSingle(Markers[nextWayPoint - 1].Y + dypre * cosDist);
            ptOnPath.z = ptPlane.z;
            ptOnPath.vec.x = Convert.ToSingle(dxpre);
            ptOnPath.vec.y = Convert.ToSingle(dypre);
            ptOnPath.windx = ptPlane.windx;
            ptOnPath.windy = ptPlane.windy;


            return true;
        }

        public bool GetClosestPositionOnPath(FlightPathPoint ptPlane, float dist, out FlightPathPoint ptOnPath)
        {
            
            ptOnPath = new FlightPathPoint();
            double dxpre, dypre, dlpre;
            if (nextWayPoint >= Markers.Count || nextWayPoint < 1)
            {
                return false;
            }

            if (Markers[nextWayPoint].dijkstraPath.Count > 1)
            {
                if (!Markers[nextWayPoint].GetTarget(ptPlane.getPosition(), dist, out Point3d ptTarget))
                    return false;

                ptOnPath.Set(ptTarget);
                return true;
            }

            dxpre = Markers[nextWayPoint].X - Markers[nextWayPoint - 1].X;
            dypre = Markers[nextWayPoint].Y - Markers[nextWayPoint - 1].Y;
            dlpre = Math.Sqrt(dxpre * dxpre + dypre * dypre);
            if (dlpre == 0)
                return false;
            dxpre /= dlpre;
            dypre /= dlpre;
            double dxplane = ptPlane.x - Markers[nextWayPoint - 1].X;
            double dyplane = ptPlane.y - Markers[nextWayPoint - 1].Y;
            double cosDist = dxpre * dxplane + dypre * dyplane;
            cosDist += dist;
            if (cosDist < 0)
                cosDist = 0;
            else if (cosDist > dlpre)
                cosDist = dlpre;


            ptOnPath.x = Convert.ToSingle(Markers[nextWayPoint - 1].X + dxpre * cosDist);
            ptOnPath.y = Convert.ToSingle(Markers[nextWayPoint - 1].Y + dypre * cosDist);
            ptOnPath.z = ptPlane.z;
            ptOnPath.vec.x = Convert.ToSingle(dxpre);
            ptOnPath.vec.y = Convert.ToSingle(dypre);
            ptOnPath.windx = ptPlane.windx;
            ptOnPath.windy = ptPlane.windy;


            return true;
        }


        public string GetMarkerText(int i)
        {
            if (i >= Markers.Count || i < 0)
            {
                return "";
            }
            return Markers[i].strMarker;
        }
        public string GetMarkerText()
        {
            if (nextWayPoint >= Markers.Count || nextWayPoint < 0)
            {
                return "";
            }
            return Markers[nextWayPoint].strMarker;
        }

        private bool InitHeightPath()
        {
            //Path100mInterval.Clear();
            float dStep = 100;
            float dDelta = 0;
            for (int i = 0; i < Markers.Count-1; i++)
            {

                float dxpre = 0;
                float dypre = 0;
                float dlpre = 0;

                dxpre = Markers[i + 1].X - Markers[i].X;
                dypre = Markers[i + 1].Y - Markers[i].Y;
                dlpre = Convert.ToSingle(Math.Sqrt(dxpre * dxpre + dypre * dypre));
                if (dlpre == 0)
                    continue;

                dxpre /= dlpre;
                dypre /= dlpre;

                Vector3d vecDir = new Vector3d(dxpre, dypre, 0);

                float dPos = dDelta;
                while (dPos < dlpre)
                {

                    Point3d pt = new Point3d();
                    pt.X = Markers[i].X + dPos * dxpre;
                    pt.Y = Markers[i].Y + dPos * dypre;
                    //FlightPlanPointData flightPlanPointData = new FlightPlanPointData(pt, vecDir);
                    //Path100mInterval.Add(flightPlanPointData);
                    dPos += dStep;
                }
                dDelta = dPos - dlpre;
            }

            return true;
        }

        public bool GetLandscape(string filename)
        {
            myFlightPlanFileName = filename;
            string strContent = File.ReadAllText(myFlightPlanFileName);

            myFlightPlanName = Path.GetFileNameWithoutExtension(myFlightPlanFileName);

            if (strContent.Length == 0)
                return false;


            if (!getToken(ref strContent, "Landscape", out string strLandscape))
                return false;

            StrLandscape = strLandscape;


            if (!getToken(ref strContent, "WindDir", out float dWindDeg))
                return false;

            vecWind = new Vector3d(Math.Sin(Utils.deg2rad(dWindDeg)), -Math.Cos(Utils.deg2rad(dWindDeg)), 0);


            if (!getToken(ref strContent, "WindSpeed", out float fValue))
                return false;

            windSpeed = fValue;


            if (!getToken(ref strContent, "ThermalsInversionheight", out fValue))
                return false;

            Properties.Settings.Default.CloudInversionHeight = fValue;

            if (!getToken(ref strContent, "StartType", out fValue))
                return false;
            StartType = Convert.ToInt32(fValue);


            return true;
        }

        public bool Init(string filename, TextBox textBox = null)
        {
            //Path100mInterval = new List<FlightPlanPointData>();
            myFlightPlanFileName = filename;
            string strContent = File.ReadAllText(myFlightPlanFileName);

            myFlightPlanName = Path.GetFileNameWithoutExtension(myFlightPlanFileName);

            if (strContent.Length == 0)
                return false;


            if (!getToken(ref strContent, "Landscape", out string strLandscape))
                return false;

            StrLandscape = strLandscape;

            //if (!NaviCon.initLandscape(StrLandscape))
            //    return false;

            string strDirHeightMapsSource = Properties.Settings.Default.CondorApplicationPath +
                "\\Landscapes\\" +
                strLandscape + "\\HeightMaps\\";
            string strDirHeightMapsNewSource = Path.GetTempPath() +
                "CondorAutoPilot\\Landscapes\\" +
                strLandscape + "\\HeightMaps\\";
            DirectoryInfo dirNewSource = new DirectoryInfo(strDirHeightMapsNewSource);
            DirectoryInfo dirSource = new DirectoryInfo(strDirHeightMapsSource);
            Directory.CreateDirectory(strDirHeightMapsNewSource);
            FileInfo[] files = dirSource.GetFiles();
            foreach (FileInfo file in files)
            {
                string fileNew = strDirHeightMapsNewSource + file.Name;
                if (!File.Exists(fileNew))
                {
                    if (textBox != null)
                    {
                        textBox.Text = file.Name;
                        textBox.Refresh();
                    }
                    file.CopyTo(fileNew, true);
                }
            }

            
            if (!getToken(ref strContent, "Count", out int iCountMarkers))
                return false;

            Markers.Clear();
            for (int i = 0; i < iCountMarkers; i++)
            {
                string strMarker;
                float X, Y, Z, A, R;
                FlightPlanMarker markeri = new FlightPlanMarker();
                if (!getToken(ref strContent, "TPName" + i.ToString(), out strMarker))
                    return false;
                if (!getToken(ref strContent, "TPPosX" + i.ToString(), out X))
                    return false;
                if (!getToken(ref strContent, "TPPosY" + i.ToString(), out Y))
                    return false;
                if (!getToken(ref strContent, "TPPosZ" + i.ToString(), out Z))
                    return false;
                if (!getToken(ref strContent, "TPAngle" + i.ToString(), out A))
                    return false;
                if (!getToken(ref strContent, "TPRadius" + i.ToString(), out R))
                    return false;

                if (A != 360)
                    R = 0;
                markeri.set(strMarker, X, Y, Z, R);
                Markers.Add(markeri);
            }

            if (!getToken(ref strContent, "StartTime", out float dStartTime))
                return false;
            if (!getToken(ref strContent, "RaceStartDelay", out float dRaceStartDelay))
                return false;
            RaceStartTime = dStartTime + dRaceStartDelay;


            if (!getToken(ref strContent, "Name", out string strPlaneName))
                return false;

            if (0 == strPlaneName.CompareTo("GrunauBaby"))
            {
                HasElectronics = false;
                ReferenceSpeed = 70;
            }
            else
            {
                HasElectronics = true;
                ReferenceSpeed = 125;
            }


            CalcSafeLocations(400);
            setArea();
            InitHeightPath();


            return true;
        }

        //public bool GetPathIndexOn(float distance, out int iPos)
        //{
        //    int iDist = Convert.ToInt32((RemainingDistance - distance) / 100);
        //    iPos = Math.Max(Math.Min(Math.Max(Path100mInterval.Count - iDist, 0), Path100mInterval.Count - 1), 0);
        //    RemainingIntegrationLiftPotential = Path100mInterval[iPos].dIntegration;

        //    return true;
        //}
        //public bool GetPathIndexOnMinimum(float distance, out int iPos)
        //{
        //    RemainingDistanceMinimum = Math.Min(RemainingDistanceMinimum, RemainingDistance);
        //    int iDist = Convert.ToInt32((RemainingDistanceMinimum - distance) / 100);
        //    iPos = Math.Max(Math.Min(Math.Max(Path100mInterval.Count - iDist, 0), Path100mInterval.Count - 1), 0);

        //    return true;
        //}
    }
}
