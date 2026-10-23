using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using GMap.NET.WindowsForms;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms.Markers;


namespace CondorAutoPilot
{
    public partial class Planning : Form
    {
        private MapCondor myMapPlanning = new MapCondor();
        private FlightPlanData myFlightPlanData = new FlightPlanData();
        private List<CondorUDPData> myCondorUDPDataLog = new List<CondorUDPData>();

        private TerrainMap myTerrainMap;

        enum Modus { kAdd, kInsert, kMove, kDelete, kFunction, kInspect, kDijkstra, kDijkstraLap, kNone };
        private Modus myModus;

        private int mySelectedWayPoint;
        private int myClickIndex;

        private int myCondorUDPDataIndex = 0;

        private FlightPathPoint ptStart, ptTarget;

        public Planning()
        {
            InitializeComponent();
            myModus = Modus.kNone;
        } 

        public void Init(FlightPlanData flightPlanData, TerrainMap terrainMap, List<CondorUDPData> condorUDPDataLog)
        {
            string strFile;
            myTerrainMap = terrainMap;
            myFlightPlanData = flightPlanData;
            myCondorUDPDataLog = condorUDPDataLog;
            myClickIndex = 0;


            strFile = Properties.Settings.Default.CondorFlightPlanPath + @"\" + Properties.Settings.Default.FlightPlan;
            if (File.Exists(strFile))
            {
                myMapPlanning.Init(ref gMapPlanning, GMapProviders.GoogleTerrainMap, 8);

                //myFlightPlanData.Init(strFile);

                myMapPlanning.AddFlightPlan(myFlightPlanData, true);

                myMapPlanning.AddTerrainMap(myTerrainMap);
                myMapPlanning.AddPlanePath(myCondorUDPDataLog, myTerrainMap);
            }

        }

 
        private void gMapPlanning_MouseMove(object sender, MouseEventArgs e)
        {
            float lat = Convert.ToSingle(gMapPlanning.FromLocalToLatLng(e.X, e.Y).Lat);
            float lng = Convert.ToSingle(gMapPlanning.FromLocalToLatLng(e.X, e.Y).Lng);

            if ((myModus == Modus.kMove || myModus == Modus.kDelete) && mySelectedWayPoint == -1)
            {
                //int iFound = myFlightPlanExtended.FindClosestWayPoint(lat, lng, Properties.Settings.Default.InitStartLat, Properties.Settings.Default.InitStartLon);

                //if (iFound > -1)
                //{
                //    if (myFlightPlanExtended.GetWayPoint(iFound, out FlightPlanExtendedLocation pt))
                //    {
                //        myMapPlanning.SetMarkerPosition(pt.X, pt.Y, true, true);
                //    }
                //}
            }
            if ((myModus == Modus.kMove || myModus == Modus.kDelete) && mySelectedWayPoint > -1)
            {
                // myMapPlanning.SetMarkerPosition(lat, lng, false, true);

                //if (myModus == Modus.kMove)
                //{
                //    FlightPlanExtendedLocation pt = new FlightPlanExtendedLocation(lat, lng, 0, 0);
                //    myFlightPlanExtended.SetWayPoint(mySelectedWayPoint, pt);
                //    myMapPlanning.AddFlightPlanExtended(myFlightPlanExtended);
                //}

            }

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            myModus = Modus.kAdd;
            myClickIndex = 0;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            myModus = Modus.kInsert;
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            myModus = Modus.kMove;
            mySelectedWayPoint = -1;
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            myModus = Modus.kDelete;
            mySelectedWayPoint = -1;
        }
        private void radioButton5_CheckedChanged_1(object sender, EventArgs e)
        {
            myModus = Modus.kFunction;
        }

        private void radioButton6_CheckedChanged(object sender, EventArgs e)
        {
            myModus = Modus.kInspect;
        }

        private void radioButton7_CheckedChanged(object sender, EventArgs e)
        {
            myModus = Modus.kDijkstra;
            myClickIndex = 0;
        }
        private void radioButton8_CheckedChanged(object sender, EventArgs e)
        {
            myModus = Modus.kDijkstraLap;
            myClickIndex = 0;
        }
        private void radioButton9_CheckedChanged(object sender, EventArgs e)
        {
            myModus = Modus.kNone;

        }

        private void gMapPlanning_MouseClick(object sender, MouseEventArgs e)
        {
            double lat = gMapPlanning.FromLocalToLatLng(e.X, e.Y).Lat;
            double lng = gMapPlanning.FromLocalToLatLng(e.X, e.Y).Lng;


            if (e.Button == MouseButtons.Left && myModus == Modus.kInspect)
            {
                //FlightPlanExtendedLocation pt = new FlightPlanExtendedLocation(lat, lng, 0, 0);
                //Node node = new Node();
                //myTerrainMap.GetNodeDataCoarse(pt.getPosition(), out node);
                //textBoxInspect.Text = node.function.ToString("F3");
            }


            else if (e.Button == MouseButtons.Left && myModus == Modus.kFunction)
            {
                //FlightPlanExtendedLocation pt = new FlightPlanExtendedLocation(lat, lng, 0, 0);
                //myMapPlanning.SetTerrainAnalysis(pt.X, pt.Y, ref myTerrainMap);
            }

            else if (e.Button == MouseButtons.Left && myModus == Modus.kMove)
            {
                //if (mySelectedWayPoint == -1)
                //{
                //    mySelectedWayPoint = myFlightPlanExtended.FindClosestWayPoint(lat, lng, Properties.Settings.Default.InitStartLat, Properties.Settings.Default.InitStartLon);
                //}
                //else
                //{
                //    FlightPlanExtendedLocation pt = new FlightPlanExtendedLocation(lat, lng, 0, 0);
                //    myFlightPlanExtended.SetWayPoint(mySelectedWayPoint, pt);
                //    mySelectedWayPoint = -1;
                //    myMapPlanning.AddFlightPlanExtended(myFlightPlanExtended);
                //}
                return;
            }

            else if (e.Button == MouseButtons.Left && myModus == Modus.kDelete)
            {
                //mySelectedWayPoint = myFlightPlanExtended.FindClosestWayPoint(lat, lng, Properties.Settings.Default.InitStartLat, Properties.Settings.Default.InitStartLon);
                //if (mySelectedWayPoint > -1)
                //{
                //    myFlightPlanExtended.RemoveWayPoint(mySelectedWayPoint);
                //    mySelectedWayPoint = -1;
                //    myMapPlanning.AddFlightPlanExtended(myFlightPlanExtended);
                //}
                return;
            }

            else if (e.Button == MouseButtons.Left && myModus == Modus.kAdd)
            {
                //myFlightPlanExtended.AddLocationToWayPoints(lat, lng, 0, 0);
                //myMapPlanning.AddFlightPlanExtended(myFlightPlanExtended);
                return;
            }

            else if (e.Button == MouseButtons.Left && myModus == Modus.kDijkstra)
            {
                Point3d ptMap = new Point3d(Convert.ToSingle(lat), Convert.ToSingle(lng));
                Node node = new Node();
                myTerrainMap.GetNodeDataCoarse(ptMap, out node);
                Point3d pt = new Point3d(ptMap.X, ptMap.Y, node.height + 1500);

                if (myClickIndex == 0)
                {
                    ptStart = new FlightPathPoint(pt);
                    myClickIndex = 1;
                }
                else if(myClickIndex == 1)
                {
                    ptTarget = new FlightPathPoint(pt);
                    myClickIndex = 0;
                    ptStart.vec.x = 0;
                    ptStart.vec.y = 1;
                    ptStart.speed = 50;
                    ptTarget.vec.x = 0;
                    ptTarget.vec.y = 1;
                    ptTarget.speed = 50;


                    Dijkstra dijkstra = new Dijkstra(myTerrainMap, myFlightPlanData);
                    dijkstra.myThreadStatus = ThreadStatus.kWaitingForInput;
                    dijkstra.Calc(ptStart, ptTarget, 500);
                    //myMapPlanning.AddTerrainMap(myTerrainMap);
                    myMapPlanning.AddDijkstraPath(dijkstra, true);
                }


                return;
            }

            else if (e.Button == MouseButtons.Left && myModus == Modus.kDijkstraLap)
            {
                Point3d ptMap = new Point3d(Convert.ToSingle(lat), Convert.ToSingle(lng));
                Node node = new Node();
                myTerrainMap.GetNodeDataCoarse(ptMap, out node);
                Point3d pt = new Point3d(ptMap.X, ptMap.Y, node.height + 1500);

                if (myClickIndex == 0)
                {
                    ptStart = new FlightPathPoint(pt);
                    myClickIndex = 1;
                }
                else if (myClickIndex == 1)
                {
                    ptTarget = new FlightPathPoint(pt);
                    myClickIndex = 0;

                    Dijkstra dijkstra = new Dijkstra(myTerrainMap, myFlightPlanData);
                    dijkstra.myThreadStatus = ThreadStatus.kWaitingForInput;
                    dijkstra.CalcPlanner(ptStart.getPosition(), ptTarget.getPosition());
                    //myMapPlanning.AddTerrainMap(myTerrainMap);
                    myMapPlanning.AddDijkstraPath(dijkstra, true);
                }


                return;
            }
        }

        private void Planning_Load(object sender, EventArgs e)
        {

        }

        private void DrawSinglePath(int index)
        {
            CondorUDPData condorUDPData = myCondorUDPDataLog[index];

            FlightPathPoint ptStart = condorUDPData.GetFlightPoint();
            ptStart.windx = myFlightPlanData.vecWind.x;
            ptStart.windy = myFlightPlanData.vecWind.y;

            myTerrainMap.CheckForRidge_V2(ptStart);

            myFlightPlanData.nextWayPoint = condorUDPData.wayPoint;
            FlightPathPoint ptTarget = new FlightPathPoint(myFlightPlanData.Markers[condorUDPData.wayPoint].getPoint());
            Dijkstra dijkstra = new Dijkstra(myTerrainMap, myFlightPlanData);


            if (condorUDPData.thermal == 1)
            {
                if (myFlightPlanData.GetClosestPositionOnPath(ptStart, 35000, out FlightPathPoint ptOnPath))
                    dijkstra.ThreadSetData(ptStart, ptOnPath, new FlightPathPoint(condorUDPData.thermalPoint), ptStart.speed * 4, true);
                else
                    dijkstra.ThreadSetData(ptStart, myFlightPlanData.getNextMarkerPosition(true), new FlightPathPoint(condorUDPData.thermalPoint), ptStart.speed * 4, true);
            }
            else
            {
                if (myFlightPlanData.GetClosestPositionOnPath(ptStart, 35000, out FlightPathPoint ptOnPath))
                {
                    if (myTerrainMap.bRidge)
                        dijkstra.ThreadSetData(ptStart, ptOnPath, ptStart.speed * 4, false);
                    else
                        dijkstra.ThreadSetData(ptStart, ptOnPath, ptStart.speed * 5, false);
                }
                else
                {
                    if (myTerrainMap.bRidge)
                        dijkstra.ThreadSetData(ptStart, myFlightPlanData.getNextMarkerPosition(true), ptStart.speed * 4, false);
                    else
                        dijkstra.ThreadSetData(ptStart, myFlightPlanData.getNextMarkerPosition(true), ptStart.speed * 5, false);
                }
            }
            dijkstra.ThreadSetRange(condorUDPData.dijkstraRange);
            dijkstra.ThreadRunCore();
            myMapPlanning.AddDijkstraPath(dijkstra, true);
            textBoxInspect.Text = dijkstra.myDijkstraPath.PathNodes.Count().ToString();


        }

        private void gMapPlanning_OnMarkerClick(GMapMarker item, MouseEventArgs e)
        {
            CondorUDPData condorUDPData = (CondorUDPData)item.Tag;
            myCondorUDPDataIndex = condorUDPData.index;
            DrawSinglePath(myCondorUDPDataIndex);
        }

        private void trackBarRange_ValueChanged(object sender, EventArgs e)
        {
            textBoxRange.Text = (trackBarRange.Value * 1000).ToString();
            textBoxRange.Refresh();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int iMax = Math.Min(myCondorUDPDataIndex + 50, myCondorUDPDataLog.Count());
            for (int i = myCondorUDPDataIndex; i < iMax; i++)
            {
                DrawSinglePath(i);
            }
            myCondorUDPDataIndex = iMax;
        }

        private void buttonExportAICSV_Click(object sender, EventArgs e)
        {
            string strFile = Properties.Settings.Default.CondorFlightPlanPath + @"\" + Properties.Settings.Default.FlightPlan;
            strFile = strFile.Replace(".fpl", "L.csv");
            strFile = strFile.Replace(".blg", "L.csv");
            float fHeightNorm = 4000;
            double dWindRef = myFlightPlanData.windSpeed;

            StringBuilder csvContent = new StringBuilder();
            String newline;
            newline = "VarioNorm,AltitudeNorm,Laplace,GM500,GM250,G0,G250,G500,G1000,G1500,G2500,G5000,G10000";

            csvContent.AppendLine(newline);

            float[] dist = new float[]{ -500, -250, 0, 250, 500, 1000, 1500, 2500, 5000, 10000 };

            for (int i = 0; i < myCondorUDPDataLog.Count(); i += 20)
            {
                double dAltitude = myCondorUDPDataLog[i].altitude / fHeightNorm;
                double dVarioNorm = myCondorUDPDataLog[i].vario / dWindRef;
                myTerrainMap.GetNodeData(myCondorUDPDataLog[i].x, myCondorUDPDataLog[i].y, out Node node);
                double dLaplace = node.function;
                float refHeight = node.height;

                newline = dVarioNorm.ToString("F4") + "," +
                    dAltitude.ToString("F4") + "," +
                    dLaplace.ToString("F4");

                for (int j = 0; j < dist.Length; j++)
                {
                    float x = myCondorUDPDataLog[i].x - dist[j] * myFlightPlanData.vecWind.x;
                    float y = myCondorUDPDataLog[i].y - dist[j] * myFlightPlanData.vecWind.y;
                    myTerrainMap.GetHeight(x, y, out float fHeight);
                    if (j == 2)
                        fHeight /= fHeightNorm;
                    else
                        fHeight = (refHeight - fHeight) / fHeightNorm;

                    newline = newline + "," + fHeight.ToString("F4");
                }
                csvContent.AppendLine(newline);
            }
            File.WriteAllText(strFile, csvContent.ToString());
        }

        private void plannerClosed(object sender, FormClosedEventArgs e)
        {
//            myFlightPlanExtended.WriteToFile();
        }

    }
}
