using GMap.NET.WindowsForms;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms.Markers;


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using CondorAutoPilot.AI;

namespace CondorAutoPilot
{
    class MapCondor
    {
        private GMapControl my_gMapControl;

        private Bitmap myGlider = global::CondorAutoPilot.Properties.Resources.Glider02;
        private Bitmap myMarker = global::CondorAutoPilot.Properties.Resources.Marker;
        private Bitmap myMarkerSmall = global::CondorAutoPilot.Properties.Resources.MarkerSmall;
        private Bitmap myWindDirection = global::CondorAutoPilot.Properties.Resources.WindDirection;
        private Bitmap myPhotoLocation = global::CondorAutoPilot.Properties.Resources.PhotoLocation;
        private Bitmap myTargetThermal = global::CondorAutoPilot.Properties.Resources.TargetThermal;
        private Bitmap myTargetStrongThermal = global::CondorAutoPilot.Properties.Resources.TargetStrongThermal;
        private Bitmap myBmpAirport = global::CondorAutoPilot.Properties.Resources.Airport;

        private int myLastTRNIndex = 0;

        public MapCondor()
        {
            GoogleMapProvider.Instance.ApiKey = Properties.Settings.Default.GoogleApiKey;// "YOUR_GOOGLE_API_KEY";
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
        }

        public void Init(ref GMapControl gMapControl, GMapProvider mapProvider, double dZoom)
        {

            my_gMapControl = gMapControl;
            //my_gMapControl.DragButton = MouseButtons.Left;
            my_gMapControl.MapProvider = mapProvider;
            double dLat = Properties.Settings.Default.InitLat;
            double dLong = Properties.Settings.Default.InitLon;
            my_gMapControl.Position = new GMap.NET.PointLatLng(dLat, dLong);
            my_gMapControl.MinZoom = 1;
            my_gMapControl.MaxZoom = 100;
            my_gMapControl.Zoom = dZoom;
            my_gMapControl.ShowCenter = false;

            GMapOverlay airport = new GMapOverlay("Airport");
            my_gMapControl.Overlays.Add(airport);
            GMapOverlay flightPlanExtendedEdit = new GMapOverlay("flightplanextendededit");
            my_gMapControl.Overlays.Add(flightPlanExtendedEdit);
            GMapOverlay terrainAnalysis = new GMapOverlay("TerrainAnalysis");
            my_gMapControl.Overlays.Add(terrainAnalysis);
            GMapOverlay ranges = new GMapOverlay("Ranges");
            my_gMapControl.Overlays.Add(ranges);
            GMapOverlay photoLocation = new GMapOverlay("PhotoLocation");
            my_gMapControl.Overlays.Add(photoLocation);
            GMapOverlay flightPlans = new GMapOverlay("flightplans");
            my_gMapControl.Overlays.Add(flightPlans);
            GMapOverlay flightPlanExtended = new GMapOverlay("flightplanextended");
            my_gMapControl.Overlays.Add(flightPlanExtended);
            GMapOverlay dijkstraPath = new GMapOverlay("dijkstraPath");
            my_gMapControl.Overlays.Add(dijkstraPath);
            GMapOverlay planePosition = new GMapOverlay("PlanePosition");
            my_gMapControl.Overlays.Add(planePosition);
            GMapOverlay windDirection = new GMapOverlay("WindDirection");
            my_gMapControl.Overlays.Add(windDirection);
            GMapOverlay planePath = new GMapOverlay("PlanePath");
            my_gMapControl.Overlays.Add(planePath);
            GMapOverlay thermalLocation = new GMapOverlay("ThermalLocation");
            my_gMapControl.Overlays.Add(thermalLocation);



            myGlider = Utils.ProcessBMP(myGlider);
            myMarker = Utils.ProcessBMP(myMarker);
            myMarkerSmall = Utils.ProcessBMP(myMarkerSmall);
            myPhotoLocation = Utils.ProcessBMP(myPhotoLocation);
            myWindDirection = Utils.ProcessBMP(myWindDirection);
            myTargetThermal = Utils.ProcessBMP(myTargetThermal);
            myTargetStrongThermal = Utils.ProcessBMP(myTargetStrongThermal);


        }

        public void AddFlightPlan(FlightPlanData flightPlanData, bool bZoomOverall)
        {
            for (int i = 0; i < my_gMapControl.Overlays.Count; i++)
            {
                string str = my_gMapControl.Overlays[i].Id;
                if (str == "flightplans")
                {
                    my_gMapControl.Overlays[i].Routes.Clear();
                    my_gMapControl.Overlays[i].Markers.Clear();

                    GMapOverlay flightPlans = my_gMapControl.Overlays[i];
                    flightPlanData.getMapMarkers(ref flightPlans);
                    flightPlanData.getMapRoute(ref flightPlans);
                    Properties.Settings.Default.InitLat = flightPlanData.markerCenter.lat;
                    Properties.Settings.Default.InitLon = flightPlanData.markerCenter.lon;
                    if (bZoomOverall)
                        my_gMapControl.ZoomAndCenterMarkers("flightplans");
                    my_gMapControl.Overlays[i].Markers.Clear();
                    my_gMapControl.Refresh();
                }
            }



        }



        public void AddDijkstraPath(Dijkstra dijkstra, bool bShow)
        {
            for (int i = 0; i < my_gMapControl.Overlays.Count; i++)
            {
                string str = my_gMapControl.Overlays[i].Id;
                if (str == "dijkstraPath")
                {
                    my_gMapControl.Overlays[i].Routes.Clear();
                    my_gMapControl.Overlays[i].Markers.Clear();

                    GMapOverlay myDijkstraOverlay = my_gMapControl.Overlays[i];
                    if (bShow)
                    {
                        dijkstra.AddMapItems(myDijkstraOverlay, myMarkerSmall);
                        my_gMapControl.Refresh();
                    }
                }
            }
        }
        public void AddPlanePath(List<CondorUDPData> myCondorUDPDataLog, TerrainMap terrainMap)
        {
            for (int i = 0; i < my_gMapControl.Overlays.Count; i++)
            {
                string str = my_gMapControl.Overlays[i].Id;
                if (str == "PlanePath")
                {
                    my_gMapControl.Overlays[i].Routes.Clear();
                    my_gMapControl.Overlays[i].Markers.Clear();

                    for (int j = 0; j < myCondorUDPDataLog.Count; j += (j > myCondorUDPDataLog.Count-100)?1:3)
                    {
                        float dlat, dlon;
                        Form1.naviCon64.calcLatLon(myCondorUDPDataLog[j].x, myCondorUDPDataLog[j].y, out dlat, out dlon);

                        GMarkerGoogle marker = new GMarkerGoogle(new PointLatLng(dlat, dlon), myMarkerSmall);
                        marker.Tag = myCondorUDPDataLog[j];
                        marker.Offset = new Point(-myMarkerSmall.Width / 2, -myMarkerSmall.Height / 2);
                        marker.ToolTipMode = MarkerTooltipMode.OnMouseOver;
                        marker.ToolTipText = myCondorUDPDataLog[j].ToAugmentedString(terrainMap);
                        my_gMapControl.Overlays[i].Markers.Add(marker);
                    }
               }
            }
        }


        public void AddTerrainMap(TerrainMap terrainMap)
        {
            if (false)
            {
                for (int i = 0; i < my_gMapControl.Overlays.Count; i++)
                {
                    string str = my_gMapControl.Overlays[i].Id;
                    if (str == "TerrainAnalysis")
                    {
                        terrainMap.FillMap(ref my_gMapControl, i, ref myLastTRNIndex);
                        my_gMapControl.Refresh();
                    }
                }

            }
        }
        public void AddLocalTerrainMap(TerrainMap terrainMap, FlightPathPoint flightPoint, bool bShow)
        {
            int iMaps = 0;
            for (int i = 0; i < my_gMapControl.Overlays.Count; i++)
            {
                string str = my_gMapControl.Overlays[i].Id;
                if (str == "TerrainAnalysis")
                {
                    my_gMapControl.Overlays[i].Polygons.Clear();
                    if (bShow)
                        terrainMap.FillLocalMap(ref my_gMapControl, i, flightPoint);
                    iMaps++;
                }
                else if (str == "Ranges")
                {
                    my_gMapControl.Overlays[i].Routes.Clear();
                    if (bShow)
                        terrainMap.FillRangesMap(ref my_gMapControl, i, flightPoint);
                    iMaps++;
                }
                if (bShow && iMaps>0)
                    my_gMapControl.Refresh();

            }
        }

        public void AddMLTerrainMap(TerrainMap terrainMap, Point3d ptCenter, WeightSet weightSet)
        {
            int iMaps = 0;
            for (int i = 0; i < my_gMapControl.Overlays.Count; i++)
            {
                string str = my_gMapControl.Overlays[i].Id;
                if (str == "TerrainAnalysis")
                {
                    my_gMapControl.Overlays[i].Polygons.Clear();
                    terrainMap.FillMLLocalMap(ref my_gMapControl, i, ptCenter, weightSet);
                    iMaps++;
                }
                 my_gMapControl.Refresh();

            }
        }


        public void AddAirport(Airports airports)
        {
            for (int i = 0; i < my_gMapControl.Overlays.Count; i++)
            {
                string str = my_gMapControl.Overlays[i].Id;
                if (str == "Airport")
                {
                    my_gMapControl.Overlays[i].Markers.Clear();
                    my_gMapControl.Overlays[i].Routes.Clear();


                    if (my_gMapControl.Overlays[i].Markers.Count == 0)
                    {
                        if (my_gMapControl.Zoom > 12)
                        {
                            Point3d pt = airports.CurrentAirport.GetPosition();
                            Form1.naviCon64.calcLatLon(pt.X, pt.Y, out float dLat, out float dLon);

                            Bitmap bmpAirport = Utils.RotateImg(myBmpAirport, airports.CurrentAirport.Orientation);
                            //GMarkerGoogle marker = new GMarkerGoogle(new PointLatLng(airports.CurrentAirport.Lat, airports.CurrentAirport.Lon), bmpAirport);
                            GMarkerGoogle marker = new GMarkerGoogle(new PointLatLng(dLat, dLon), bmpAirport);
                            marker.Offset = new Point(-bmpAirport.Width / 2, -bmpAirport.Height / 2);
                            my_gMapControl.Overlays[i].Markers.Add(marker);

                            Vector3d vecAirport = new Vector3d(Utils.deg2rad(airports.CurrentAirport.Orientation));
                            Form1.naviCon64.calcLatLon(pt.X + 5000 * vecAirport.x, pt.Y + 5000 * vecAirport.y, out float dLat1, out float dLon1);
                            Form1.naviCon64.calcLatLon(pt.X - 5000 * vecAirport.x, pt.Y - 5000 * vecAirport.y, out float dLat2, out float dLon2);

                            List<PointLatLng> pointsWay = new List<PointLatLng>();
                            pointsWay.Add(new PointLatLng(dLat1, dLon1));
                            pointsWay.Add(new PointLatLng(dLat2, dLon2));
                            GMapRoute route1 = new GMapRoute(pointsWay, "Airport");
                            route1.Stroke = new Pen(Color.GreenYellow, 7);
                            route1.Stroke.DashCap = System.Drawing.Drawing2D.DashCap.Round;
                            GMapRoute route2 = new GMapRoute(pointsWay, "Airport");
                            route2.Stroke = new Pen(Color.Black, 1);
                            my_gMapControl.Overlays[i].Routes.Add(route1);
                            my_gMapControl.Overlays[i].Routes.Add(route2);
                        }

                        my_gMapControl.Refresh();
                    }
            }
            }

        }


        public void IncreaseZoom(bool bZoomOut)
        {
            if (bZoomOut)
                my_gMapControl.Zoom++;
            else
                my_gMapControl.Zoom--;
        }


        public void SetMarkerPosition(float x, float y, bool bIsXY, bool bVisible)
        {
            float dLat, dLong;

            if (bIsXY)
                Form1.naviCon64.calcLatLon(x, y, out dLat, out dLong);
            else
            {
                dLat = x;
                dLong = y;
            }
 
            for (int i = 0; i < my_gMapControl.Overlays.Count; i++)
            {
                if (my_gMapControl.Overlays[i].Id == "flightplanextendededit")
                {

                    if (my_gMapControl.Overlays[i].Markers.Count == 0)
                    {
                        GMarkerGoogle marker = new GMarkerGoogle(new PointLatLng(dLat, dLong), myMarker);
                        marker.IsVisible = bVisible;
                        marker.Offset = new Point(-myMarker.Width / 2, -myMarker.Height / 2);
                        my_gMapControl.Overlays[i].Markers.Add(marker);
                    }
                    else
                    {
                        my_gMapControl.Overlays[i].Markers[0].Position = new PointLatLng(dLat, dLong);
                        my_gMapControl.Overlays[i].Markers[0].Offset = new Point(-myMarker.Width / 2, -myMarker.Height / 2);
                        my_gMapControl.Overlays[i].Markers[0].IsVisible = bVisible;
                    }
                }
            }
        }

        public void SetPlanePosition(float x, float y, float vx, float vy, bool bMoveMapToPlane)
        {
            float dLat, dLong;
            //NaviCon.getLatLonFromXY(x + 30*vx, y + 30*vy, out dLat, out dLong);// 2min forward looking 
            // my_gMapControl.Position = new GMap.NET.PointLatLng(dLat, dLong);

            Form1.naviCon64.calcLatLon(x, y, out dLat, out dLong);
            if (bMoveMapToPlane)
            {
                my_gMapControl.Position = new GMap.NET.PointLatLng(dLat, dLong);
            }

            for (int i = 0; i < my_gMapControl.Overlays.Count; i++)
            {
                if (my_gMapControl.Overlays[i].Id == "PlanePosition")
                {

                    if (my_gMapControl.Overlays[i].Markers.Count == 0)
                    {
                       GMarkerGoogle marker = new GMarkerGoogle(new PointLatLng(dLat, dLong), GMarkerGoogleType.green);
                        my_gMapControl.Overlays[i].Markers.Add(marker);
                    }

                    if (vx == 0 && vy == 0)
                    {
                        my_gMapControl.Overlays[i].Markers[0].Position = new PointLatLng(dLat, dLong);
                    }
                    else
                    {
                        float fAngle = Convert.ToSingle(Utils.rad2deg(Math.Atan2(vy, vx)) + 180);
                        Bitmap bmpGlider = Utils.RotateImg(myGlider, fAngle);
                        my_gMapControl.Overlays[i].Markers[0] = new GMarkerGoogle(new PointLatLng(dLat, dLong), bmpGlider);
                        my_gMapControl.Overlays[i].Markers[0].Offset = new Point(-bmpGlider.Width / 2, -bmpGlider.Height / 2);
                    }

                }
            }

        }

        public void SetThermalTargets(FlightPathPoint targetThermal, FlightPathPoint targetStrongThermal, bool bAddStrong)
        {
            if (targetThermal == null)
                return;
            Form1.naviCon64.calcLatLon(targetThermal.x, targetThermal.y, out float dLat, out float dLong);

            Form1.naviCon64.calcLatLon(targetStrongThermal.x, targetStrongThermal.y, out float dStrongLat, out float dStrongLong);


            for (int i = 0; i < my_gMapControl.Overlays.Count; i++)
            {
                if (my_gMapControl.Overlays[i].Id == "ThermalLocation")
                {

                    if (my_gMapControl.Overlays[i].Markers.Count == 0)
                    {
                        GMarkerGoogle marker = new GMarkerGoogle(new PointLatLng(dLat, dLong), myTargetThermal);
                        marker.Offset = new Point(-myTargetThermal.Width / 2, -myTargetThermal.Height / 2);
                        my_gMapControl.Overlays[i].Markers.Add(marker);
                        GMarkerGoogle markerStrong = new GMarkerGoogle(new PointLatLng(dStrongLat, dStrongLong), myTargetStrongThermal);
                        markerStrong.Offset = new Point(-myTargetStrongThermal.Width / 2, -myTargetStrongThermal.Height / 2);
                        my_gMapControl.Overlays[i].Markers.Add(markerStrong);
                    }
                    else if (my_gMapControl.Overlays[i].Markers.Count == 2)
                    {
                        my_gMapControl.Overlays[i].Markers[0].Position = new PointLatLng(dLat, dLong);
                        my_gMapControl.Overlays[i].Markers[1].Position = new PointLatLng(dStrongLat, dStrongLong);
                        my_gMapControl.Overlays[i].Markers[1].IsVisible = bAddStrong;
                    }

                    

                }
            }

        }

        public void SetTarget(Point3d target)
        {
            if (target == null)
                return;
            Form1.naviCon64.calcLatLon(target.X, target.Y, out float dLat, out float dLong);

            for (int i = 0; i < my_gMapControl.Overlays.Count; i++)
            {
                if (my_gMapControl.Overlays[i].Id == "ThermalLocation")
                {

                    if (my_gMapControl.Overlays[i].Markers.Count == 0)
                    {
                        GMarkerGoogle marker = new GMarkerGoogle(new PointLatLng(dLat, dLong), myTargetThermal);
                        marker.Offset = new Point(-myTargetThermal.Width / 2, -myTargetThermal.Height / 2);
                        my_gMapControl.Overlays[i].Markers.Add(marker);
                    }
                    else if (my_gMapControl.Overlays[i].Markers.Count == 1)
                    {
                        my_gMapControl.Overlays[i].Markers[0].Position = new PointLatLng(dLat, dLong);
                    }



                }
            }

        }


        public void SetWindDirection(float x, float y, float wx, float wy)
        {
            float dLat, dLong;

            Form1.naviCon64.calcLatLon(x, y, out dLat, out dLong);

            for (int i = 0; i < my_gMapControl.Overlays.Count; i++)
            {
                if (my_gMapControl.Overlays[i].Id == "WindDirection")
                {

                    if (my_gMapControl.Overlays[i].Markers.Count == 0)
                    {
                        GMarkerGoogle marker = new GMarkerGoogle(new PointLatLng(dLat, dLong), GMarkerGoogleType.green);
                        my_gMapControl.Overlays[i].Markers.Add(marker);
                    }

                    if (wx == 0 && wy == 0)
                    {
                        my_gMapControl.Overlays[i].Markers[0].Position = new PointLatLng(dLat, dLong);
                    }
                    else
                    {
                        float fAngle = Convert.ToSingle(Utils.rad2deg(Math.Atan2(wy, wx)) + 180);
                        Bitmap bmpWindDirection = Utils.RotateImg(myWindDirection, fAngle);
                        my_gMapControl.Overlays[i].Markers[0] = new GMarkerGoogle(new PointLatLng(dLat, dLong), bmpWindDirection);
                        my_gMapControl.Overlays[i].Markers[0].Offset = new Point(-bmpWindDirection.Width / 2, -bmpWindDirection.Height / 2);
                    }

                }
            }

        }

        public void SetPhotoLocation(float dLat, float dLong)
        {
            for (int i = 0; i < my_gMapControl.Overlays.Count; i++)
            {
                if (my_gMapControl.Overlays[i].Id == "PhotoLocation")
                {

                    if (my_gMapControl.Overlays[i].Markers.Count == 0)
                    {
                        GMarkerGoogle marker = new GMarkerGoogle(new PointLatLng(dLat, dLong), myPhotoLocation);
                        my_gMapControl.Overlays[i].Markers.Add(marker);
                    }

                    my_gMapControl.Overlays[i].Markers[0] = new GMarkerGoogle(new PointLatLng(dLat, dLong), myPhotoLocation);
                    my_gMapControl.Overlays[i].Markers[0].Offset = new Point(0, -myPhotoLocation.Height);// -myPhotoLocation.Width / 2, -myPhotoLocation.Height / 2);

                }
            }

        }

        public void SetLocationLatLong(float dLat, float dLong)
        {
            my_gMapControl.Position = new GMap.NET.PointLatLng(dLat, dLong);
        }


        public void SetTerrainAnalysis(float x, float y, ref TerrainMap terrainMap)
        {

            for (int i = 0; i < my_gMapControl.Overlays.Count; i++)
            {
                if (my_gMapControl.Overlays[i].Id == "TerrainAnalysis")
                {
                    terrainMap.FillMap(ref my_gMapControl, i, ref myLastTRNIndex);
                }
            }

        }

    }
}
