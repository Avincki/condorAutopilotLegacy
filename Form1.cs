using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Diagnostics;
using WindowsInput.Native;
using WindowsInput;


using System.Net;
using System.Drawing.Drawing2D;
using GMap.NET.MapProviders;

using CondorAutoPilot.ThermalML;
using CondorAutoPilot.ControlViewer;
using CondorAutoPilot.YoloViewer;
using CondorAutoPilot.InfoWindow;
//using TensorFlow;


namespace CondorAutoPilot
{
    
    public partial class Form1 : Form
    {
        static public NaviCon64 naviCon64 = new NaviCon64();

        private static readonly int VK_F2 = 0x71; //This is the F2 key.
        private static readonly int VK_F3 = 0x72;
        private static readonly int VK_F9 = 0x78;
        private static readonly int VK_F10 = 0x79;
        private static readonly int VK_F11 = 0x7A;
        private static readonly int VK_F12 = 0x7B;
        //private static readonly int VK_SHIFT = 0x10;
        //private static readonly int VK_CTRL = 0x11;
        private static readonly int VK_AUTOPILOT = 0x41;

        private static readonly int VK_E = 0x45;
        private static readonly int VK_O = 0x4F;
        private static readonly int VK_I = 0x49;
        private static readonly int VK_M = 0x4D;

        private bool bAutoPilot;
        private bool bReplay;

        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        public bool bIsRunning = false;
        const string userRoot = "HKEY_CURRENT_USER";
        const string subkey = "CondorGimball";
        const string keyName = userRoot + "\\" + subkey;

        private SolidBrush brushBrown = new SolidBrush(Color.FromArgb(147, 78, 0));
        private SolidBrush brushDarkBrown = new SolidBrush(Color.FromArgb(117, 58, 0));
        private SolidBrush brushBlue = new SolidBrush(Color.FromArgb(42, 94, 190));
        private SolidBrush brushWhite = new SolidBrush(Color.White);
        private SolidBrush brushYellow = new SolidBrush(Color.Yellow);
        private SolidBrush brushBlack = new SolidBrush(Color.Black);
        private SolidBrush brushGreen = new SolidBrush(Color.Green);
        private SolidBrush brushCyan = new SolidBrush(Color.Cyan);
        private SolidBrush brushRed = new SolidBrush(Color.Red);
        private SolidBrush brushDarkGray = new SolidBrush(Color.DarkGray);

        private Pen penWhite = new Pen(Color.White, 3);
        private Pen penBlue = new Pen(Color.Blue, 3);
        private Pen penRed = new Pen(Color.Red, 3);
        private Pen penGreen = new Pen(Color.LightGreen, 5);
        private Pen penYellow = new Pen(Color.Yellow, 6);
        private Pen penYellowThin = new Pen(Color.Yellow, 3);
        private Pen penWhiteThin = new Pen(Color.White, 2);
        private Pen penBlackThin = new Pen(Color.Black, 1);
        private Pen penGrayThin = new Pen(Color.Gray, 1);
        private Pen penBlack = new Pen(Color.Black, 2);
        private Pen penRedThin = new Pen(Color.Red, 1);

        private Pen penRedTrans = new Pen(Color.FromArgb(130, 255, 0, 0), 5);
        private Pen penBlueTrans = new Pen(Color.FromArgb(130, 0, 0, 255), 5);

        private Font myFont15 = new Font("Arial", 15);
        private Font myFont25 = new Font("Arial", 25);

        private FlightPath myFlightPath = new FlightPath();

        private int nSecond;
        private int nTickPhotos;
        private int nTickAutoPilot;
        private FlightPathPoint flightPoint = new FlightPathPoint();

        private string m_strMyIPAddress = "";

        private MapCondor myMapOverView = new MapCondor();
        private MapCondor myMapDetailed = new MapCondor();


        private PointF ptSmoothedWindDir = new PointF(0, 0);

        private FlightPlanData myFlightPlanData = new FlightPlanData();

        private ReadLocation myReadLocation = new ReadLocation();

        private Random myRandom = new Random();

        private CondorUDPData myCondorUDPData = new CondorUDPData();
        private List<CondorUDPData> myCondorUDPDataLog = new List<CondorUDPData>();
        private int myCondorUDPDataLogPos = 0;

        private AutoPilot myAutoPilot = new AutoPilot();
        private ThermalLocation myThermalLocation = new ThermalLocation();

        private Airports myAirports = new Airports();

        private Thread myThreadListener;// = new Thread(myListener.StartListener);
        Listener myListener = new Listener();

        private Thread myThreadForControlViewer;
        ControlViewerThread myControlViewerThread = new ControlViewerThread();

        private YoloData myYoloData = new YoloData();
        private Thread myThreadForYoloViewer;
        YoloViewerThread myYoloViewerThread = new YoloViewerThread();


        private Thread myThreadPlanner;

        private Thread myThreadDijkstra;
        private Dijkstra myDijkstra = new Dijkstra();

        private DijkstraPath myDijkstraPath = new DijkstraPath();
        private FlightPathPoint myDijkstraFlightTarget = new FlightPathPoint();
        private double myDijkstraFlightRange = 0;

        private WaveFinder myWaveFinder = new WaveFinder();

        private double dDeltaTimeMax = 0;

        private TerrainMap myTerrainMap = new TerrainMap();
        private int myStartBuffer = 200;

        private int iCallF2 = 5985;

        //        private int iShowFunctionTerrain = 0;

        

        float m_dVxOld, m_dVyOld;
        double m_dTimePast;
        double m_dTimeGameStart;
        string m_strTime;

        float m_dWindX, m_dWindY;

        bool m_bJustStarted = true;

        bool m_bNeedInitFlightPlan = true;


        private void SetFlightPlan()
        {
            string strFile;
            if (!m_bNeedInitFlightPlan)
                return;
            Properties.Settings.Default.InfoWindow = "";

            strFile = Properties.Settings.Default.CondorFlightPlanPath + @"\" + Properties.Settings.Default.FlightPlan;
            if (File.Exists(strFile))
            {
                buttonGetFlightPlan.Text = Properties.Settings.Default.FlightPlan;
                buttonGetFlightPlan.Refresh();

                myFlightPlanData.GetLandscape(strFile); // needed to get landscape to init TerrainMap

                if (myTerrainMap.InitMap(myFlightPlanData, textBoxInfo))
                    textBoxInfo.Text = "Success";
                else
                    textBoxInfo.Text = "Failed";

                myFlightPlanData.Init(strFile, textBoxInfo); // continue to read landscape after Navicon64 is initiated in Terrainmap

                var dt = DateTime.Now;
                myTerrainMap.InitFlightPathRange(25000, myFlightPlanData, textBoxInfo);
                var ts = DateTime.Now - dt;
                var myProcessTime = ts.TotalMilliseconds;


                myMapOverView.AddFlightPlan(myFlightPlanData, true);
                myMapDetailed.AddFlightPlan(myFlightPlanData, false);

                myTerrainMap.CalculateDistanceForFinalApproach(ref myFlightPlanData);
                //FillYoloData(TerrainMap terrainMap, FlightPlanData flightPlanData)
                //myTerrainMap.ProcessTerrainMap(myFlightPlanData, textBoxInfo);

                Refresh();

                if (myAirports.Init(myFlightPlanData))
                {
                    if (myAirports.SetCurrentAirport(myFlightPlanData.Markers.Last().getPoint(), myFlightPlanData.vecWind)){
                        if (myAirports.CurrentAirport.GetVector().dotProduct(myFlightPlanData.vecWind) > 0)
                            myAirports.CurrentAirport.Orientation += 180;

                        myMapDetailed.AddAirport(myAirports);
                    }
                }
                myMapDetailed.SetWindDirection(myAirports.CurrentAirport.X, myAirports.CurrentAirport.Y,
                    myFlightPlanData.vecWind.x, myFlightPlanData.vecWind.y);


                myMapDetailed.AddTerrainMap(myTerrainMap);

                float dLan, dLon;
                if (myFlightPlanData.GetMarkerLocation(0, out dLan, out dLon))
                {
                    myMapDetailed.SetLocationLatLong(dLan, dLon);
                    Properties.Settings.Default.InitStartLat = dLan;
                    Properties.Settings.Default.InitStartLon = dLon;
                }

                float dX, dY, dZ;
                if (myFlightPlanData.GetMarkerLocationXYZ(0, out dX, out dY, out dZ))
                {
                    flightPoint.x = dX;
                    flightPoint.y = dY;
                    myTerrainMap.GetHeight(flightPoint.getPosition(), out flightPoint.z);
                    flightPoint.z += 150;
                    flightPoint.vario = 0;
                    myFlightPath.Init(flightPoint);
                };

                myDijkstra.PrepareTerrainMap(myFlightPlanData, myTerrainMap);


                {
                    Dijkstra dijkstraPrePlanner = new Dijkstra();
                    dijkstraPrePlanner.PrepareTerrainMap(myFlightPlanData, myTerrainMap, false);
                    //dijkstraPrePlanner.MakePlannerPaths();
                    myThreadPlanner = new Thread(dijkstraPrePlanner.MakePlannerPaths);
                    myThreadPlanner.Start();
                }


                m_bNeedInitFlightPlan = false;
            }
            else
            {
                buttonGetFlightPlan.Text = "Select Flightplan";
            }
        }
        private void buttonGetFlightPlan_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "fpl files (*.fpl,*.blg)|*.fpl;*.blg";
            dialog.InitialDirectory = Properties.Settings.Default.CondorFlightPlanPath;
            dialog.Title = "Please select a file plan.";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string strFlightPlan = Path.GetFileName(dialog.FileName);
                Properties.Settings.Default.CondorFlightPlanPath = Path.GetDirectoryName(dialog.FileName);
                Properties.Settings.Default.FlightPlan = strFlightPlan;
                m_bNeedInitFlightPlan = true;
                buttonGetFlightPlan.Text = Properties.Settings.Default.FlightPlan;

                //SetFlightPlan();
            }

        }


        private PointF addVectors(PointF pt, float dX, SizeF vecX, float dY, SizeF vecY)
        {
            SizeF vecXL = new SizeF(dX * vecX.Width, dX * vecX.Height);
            SizeF vecYL = new SizeF(dY * vecY.Width, dY * vecY.Height);

            PointF ptOut = PointF.Add(PointF.Add(pt, vecXL), vecYL);
            return ptOut;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.YoloTop = myYoloViewerThread.myYoloViewer.Top;
            Properties.Settings.Default.YoloLeft = myYoloViewerThread.myYoloViewer.Left;
            myYoloViewerThread.ThreadStop();

            Properties.Settings.Default.ControlTop = myControlViewerThread.myControlViewer.Top;
            Properties.Settings.Default.ControlLeft = myControlViewerThread.myControlViewer.Left;
            myControlViewerThread.ThreadStop();

            myDijkstra.ThreadStop();
            if (myThreadDijkstra != null && myThreadDijkstra.ThreadState == System.Threading.ThreadState.Running)
                myThreadDijkstra.Abort();


            if (myThreadPlanner != null && myThreadPlanner.ThreadState == System.Threading.ThreadState.Running)
                myThreadPlanner.Abort();

            Properties.Settings.Default.FormWidth = this.Width;
            Properties.Settings.Default.FormHeight = this.Height;
            Properties.Settings.Default.FormTop = this.Top;
            Properties.Settings.Default.FormLeft = this.Left;
            Properties.Settings.Default.FormWindowState = (int)this.WindowState;

            Properties.Settings.Default.Save();
            if (!bReplay && myCondorUDPDataLog.Count() > 120)
            {
                string strFile = Properties.Settings.Default.CondorFlightPlanPath + @"\" + Properties.Settings.Default.FlightPlan;
                strFile = strFile.Replace(".fpl", DateTime.Now.ToString("_yyyyMMdd_HHmm") + ".blg");
                string message = "Save Autopilot log data?\n" + strFile;
                string title = "Close Condor Autopilot";
                MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                DialogResult result = MessageBox.Show(message, title, buttons);
                if (result == DialogResult.Yes)
                {
                    CondorUDPData.WriteLog(strFile, Properties.Settings.Default.FlightPlan, myCondorUDPDataLog);
                }
            }

        }

        public Form1()
        {
            Properties.Settings.Default.InfoWindow = "";
            bReplay = false;
            bIsRunning = false;
            myCondorUDPData = new CondorUDPData();
            m_dVxOld = 0;
            m_dVyOld = 0;

            m_strMyIPAddress = "127.0.0.1";
            InitializeComponent();


            myThreadDijkstra = new Thread(myDijkstra.ThreadRun);
            myThreadDijkstra.Priority = ThreadPriority.Highest;
            myThreadDijkstra.Start();


            penWhite.DashCap = System.Drawing.Drawing2D.DashCap.Round;
            penBlue.DashCap = System.Drawing.Drawing2D.DashCap.Round;
            penRed.DashCap = System.Drawing.Drawing2D.DashCap.Round;
            penYellow.DashCap = System.Drawing.Drawing2D.DashCap.Round;
            penYellowThin.DashCap = System.Drawing.Drawing2D.DashCap.Round;
            penWhiteThin.DashCap = System.Drawing.Drawing2D.DashCap.Round;

            nSecond = 0;
            nTickPhotos = Properties.Settings.Default.NewPhotoDelay*600;
            myCondorUDPData.time = 0;
            m_dTimePast = 0;
            m_dTimeGameStart = 0;

            nTickAutoPilot = 0;

            myMapOverView.Init(ref gMapSmall, GMapProviders.GoogleTerrainMap, 8);
            myMapDetailed.Init(ref gMapLarge, GMapProviders.GoogleTerrainMap, 12);

        
            m_dWindX = m_dWindY = 0;

            bAutoPilot = false;

            myReadLocation.Init();
            buttonGetFlightPlan.Text = Properties.Settings.Default.FlightPlan;

            myWaveFinder.Init(myFlightPath);
        }

        private void buttonPlanner_Click(object sender, EventArgs e)
        {
            myDijkstra.ThreadStop();
            timer1.Stop();
            Planning formPlanning = new Planning();
            formPlanning.Init(myFlightPlanData, myTerrainMap, myCondorUDPDataLog);
            formPlanning.ShowDialog();
        }

        private void myMouseClick(object sender, MouseEventArgs e)
        {
        }

        private void CheckCondorPath()
        {
            if (!File.Exists(Properties.Settings.Default.CondorApplicationPath + @"\navicon.dll"))
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Filter = "fpl files (*.dll)|*.dll";
                dialog.InitialDirectory = @"C:\";
                dialog.Title = "Locate navicon.dll file in condor-application location.";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    Properties.Settings.Default.CondorApplicationPath = Path.GetDirectoryName(dialog.FileName);
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBoxIP.Text = m_strMyIPAddress;

            this.Width = Properties.Settings.Default.FormWidth;
            this.Height = Properties.Settings.Default.FormHeight;
            this.Top = Properties.Settings.Default.FormTop;
            this.Left = Properties.Settings.Default.FormLeft;
            this.WindowState = (FormWindowState)Properties.Settings.Default.FormWindowState;


            CheckCondorPath();
        }

 
        private void buttonStartStop_Click(object sender, EventArgs e)
        {
            if (bIsRunning)
            {
                Stop();
            }
            else
            {
                Start();
            }

        }

        private void Stop()
        {
            bIsRunning = false;
            Registry.SetValue(keyName, "Run", bIsRunning, RegistryValueKind.QWord);
            timer1.Stop();
            buttonStartStop.Text = "Run";
            myThreadListener.Abort();
        }
        private void Start()
        {
            myControlViewerThread.Init(myCondorUDPDataLog);
            myThreadForControlViewer = new Thread(myControlViewerThread.Load);
            myThreadForControlViewer.Start();

            myYoloViewerThread.Init(myListener.condorUDPData, myYoloData);
            myThreadForYoloViewer = new Thread(myYoloViewerThread.Load);
            myThreadForYoloViewer.Start();


            Properties.Settings.Default.InfoWindowRun = true;
            Thread myThreadForInfoWindow;
            InfoWindowThread myInfoWindowThread = new InfoWindowThread();
            myThreadForInfoWindow = new Thread(myInfoWindowThread.Load);
            myThreadForInfoWindow.Start();


            bReplay = false; ;
            if (Properties.Settings.Default.FlightPlan.Substring(Properties.Settings.Default.FlightPlan.Length-3) == "blg")
            {
                string strFile = Properties.Settings.Default.CondorFlightPlanPath + @"\" + Properties.Settings.Default.FlightPlan;
                if (CondorUDPData.ReadLog(strFile, out string strFlightPlan, myCondorUDPDataLog))
                {
                    Properties.Settings.Default.FlightPlan = strFlightPlan;
                    bReplay = true;
                }
                else
                {
                    textBoxInfo.Text = "Replay error";
                    return;
                }

            }
            SetFlightPlan();
            bIsRunning = true;
            nSecond = 0;
            nTickPhotos = Properties.Settings.Default.NewPhotoDelay * 1800;
            Registry.SetValue(keyName, "Run", bIsRunning, RegistryValueKind.QWord);




            Properties.Settings.Default.InfoWindowRun = false;
            //myThreadForInfoWindow.ThreadStop();

            myThreadListener = new Thread(myListener.StartListener);
            myThreadListener.Start();

            myControlViewerThread.Run();
            myYoloViewerThread.Run();

            timer1.Start();
            buttonStartStop.Text = "Stop";
            
            if (bReplay)
            {
                textBoxInfo.Text = "Running Log";
            }
            else
            {
                textBoxInfo.Text = "Running";
            }

            switch (myFlightPlanData.StartType)
            {
                case 0:
                    {
                        myAutoPilot.ModeStatus = Mode.InTow;
                        break;
                    }
                case 1:
                    {
                        myAutoPilot.ModeStatus = Mode.InTow;
                        break;
                    }
                case 2:
                    {
                        myAutoPilot.ModeStatus = Mode.Normal;
                        break;
                    }
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            bool bVK_F2 = Utils.KeyPressed(VK_F2);
            bool bVK_F3 = Utils.KeyPressed(VK_F3);
            bool bVK_F9 = Utils.KeyPressed(VK_F9);
            bool bVK_F10 = Utils.KeyPressed(VK_F10);
            bool bVK_F11 = Utils.KeyPressed(VK_F11);
            bool bVK_F12 = Utils.KeyPressed(VK_F12);
            bool bVK_E = Utils.KeyPressed(VK_E);
            bool bVK_O = Utils.KeyPressed(VK_O);
            bool bVK_I = Utils.KeyPressed(VK_I);
            bool bVK_M = Utils.KeyPressed(VK_M);
            bool bVK_AutoPilot = Utils.KeyPressed(VK_AUTOPILOT);

            if (nSecond == 5)
            {
                Process currentProcess = Process.GetCurrentProcess();
                double dMB = 0.000001 * currentProcess.PrivateMemorySize64;
                textBoxMemory.Text = dMB.ToString("F2") + " MB. " + myTerrainMap.NumberOfTRNMatrices().ToString();
                if (dMB > 5000)
                    GC.Collect();

                //Thread threadPlanner = new Thread(myDijkstra.MakePlannerPathNext);
                //threadPlanner.Start();

            }

            if (bAutoPilot)
            {
                iCallF2++;
                if (iCallF2 > 6000 && myAutoPilot.InStateOfAvoidance == 0)// call every 10 min 
                {
                    myAutoPilot.PushKey(VirtualKeyCode.F2);
                    myAutoPilot.Wait(200);
                    iCallF2 = 0;
                    bVK_F2 = true;
                }
                if (myTerrainMap.bRidge)
                    iCallF2 += 2;

                if (myCondorUDPData.water == 0 && (m_dTimePast - m_dTimeGameStart < 0.01))
                {
                    textBoxInfo.BackColor = Color.Red;
                    textBoxInfo.Text = "No Water";
                }
                else
                {
                    textBoxInfo.BackColor = Color.DarkOrange;
                }
                textBoxInfo.ForeColor = Color.Black;
                //if (bReplay)
                //    textBoxInfo.Text = "Replay Autopilot";
                //else
                //    textBoxInfo.Text = "Autopilot";
            }
            else
            {
                textBoxInfo.BackColor = Color.Green;
                textBoxInfo.ForeColor = Color.Black;
                if (bReplay)
                    textBoxInfo.Text = "Replay";
                else
                    textBoxInfo.Text = "Manual";
            }

            if (bVK_F9)
            {
                myAutoPilot.Debug = !myAutoPilot.Debug;
            }


            if (bVK_E)
            {
                myThermalLocation.StopThermal();
            }

            if (bVK_M)
            {
                myAutoPilot.Pause(1500);
            }

            if (bVK_AutoPilot)
            {
                bAutoPilot = !bAutoPilot;
                if (bAutoPilot)
                {
                    textBoxInfo.Text = "AutoPilot";
                    myAutoPilot.SetSpeed(215);
                    myAutoPilot.Wait(500);
                }
                else
                {
                    textBoxInfo.Text = "";
                    myAutoPilot.Clear();
                }
            }

            if (bVK_I)
            {
                if (bAutoPilot)
                {
                    myAutoPilot.SetSpeed(myAutoPilot.AirSpeedKmH - 5);
                }
                else
                {
                    myFlightPlanData.SetNextWayPoint(-1);
                }
            }

            if (bVK_O)
            {
                if (bAutoPilot)
                {
                    myAutoPilot.SetSpeed(myAutoPilot.AirSpeedKmH + 5);
                }
                else
                {
                    myFlightPlanData.SetNextWayPoint(1);
                }
            }

            if (bVK_I || bVK_O)
            {
                if (bAutoPilot)
                {
                    richTextBox.AppendText(Environment.NewLine + "Target Speed Up: " + myAutoPilot.AirSpeedKmH.ToString() + " km/h");
                }
                else
                {
                    richTextBox.AppendText(Environment.NewLine + "new WayPoint: " + myFlightPlanData.GetMarkerText());

                }
                richTextBox.ScrollToCaret();
            }

            if (bVK_F10)
            {
                myMapDetailed.IncreaseZoom(false);
                myMapDetailed.AddAirport(myAirports);
            }
            if (bVK_F11)
            {
                myMapDetailed.IncreaseZoom(true);
                myMapDetailed.AddAirport(myAirports);
            }

            if (bVK_F12)
            {
                Properties.Settings.Default.SaveYoloBitmap = true;
            }

            if (bVK_F2)// || bVK_F3)
            {
                float x, y;

                if (myReadLocation.GetLocation(out float dLat, out float dLon))
                {
                    Form1.naviCon64.calcUTMIter(dLat, dLon, out x, out y);
                    double dCorrection;
                    dCorrection = Math.Sqrt(Math.Pow(flightPoint.x - x, 2) + Math.Pow(flightPoint.y - y, 2));

                    if (dCorrection < 10000 || bReplay)
                    {
                        flightPoint.x = x;
                        flightPoint.y = y;
                    }
                    else
                    {
                        richTextBox.AppendText(Environment.NewLine + "F2: FAILED");
                    }


                    dCorrection = Math.Round(dCorrection, 1);
                    richTextBox.AppendText(Environment.NewLine + "F2: " + myReadLocation.my_strResult);
                    richTextBox.AppendText(Environment.NewLine + "Correction: " + dCorrection.ToString() + "m");
                }

                if (bReplay)
                {

                    if (myReadLocation.GetAltitude(out float dAltitude))
                    {

                        richTextBox.AppendText(Environment.NewLine + "F2: " + myReadLocation.my_strResult);
                        richTextBox.AppendText(Environment.NewLine + "Altitude: " + dAltitude.ToString() + "m");

                        myCondorUDPDataLogPos = CondorUDPData.Locate(flightPoint.getPosition(), myCondorUDPDataLog);

                        if (myCondorUDPDataLogPos < 0)
                            myCondorUDPDataLogPos = 0;
                        richTextBox.AppendText(Environment.NewLine + "Log Pos: " + myCondorUDPDataLogPos.ToString());
                        richTextBox.AppendText(Environment.NewLine + "Log Height: " + myCondorUDPDataLog[myCondorUDPDataLogPos].altitude.ToString());


                    }
                }
                richTextBox.ScrollToCaret();

            }

            //richTextBox.AppendText(Environment.NewLine + "Message = " + myCondorUDPData.message);

            nTickPhotos++;
            int delayPhoto = 600;
            if (bAutoPilot)
                delayPhoto = 180;
            if (nTickPhotos > Properties.Settings.Default.NewPhotoDelay * delayPhoto) 
            {

                nTickPhotos = 0;
                float lat, lng;


                Form1.naviCon64.calcLatLon(myDijkstra.myPhotoLocation.X, myDijkstra.myPhotoLocation.Y, out lat, out lng);
  
                List<GoogleRequest.photo> photos = new List<GoogleRequest.photo>();
                if (GoogleRequest.GetPhotoList(lat, lng, ref photos))
                {
                    if (photos.Count > 0)
                    {

                        int i = myRandom.Next(photos.Count);
                        i = Math.Min(i, photos.Count - 1);

                        GoogleRequest.SetPhoto(pictureBoxPhoto, textBoxPhotoInfo, richTextBox, photos[i]);
                        myMapDetailed.SetPhotoLocation(photos[i].lat, photos[i].lng);
                    }
                }

                
            }
 

            nSecond++;
            if (nSecond == 10) nSecond = 0;
            if (nSecond == -1)
            {
                try
                {
                    Process process = Process.GetProcessesByName("Gimball")[0];
                    if (process != null)
                    {
                        process.WaitForInputIdle();
                        IntPtr s = process.MainWindowHandle;
                        SetForegroundWindow(s);
                    }
                }
                catch
                {
                    // nothing required, just pass
                }
            }

            if (bIsRunning)
            {


                if (nSecond == 0)
                {
                    if (myFlightPlanData.SetNextWayPoint(flightPoint.getPosition(), myCondorUDPData.time))
                    {
                        myTerrainMap.dSideOffsetForUpDraft = 0;
                    }

                    if (bReplay)
                    {
                        myCondorUDPDataLogPos = CondorUDPData.Locate(myListener.condorUDPData.time,
                            myCondorUDPDataLogPos, myCondorUDPDataLog);
                        if (myCondorUDPDataLogPos > -1)
                        {
                            myCondorUDPData.Interpolate(myListener.condorUDPData.time,
                            myCondorUDPDataLog[myCondorUDPDataLogPos],
                            myCondorUDPDataLog[myCondorUDPDataLogPos + 1]);

                            flightPoint.x = myCondorUDPData.x;
                            flightPoint.y = myCondorUDPData.y;
                            myFlightPlanData.nextWayPoint = myCondorUDPData.wayPoint;
                            myDijkstra.myFlightRange = myCondorUDPData.dijkstraRange;
                        }

                    }
                    else
                    {
                        myCondorUDPData.myClone(myListener.condorUDPData);
                        myCondorUDPData.index = myCondorUDPDataLog.Count();
                        myCondorUDPData.x = flightPoint.x;
                        myCondorUDPData.y = flightPoint.y;
                        myCondorUDPData.wayPoint = myFlightPlanData.nextWayPoint;
                        myCondorUDPData.thermal = Convert.ToInt32(myThermalLocation.bInThermal);
                        myCondorUDPData.thermalPoint = new Point3d(myThermalLocation.thermalPoint.getPosition());
                        myCondorUDPData.dijkstraRange = Convert.ToSingle(myDijkstra.myFlightRange);
                        myCondorUDPData.targetSpeed = myAutoPilot.TargetSpeed;
                        myCondorUDPData.targetRange = Convert.ToSingle(myThermalLocation.dRangeRequired);
                        myCondorUDPData.range = Convert.ToSingle(myDijkstra.myFlightRange);
                        myCondorUDPData.targetHeading = myAutoPilot.TargetHeading;
                        myCondorUDPData.stickPitch = myAutoPilot.stickPitch;
                        myCondorUDPData.stickRoll = myAutoPilot.stickRoll;


                        CondorUDPData newCondorUDPData = new CondorUDPData(myCondorUDPData);
                        myCondorUDPDataLog.Add(newCondorUDPData);
                    }
                }
                if (!bReplay)
                {
                    myCondorUDPData = myListener.condorUDPData;

                    if (myAutoPilot.ModeStatus == Mode.Normal &&  myFlightPath.HasCrashed())
                    {
                        myAutoPilot.PushKey(VirtualKeyCode.VK_P);
                        textBoxInfo.Text = "Crashed";
                        textBoxInfo.Refresh();
                        Stop();
                    }
                }


                if (myStartBuffer > 0)
                {
                    myCondorUDPData.vario = 0;
                    myStartBuffer--;
                }
                var hours = Math.Floor(myCondorUDPData.time);
                var mins = Math.Floor(60 * (myCondorUDPData.time - hours));
                if (hours > 24)
                    hours -= 24;
                string strMin = mins.ToString();
                if (mins < 10)
                    strMin = "0" + strMin;
                m_strTime = hours.ToString() + ":" + strMin;

                if (m_bJustStarted)
                {
                    m_bJustStarted = false;
                    m_dTimePast = myCondorUDPData.time;
                }
                 float dSpeed = Convert.ToSingle(Math.Round(Math.Sqrt(myCondorUDPData.vx * myCondorUDPData.vx +
                    myCondorUDPData.vy * myCondorUDPData.vy) * 3.6));

                integratePosition();
                updateGimBall();
                updateTopView();

            }



            nTickAutoPilot++;
            if (nTickAutoPilot == 10) nTickAutoPilot = 0;

            if (nTickAutoPilot == 0)
            {
                myThermalLocation.Calc(myFlightPath, myTerrainMap, myFlightPlanData, myDijkstra);
                if (myAutoPilot.InLanding() || (myFlightPlanData.RemainingDistance + 15000) < myDijkstra.myFlightRange)
                {
                    myThermalLocation.bInThermal = false;
                }

                myTerrainMap.CheckForRidge_V2(flightPoint);
                if (myTerrainMap.bRidge && myThermalLocation.bInThermal)
                    myTerrainMap.CalcRidgeDirection(flightPoint, myFlightPlanData.vecWind);


                myWaveFinder.Calc(myTerrainMap.bRidge || myThermalLocation.bInThermal);

                if (myAutoPilot.InLanding())
                {
                    myDijkstra.ThreadSetData(flightPoint, new FlightPathPoint(myAirports.CurrentAirport.GetTarget(flightPoint.getPosition())), flightPoint.speed * 3, false);
                    //myDijkstra.myDijkstraPath.PathNodes.Clear();

                }
                else if (myThermalLocation.bInThermal)
                {
                    if (myFlightPlanData.GetClosestPositionOnPath(flightPoint, 25000, out FlightPathPoint ptOnPath))
                        myDijkstra.ThreadSetData(flightPoint, ptOnPath, myThermalLocation.thermalTargetPoint, flightPoint.speed * 4, true);
                    else
                        myDijkstra.ThreadSetData(flightPoint, myFlightPlanData.getNextMarkerPosition(true), myThermalLocation.thermalTargetPoint, flightPoint.speed * 4, true);
                }
                else if (myTerrainMap.bRidge)
                {
                    if (myFlightPlanData.GetClosestPositionOnPath(flightPoint, 25000, out FlightPathPoint ptOnPath))
                    {
                        myDijkstra.ThreadSetData(flightPoint, ptOnPath, flightPoint.speed * 4, false);
                        myDijkstra.ThreadSetData(flightPoint, ptOnPath, flightPoint.speed * 4, false);
                    }
                    else
                    {    
                        myDijkstra.ThreadSetData(flightPoint, myFlightPlanData.getNextMarkerPosition(true), flightPoint.speed * 4, false);
                    }
                }
                else
                {
                    if (myFlightPlanData.GetClosestPositionOnPath(flightPoint, 50000, out FlightPathPoint ptOnPath))
                    {
                        myDijkstra.ThreadSetData(flightPoint, ptOnPath, flightPoint.speed * 5, false);
                    }
                    else
                    {
                        myDijkstra.ThreadSetData(flightPoint, myFlightPlanData.getNextMarkerPosition(true), flightPoint.speed * 5, false);
                    }

                }
            }

            if (nTickAutoPilot == 9 && bAutoPilot)
            {
                if (myDijkstra.myThreadStatus == ThreadStatus.kDataReady)
                {
                    myDijkstra.ThreadGetData(myDijkstraPath, myDijkstraFlightTarget, ref myDijkstraFlightRange);
                    myMapDetailed.AddDijkstraPath(myDijkstra, !myAutoPilot.InLanding());
                }
                //                myDijkstra.myTarget.ExtrapolatedTarget(flightPoint, myDijkstra.myPtFlightTarget, myDijkstraFlightTarget);
                float dStep = flightPoint.distanceTo(myDijkstra.myPtFlightTarget) - myDijkstra.myFlightTargetInitialDistance;
                if (dStep < 0)
                    dStep = 0;
                myDijkstraFlightTarget.Add(myDijkstra.myPtFlightTarget, myDijkstra.myFlightTargetDir, dStep, 0);


                myMapDetailed.AddLocalTerrainMap(myTerrainMap, flightPoint, true);




                myAutoPilot.InReplay = bReplay;

                myTerrainMap.GetHeight(flightPoint.x, flightPoint.y, out float dTerrainHeight);



                bool bIsStillBusy = myFlightPlanData.GetSafeWayPointLocation(myFlightPlanData.nextWayPoint,
                                       flightPoint.x, flightPoint.y,
                                       out float x, out float y, out float z);


                myFlightPlanData.CalcRemainingDistance(flightPoint.x, flightPoint.y);

                myTerrainMap.IsInFinalApproach = (myFlightPlanData.RemainingDistance + 15000) < myDijkstra.myFlightRange;
                if (myFlightPlanData.RemainingDistance < 5000 && myFlightPlanData.RemainingDistance> 4500)
                {
                    myAutoPilot.StartDumpingWater(myCondorUDPData);
                }

                if (myAutoPilot.Debug)
                {
                    int i = 0;
                }
                if (bIsStillBusy
                    && myThermalLocation.bInThermal
                    && ((myFlightPlanData.RemainingDistance + 15000)  > myDijkstra.myFlightRange)
                    )
                {
                    myTerrainMap.UpDraftTarget = myDijkstraFlightTarget;
                    if (myTerrainMap.bRidge)
                    {
                        myAutoPilot.SetTarget(myDijkstraFlightTarget.x, myDijkstraFlightTarget.y);
                    }
                    else
                    {
                        myAutoPilot.SetTarget(myThermalLocation.thermalTargetPoint.x, myThermalLocation.thermalTargetPoint.y);
                    }
                    myAutoPilot.Calc(flightPoint, myCondorUDPData, myThermalLocation, 
                        myTerrainMap, myFlightPlanData, myDijkstra, myAirports.CurrentAirport, myYoloData, richTextBox);
                    if (myThermalLocation.bVeryStrongThermal)
                        textBoxInfo.Text = "Very Strong Thermal";
                    else if (myThermalLocation.bStrongThermal)
                        textBoxInfo.Text = "Strong Thermal";
                    else if (myThermalLocation.bNormalThermal)
                        textBoxInfo.Text = "Thermal";
                    else if (myThermalLocation.bWeakThermal)
                        textBoxInfo.Text = "Weak Thermal";

                }
                else if (bIsStillBusy)
                {
                    if (myFlightPlanData.GetDistanceToNextWayPoint(flightPoint) > 4000 )
                    {
                        if (myDijkstraPath.Ready && myDijkstraFlightTarget != null)
                        {
                            if (myThermalLocation.bInThermal && !myTerrainMap.bRidge
                                && ((myFlightPlanData.RemainingDistance + 15000) > myDijkstra.myFlightRange)
                                )
                            {
                                myAutoPilot.SetTarget(myThermalLocation.thermalTargetPoint.x, myThermalLocation.thermalTargetPoint.y);
                            }
                            else
                            {
                                Point3d ptTarget = myDijkstraFlightTarget.getPosition();
                                myAutoPilot.SetTarget(ptTarget.X, ptTarget.Y);
                            }

                        }
                    }
                    else if (myFlightPlanData.GetSafeWayPointLocation(myFlightPlanData.nextWayPoint, flightPoint.x, flightPoint.y, out float dXT, out float dYT, out float dZT))
                        myAutoPilot.SetTarget(dXT, dYT);
                    else
                        myAutoPilot.SetTarget(x, y);

                    if (myTerrainMap.IsInFinalApproach)
                        textBoxInfo.Text = "Final Approach";
                    else
                        textBoxInfo.Text = "Target";
                    myThermalLocation.bInThermal = false;
                    myAutoPilot.Calc(flightPoint, myCondorUDPData, myThermalLocation, myTerrainMap, myFlightPlanData, myDijkstra, myAirports.CurrentAirport, myYoloData, richTextBox);
                }
                else
                {
                    if (flightPoint.speed < 3)
                    {
                        myAutoPilot.CloseBrakes();
                        myAutoPilot.Pause(3000);
                        textBoxInfo.Text = "Landed";
                        Stop();
                    }
                    else if (flightPoint.speed < 15)
                    {
                        myAutoPilot.CloseBrakes();
                    }
                    else
                    {
                        Point3d ptTarget = myAirports.CurrentAirport.GetTarget(flightPoint.getPosition());

                        if (false && myAirports.CurrentAirport.PreLanding)
                            myAutoPilot.SetTarget(myDijkstraFlightTarget.x, myDijkstraFlightTarget.y);
                        else
                            myAutoPilot.SetTarget(ptTarget.X, ptTarget.Y);

                        myAutoPilot.SetTargetAltitude(ptTarget.Z, myCondorUDPData);
                        textBoxInfo.Text = "Landing";
                        myThermalLocation.bInThermal = false;
                        myAutoPilot.Calc(flightPoint, myCondorUDPData, myThermalLocation, myTerrainMap, myFlightPlanData, myDijkstra, myAirports.CurrentAirport, myYoloData, richTextBox);

                    }

                }
                if (myDijkstra.myIsEscape)
                {
                    textBoxInfo.Text += ", Escape Mode";
                }
                else if (myTerrainMap.bRidgeLeft)
                {
                    textBoxInfo.Text += ", Ridge on Left";
                }
                else if (myTerrainMap.bRidgeRight)
                {
                    textBoxInfo.Text += ", Ridge on Right";
                }

                if (myAutoPilot.Debug)
                    textBoxInfo.Text = "Debug";

                if (myAutoPilot.ModeStatus == Mode.InTow)
                {
                    textBoxInfo.Text = "Towing";
                }

                double dRange = 45000 + Utils.interpolate(myThermalLocation.varioMax, 5.5f, 0, 9, 100000);
                richTextBox1.Clear();
                double deltaTime = 0;
                if (myCondorUDPDataLog.Count() > 2)
                {
                    deltaTime = 3600 * (myCondorUDPDataLog[myCondorUDPDataLog.Count() - 1].time -
                        myCondorUDPDataLog[myCondorUDPDataLog.Count() - 2].time
                        );
                }
                if (deltaTime > dDeltaTimeMax)
                    dDeltaTimeMax = deltaTime;
                else
                    dDeltaTimeMax *= 0.995;

                richTextBox1.AppendText(Environment.NewLine
                    + "Delta Time: " + deltaTime.ToString("F2")
                    + "/" + dDeltaTimeMax.ToString("F2")
                    );
                richTextBox1.AppendText(Environment.NewLine
                    + "Range: " + myDijkstra.myFlightRange.ToString("F2")
                    + " / " + myThermalLocation.dRangeRequired.ToString("F2"));

                richTextBox1.AppendText(Environment.NewLine
                    + "RidgeCrossing: " + myDijkstra.myRidgeCrossingRange.ToString("F0")
                    + " ; " + myDijkstra.myRidgeCrossingRatio.ToString("F2")
                    + " ; " + myDijkstra.myRidgeCrossingGap.ToString("F0")
                    );


                richTextBox1.AppendText(Environment.NewLine
                    + "Lift forecast: " + myDijkstra.myLiftForecast.ToString("F2"));
                richTextBox1.AppendText(Environment.NewLine
                    + "Speed setting: " + myAutoPilot.SpeedSetting);

                Node node = new Node();
                myTerrainMap.GetSpecialNode(flightPoint.getPosition(), out node);
                richTextBox1.AppendText(Environment.NewLine
                    + "weight: " + node.weight.ToString("F3")
                    + " fu: " + node.function.ToString("F3")
                    + " fF: " + node.functionF.ToString("F3")
                    );
                richTextBox1.AppendText(Environment.NewLine
                    + "Water: " + myCondorUDPData.water.ToString("F2"));
                richTextBox1.AppendText(Environment.NewLine
                    + "VarioMax: " + myThermalLocation.varioMax.ToString("F2"));
                richTextBox1.AppendText(Environment.NewLine
                    + "QX: " + myAirports.CurrentAirport.QX.ToString("F2")
                    + "QY: " + myAirports.CurrentAirport.QY.ToString("F2")
                    + "QZ: " + myAirports.CurrentAirport.QZ.ToString("F2")
                    );
                if (myFlightPlanData.CurrentMarker() != null)
                {
                    richTextBox1.AppendText(Environment.NewLine
                        + "Marker #" + myFlightPlanData.nextWayPoint.ToString()
                        + ", C = " + myFlightPlanData.CurrentMarker().dijkstraPath.Count.ToString()
                        );
                }




            }
        }

        private void getWindArrow(PointF ptCenter, SizeF vecWX, SizeF vecWY, ref PointF[] curveWind)
        {
            //Condor does not correctly calculate TAS
            float dAirX, dAirY;

            double dAirHorizontal;
            dAirHorizontal = Math.Sqrt(Math.Abs(myCondorUDPData.tas * myCondorUDPData.tas - myCondorUDPData.vz * myCondorUDPData.vz));
            if (dAirHorizontal < 1)
                return;

            dAirX = -Convert.ToSingle(dAirHorizontal * Math.Sin(myCondorUDPData.yaw));
            dAirY = Convert.ToSingle(dAirHorizontal * Math.Cos(myCondorUDPData.yaw));

            m_dWindX = 0.05f * (myCondorUDPData.vx - dAirX) + 0.95f * m_dWindX;
            m_dWindY = 0.05f * (myCondorUDPData.vy - dAirY) + 0.95f * m_dWindY;
            float dWind =Convert.ToSingle(Math.Sqrt(m_dWindX * m_dWindX + m_dWindY * m_dWindY));

            m_dWindX = myFlightPlanData.vecWind.x * dWind;
            m_dWindY = myFlightPlanData.vecWind.y * dWind;

            float vX = m_dWindX / dWind;
            float vY = m_dWindY / dWind;

            float dL = 10 + 3 * dWind;

            PointF ptCenterT = new PointF(0,0);
            SizeF vecX = new SizeF(-vX, -vY);
            SizeF vecY = new SizeF(vY, -vX);

            PointF[] curveWindT = new PointF[7];
            curveWindT[0] = addVectors(ptCenterT, -0.5f * dL, vecX, -5, vecY);
            curveWindT[1] = addVectors(ptCenterT, -0.5f * dL, vecX, 5, vecY);
            curveWindT[2] = addVectors(ptCenterT, 0.3f * dL, vecX, 5, vecY);
            curveWindT[3] = addVectors(ptCenterT, 0.3f * dL, vecX, 10, vecY);
            curveWindT[4] = addVectors(ptCenterT, 0.5f * dL, vecX, 0, vecY);
            curveWindT[5] = addVectors(ptCenterT, 0.3f * dL, vecX, -10, vecY);
            curveWindT[6] = addVectors(ptCenterT, 0.3f * dL, vecX, -5, vecY);

            for (int i = 0; i < 7; i++)
            {
                curveWind[i] = addVectors(ptCenter, 
                    curveWindT[i].X, 
                    vecWX, curveWindT[i].Y, vecWY);
            }

        }

        private void integratePosition()
        {
            if (m_dTimePast == 0)
            {
                m_dTimeGameStart = myCondorUDPData.time;
                m_dTimePast = myCondorUDPData.time;
            }

            flightPoint.x += Convert.ToSingle((myCondorUDPData.vx + m_dVxOld) * 1800 * (myCondorUDPData.time - m_dTimePast));
            flightPoint.y += Convert.ToSingle((myCondorUDPData.vy + m_dVyOld) * 1800 * (myCondorUDPData.time - m_dTimePast));

            m_dTimePast = myCondorUDPData.time;


            m_dVxOld = myCondorUDPData.vx;
            m_dVyOld = myCondorUDPData.vy;

            if (myCondorUDPData.altitude > 0)
                flightPoint.z = myCondorUDPData.altitude;
            else
                myTerrainMap.GetHeight(flightPoint.x, flightPoint.y, out flightPoint.z);

            if (m_dTimePast - m_dTimeGameStart < 0.005)
                flightPoint.vario = 0;
            else 
                flightPoint.vario = myCondorUDPData.vario;

            flightPoint.vec = new Vector3d(myCondorUDPData.vx, myCondorUDPData.vy, 0);
            flightPoint.speed = flightPoint.vec.length();
            flightPoint.vec.normalize();
            flightPoint.timestamp = myCondorUDPData.time;
            flightPoint.windx = m_dWindX;
            flightPoint.windy = m_dWindY;



            if (nSecond == 0)
            {
                myMapDetailed.SetPlanePosition(flightPoint.x, flightPoint.y, myCondorUDPData.vx, myCondorUDPData.vy, true);
                myMapDetailed.SetWindDirection(flightPoint.x, flightPoint.y, m_dWindX, m_dWindY);
                myMapDetailed.SetTarget(myAutoPilot.GetTarget());
       //         myMapDetailed.SetThermalTargets(myTerrainMap.UpDraftTarget, myTerrainMap.UpDraftPotentialTarget, myTerrainMap.HasUpDraftPotentialTarget);
                myFlightPath.Add(flightPoint);
                myMapOverView.SetPlanePosition(flightPoint.x, flightPoint.y, 0, 0, false);
            }
        }

        private void updateGimBall()
        {

            float dXBank = Convert.ToSingle(Math.Cos(myCondorUDPData.bank));
            float dYBank = Convert.ToSingle(Math.Sin(myCondorUDPData.bank));


            Bitmap bitmapGimBall = new Bitmap (pictureGimBall.Width, pictureGimBall.Height);
            Graphics gfx = Graphics.FromImage(bitmapGimBall);

            gfx.SmoothingMode =
                    System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            gfx.FillRectangle(brushBrown, 0, 0, pictureGimBall.Width, pictureGimBall.Height);
            gfx.FillRectangle(brushBlue, 0, 0, pictureGimBall.Width, pictureGimBall.Height/2);

            float dXStart = 510;
            float dYStart = 230 - 8 * Utils.rad2deg(myCondorUDPData.pitch);

 
            PointF ptStart = new PointF(dXStart, dYStart);
            PointF ptStartW = new PointF(510, 230);
            SizeF vecX = new SizeF(dXBank, -dYBank);
            SizeF vecY = new SizeF(dYBank, dXBank);
            SizeF vecWX = new SizeF(1, 0);
            SizeF vecWY = new SizeF(0, 1);

            PointF pt = new PointF();

            Vector3d vecView = new Vector3d(Convert.ToSingle(-Math.Sin(myCondorUDPData.yaw)), Convert.ToSingle(Math.Cos(myCondorUDPData.yaw)), 0);

            Viewer viewer = new Viewer(
                flightPoint.getPosition(),
                vecView,
                5, 5, 15);

            float dSurfaceElevation;
            float dMapOffset;
            if (myTerrainMap.GetHeight(flightPoint.x, flightPoint.y, out dSurfaceElevation))
            {
                dMapOffset = 2 * (flightPoint.z - dSurfaceElevation) + 200;
            }
            else
            {
                dMapOffset = 800;
            }
            float dX = flightPoint.getPosition().X + dMapOffset * flightPoint.vec.x;
            float dY = flightPoint.getPosition().Y + dMapOffset * flightPoint.vec.y;
            List<Tuple<double, Point3d[]>> localHeightMap = new List<Tuple<double, Point3d[]>>();
            localHeightMap = myTerrainMap.getArea(dX, dY, flightPoint, 10, 4);
            int nSize = localHeightMap.Count();
            for (int i = 0; i < nSize; i++)
            {
                PointF[] ptScreen = new PointF[4];
                int iOk = 0;
                for (int j = 0; j < 4; j++)
                {
                    if (viewer.getPolarScreen(localHeightMap[i].Item2[j], ref ptScreen[j]))
                    {
                        iOk++;
                        ptScreen[j] = addVectors(ptStartW, -ptScreen[j].X, vecWX, -ptScreen[j].Y, vecWY);
                        //ptScreen[j].X = ptScreen[j].X + i;
                    }
                }

                if (iOk == 4)
                {
                    gfx.FillPolygon(brushDarkBrown, ptScreen);

                    if (myTerrainMap.bNeedAvoidance)
                        gfx.DrawPolygon(penRedThin, ptScreen);
                    else
                        gfx.DrawPolygon(penBlackThin, ptScreen);
                }

            }


            for (int i = 5; i < myFlightPath.path.Length; i++)
            {
                PointF[] ptScreen = new PointF[4];
                PointF[] ptScreenLine = new PointF[2];
                if (viewer.getScreen(myFlightPath.path[i], myCondorUDPData.time, ref ptScreen))
                {
                    for (int j = 0; j < 4; j++)
                    {
                        ptScreen[j] = addVectors(ptStartW, -ptScreen[j].X, vecWX, -ptScreen[j].Y, vecWY);
                    }
                    ptScreenLine[0] = ptScreen[0];
                    ptScreenLine[1] = ptScreen[2];
                    bool bIsSmall = Math.Abs(ptScreenLine[0].X - ptScreenLine[1].X) < 2;
                    if (bIsSmall)
                    {
                        ptScreenLine[1].X = 0.5f * (ptScreenLine[0].X + ptScreenLine[1].X);
                        ptScreenLine[0].X = ptScreenLine[1].X;
                        for (int j = 0; j < 4; j++)
                        {
                            if (ptScreenLine[1].Y > ptScreen[j].Y)
                                ptScreenLine[1].Y = ptScreen[j].Y;
                            if (ptScreenLine[0].Y < ptScreen[j].Y)
                                ptScreenLine[0].Y = ptScreen[j].Y;
                        }
                        ptScreenLine[0].Y += 2;
                        ptScreenLine[1].Y -= 2;
                    }


                    int iWhite = Math.Max(10, 255 - Convert.ToInt32(Math.Abs(myFlightPath.path[i].vario * 50.0)));
                    if (myFlightPath.path[i].vario > 0)
                    {
                        Pen pen = new Pen(Color.FromArgb(250 - 2 * i, 255, iWhite, iWhite), 5);
                        pen.DashCap = System.Drawing.Drawing2D.DashCap.Round;
                        if (bIsSmall)
                            gfx.DrawLine(pen, ptScreenLine[0], ptScreenLine[1]);
                        else
                            gfx.DrawPolygon(pen, ptScreen);
                    }
                    else
                    {
                        Pen pen = new Pen(Color.FromArgb(250 - 2 * i, iWhite, iWhite, 255), 5);
                        pen.DashCap = System.Drawing.Drawing2D.DashCap.Round;
                        if (bIsSmall)
                            gfx.DrawLine(pen, ptScreenLine[0], ptScreenLine[1]);
                        else
                            gfx.DrawPolygon(pen, ptScreen);
                    }
                }
            }


            //for (int i = 1; i < Math.Min( myDijkstra.myDijkstraPath.PathNodes.Count(), 20); i++)
            //{
            //    PointF[] ptScreen = new PointF[4];
            //    if (viewer.getScreen(myDijkstra.myDijkstraPath.PathNodes[i-1], myDijkstra.myDijkstraPath.PathNodes[i], ref ptScreen))

            //    {
            //        for (int j = 0; j < 4; j++)
            //        {
            //            ptScreen[j] = addVectors(ptStartW, -ptScreen[j].X, vecWX, -ptScreen[j].Y, vecWY);
            //        }
            //        gfx.DrawPolygon(penYellowThin, ptScreen);
            //    }
            //}

            var ptCenter = new PointF(510, 230);

            //pitch lines 10 deg
            for (int i = -2; i < 3; i++)
            {
                if (i == 0)
                    continue;

                var pta = addVectors(ptCenter, -80 - Math.Abs(i) * 20, vecWX, i * 80, vecWY);
                var ptb = addVectors(ptCenter, 80 + Math.Abs(i) * 20, vecWX, i * 80, vecWY);
                var ptc = addVectors(ptb, 10, vecWX, -10, vecWY);

                gfx.DrawLine(penWhite, pta, ptb);
                string str;
                str = (Math.Abs(i) * 10).ToString();
                gfx.DrawString(str, myFont15, brushWhite, ptc);
            }

            //pitch lines 5 deg
            for (int i = -2; i < 2; i++)
            {

                var pta = addVectors(ptCenter, -50, vecWX, i * 80 + 40, vecWY);
                var ptb = addVectors(ptCenter, 50, vecWX, i * 80 + 40, vecWY);

                gfx.DrawLine(penWhite, pta, ptb);
            }



            PointF[] curvePlane = new PointF[7];
            curvePlane[0] = addVectors(ptStart, -150, vecX, 0, vecY);
            curvePlane[1] = addVectors(ptStart, -50, vecX, 0, vecY);
            curvePlane[2] = addVectors(ptStart, -25, vecX, 25, vecY);
            curvePlane[3] = ptStart;
            curvePlane[4] = addVectors(ptStart, 25, vecX, 25, vecY);
            curvePlane[5] = addVectors(ptStart, 50, vecX, 0, vecY);
            curvePlane[6] = addVectors(ptStart, 150, vecX, 0, vecY);

            gfx.DrawLines(penYellow, curvePlane);


            if (bAutoPilot)
            {// draw pitchtarget
                float dPitchTarget = -8* Convert.ToSingle(Utils.rad2deg(myAutoPilot.dPitchTarget));
                var pta = addVectors(ptCenter, -50 , vecWX, dPitchTarget, vecWY);
                var ptb = addVectors(ptCenter, 50, vecWX, dPitchTarget, vecWY);
                var ptc = addVectors(ptb, 10, vecWX, -10, vecWY);

                gfx.DrawLine(penGreen, pta, ptb);
            }


            int iStep = 30;

            int iPos = Convert.ToInt32(Math.Floor(Utils.rad2deg(myCondorUDPData.yaw) / 30)) * 30;
            int iStart = Convert.ToInt32(Utils.rad2deg(myCondorUDPData.yaw) - iPos);

            // yaw lines
            for (int i = -3; i < 5; i++)
            {
                pt.X = 510 - 5 * (iStart + i* iStep) ;
                pt.Y = 230;

                gfx.DrawLine(penWhiteThin, pt.X, pt.Y, pt.X , pt.Y + 20 );
                int iPos0 = (iPos - 30 * i);
                if (iPos0 >= 360)
                    iPos0 -= 360;
                if (iPos0 < 0)
                    iPos0 += 360;
                string str = iPos0.ToString();
                gfx.DrawString(str, myFont15, brushWhite, pt.X - 15, pt.Y + 30);
            }



            PointF[] curveTriangle = new PointF[3];
            curveTriangle[0] = ptStartW;
            curveTriangle[1] = addVectors(ptStartW, -10, vecWX, 20, vecWY);
            curveTriangle[2] = addVectors(ptStartW, 10, vecWX, 20, vecWY);
            gfx.FillPolygon(brushDarkGray, curveTriangle);
            curveTriangle[1] = addVectors(ptStartW, -10, vecWX, -20, vecWY);
            curveTriangle[2] = addVectors(ptStartW, 10, vecWX, -20, vecWY);
            gfx.FillPolygon(brushDarkGray, curveTriangle);


            AddFlightDirectionAndTurnPoints(ref gfx, ref viewer);


            int iDir;
            Pen penToUse;
            if (myCondorUDPData.vario < 0)
            {
                iDir = 1;
                penToUse = penBlue;
            }
            else
            {
                iDir = -1;
                penToUse = penRed;
            }
            float[] eVarios = { 0.5f, 1, 2, 3, 4, 5, 6, 7, 8, 10, 12 };
            for (int i = 0; i < 11 && eVarios[i] < Math.Abs(myCondorUDPData.vario); i += 1)
            {
                var pta = addVectors(ptStartW, -40, vecWX, iDir * eVarios[i] * 12, vecWY);
                var ptb = addVectors(ptStartW, 0, vecWX, iDir * eVarios[i] * 16, vecWY);
                var ptc = addVectors(ptStartW, 40, vecWX, iDir * eVarios[i] * 12, vecWY);
                gfx.DrawLine(penToUse, pta, ptb);
                gfx.DrawLine(penToUse, ptc, ptb);
            }


            gfx.DrawString(m_strTime, myFont25, brushWhite, 15, 10);


            double dRemainingDistance = (Math.Floor(myFlightPlanData.RemainingDistance / 10)) / 100;

            gfx.DrawString(dRemainingDistance.ToString("F2"), myFont25, brushWhite, 15, 40);
            
            gfx.DrawString(myCondorUDPData.altitude.ToString("F0"), myFont25, brushWhite, 920, 10);

            if (myTerrainMap.GetHeight(flightPoint.x, flightPoint.y,out dSurfaceElevation))
            {
                gfx.DrawString(dSurfaceElevation.ToString(), myFont25, brushRed, 920, 40);

            }
            gfx.DrawString(flightPoint.vario.ToString("F1"), myFont25, brushWhite, 480, 10);

            SolidBrush brushSpeed = brushRed;
            double dSpeedTAS = flightPoint.speed * 3.6;
            double dSpeedIAS = myCondorUDPData.ias * 3.6;
            if (dSpeedIAS > 210)
            {
                brushSpeed = brushRed;
            }
            else if(dSpeedIAS > 200)
            {
                brushSpeed = brushYellow;
            }
            else if (dSpeedIAS > 110)
            {
                brushSpeed = brushWhite;
            }
            else 
            {
                brushSpeed = brushRed;
            }
            string strSpeed = dSpeedIAS.ToString("F0") + " / "
                + dSpeedTAS.ToString("F0");
            gfx.DrawString(strSpeed, myFont25, brushSpeed, 440, 410);




            pictureGimBall.Image = bitmapGimBall;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            myThreadDijkstra.Abort();
            if (myThreadListener != null)
                myThreadListener.Abort();
        }

        private void buttonML_Lift_Click(object sender, EventArgs e)
        {
            CThermalML formThermalML = new CThermalML(myFlightPlanData, myTerrainMap, myCondorUDPDataLog);
            formThermalML.ShowDialog();

        }

        private void AddFlightDirectionAndTurnPoints(ref Graphics gfx, ref Viewer viewer)
        {
            Point3d ptTarget = viewer.m_ptEye.add(10000, new Vector3d(myCondorUDPData.vx, myCondorUDPData.vy, 0));
            PointF ptStart = new PointF(0,0);
            if (viewer.getPolarScreen(ptTarget, ref ptStart))
            {
                ptSmoothedWindDir.X = 0.9f*ptSmoothedWindDir.X + 0.1f*ptStart.X;
                ptSmoothedWindDir.Y = ptStart.Y;
                PointF[] curveTriangle = new PointF[3];
                ptStart.X = 510 - ptSmoothedWindDir.X;
                ptStart.Y = 230 + ptSmoothedWindDir.Y;
                curveTriangle[0] = ptStart;
                curveTriangle[1] = addVectors(ptStart, -15, new SizeF(1,0), 30, new SizeF(0, 1));
                curveTriangle[2] = addVectors(ptStart, 15, new SizeF(1, 0), 30, new SizeF(0, 1));
                gfx.FillPolygon(brushBlack, curveTriangle);

            }
            PointF[] markerLine = new PointF[2];
            bool bMarkerOk = false;
            bool bSafeMarkerOk = false;
            if (myFlightPlanData.GetMarkerLocationXYZ(myFlightPlanData.nextWayPoint, out ptTarget.X, out ptTarget.Y, out ptTarget.Z))
            {
                double dDistance;
                dDistance = (Math.Floor(Math.Sqrt(Math.Pow(ptTarget.X - flightPoint.x, 2) + Math.Pow(ptTarget.Y - flightPoint.y, 2)) / 10)) / 100;
                ptTarget.Z = viewer.m_ptEye.Z;
                if (viewer.getPolarScreen(ptTarget, ref ptStart))
                {
                    PointF[] curveTriangle = new PointF[3];
                    ptStart.X = 510 - ptStart.X;
                    ptStart.Y += 230;
                    curveTriangle[0] = ptStart;
                    curveTriangle[1] = addVectors(ptStart, -10, new SizeF(1, 0), -20, new SizeF(0, 1));
                    curveTriangle[2] = addVectors(ptStart, 10, new SizeF(1, 0), -20, new SizeF(0, 1));
                    gfx.FillPolygon(brushBlack, curveTriangle);
                    bMarkerOk = true;
                    markerLine[0] = curveTriangle[1];

                    string str = (myFlightPlanData.nextWayPoint + 1).ToString() + " "
                        + myFlightPlanData.GetMarkerText(myFlightPlanData.nextWayPoint) + " "
                        + dDistance.ToString("F2");


                    Utils.DrawRotatedTextAt(gfx, 0, str, ptStart.X + 15, ptStart.Y - 35, myFont15, brushBlack);
                }
            }
            if (myFlightPlanData.GetSafeWayPointLocation(myFlightPlanData.nextWayPoint,
                flightPoint.x, flightPoint.y,
                out ptTarget.X, out ptTarget.Y, out ptTarget.Z))
            {
                ptTarget.Z = viewer.m_ptEye.Z;
                if (viewer.getPolarScreen(ptTarget, ref ptStart))
                {
                    PointF[] curveTriangle = new PointF[3];
                    ptStart.X = 510 - ptStart.X;
                    ptStart.Y += 230;
                    curveTriangle[0] = ptStart;
                    curveTriangle[1] = addVectors(ptStart, -10, new SizeF(1, 0), -20, new SizeF(0, 1));
                    curveTriangle[2] = addVectors(ptStart, 10, new SizeF(1, 0), -20, new SizeF(0, 1));
                    gfx.DrawPolygon(penBlackThin, curveTriangle);
                    bSafeMarkerOk = true;
                    markerLine[1] = curveTriangle[1];
                }
            }
            if (bMarkerOk && bSafeMarkerOk)
            {
                gfx.DrawLine(penBlackThin, markerLine[0], markerLine[1]);
            }
        }

        private void updateTopView()
        {

            Bitmap bitmapTopView = new Bitmap(pictureTopView.Width, pictureTopView.Height);
            Graphics gfx = Graphics.FromImage(bitmapTopView);

            gfx.SmoothingMode =
                    System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            

            gfx.FillRectangle(brushBrown, 0, 0, pictureGimBall.Width, 2*pictureGimBall.Height);


            Vector3d vecView = new Vector3d(Convert.ToSingle(-Math.Sin(myCondorUDPData.yaw)), Convert.ToSingle(Math.Cos(myCondorUDPData.yaw)), 0);
            Vector3d vecSide = new Vector3d(Convert.ToSingle(Math.Cos(myCondorUDPData.yaw)), Convert.ToSingle(Math.Sin(myCondorUDPData.yaw)), 0);

            PointF[] curveWind = new PointF[7];
            getWindArrow(new PointF(250, 250),
                new SizeF(3 * vecView.y, 3 * vecView.x),
                new SizeF(-3 * vecView.x, 3 * vecView.y),
                ref curveWind);

            gfx.FillPolygon(brushBlue, curveWind);

            gfx.DrawEllipse(penBlack, 187, 187, 126, 126);// 500 m diameter
            gfx.DrawEllipse(penBlack, 125, 125, 250, 250);// 1000 m diameter




            for (int i = myFlightPath.path.Length-1; i >=0 ; i--)
            {
                Vector3d vec = new Vector3d(myFlightPath.path[0].getPosition(), myFlightPath.path[i].getPosition());
                PointF ptCircle = new PointF(-vec.dotProduct(vecSide)/4, -vec.dotProduct(vecView)/4);

                int iWhite = Math.Max(10, 255 - Convert.ToInt32(Math.Abs(myFlightPath.path[i].vario * 50.0)));
                if (myFlightPath.path[i].vario > 0)
                {
                    SolidBrush brush = new SolidBrush(Color.FromArgb(250 - 2 * i, 255, iWhite, iWhite));
                    gfx.FillEllipse(brush, 250+ptCircle.X - 8, 250 + ptCircle.Y - 8, 16, 16);
                }
                else
                {
                    SolidBrush brush = new SolidBrush(Color.FromArgb(250 - 2 * i, iWhite, iWhite, 255));
                    gfx.FillEllipse(brush, 250 + ptCircle.X - 8, 250 + ptCircle.Y - 8, 16, 16);
                }
            }

            if (myTerrainMap.bNeedAvoidance)
            {
                Vector3d vec = new Vector3d(myFlightPath.path[0].getPosition(),
                    myTerrainMap.AntiCollisionDirectionTarget.getPosition());
                PointF ptCircle = new PointF(-vec.dotProduct(vecSide) / 4, -vec.dotProduct(vecView) / 4);
                gfx.FillEllipse(brushRed, 250 + ptCircle.X - 20, 250 + ptCircle.Y - 20, 40, 40);

            }
            if (myThermalLocation.bInThermal)
            {
                Vector3d vec = new Vector3d(myFlightPath.path[0].getPosition(), 
                    myThermalLocation.thermalPoint.getPosition());
                PointF ptCircle = new PointF(-vec.dotProduct(vecSide) / 4, -vec.dotProduct(vecView) / 4);
                gfx.FillEllipse(brushGreen, 250 + ptCircle.X - 20, 250 + ptCircle.Y - 20, 40, 40);

                vec = new Vector3d(myFlightPath.path[0].getPosition(),
                    myThermalLocation.thermalTargetPoint.getPosition());
                PointF ptTargetCircle = new PointF(-vec.dotProduct(vecSide) / 4, -vec.dotProduct(vecView) / 4);
                if (myThermalLocation.bEight)
                    gfx.FillEllipse(brushCyan, 250 + ptTargetCircle.X - 10, 250 + ptTargetCircle.Y - 10, 20, 20);
                else
                    gfx.FillEllipse(brushGreen, 250 + ptTargetCircle.X - 10, 250 + ptTargetCircle.Y - 10, 20, 20);

            }

            gfx.DrawLine(penYellow, 200, 250, 300, 250);
            gfx.DrawLine(penYellow, 250, 240, 250, 280);
            gfx.DrawLine(penYellow, 244, 280, 256, 280);

            pictureTopView.Image = bitmapTopView;
        }
    }
}

