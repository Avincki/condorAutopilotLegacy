using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System.Drawing;
using System.Windows.Forms;

namespace CondorAutoPilot
{
    enum ThreadStatus
    {
        kNeedInit, kWaitingForInput, kInputReady, kProcessing, kDataReady, kStop
    }

    class DijkstraNode
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public double CostOptimum { get; set; }

        public int IPreOptimum { get; set; }// area
        public int JPreOptimum { get; set; }// line in area
        public int KPreOptimum { get; set; }// node in line

        public Point3d Point3d()
        {
            return new Point3d(X, Y, Z);
        }
        public DijkstraNode()
        {
            X = 0;
            Y = 0;
            Z = 0;
            CostOptimum = 0;
            IPreOptimum = -1;
            JPreOptimum = -1;
            KPreOptimum = -1;
        }
        public DijkstraNode(FlightPathPoint pt)
        {
            X = pt.x;
            Y = pt.y;
            Z = pt.z;
            CostOptimum = 0;
            IPreOptimum = -1;
            JPreOptimum = -1;
            KPreOptimum = -1;
        }
        public DijkstraNode(Point3d pt)
        {
            X = pt.X;
            Y = pt.Y;
            Z = pt.Z;
            CostOptimum = 0;
            IPreOptimum = -1;
            JPreOptimum = -1;
            KPreOptimum = -1;
        }

        public void Clone(DijkstraNode other)
        {
            X = other.X;
            Y = other.Y;
            Z = other.Z;
            CostOptimum = other.CostOptimum;
            IPreOptimum = other.IPreOptimum;
            JPreOptimum = other.JPreOptimum;
            KPreOptimum = other.KPreOptimum;
        }
        public void Clone(FlightPathPoint pt)
        {
            X = pt.x;
            Y = pt.y;
            Z = pt.z;
            CostOptimum = 0;
            IPreOptimum = -1;
            JPreOptimum = -1;
            KPreOptimum = -1;
        }

    }

    class DijkstraLine
    {
        public double Radius { get; set; }
        public List<DijkstraNode> Nodes { get; set; }
        public int NodeCount { get; set; }

        public DijkstraLine()
        {
            Radius = 0;
            NodeCount = 0;
            Nodes = new List<DijkstraNode>();
            for (int i = 0; i < 400; i++)
            {
                DijkstraNode node = new DijkstraNode();
                Nodes.Add(node);
            }
        }

    }

     class DijkstraPath
    {
        public List<Point3d> PathNodes { get; set; }
        public int PathCount { get; set; }
        public bool Ready { get; set; }

        public int ThermalSwitchPosition { get; set; }
        public int GlideEndPosition { get; set; }
        public int SmoothEndPosition { get; set; }
        public int StartPlanner { get; set; }

        public double Curvature { get; set; } // 1 is a straight line -1 is a 180 turn
        public bool CloseToGround { get; set; } 

        public DijkstraPath()
        {
            PathNodes = new List<Point3d>();
            Ready = true;
            CloseToGround = false;

        }

        public void CalcQuality(TerrainMap terrainMap, FlightPathPoint ptFlightStart)
        {
            CloseToGround = false;

            for (int i = 0; i < Math.Min(PathNodes.Count(), 9); i++)
            {
                if (terrainMap.GetNodeData(PathNodes[i].X, PathNodes[i].Y, out Node node))
                {
                    if (node.safeHeight() + 200 > PathNodes[i].Z)
                    {
                        CloseToGround = true;
                        break;
                    }
                }
            }

            Curvature = 1;
            if (PathNodes.Count() < 7)
            {
                Curvature = 0;
                return;
            }
            Vector3d vecDir = ptFlightStart.vec;
            vecDir.normalize();
            for (int i = 2; i < 6; i++)
            {
                Vector3d vecDir2 = new Vector3d(PathNodes[i - 1], PathNodes[i]);
                vecDir2.normalize();

                Curvature = Math.Min(Curvature, vecDir.dotProduct(vecDir2));
                Curvature = Math.Max(0, Curvature);
            }
            Curvature *= Curvature;

            SmoothEndPosition = PathNodes.Count();
            for (int i = 2; i < PathNodes.Count()-1; i++)
            {
                Vector3d vecDir1 = new Vector3d(PathNodes[i - 1], PathNodes[i]);
                vecDir1.normalize();
                Vector3d vecDir2 = new Vector3d(PathNodes[i], PathNodes[i+1]);
                vecDir2.normalize();

                if (vecDir1.dotProduct(vecDir2) < 0.1)
                {
                    SmoothEndPosition = i;
                    break;
                }
            }

        }


        public bool GetMapRoute(List<PointLatLng> pointsWay, 
            List<PointLatLng> pointsWayThermal, 
            List<PointLatLng> pointsWayNonGlide,
            List<PointLatLng> pointsWayPlanner)
        {
            pointsWay.Clear();
            pointsWayThermal.Clear();
            pointsWayNonGlide.Clear();
            pointsWayPlanner.Clear();
            if (PathNodes.Count == 0)
                return false;

            for (int j = 0; j < 4; j++)
            {
                int iStart, iEnd;

                if (j == 0)
                {
                    iStart = 0;
                    iEnd = ThermalSwitchPosition;
                }
                else if (j == 1)
                {
                    iStart = Math.Max(0, ThermalSwitchPosition);
                    iEnd = Math.Min(PathNodes.Count, GlideEndPosition);

                }
                else if (j == 2)
                {
                    iStart = Math.Max(0, Math.Min(GlideEndPosition, PathNodes.Count));
                    iEnd = Math.Min(Math.Max(StartPlanner, GlideEndPosition), PathNodes.Count);

                }
                else
                {
                    iStart = Math.Max(0, Math.Min(Math.Min(StartPlanner, GlideEndPosition), PathNodes.Count));
                    iEnd = PathNodes.Count;

                }
                for (int i = iStart; i < iEnd; i++)
                {
                    if (i > SmoothEndPosition)
                        break;

                    float dlat, dlon;
                    Form1.naviCon64.calcLatLon(PathNodes[i].X, PathNodes[i].Y, out dlat, out dlon);

                    if (j == 0)
                        pointsWayThermal.Add(new PointLatLng(dlat, dlon));
                    else if (j == 1)
                        pointsWay.Add(new PointLatLng(dlat, dlon));
                    else if (j == 2)
                        pointsWayNonGlide.Add(new PointLatLng(dlat, dlon));
                    else
                        pointsWayPlanner.Add(new PointLatLng(dlat, dlon));
                }
            }
            return true;
        }


    }

    class DijkstraThreadData
    {
        public FlightPathPoint ptStart { get; set; }
        public FlightPathPoint ptTarget { get; set; }
        public FlightPathPoint ptThermalTarget { get; set; }
        public bool bIsShortRange { get; set; }

        public float fDelta { get; set; }
    }

    class Dijkstra
    {
        private Object myLock = new Object();

        public FlightPathPoint myPtFlightTarget { get; set; }
        public FlightPathPoint myPtFlightFarTarget { get; set; }
        public FlightPathPoint myPtFlightStart { get; set; }
        internal float myFlightTargetInitialDistance { get; set; }
        internal Vector3d myFlightTargetDir { get; set; }


        //public DijkstraTarget myTarget { get; set; }

        public DijkstraPath myDijkstraPath = new DijkstraPath();

        public Point3d myPhotoLocation { get; set; }

        public double myFlightRange { get; set; }
        public double myRidgeCrossingRange { get; set; }
        public double myRidgeCrossingGap { get; set; }
        public double myRidgeCrossingRatio { get; set; }
        public double myLiftForecast { get; set; }

        DijkstraThreadData myThreadData { get; set; }

        public ThreadStatus myThreadStatus { get; set; }
        public bool myInlanding { get; set; }

        public bool myIsEscape { get; set; }

        private int myStayEscape;

        private List<DijkstraLine> myDijkstraLines = new List<DijkstraLine>();
        private int myLinesCount = 0;

        private TerrainMap myTerrainMap;
        private FlightPlanData myFlightPlanData;

        private Vector3d myVecFlight = new Vector3d();
        private Vector3d myVecWind = new Vector3d();

        private bool myBNeedInitTarget = true;

        private ML_Updraft myML_Updraft = new ML_Updraft();

        private List<double> dAngs = new List<double>(new double[]
        {
            0,
            3 * Math.PI / 180 ,
            7 * Math.PI / 180 ,
            12 * Math.PI / 180 ,
            18 * Math.PI / 180 ,
            30 * Math.PI  / 180 ,
            45 * Math.PI  / 180 ,
            60 * Math.PI  / 180 ,
            75 * Math.PI  / 180 ,
            90 * Math.PI  / 180 ,
            105 * Math.PI  / 180 ,
            120 * Math.PI  / 180 ,
            140 * Math.PI  / 180 ,
            160 * Math.PI  / 180 ,
            180 * Math.PI  / 180
        });

        private void Init()
        {
            myPhotoLocation = new Point3d();        
            myDijkstraPath.PathNodes.Clear();
            myDijkstraPath.Ready = true;
            myPtFlightTarget = new FlightPathPoint();
            myPtFlightFarTarget = new FlightPathPoint();
            myPtFlightStart = new FlightPathPoint();
            myThreadData = new DijkstraThreadData();
            myFlightTargetDir = new Vector3d();

            myBNeedInitTarget = true;
            myInlanding = false;
            myStayEscape = 0;
            for (int i = 0; i < 400; i++)
            {
                DijkstraLine line = new DijkstraLine();
                myDijkstraLines.Add(line);
            }

            myML_Updraft.Initialize();
        }

        public Dijkstra()
        {
            Init();
        }
        public Dijkstra(TerrainMap terrainMap, FlightPlanData flightPlanData)
        {
            myTerrainMap = terrainMap;
            myFlightPlanData = flightPlanData;
            Init();
        }

        public void MakePlannerPaths()
        {
            for (int i = 1; i < myFlightPlanData.Markers.Count(); i++)
            {
                string strPrefix = "Making Lap " + i.ToString() + "/" + (myFlightPlanData.Markers.Count()-1).ToString();
 //               textBoxInfo.Refresh();
                CalcPlanner(myFlightPlanData.Markers[i - 1].getPoint(), myFlightPlanData.Markers[i].getPoint(), null, strPrefix);
                myFlightPlanData.Markers[i].SetDijkstraPath(myDijkstraPath.PathNodes);
            }
        }


        public bool AddMapItems(GMapOverlay mapOverlay, Bitmap bitMapMarkerSmall)
        {
            List<PointLatLng> pointsWay = new List<PointLatLng>();
            List<PointLatLng> pointsWayThermal = new List<PointLatLng>();
            List<PointLatLng> pointsWayNonGlide = new List<PointLatLng>();
            List<PointLatLng> pointsPlanner = new List<PointLatLng>();
            lock (myLock)
            {
                //if (myThreadStatus == ThreadStatus.kWaitingForInput)
                {
                    bool bShowMarkers = false;
                    if (bShowMarkers)
                    {
                        for (int i = 0; i < myDijkstraLines.Count; i++)
                        {
                            for (int j = 0; j < myDijkstraLines[i].Nodes.Count; j++)
                            {
                                float dlat, dlon;
                                Form1.naviCon64.calcLatLon(myDijkstraLines[i].Nodes[j].X, myDijkstraLines[i].Nodes[j].Y, out dlat, out dlon);

                                GMarkerGoogle marker = new GMarkerGoogle(new PointLatLng(dlat, dlon), bitMapMarkerSmall);
                                marker.Offset = new Point(-bitMapMarkerSmall.Width / 2, -bitMapMarkerSmall.Height / 2);
                                mapOverlay.Markers.Add(marker);
                            }
                        }

                    }
                    myDijkstraPath.GetMapRoute(pointsWay, pointsWayThermal, pointsWayNonGlide, pointsPlanner);

                }
            }

            if (pointsWayThermal.Count() > 0)
            {
                GMapRoute route1 = new GMapRoute(pointsWayThermal, "Dijkstra");
                route1.Stroke = new Pen(Color.Magenta, 7);
                route1.Stroke.DashCap = System.Drawing.Drawing2D.DashCap.Round;
                mapOverlay.Routes.Add(route1);
                GMapRoute route2 = new GMapRoute(pointsWayThermal, "Dijkstra");
                route2.Stroke = new Pen(Color.Black, 1);
                mapOverlay.Routes.Add(route2);
            }

            if (pointsWay.Count() > 0)
            {
                GMapRoute route3 = new GMapRoute(pointsWay, "Dijkstra");
                if (myIsEscape)
                    route3.Stroke = new Pen(Color.OrangeRed, 7);
                else
                    route3.Stroke = new Pen(Color.Yellow, 7);

                route3.Stroke.DashCap = System.Drawing.Drawing2D.DashCap.Round;
                mapOverlay.Routes.Add(route3);
                GMapRoute route4 = new GMapRoute(pointsWay, "Dijkstra");
                route4.Stroke = new Pen(Color.Black, 1);
                mapOverlay.Routes.Add(route4);
            }

            if (pointsWayNonGlide.Count() > 0)
            {
                GMapRoute route5 = new GMapRoute(pointsWayNonGlide, "Dijkstra");
                route5.Stroke = new Pen(Color.Red, 7);
                route5.Stroke.DashCap = System.Drawing.Drawing2D.DashCap.Round;
                mapOverlay.Routes.Add(route5);
                GMapRoute route6 = new GMapRoute(pointsWayNonGlide, "Dijkstra");
                route6.Stroke = new Pen(Color.Black, 1);
                mapOverlay.Routes.Add(route6);
            }
            
            if (pointsPlanner.Count() == 0)
            {
                if (myFlightPlanData.CurrentMarker() != null )
                {
                    myFlightPlanData.CurrentMarker().GetMapRoute(pointsPlanner);
                }
            }

            if (pointsPlanner.Count() > 0)
            { 
                GMapRoute route7 = new GMapRoute(pointsPlanner, "Dijkstra");
                route7.Stroke = new Pen(Color.LightGreen, 7);
                route7.Stroke.DashCap = System.Drawing.Drawing2D.DashCap.Round;
                mapOverlay.Routes.Add(route7);
                GMapRoute route8 = new GMapRoute(pointsPlanner, "Dijkstra");
                route8.Stroke = new Pen(Color.Black, 1);
                mapOverlay.Routes.Add(route8);
            }




            return true;
        }



        public void PrepareTerrainMap(FlightPlanData flightPlanData, TerrainMap terrainMap, bool bDoCalc = true)
        {
            myTerrainMap = terrainMap;
            myFlightPlanData = flightPlanData;


            if (bDoCalc)
            {
                for (int i = 0; i < myFlightPlanData.Markers.Count - 1; i++)
                {
                    FlightPathPoint ptStart = new FlightPathPoint(myFlightPlanData.Markers[i].getPoint());
                    FlightPathPoint ptEnd = new FlightPathPoint(myFlightPlanData.Markers[i + 1].getPoint());
                    Calc(ptStart, ptEnd, 2000);
                }
            }
        }
        public void ThreadSetData(FlightPathPoint ptStart, FlightPathPoint ptTarget, FlightPathPoint ptThermalTarget, float fDelta, bool bIsThermal)
        {
            lock (myLock)
            {
                myThreadData.ptStart = ptStart;
                myThreadData.ptTarget = ptTarget;
                myThreadData.ptThermalTarget = ptThermalTarget;
                myThreadData.fDelta = fDelta;
                myThreadData.bIsShortRange = bIsThermal;

                myThreadStatus = ThreadStatus.kInputReady;

            }
        }
        public void ThreadSetData(FlightPathPoint ptStart, FlightPathPoint ptTarget, float fDelta, bool bIsThermal)
        {
            lock (myLock)
            {
                myThreadData.ptStart = ptStart;
                myThreadData.ptTarget = ptTarget;
                myThreadData.fDelta = fDelta;
                myThreadData.bIsShortRange = bIsThermal;

                myThreadStatus = ThreadStatus.kInputReady;

            }
        }

        public void ThreadSetRange(double dRange)
        {
            myFlightRange = dRange;
        }

        public bool ThreadGetData(DijkstraPath dijkstraPath, FlightPathPoint flightTarget, ref double dijkstraFlightRange)
        {

            lock (myLock)
            {
                if (myThreadStatus == ThreadStatus.kDataReady)
                {
                    dijkstraPath = myDijkstraPath;
                    flightTarget.Set(myPtFlightTarget.getPosition());
                    dijkstraFlightRange = myFlightRange;

                    myThreadStatus = ThreadStatus.kWaitingForInput;
                }
                else
                    return false;
            }
            return true;
        }
        public void ThreadStop()
        {
            lock (myLock)
            {
                myThreadStatus = ThreadStatus.kStop;
            }
        }

        public void ThreadRunCore()
        {
            myThreadStatus = ThreadStatus.kProcessing;
            myDijkstraPath.PathNodes.Clear();
            myDijkstraPath.ThermalSwitchPosition = 0;
            if (myThreadData.bIsShortRange)
            {
                CalcThermalTarget(myThreadData.ptStart, myThreadData.ptThermalTarget, myThreadData.fDelta);
                if (myDijkstraPath.PathNodes.Count() > 1)
                {
                    myDijkstraPath.ThermalSwitchPosition = myDijkstraPath.PathNodes.Count();
                    FlightPathPoint ptNewStart = new FlightPathPoint(myThreadData.ptStart);
                    ptNewStart.x = myDijkstraPath.PathNodes.Last().X;
                    ptNewStart.y = myDijkstraPath.PathNodes.Last().Y;
                    Vector3d vecDir = new Vector3d(
                        myDijkstraPath.PathNodes[myDijkstraPath.ThermalSwitchPosition - 2],
                        myDijkstraPath.PathNodes.Last());
                    vecDir.normalize();
                    ptNewStart.speed = myThreadData.ptStart.speed;
                    ptNewStart.vec = new Vector3d(vecDir);
                    Calc(ptNewStart, myThreadData.ptTarget, 2 * myThreadData.fDelta);
                    SetThermalTarget(myThreadData.ptTarget);
                }
                else
                {
                    myDijkstraPath.PathNodes.Clear();
                    Calc(myThreadData.ptStart, myThreadData.ptTarget, myThreadData.fDelta);
                }
            }
            else
            {
                Calc(myThreadData.ptStart, myThreadData.ptTarget, myThreadData.fDelta);
                if (myDijkstraPath.PathNodes.Count() < 4)
                {
                    Vector3d vec = new Vector3d(myThreadData.ptStart.getPosition(), myThreadData.ptTarget.getPosition());
                    vec.normalize();
                    Point3d ptTarget = myThreadData.ptStart.getPosition().add(2000, vec);
                    FlightPathPoint ptNewTarget = new FlightPathPoint(ptTarget);
                    CalcThermalTarget(myThreadData.ptStart, ptNewTarget, myThreadData.fDelta);
                }
            }

            bool checkEscape = true;
            if (myDijkstraPath.PathNodes.Count() > 1)
            {
                if (myThreadData.ptTarget.distanceTo(myDijkstraPath.PathNodes.Last()) < 2000)
                {
                    checkEscape = false;
                }
            }


            if (checkEscape && (myDijkstraPath.PathNodes.Count() < 6 ))
            {
                myDijkstraPath.PathNodes.Clear();
                CalcEscape(myThreadData.ptStart, myThreadData.ptTarget, 0.75f * myThreadData.fDelta);
                myStayEscape = 3;
                myIsEscape = !myInlanding;
            }
            else
            {
                
                if (myStayEscape > 0)
                {
                    myStayEscape--;
                }
                myIsEscape = myStayEscape > 0;
            }


            SetTarget(!myIsEscape);
            myThreadStatus = ThreadStatus.kWaitingForInput;

        }

        public void ThreadRun()
        {
            
            myThreadStatus = ThreadStatus.kNeedInit;

            while (true)
            {
                switch (myThreadStatus)
                {
                    case ThreadStatus.kStop:
                        {
                            return;
                        }
                    case ThreadStatus.kInputReady:
                        {
                            ThreadRunCore();
                            myThreadStatus = ThreadStatus.kDataReady;

                            break;
                        }
                }
            }


        }

 
        public void CalcThermalTarget(FlightPathPoint ptStart, FlightPathPoint ptTarget, float fDelta)
        {
            if (fDelta < 10)
            {
                myDijkstraPath.Ready = true;
                return;
            }

            myPtFlightStart = ptStart;
            myVecFlight = ptStart.vec;
            myVecFlight.normalize();


            float dSinkRate = Math.Max(-ptStart.vario, 0)/ ptStart.speed + 0.03f;

            myDijkstraPath.Ready = false;

            //myDijkstraLines.Clear();
            myLinesCount = 0;
            ptTarget.z = ptStart.z;
            double dDistance = ptStart.distanceTo(ptTarget.getPosition()) + 2000;
            if (dDistance > 5000)
                dDistance = 5000;
            myVecWind = ptStart.getWind();


            FlightPathPoint ptTemp = new FlightPathPoint(ptStart);
            ptTemp.Add(ptStart, myVecFlight, 0.5*fDelta, 0);
            //DijkstraNode nodeStart = new DijkstraNode(ptTemp);
            //DijkstraLine line1 = new DijkstraLine(0);
            //line1.Nodes.Add(nodeStart);
            //myDijkstraLines.Add(line1);
            myDijkstraLines[myLinesCount].Nodes[0].Clone(ptTemp);
            myDijkstraLines[myLinesCount].NodeCount = 1;
            myLinesCount++;

            float dH5000 = ptStart.z;

            float dRad = fDelta;
            float dRadDelta = fDelta;

            while (dRad < dDistance)
            {
                double dStep = 2 * Math.PI / 180;
                double dAngle = dStep;

//                DijkstraLine line2 = new DijkstraLine(dRad);
                myDijkstraLines[myLinesCount].Radius = dRad;
                myDijkstraLines[myLinesCount].NodeCount = 0;
                ptTemp.Add(ptStart, myVecFlight, dRad, 0);
                ptTemp.z = ptStart.z - dRad * dSinkRate - 50;
                //DijkstraNode node0 = new DijkstraNode(ptTemp);
                //line2.Nodes.Add(node0);
                myDijkstraLines[myLinesCount].Nodes[0].Clone(ptTemp);
                myDijkstraLines[myLinesCount].NodeCount++;

                while (dAngle < Math.PI)
                {
                    double dCos = dRad * Math.Cos(dAngle);
                    double dSin = dRad * Math.Sin(dAngle);
                    ptTemp.Add(ptStart, myVecFlight, dCos, dSin);
                    ptTemp.z = ptStart.z - dRad * dSinkRate;
                    //DijkstraNode node1 = new DijkstraNode(ptTemp);
                    //line2.Nodes.Add(node1);
                    myDijkstraLines[myLinesCount].Nodes[myDijkstraLines[myLinesCount].NodeCount].Clone(ptTemp);
                    myDijkstraLines[myLinesCount].NodeCount++;

                    ptTemp.Add(ptStart, myVecFlight, dCos, -dSin);
                    ptTemp.z = ptStart.z - dRad * dSinkRate;
                    //DijkstraNode node2 = new DijkstraNode(ptTemp);
                    //line2.Nodes.Add(node2);
                    myDijkstraLines[myLinesCount].Nodes[myDijkstraLines[myLinesCount].NodeCount].Clone(ptTemp);
                    myDijkstraLines[myLinesCount].NodeCount++;

                    dAngle = 1.1 * dAngle + dStep;
                }

                //myDijkstraLines.Add(line2);
                myLinesCount++;


                dRad += dRadDelta;
                dRadDelta *= 1.25f;
            }

            SolveThermalTarget(ptTarget);

            SetThermalTarget(ptTarget);
            //CalcRange(ptStart.z);
            myDijkstraPath.Ready = true;

        }

        public void SetThermalTarget(FlightPathPoint ptTarget)
        {
            if (myDijkstraPath.PathNodes.Count() > 4)
            {
                if (myBNeedInitTarget)
                {
                    myPtFlightTarget = new FlightPathPoint(myDijkstraPath.PathNodes[2]);
                    myBNeedInitTarget = false;
                }
                else
                {
                    //myPtFlightTarget.x = .7f * myPtFlightTarget.x + .3f * myDijkstraPath.PathNodes[4].X;
                    //myPtFlightTarget.y = .7f * myPtFlightTarget.y + .3f * myDijkstraPath.PathNodes[4].Y;
                    myPtFlightTarget.x = myDijkstraPath.PathNodes[3].X;
                    myPtFlightTarget.y = myDijkstraPath.PathNodes[3].Y;
                    myFlightTargetDir.set(new Vector3d(myPtFlightStart.getPosition(), myDijkstraPath.PathNodes[3]));
                    myFlightTargetDir.z = 0;
                    myFlightTargetInitialDistance = myFlightTargetDir.length();
                    myFlightTargetDir.normalize();
                    myPtFlightFarTarget.Set(myDijkstraPath.PathNodes[Math.Min(myDijkstraPath.PathNodes.Count() - 1, 20)]);

                    //myTarget.myFlightTargetDir.set(new Vector3d (myPtFlightStart.getPosition(), myDijkstraPath.PathNodes[3]));
                    //myTarget.myFlightTargetDir.z = 0;
                    //myTarget.myFlightTargetInitialDistance = myTarget.myFlightTargetDir.length();
                    //myTarget.myFlightTargetDir.normalize();


                }
            }
            else
            {
                myPtFlightTarget.x = ptTarget.x;
                myPtFlightTarget.y = ptTarget.y;
            }

//            myTerrainMap.makeTargetSafe(myPtFlightStart, myPtFlightTarget);


        }


        public bool SolveThermalTarget(FlightPathPoint ptTarget)
        {
            for (int i = 1; i < myLinesCount; i++)
            {
                double dDeltaRadius = myDijkstraLines[i].Radius - myDijkstraLines[i - 1].Radius;

                for (int j = 0; j < myDijkstraLines[i].NodeCount; j++)
                {
                    if (myThreadStatus == ThreadStatus.kStop)
                        return false;

                    double dCostMin = 999999;
                    int jPreOptimum = -1;
                    for (int jpre = 0; jpre < myDijkstraLines[i - 1].NodeCount; jpre++)
                    {
                       // if (myDijkstraLines[i].Nodes[j].Point3d().distanceXYTo(myDijkstraLines[i - 1].Nodes[jpre].Point3d()) > dDeltaRadius * 3)
                       //     continue; // save calc time


                        int jpre2 = -1;
                        if (i - 2 >= 0)
                        {
                            jpre2 = myDijkstraLines[i - 1].Nodes[jpre].KPreOptimum;
                        }
                        if (CostThermalTarget(i, j, i - 1, jpre, i - 2, jpre2, ptTarget, out double dCost))
                        {
                            if (dCostMin > dCost + myDijkstraLines[i - 1].Nodes[jpre].CostOptimum)
                            {
                                dCostMin = dCost + myDijkstraLines[i - 1].Nodes[jpre].CostOptimum;
                                jPreOptimum = jpre;
                            }
                        }
                    }

                    if (jPreOptimum >= 0)
                    {
                        myDijkstraLines[i].Nodes[j].CostOptimum = dCostMin;
                        myDijkstraLines[i].Nodes[j].KPreOptimum = jPreOptimum;
                    }
                    else
                    {
                        myDijkstraLines[i].Nodes[j].CostOptimum = dCostMin;
                        myDijkstraLines[i].Nodes[j].KPreOptimum = -1;
                    }
                }
            }
            MakePath(false);

            return true;
        }


        public bool CostThermalTarget(DijkstraNode node, DijkstraNode nodePre, DijkstraNode nodePre2, FlightPathPoint ptTarget, out double dCost)
        {
            Vector3d vecDir = new Vector3d(nodePre.Point3d(), node.Point3d());
            Vector3d vecWind = myPtFlightStart.getWind();
            vecWind.normalize();

            Vector3d vecToNode = myPtFlightStart.vectorTo(node.Point3d());
            double distanceToNode = myPtFlightStart.distanceTo(node.Point3d());
            int iWind = 0;
            if (vecWind.dotProduct(vecToNode)* distanceToNode > 200)
                iWind = 1;
            vecToNode.normalize();
            Vector3d vecStartToTarget = myPtFlightStart.vectorTo(ptTarget);
            vecStartToTarget.normalize();


            myTerrainMap.GetNodeData(myPtFlightStart.x, myPtFlightStart.y, out Node nodeStart);
            double dEscapeHeight = 0;
            if (myPtFlightStart.z < nodeStart.safeHeight() + 150)
            {
                dEscapeHeight = nodeStart.safeHeight() + 125 - myPtFlightStart.z;
            }

            double dCostNotTowardsTarget = 1;
            if (vecToNode.dotProduct(vecStartToTarget) < 0)
                dCostNotTowardsTarget = 3;

            float dLen = vecDir.length();
            vecDir.normalize();


            double dDistStart = myPtFlightStart.distanceTo(node.Point3d());


            FlightPathPoint ptNode = new FlightPathPoint(node.Point3d());
            double dAccesTarget = 0;
            if (!myTerrainMap.getScanAccessibility(ptNode, ptTarget, 50))
                dAccesTarget = 4; 
            double dDistToTarget = ptTarget.distanceTo(node.Point3d());
            double dFactTarget = Utils.interpolate(dDistToTarget, 0, dAccesTarget, 5000, 5);



            double dSpeedDist = 0.15 * myPtFlightStart.speed * myPtFlightStart.speed;
            dCost = 0.0;

            double dWind = Utils.interpolate(dDistStart, 0,1, 2000, 0)*myPtFlightStart.getWind().dotProduct(vecDir);


            double dDev;

            if (nodePre2 != null)
            {
                Vector3d vecDir2 = new Vector3d(nodePre2.Point3d(), nodePre.Point3d());
                vecDir2.normalize();
                dDev = 1 - vecDir.dotProduct(vecDir2);
                double dCutOff = Utils.interpolate(dDistStart, 2000, 0.15, 7000, 0.5);
                if (dDev > dCutOff)
                    return false;
            }
            else
            {
                dDev = 1 - vecDir.dotProduct(myVecFlight);
                if (dDev > 0.08)
                    return false;
            }

            float fSpacing = 90;


            for (float dP = 0; dP < dLen; dP += fSpacing)
            {
                float fHeight = nodePre.Z + vecDir.z * dP;
                myTerrainMap.GetSpecialNode(nodePre.X + vecDir.x * dP, nodePre.Y + vecDir.y * dP, out Node nodeTerrain);
                float fFunction = nodeTerrain.functionF;
                if (myInlanding && fFunction > 0)
                    fFunction = 0;
                double dHeightCheck = nodeTerrain.safeHeight();

                if (dDistStart < 4000)
                {
                    float dSide = 50;
                    float dFront = dP + 150;

                    if (
                        myTerrainMap.GetNodeData(nodePre.X + vecDir.x * dFront - vecDir.y * dSide, nodePre.Y + vecDir.y * dFront + vecDir.x * dSide, out Node nodeLeft)
                        && myTerrainMap.GetNodeData(nodePre.X + vecDir.x * dFront + vecDir.y * dSide, nodePre.Y + vecDir.y * dFront - vecDir.x * dSide, out Node nodeRight)
                        )
                    {
                        if (nodeLeft.safeHeight() > dHeightCheck)
                            dHeightCheck = nodeLeft.safeHeight();
                        if (nodeRight.safeHeight() > dHeightCheck)
                            dHeightCheck = nodeRight.safeHeight();
                    }
                }
                dHeightCheck += 100 - dEscapeHeight;

                if (fHeight < dHeightCheck)
                {
                    return false;
                }
                else if (fHeight < dHeightCheck + 300)
                {
                    dCost += (dDev * dSpeedDist + dWind + fSpacing * (dFactTarget + dCostNotTowardsTarget)) + 1.1*(nodeTerrain.safeHeight() + 200 - fHeight);
                    dCost += iWind * fSpacing;

                    if (nodeTerrain.function > 0)
                        dCost -= Utils.interpolate(fHeight - nodeTerrain.height, 0, 8, 500, 4) * fSpacing * fFunction;
                    else
                        dCost -=  4 * fSpacing * fFunction;
                }

                else
                {
                    double dFactHeight = Utils.interpolate(myTerrainMap.dClearanceReferenceHeight - nodeTerrain.height, 0, 1.5, 1500, 0.2);
                    dCost += iWind * fSpacing;
                    if (nodeTerrain.function > 0)
                        dCost += dDev * (dSpeedDist + 0.2  *dWind) + dWind + fSpacing * dFactTarget * dCostNotTowardsTarget - fSpacing * dFactHeight * fFunction;
                    else
                        dCost += dDev * (dSpeedDist + 0.2 * dWind) + dWind + fSpacing * (1+dFactTarget * dCostNotTowardsTarget) - fSpacing * fFunction;
                }

                double dTowardsSlope = vecDir.dotProduct(nodeTerrain.getNormal());
                if (dTowardsSlope < -0.3)
                {
                    dCost -= fSpacing * dTowardsSlope * Utils.interpolate(fHeight - nodeTerrain.safeHeight(), 50, 5, 400, 0);
                }


            }

            return true;

        }

        public bool CostThermalTarget(int i, int j, int ipre, int jpre, int ipre2, int jpre2, FlightPathPoint ptTarget, out double dCost)
        {
            if (jpre2 >= 0 && ipre2 >= 0)
            {
                return CostThermalTarget(myDijkstraLines[i].Nodes[j], myDijkstraLines[ipre].Nodes[jpre], myDijkstraLines[ipre2].Nodes[jpre2], ptTarget, out dCost);
            }
            else
            {
                return CostThermalTarget(myDijkstraLines[i].Nodes[j], myDijkstraLines[ipre].Nodes[jpre], null, ptTarget, out dCost);
            }
        }

        public void Calc(FlightPathPoint ptStart, FlightPathPoint ptTarget, float fDelta)
        {
            if (fDelta < 10)
            {
                myDijkstraPath.Ready = true;
                return;
            }

            double dDistance = ptStart.distanceTo(ptTarget.getPosition());

            if (dDistance < 4000)
            {
                CalcThermalTarget(ptStart, ptTarget, 0.7f * fDelta);
                return;
            }

            myPtFlightStart = ptStart;
            float dSinkRate = 0;
            if (ptStart.speed > 0)
                dSinkRate = Math.Max(-Math.Max(ptStart.vario, -7), 0.5f) / ptStart.speed;

            myDijkstraPath.Ready = false;

            //myDijkstraLines.Clear();
            myLinesCount = 0;
            ptTarget.z = ptStart.z;

            if (dDistance < 10)
            {
                myDijkstraPath.Ready = true;
                return;
            }

            Vector3d vecDir = ptStart.vectorTo(ptTarget.getPosition());
            myVecFlight = ptStart.vec;
            myVecFlight.normalize();
            double dTurnAngle = myVecFlight.angleXYTo(vecDir);
            myVecWind = ptStart.getWind();


            FlightPathPoint ptTemp = new FlightPathPoint(ptStart);
            // ptTemp.Add(ptStart, 5 * ptStart.vec.x, 5 * ptStart.vec.y);

            //DijkstraNode nodeStart = new DijkstraNode(ptTemp);
            //nodeStart.KPreOptimum = 0;
            //DijkstraLine line1 = new DijkstraLine(0);
            //line1.Nodes.Add(nodeStart);
            //myDijkstraLines.Add(line1);
            myDijkstraLines[myLinesCount].Nodes[0].Clone(ptTemp);
            myDijkstraLines[myLinesCount].Nodes[0].KPreOptimum = 0;

            myDijkstraLines[myLinesCount].NodeCount = 1;
            myLinesCount++;

            float dH5000 = ptStart.z;

            float dRad = fDelta;
            float dRadDelta = fDelta;
            while (dRad < dDistance - 2000)
            {

                //DijkstraLine line2 = new DijkstraLine(dRad);
                myDijkstraLines[myLinesCount].Radius = dRad;
                myDijkstraLines[myLinesCount].NodeCount = 0;
                float dMidRange = 6000;

                if (dRad < dMidRange) // do full circle
                {
                    double dTurnDelta = dRad / dMidRange;
                    dTurnDelta = dTurnAngle * (1 - (1 - dTurnDelta) * (1 - dTurnDelta));

                    double dStep = 2 * Math.PI / 180;
                    double dAngle = dStep;

                    while (dAngle < Math.PI)
                    {
                        double dCos = dRad * Math.Cos(dAngle + dTurnDelta);
                        double dSin = dRad * Math.Sin(dAngle + dTurnDelta);
                        ptTemp.Add(ptStart, myVecFlight, dCos, dSin);
                        ptTemp.z = ptStart.z - dRad * dSinkRate - 50;
                        //DijkstraNode node = new DijkstraNode(ptTemp);
                        //line2.Nodes.Add(node);
                        myDijkstraLines[myLinesCount].Nodes[myDijkstraLines[myLinesCount].NodeCount].Clone(ptTemp);
                        myDijkstraLines[myLinesCount].NodeCount++;

                        dCos = dRad * Math.Cos(-dAngle + dTurnDelta);
                        dSin = dRad * Math.Sin(-dAngle + dTurnDelta);
                        ptTemp.Add(ptStart, myVecFlight, dCos, dSin);
                        if (dRad < 5000)
                        {
                            ptTemp.z = ptStart.z - dRad * dSinkRate;
                            dH5000 = ptTemp.z;
                        }
                        else
                        {
                            ptTemp.z = dH5000;
                        }
                        //node = new DijkstraNode(ptTemp);
                        //line2.Nodes.Add(node);
                        myDijkstraLines[myLinesCount].Nodes[myDijkstraLines[myLinesCount].NodeCount].Clone(ptTemp);
                        myDijkstraLines[myLinesCount].NodeCount++;

                        dAngle = 1.1 * dAngle + dStep;

                    }
                }
                else
                {
                    int nSteps = 25;
                    if (dRad > 40000)
                        nSteps = 12;

                    double dQ1 = dRad / dDistance;
                    double dQ2 = 1 - 2 * dQ1;
                    double dW = (10000 - dQ1*8000 + 12000 * (1 - dQ2 * dQ2)) / (nSteps * (2 - dQ1));
                    double dAngStep = dW / dRad;
                    for (double dAng = -nSteps * dAngStep; dAng <= nSteps * dAngStep; dAng += dAngStep)
                    {
                        double dCos = dRad * Math.Cos(dAng);
                        double dSin = dRad * Math.Sin(dAng);

                        ptTemp.Add(ptStart, vecDir, dCos, dSin);
                        if (dRad < 10000)
                        {
                            //dH10000 = Math.Min(5000, dH5000 - (dRad - dMidRange) / 50);
                            //ptTemp.z = dH10000;
                            ptTemp.z = dH5000;
                        }
                        else
                            ptTemp.z = Math.Min(5000, dH5000 + (dRad - 10000) / 20);

                        //DijkstraNode node = new DijkstraNode(ptTemp);
                        //line2.Nodes.Add(node);
                        myDijkstraLines[myLinesCount].Nodes[myDijkstraLines[myLinesCount].NodeCount].Clone(ptTemp);
                        myDijkstraLines[myLinesCount].NodeCount++;
                    }

                }

//                myDijkstraLines.Add(line2);
                myLinesCount++;

                dRad += dRadDelta;
                dRadDelta *= 1.15f;
            }

//            DijkstraLine line3 = new DijkstraLine(dDistance);
            ptTarget.z = 5000;
            //DijkstraNode nodeLast = new DijkstraNode(ptTarget);
            //line3.Nodes.Add(nodeLast);
            //myDijkstraLines.Add(line3);

            if (myFlightPlanData.GetNextSafeWayPoint().getPoint().distanceXYTo(ptTarget.getPosition()) < 1000)
            {
                myDijkstraLines[myLinesCount].NodeCount = 0;
                myDijkstraLines[myLinesCount].Nodes[0].Clone(ptTarget);
                myDijkstraLines[myLinesCount].NodeCount++;
            }
            else
            {

                myDijkstraLines[myLinesCount].NodeCount = 0;
                for (int i = -4; i < 5; i++)
                {
                    ptTemp.Add(ptTarget, vecDir, 0, i * 400);
                    myDijkstraLines[myLinesCount].Nodes[myDijkstraLines[myLinesCount].NodeCount].Clone(ptTemp);
                    myDijkstraLines[myLinesCount].NodeCount++;
                }
            }
            myLinesCount++;

            Solve();

           // SetTarget();
            CalcRange(ptStart.z);
            myDijkstraPath.Ready = true;

        }

        public void SetTarget(bool bSmooth)
        {
            if (myDijkstraPath.PathNodes.Count() > 7)
            {
                if (myBNeedInitTarget)
                {
                    myPtFlightTarget = new FlightPathPoint(myDijkstraPath.PathNodes[4]);
                    myBNeedInitTarget = false;
                }
                else
                {
                    if (bSmooth)
                    {
                        myPtFlightTarget.x = .6f * myPtFlightTarget.x + 0.4f * myDijkstraPath.PathNodes[4].X;
                        myPtFlightTarget.y = .6f * myPtFlightTarget.y + 0.4f * myDijkstraPath.PathNodes[4].Y;
                        //    myPtFlightTarget.z = myPtFlightStart.z;
                        //    myTarget.Set(myPtFlightTarget, myDijkstraPath.PathNodes[4], myDijkstraPath.PathNodes[5]);
                        myFlightTargetDir = new Vector3d(myDijkstraPath.PathNodes[4], myDijkstraPath.PathNodes[5]);
                        myFlightTargetDir.z = 0;
                        myFlightTargetDir.normalize();
                        myFlightTargetInitialDistance = myPtFlightStart.distanceTo(myDijkstraPath.PathNodes[5]);
                        myPtFlightTarget.z = myPtFlightStart.z;

                    }
                    else
                    {
                        myPtFlightTarget.x = myDijkstraPath.PathNodes[5].X;
                        myPtFlightTarget.y = myDijkstraPath.PathNodes[5].Y;
                        //myPtFlightTarget.z = myPtFlightStart.z;
                        //myTarget.Set(myPtFlightTarget, myDijkstraPath.PathNodes[5], myDijkstraPath.PathNodes[6]);
                        myFlightTargetDir = new Vector3d(myDijkstraPath.PathNodes[4], myDijkstraPath.PathNodes[5]);
                        myFlightTargetDir.z = 0;
                        myFlightTargetDir.normalize();
                        myFlightTargetInitialDistance = myPtFlightStart.distanceTo(myDijkstraPath.PathNodes[5]);
                        myPtFlightTarget.z = myPtFlightStart.z;


                    }
                    myPtFlightFarTarget.Set(myDijkstraPath.PathNodes[Math.Min(myDijkstraPath.PathNodes.Count() - 1, 20)]);

                }
            }
            else if (myDijkstraPath.PathNodes.Count() > 3)
            {
                myPtFlightTarget.x = myDijkstraPath.PathNodes[3].X;
                myPtFlightTarget.y = myDijkstraPath.PathNodes[3].Y;
                myFlightTargetDir.set(new Vector3d(myDijkstraPath.PathNodes[2], myDijkstraPath.PathNodes[3]));
                myFlightTargetDir.z = 0;
                myFlightTargetDir.normalize();
                myFlightTargetInitialDistance = myPtFlightStart.distanceTo(myDijkstraPath.PathNodes[2]);
                myPtFlightTarget.z = myPtFlightStart.z;

//                myTarget.Set(myPtFlightTarget, myDijkstraPath.PathNodes[2], myDijkstraPath.PathNodes[3]);

            }
        }



        public bool Cost(DijkstraNode node, DijkstraNode nodePre, DijkstraNode nodePre2, bool bLast, out double dCost, out float dExtraHeight)
        {
            dExtraHeight = 0;
            Vector3d vecDir = new Vector3d(nodePre.Point3d(), node.Point3d());
            float dLen = vecDir.length();
            vecDir.normalize();

            double dDistStart = myPtFlightStart.distanceTo(node.Point3d());
            double dWind = Utils.interpolate(dDistStart, 0, 1.5, 1000, 0) * myPtFlightStart.getWind().dotProduct(vecDir);

            double dSpeedDist = 0.15 * myPtFlightStart.speed * myPtFlightStart.speed;

            myTerrainMap.GetNodeData(myPtFlightStart.x, myPtFlightStart.y, out Node nodeStart);
            double dEscapeHeight = 0;
            if (myPtFlightStart.z < nodeStart.safeHeight() + 200)
            {
                dEscapeHeight = nodeStart.safeHeight() + 150 - myPtFlightStart.z;
            }

            dCost = 0.0;

            if (nodePre.KPreOptimum == -1)
                return false;

            double dDev;

            if (nodePre2 != null)
            {
                Vector3d vecDir2 = new Vector3d(nodePre2.Point3d(), nodePre.Point3d());
                vecDir2.normalize();
                dDev = 1 - vecDir.dotProduct(vecDir2);
                if (!bLast && dDev > 0.15 && dDistStart < 7000)
                    return false;
            }
            else
            {
                dDev = 1 - vecDir.dotProduct(myVecFlight);
                if (!bLast && (dDev > 0.1))
                    return false;
            }

            float fSpacing = 90;

            for (float dP = 0; dP < dLen; dP += fSpacing)
            {
                float fHeight = nodePre.Z;// + vecDir.z * dP;
                myTerrainMap.GetSpecialNode(nodePre.X + vecDir.x * dP, nodePre.Y + vecDir.y * dP, out Node nodeTerrain);
                float fFunction = nodeTerrain.functionF;
                double dHeightCheck = nodeTerrain.safeHeight();

                if (dDistStart < 4000)
                {
                    float dSide= 50;
                    float dFront = dP + 150;

                    if (
                        myTerrainMap.GetNodeData(nodePre.X + vecDir.x * dFront - vecDir.y * dSide, nodePre.Y + vecDir.y * dFront + vecDir.x * dSide, out Node nodeLeft)
                        && myTerrainMap.GetNodeData(nodePre.X + vecDir.x * dFront + vecDir.y * dSide, nodePre.Y + vecDir.y * dFront - vecDir.x * dSide, out Node nodeRight)
                        )
                    {
                            if (nodeLeft.safeHeight() > dHeightCheck)
                                dHeightCheck = nodeLeft.safeHeight();
                            if (nodeRight.safeHeight() > dHeightCheck)
                                dHeightCheck = nodeRight.safeHeight();
                    }
                }
                dHeightCheck += 100 - dEscapeHeight;

                if (myTerrainMap.IsInFinalApproach)
                    fFunction /= 2;

                if (fFunction > 0)
                {
                    dExtraHeight += fFunction  * 5 / myPtFlightStart.speed * fSpacing;
                }

                if (myInlanding && fFunction > 0)
                    fFunction = 0;

                if (fHeight < dHeightCheck)
                {
                    return false;
                }
                //else if (false)//fHeight < nodeTerrain.height + 400 - dEscapeHeight)
                //{
                //    double dFactShortRange = Utils.interpolate(myFlightRange, 10000, 3, 20000, 1);
                //    dCost += (dDev * (dSpeedDist + 0.4*dWind) + fSpacing + 2 * dWind) * Utils.interpolate(dDistStart, 100, 1, 800, 2) -
                //        dFactShortRange * fSpacing * fFunction + 
                //        0.1 * (nodeTerrain.safeHeight() + 200 - fHeight);
                //    double dFactHeight = Utils.interpolate(fHeight - nodeTerrain.height, 100, 3, 400, 1);

                //    if (fFunction > 0)
                //        dCost -= 6 * dFactHeight * fSpacing * fFunction;
                //    else
                //        dCost -= 2 * fSpacing * fFunction;

                //}
                else
                {
                    double dFactRange = Utils.interpolate(myFlightRange, 25000, 1, 50000, 0.3);
                    double dFactShortRange = Utils.interpolate(myFlightRange, 5000, 5, 20000, 1);
                    double dFactHeight =
                        Utils.interpolate(fHeight - nodeTerrain.height, 0, 4, 100, 1) *
                        Utils.interpolate(fHeight - myTerrainMap.dClearanceReferenceHeight, 0, 1, 2000, 0.5) * 
                        Utils.interpolate(myTerrainMap.dClearanceReferenceHeight - nodeTerrain.height, 0, 2, 1500, 0.8);
                    double dFactEscape = Utils.interpolate(myPtFlightStart.vario, -8, 5, 0, 1);
                    double dFactStrongF = Utils.interpolate(Math.Abs(nodeTerrain.function), 1, 1, 2, 2);

                    if (fFunction > 0)
                    {
                        dCost += dDev * (dSpeedDist + 0.2 * dWind) + dWind + fSpacing - 1 * fSpacing * dFactStrongF * dFactShortRange  * dFactRange * dFactHeight * fFunction;
                        double dFactBlock = Utils.interpolate(fHeight - nodeTerrain.safeHeight(), 50, 1, 200,0);
                        dCost += dFactBlock * fSpacing;
                        //if (fHeight - nodeTerrain.height > 100)
                        //{
                        //    double dFactLift = nodeTerrain.function * Utils.interpolate(fHeight - nodeTerrain.height, 100, 3, 400, 0);
                        //    dCost -= dFactLift * fSpacing;
                        //}
                    }
                    else
                    {
                        dCost += dDev * (dSpeedDist + 0.2 * dWind) + dWind + fSpacing - 3 * fSpacing * dFactEscape* dFactStrongF * dFactRange * dFactHeight * fFunction;

                    }
                    double dTowardsSlope = vecDir.dotProduct(nodeTerrain.getNormal());
                    if (dTowardsSlope < -0.3)
                    {
                        dCost -= fSpacing * dTowardsSlope * Utils.interpolate(fHeight - nodeTerrain.height, 50, 5, 400, 0);
                    }
                }
            }
            return true;

        }

        public bool Cost (int i, int j, int ipre, int jpre, int ipre2, int jpre2, bool bLast, out double dCost, out float dExtraHeight)
        {
            if (jpre2 >= 0 && ipre2 >= 0)
            {
                return Cost(myDijkstraLines[i].Nodes[j], myDijkstraLines[ipre].Nodes[jpre], myDijkstraLines[ipre2].Nodes[jpre2], bLast, out dCost, out dExtraHeight);
            }
            else
            {
                return Cost(myDijkstraLines[i].Nodes[j], myDijkstraLines[ipre].Nodes[jpre], null, bLast, out dCost, out dExtraHeight);
            }
        }

        private bool CalcRange(float dStartHeight)
        {
            double flightRange = 0;
            int iRidgeCrossing = 0;
            myRidgeCrossingGap = 999;
            myRidgeCrossingRange = 0;
            myRidgeCrossingRatio = 1;

            myLiftForecast = 0;
            float dStep = 250;
            double dHeight = dStartHeight + (myPtFlightStart.speed * myPtFlightStart.speed - 1000)/20;

            float fTerrainHeight = 0;
            myDijkstraPath.GlideEndPosition = 0;

            int iPos = 0;
            while (iPos < myDijkstraPath.PathNodes.Count() - 1)
            {
                if (myDijkstraPath.ThermalSwitchPosition > 5 && myDijkstraPath.ThermalSwitchPosition == iPos)
                    dHeight -= Utils.interpolate(myPtFlightStart.z, 3500, 500, 6000, 100);

                Vector3d vecDir = myDijkstraPath.PathNodes[iPos].vectorXYTo(myDijkstraPath.PathNodes[iPos + 1]);
                float dLen = myDijkstraPath.PathNodes[iPos].distanceXYTo(myDijkstraPath.PathNodes[iPos + 1]);
                float dR = 0;
                while (dR < dLen)
                {
                    flightRange += dStep;
                    dR += dStep;
                    dHeight -= dStep / 25;// L/D of 30

                    Point3d pt = myDijkstraPath.PathNodes[iPos].add(dR, vecDir);
                    if (myTerrainMap.GetSafeNodeData(pt.X, pt.Y, out Node node))
                    {
                        if (flightRange > 2500 && flightRange < 7500)
                        {
                            myTerrainMap.GetSpecialNode(pt.X, pt.Y, out Node nodeSp);
                            myLiftForecast += 2*dStep * nodeSp.functionF * Utils.interpolate(flightRange, 2500, 1, 7500, 0);
                        }

                        fTerrainHeight = node.height;
                        for (int j = -4; j < 5; j++)
                        {
                            Point3d ptSide = pt.add(0, 3 * dStep * j, vecDir);
                            myTerrainMap.GetHeight(ptSide, out float fTerrainHeightSide);
                            fTerrainHeight = Math.Min(fTerrainHeight, fTerrainHeightSide);
                        }

                        if (iRidgeCrossing == 0)
                        {
                            if (dHeight - 250 < node.height)
                            {
                                myRidgeCrossingRange = flightRange;
                                iRidgeCrossing = 1;
                            }
                        }else if (iRidgeCrossing == 1 && flightRange - myRidgeCrossingRange < 1500)
                        {
                            myRidgeCrossingGap = Math.Max(0, Math.Min(dHeight - node.height, myRidgeCrossingGap));
                        }

                        if (dHeight < fTerrainHeight)
                        {

                            if (flightRange > 0)
                                myRidgeCrossingRatio = myRidgeCrossingRange / flightRange;
                            else
                                myRidgeCrossingRatio = 1;

                            myDijkstraPath.GlideEndPosition = iPos + 1;
                            if (dHeight > 2500)
                                flightRange += Utils.interpolate(flightRange, 25000, 0, 40000, 1)*(dHeight - 2500) * 15;
                            myFlightRange = 0.8 * myFlightRange + 0.2 * flightRange;
                            return true;
                        }
                    }


                }
                dHeight -= (dR - dLen) / 30;
                iPos += 1;
            }

            //if (false)//myDijkstraPath.PathNodes.Last().distanceXYTo(myFlightPlanData.GetNextSafeWayPoint().getPoint()) < 2000)
            //{
            //    for (iPos = myFlightPlanData.nextWayPoint; iPos < myFlightPlanData.Markers.Count() - 1; iPos++)
            //    {
            //        Vector3d vecDir = myFlightPlanData.Markers[iPos].getPoint().vectorXYTo(myFlightPlanData.Markers[iPos + 1].getPoint());
            //        float dLen = myFlightPlanData.Markers[iPos].getPoint().distanceXYTo(myFlightPlanData.Markers[iPos + 1].getPoint());
            //        float dR = 0;
            //        while (dR < dLen)
            //        {
            //            flightRange += dStep;
            //            dR += dStep;
            //            dHeight -= dStep / 30;// L/D of 30
            //            Point3d pt = myFlightPlanData.Markers[iPos].getPoint().add(dR, vecDir);
            //            myTerrainMap.GetHeight(pt, out fTerrainHeight);

            //            if (dHeight < fTerrainHeight)
            //            {
            //                if (!myThreadData.bIsShortRange)
            //                    flightRange += myLiftForecast;
            //                if (flightRange > myFlightRange)
            //                    myFlightRange = 0.8 * myFlightRange + 0.2 * flightRange;
            //                else
            //                    myFlightRange = 0.9 * myFlightRange + 0.1 * flightRange;
            //                return true;
            //            }

            //        }
            //        dHeight -= (dR - dLen) / 30;
            //        iPos += 1;
            //    }

            //    if (flightRange > 0)
            //        myRidgeCrossingRatio = myRidgeCrossingRange / flightRange;
            //    else
            //        myRidgeCrossingRatio = 1;

            //    if (!myThreadData.bIsShortRange)
            //        flightRange += myLiftForecast;
            //    flightRange += myLiftForecast;
            //    if (flightRange > myFlightRange)
            //        myFlightRange = 0.8 * myFlightRange + 0.2 * flightRange;
            //    else
            //        myFlightRange = 0.9 * myFlightRange + 0.1 * flightRange;

            //    myTerrainMap.GetHeight(myDijkstraPath.PathNodes.Last(), out float fTerrainHeightSide);
            //    myFlightRange += (dHeight - fTerrainHeightSide) * 30;
            //}


 
            if (!myThreadData.bIsShortRange)
                flightRange += myLiftForecast;

            if (myThreadData.ptTarget != null && myDijkstraPath.PathNodes.Count > 0 && myThreadData.ptTarget.getPosition().distanceXYTo(myDijkstraPath.PathNodes.Last()) < 2500)
            {
                flightRange += (dHeight - fTerrainHeight) * 20;
            }

            myDijkstraPath.GlideEndPosition = iPos + 1;

            if (dStartHeight > 3500)
                flightRange += 10 * (dStartHeight - 3500);

            myFlightRange = 0.8 * myFlightRange + 0.2 * flightRange;

            return true;
        }

        private bool MakePath(bool bQuality)
        {
            lock (myLock)
            {


                //myDijkstraPath.PathNodes.Clear();
                bool dEndFound = false;
                int jPrevious = -1;
                for (int i = myLinesCount - 1; i >= 0; i--)
                {
                    if (!dEndFound)
                    {
                        int jOptimum = -1;
                        double dMin = 999999;
                        for (int j = 0; j < myDijkstraLines[i].NodeCount; j++)
                        {
                            if (myDijkstraLines[i].Nodes[j].KPreOptimum >= 0
                                &&
                                myDijkstraLines[i].Nodes[j].CostOptimum < dMin
                                )
                            {
                                dMin = myDijkstraLines[i].Nodes[j].CostOptimum;
                                jOptimum = j;
                                dEndFound = true;
                            }
                        }
                        if (dEndFound)
                        {
                            myDijkstraPath.PathNodes.Insert(myDijkstraPath.ThermalSwitchPosition, myDijkstraLines[i].Nodes[jOptimum].Point3d());
                            jPrevious = myDijkstraLines[i].Nodes[jOptimum].KPreOptimum;
                        }
                    }
                    else
                    {
                        if (jPrevious >= 0 && jPrevious < myDijkstraLines[i].NodeCount)
                        {
                            myDijkstraPath.PathNodes.Insert(myDijkstraPath.ThermalSwitchPosition, myDijkstraLines[i].Nodes[jPrevious].Point3d());
                            jPrevious = myDijkstraLines[i].Nodes[jPrevious].KPreOptimum;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                int pHi = 12;
                pHi = Math.Min(pHi, myDijkstraPath.PathNodes.Count() - 1);
                if (pHi > 0)
                    myPhotoLocation.Set(myDijkstraPath.PathNodes[pHi]);


                myDijkstraPath.CalcQuality(myTerrainMap, myPtFlightStart);
                return dEndFound;
            }
        }

        public bool Solve( double dCutOff = 0, bool bDoHeightCalc = true, TextBox textBox = null, string strPrefix = "")
        {
            for (int i = 1; i < myLinesCount; i++)
            {
                if (textBox != null)
                {
                    textBox.Text = strPrefix + ": " + i.ToString() + "/" + (myLinesCount-1).ToString();
                    textBox.Refresh();
                }
                if (myThreadStatus == ThreadStatus.kStop)
                    return false;

                Parallel.For(0, myDijkstraLines[i].NodeCount, (j) =>
                {
                  //for (int j = 0; j < myDijkstraLines[i].NodeCount; j++)
                  //  {

                    double dCostMin = 999999;
                    int jPreOptimum = -1;
                    float dExtraHeight = 0;

                    for (int jpre = 0; jpre < myDijkstraLines[i - 1].NodeCount; jpre++)
                    {
                        if (dCutOff > 0)
                        {
                            if (myDijkstraLines[i].Nodes[j].Point3d().distanceXYTo(myDijkstraLines[i].Nodes[j].Point3d()) > dCutOff)
                            {
                                continue;
                            }
                        }
                        int jpre2 = -1;
                        if (i - 2 >= 0)
                        {
                            jpre2 = myDijkstraLines[i - 1].Nodes[jpre].KPreOptimum;
                        }
                        if (Cost(i, j, i - 1, jpre, i - 2, jpre2, i == myLinesCount - 1, out double dCost, out float dExtraHeightLoc))
                        {
                            if (dCostMin > dCost + myDijkstraLines[i - 1].Nodes[jpre].CostOptimum)
                            {
                                dCostMin = dCost + myDijkstraLines[i - 1].Nodes[jpre].CostOptimum;
                                jPreOptimum = jpre;
                                dExtraHeight = dExtraHeightLoc;
                            }
                        }
                    }

                    if (jPreOptimum >= 0)
                    {
                        myDijkstraLines[i].Nodes[j].CostOptimum = dCostMin;
                        myDijkstraLines[i].Nodes[j].KPreOptimum = jPreOptimum;
                        if (bDoHeightCalc)
                            myDijkstraLines[i].Nodes[j].Z += dExtraHeight;
                    }
                    else
                    {
                        myDijkstraLines[i].Nodes[j].CostOptimum = dCostMin;
                        myDijkstraLines[i].Nodes[j].KPreOptimum = -1;
                    }
                });
            }
            MakePath(true);

            if (myFlightPlanData.CurrentMarker() != null)
            {
                myDijkstraPath.StartPlanner = myDijkstraPath.PathNodes.Count();
                myFlightPlanData.CurrentMarker().AppendToDijkstraPath(myDijkstraPath.PathNodes);
            }

            return true;
        }

        public void CalcPlanner(Point3d ptStart, Point3d ptTarget, TextBox textBoxInfo = null, string strPrefix = "")
        {
            myDijkstraPath.PathNodes.Clear();
            myFlightRange = 35000;

            float fDelta = 7000;

            ptTarget.Z = ptStart.Z = 3200;
            double dDistance = ptStart.distanceXYTo(ptTarget);
            Vector3d vecDir = new Vector3d(ptStart, ptTarget);
            vecDir.z = 0;
            vecDir.normalize();
 
            myPtFlightStart.x = ptStart.X;
            myPtFlightStart.y = ptStart.Y;
            myPtFlightStart.z = ptStart.Z;
            myPtFlightStart.speed = 50;
            myPtFlightStart.vec = vecDir;

            myLinesCount = 0;

            if (dDistance < 10)
            {
                return;
            }

            myVecFlight = vecDir;
            myVecWind = myFlightPlanData.vecWind;


            FlightPathPoint ptTemp = new FlightPathPoint(ptStart);
            myDijkstraLines[myLinesCount].Nodes[0].Clone(ptTemp);
            myDijkstraLines[myLinesCount].Nodes[0].KPreOptimum = 0;

            myDijkstraLines[myLinesCount].NodeCount = 1;
            myLinesCount++;

            float dRad = fDelta;
            float dRadDelta = fDelta;
            while (dRad < dDistance - 2000)
            {

                //DijkstraLine line2 = new DijkstraLine(dRad);
                myDijkstraLines[myLinesCount].Radius = dRad;
                myDijkstraLines[myLinesCount].NodeCount = 0;
                float dMidRange = 6000;

                if (dRad < dMidRange) // do full circle
                {
                    double dStep = 10 * Math.PI / 180;
                    double dAngle = dStep;

                    while (dAngle < Math.PI)
                    {
                        double dCos = dRad * Math.Cos(dAngle);
                        double dSin = dRad * Math.Sin(dAngle);
                        ptTemp.Add(myPtFlightStart, myVecFlight, dCos, dSin);
                        ptTemp.z = myPtFlightStart.z;
                        myDijkstraLines[myLinesCount].Nodes[myDijkstraLines[myLinesCount].NodeCount].Clone(ptTemp);
                        myDijkstraLines[myLinesCount].NodeCount++;

                        dCos = dRad * Math.Cos(-dAngle);
                        dSin = dRad * Math.Sin(-dAngle);
                        ptTemp.Add(myPtFlightStart, myVecFlight, dCos, dSin);

                        myDijkstraLines[myLinesCount].Nodes[myDijkstraLines[myLinesCount].NodeCount].Clone(ptTemp);
                        myDijkstraLines[myLinesCount].NodeCount++;

                        dAngle += dStep;

                    }
                }
                else
                {
                    int nSteps = 30;

                    double dW = 20000;
                    double dAngStep = dW / (dRad * nSteps);
                    for (double dAng = -nSteps * dAngStep; dAng <= nSteps * dAngStep; dAng += dAngStep)
                    {
                        double dCos = dRad * Math.Cos(dAng);
                        double dSin = dRad * Math.Sin(dAng);

                        ptTemp.Add(myPtFlightStart, vecDir, dCos, dSin);
                        myDijkstraLines[myLinesCount].Nodes[myDijkstraLines[myLinesCount].NodeCount].Clone(ptTemp);
                        myDijkstraLines[myLinesCount].NodeCount++;
                    }

                }

                myLinesCount++;

                dRad += fDelta;
            }


            myDijkstraLines[myLinesCount].NodeCount = 0;
            myDijkstraLines[myLinesCount].Nodes[0].Clone(new FlightPathPoint(ptTarget));
            myDijkstraLines[myLinesCount].NodeCount++;
            myLinesCount++;

            Solve(1.5*fDelta, false, textBoxInfo, strPrefix);
            myDijkstraPath.GlideEndPosition = myDijkstraPath.PathNodes.Count();
        }

        public void CalcEscape(FlightPathPoint ptStart, FlightPathPoint ptTarget, float fDelta)
        {
            myDijkstraPath.ThermalSwitchPosition = 0;
            myDijkstraPath.GlideEndPosition = 0;
            if (fDelta < 10)
            {
                myDijkstraPath.Ready = true;
                return;
            }

            double dDistance = ptStart.distanceTo(ptTarget.getPosition());

            //if (dDistance < 4000)
            //{
            //    CalcThermalTarget(ptStart, ptTarget, 0.7f * fDelta);
            //    return;
            //}

            myPtFlightStart = ptStart;
            float dSinkRate = 0;
            if (ptStart.speed > 0)
             dSinkRate = Math.Max(-Math.Max(ptStart.vario, -7), 0.5f) / ptStart.speed;

            myDijkstraPath.Ready = false;

            //myDijkstraLines.Clear();
            myLinesCount = 0;
            ptTarget.z = ptStart.z;

            if (dDistance < 10)
            {
                myDijkstraPath.Ready = true;
                return;
            }

            Vector3d vecDir = ptStart.vectorTo(ptTarget.getPosition());
            myVecFlight = ptStart.vec;
            myVecFlight.normalize();
            double dTurnAngle = myVecFlight.angleXYTo(vecDir);
            myVecWind = ptStart.getWind();


            FlightPathPoint ptTemp = new FlightPathPoint(ptStart);
            // ptTemp.Add(ptStart, 5 * ptStart.vec.x, 5 * ptStart.vec.y);

            //DijkstraNode nodeStart = new DijkstraNode(ptTemp);
            //nodeStart.KPreOptimum = 0;
            //DijkstraLine line1 = new DijkstraLine(0);
            //line1.Nodes.Add(nodeStart);
            //myDijkstraLines.Add(line1);
            myDijkstraLines[myLinesCount].Nodes[0].Clone(ptTemp);
            myDijkstraLines[myLinesCount].Nodes[0].KPreOptimum = 0;

            myDijkstraLines[myLinesCount].NodeCount = 1;
            myLinesCount++;

            float dH5000 = ptStart.z;

            float dRad = fDelta;
            float dRadDelta = fDelta;
            while (dRad < dDistance - 2000)
            {

                //DijkstraLine line2 = new DijkstraLine(dRad);
                myDijkstraLines[myLinesCount].Radius = dRad;
                myDijkstraLines[myLinesCount].NodeCount = 0;
                float dMidRange = 6000;

                if (dRad < dMidRange) // do full circle
                {
                    double dTurnDelta = dRad / dMidRange;
                    dTurnDelta = dTurnAngle * (1 - (1 - dTurnDelta) * (1 - dTurnDelta));

                    double dStep = 2 * Math.PI / 180;
                    double dAngle = dStep;

                    while (dAngle < Math.PI)
                    {
                        double dCos = dRad * Math.Cos(dAngle + dTurnDelta);
                        double dSin = dRad * Math.Sin(dAngle + dTurnDelta);
                        ptTemp.Add(ptStart, myVecFlight, dCos, dSin);
                        ptTemp.z = ptStart.z - dRad * dSinkRate - 50;
                        //DijkstraNode node = new DijkstraNode(ptTemp);
                        //line2.Nodes.Add(node);
                        myDijkstraLines[myLinesCount].Nodes[myDijkstraLines[myLinesCount].NodeCount].Clone(ptTemp);
                        myDijkstraLines[myLinesCount].NodeCount++;

                        dCos = dRad * Math.Cos(-dAngle + dTurnDelta);
                        dSin = dRad * Math.Sin(-dAngle + dTurnDelta);
                        ptTemp.Add(ptStart, myVecFlight, dCos, dSin);
                        if (dRad < 5000)
                        {
                            ptTemp.z = ptStart.z - dRad * dSinkRate;
                            dH5000 = ptTemp.z;
                        }
                        else
                        {
                            ptTemp.z = dH5000;
                        }
                        //node = new DijkstraNode(ptTemp);
                        //line2.Nodes.Add(node);
                        myDijkstraLines[myLinesCount].Nodes[myDijkstraLines[myLinesCount].NodeCount].Clone(ptTemp);
                        myDijkstraLines[myLinesCount].NodeCount++;

                        dAngle = 1.1 * dAngle + dStep;

                    }
                }
                else
                {
                    int nSteps = 25;
                    if (dRad > 40000)
                        nSteps = 12;

                    double dQ1 = dRad / dDistance;
                    double dQ2 = 1 - 2 * dQ1;
                    double dW = (10000 - dQ1 * 8000 + 12000 * (1 - dQ2 * dQ2)) / (nSteps * (2 - dQ1));
                    double dAngStep = dW / dRad;
                    for (double dAng = -nSteps * dAngStep; dAng <= nSteps * dAngStep; dAng += dAngStep)
                    {
                        double dCos = dRad * Math.Cos(dAng);
                        double dSin = dRad * Math.Sin(dAng);

                        ptTemp.Add(ptStart, vecDir, dCos, dSin);
                        if (dRad < 10000)
                        {
                            //dH10000 = Math.Min(5000, dH5000 - (dRad - dMidRange) / 50);
                            //ptTemp.z = dH10000;
                            ptTemp.z = dH5000;
                        }
                        else
                            ptTemp.z = Math.Min(5000, dH5000 + (dRad - 10000) / 20);

                        //DijkstraNode node = new DijkstraNode(ptTemp);
                        //line2.Nodes.Add(node);
                        myDijkstraLines[myLinesCount].Nodes[myDijkstraLines[myLinesCount].NodeCount].Clone(ptTemp);
                        myDijkstraLines[myLinesCount].NodeCount++;
                    }

                }

                //                myDijkstraLines.Add(line2);
                myLinesCount++;

                dRad += dRadDelta;
                dRadDelta *= 1.15f;
            }

            //            DijkstraLine line3 = new DijkstraLine(dDistance);
            ptTarget.z = 5000;
            //DijkstraNode nodeLast = new DijkstraNode(ptTarget);
            //line3.Nodes.Add(nodeLast);
            //myDijkstraLines.Add(line3);

            if (myFlightPlanData.GetNextSafeWayPoint().getPoint().distanceXYTo(ptTarget.getPosition()) < 1000)
            {
                myDijkstraLines[myLinesCount].NodeCount = 0;
                myDijkstraLines[myLinesCount].Nodes[0].Clone(ptTarget);
                myDijkstraLines[myLinesCount].NodeCount++;
            }
            else
            {

                myDijkstraLines[myLinesCount].NodeCount = 0;
                for (int i = -4; i < 5; i++)
                {
                    ptTemp.Add(ptTarget, vecDir, 0, i * 400);
                    myDijkstraLines[myLinesCount].Nodes[myDijkstraLines[myLinesCount].NodeCount].Clone(ptTemp);
                    myDijkstraLines[myLinesCount].NodeCount++;
                }
            }
            myLinesCount++;

            SolveEscape();

           // SetTarget();
            CalcRange(ptStart.z);
            myDijkstraPath.Ready = true;

        }

        public bool SolveEscape()
        {
            for (int i = 1; i < myLinesCount; i++)
            {
               for (int j = 0; j < myDijkstraLines[i].NodeCount; j++)
                {
                    if (myThreadStatus == ThreadStatus.kStop)
                        return false;

                    double dCostMin = 999999;
                    int jPreOptimum = -1;

                    for (int jpre = 0; jpre < myDijkstraLines[i - 1].NodeCount; jpre++)
                    {
                        int jpre2 = -1;
                        if (i - 2 >= 0)
                        {
                            jpre2 = myDijkstraLines[i - 1].Nodes[jpre].KPreOptimum;
                        }
                        if (CostEscape(i, j, i - 1, jpre, i - 2, jpre2, i == myLinesCount - 1, out double dCost))
                        {
                            if (dCostMin > dCost + myDijkstraLines[i - 1].Nodes[jpre].CostOptimum)
                            {
                                dCostMin = dCost + myDijkstraLines[i - 1].Nodes[jpre].CostOptimum;
                                jPreOptimum = jpre;
                            }
                        }
                    }

                    if (jPreOptimum >= 0)
                    {
                        myDijkstraLines[i].Nodes[j].CostOptimum = dCostMin;
                        myDijkstraLines[i].Nodes[j].KPreOptimum = jPreOptimum;
                    }
                    else
                    {
                        myDijkstraLines[i].Nodes[j].CostOptimum = dCostMin;
                        myDijkstraLines[i].Nodes[j].KPreOptimum = -1;
                    }
                }
            }
            MakePath(true);



            return true;
        }
        public bool CostEscape(int i, int j, int ipre, int jpre, int ipre2, int jpre2, bool bLast, out double dCost)
        {
            if (jpre2 >= 0 && ipre2 >= 0)
            {
                return CostEscape(myDijkstraLines[i].Nodes[j], myDijkstraLines[ipre].Nodes[jpre], myDijkstraLines[ipre2].Nodes[jpre2], bLast, out dCost);
            }
            else
            {
                return CostEscape(myDijkstraLines[i].Nodes[j], myDijkstraLines[ipre].Nodes[jpre], null, bLast, out dCost);
            }
        }
        public bool CostEscape(DijkstraNode node, DijkstraNode nodePre, DijkstraNode nodePre2, bool bLast, out double dCost)
        {
            Vector3d vecDir = new Vector3d(nodePre.Point3d(), node.Point3d());
            float dLen = vecDir.length();
            vecDir.normalize();

            double dDistStart = myPtFlightStart.distanceTo(node.Point3d());
            double dWind = Utils.interpolate(dDistStart, 0, 1.5, 1000, 0) * myPtFlightStart.getWind().dotProduct(vecDir);

            double dSpeedDist = 0.15 * myPtFlightStart.speed * myPtFlightStart.speed;

            myTerrainMap.GetNodeData(myPtFlightStart.x, myPtFlightStart.y, out Node nodeStart);

            dCost = 0.0;

            if (nodePre.KPreOptimum == -1)
                return false;

            double dDev;

            if (nodePre2 != null)
            {
                Vector3d vecDir2 = new Vector3d(nodePre2.Point3d(), nodePre.Point3d());
                vecDir2.normalize();
                dDev = 1 - vecDir.dotProduct(vecDir2);
                if (!bLast && dDev > 0.4 && dDistStart < 7000)
                    return false;
            }
            else
            {
                dDev = 1 - vecDir.dotProduct(myVecFlight);
                if (!bLast && (dDev > 0.3))
                    return false;
            }

            float fSpacing = 90;

            for (float dP = 0; dP < dLen; dP += fSpacing)
            {
                float fHeight = nodePre.Z;// + vecDir.z * dP;
                myTerrainMap.GetSpecialNode(nodePre.X + vecDir.x * dP, nodePre.Y + vecDir.y * dP, out Node nodeTerrain);
                float fFunction = nodeTerrain.functionF;

                float dHeightCheck = nodeTerrain.height;
                if (dDistStart < 4000)
                {
                    float dSide = 50;
                    float dFront = dP + 150;

                    if (
                        myTerrainMap.GetNodeData(nodePre.X + vecDir.x * dFront - vecDir.y * dSide, nodePre.Y + vecDir.y * dFront + vecDir.x * dSide, out Node nodeLeft)
                        && myTerrainMap.GetNodeData(nodePre.X + vecDir.x * dFront + vecDir.y * dSide, nodePre.Y + vecDir.y * dFront - vecDir.x * dSide, out Node nodeRight)
                        )
                    {
                        if (nodeLeft.height > dHeightCheck)
                            dHeightCheck = nodeLeft.height;
                        if (nodeRight.height > dHeightCheck)
                            dHeightCheck = nodeRight.height;
                    }
                }
                 dHeightCheck += 25;

                if (fHeight < dHeightCheck)
                {
                    return false;
                }
                else
                {
                    dCost += fSpacing - fSpacing * fFunction;
                    double dFactBlock = Utils.interpolate(fHeight - nodeTerrain.height, 25, 5, 300, 0);
                    double dAddedCost = Utils.interpolate(fHeight - nodeTerrain.height, 0, 500, 300, 0);
                    double dSofteningForDistance = Utils.interpolate(dDistStart, 0, 0, 500, 1);
                    dCost += dSofteningForDistance * dFactBlock * fSpacing + dAddedCost;
                    double dTowardsSlope = vecDir.dotProduct(nodeTerrain.getNormal());
                    if (dTowardsSlope < 0)
                    {
                        dCost -= fSpacing * dTowardsSlope * Utils.interpolate(fHeight - nodeTerrain.height, 50, 10, 400, 1);
                    }
                }
            }
            return true;

        }


    }



}
