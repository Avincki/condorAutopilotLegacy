using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;


using Yolov5Net.Scorer;

using Yolov5Net.Scorer.Models;
using Accord.Math;
using Accord.Statistics;
using Accord.Statistics.Analysis;

namespace CondorAutoPilot.YoloViewer
{
    public partial class YoloViewer : Form
    {
        private int indexCount = 0;
        private CondorUDPData myCondorUDPDataListener;
        private CondorUDPData myCondorUDPData = new CondorUDPData();
        private bool bRun = true;

        private SolidBrush brushWhite = new SolidBrush(Color.White);
        private SolidBrush brushYellow = new SolidBrush(Color.Yellow);
        private SolidBrush brushYellowish = new SolidBrush(Color.FromArgb(255, 215, 0));
        private SolidBrush brushBlack = new SolidBrush(Color.Black);
        private SolidBrush brushGreen = new SolidBrush(Color.Green);
        private SolidBrush brushCyan = new SolidBrush(Color.Cyan);
        private SolidBrush brushRed = new SolidBrush(Color.Red);
        private SolidBrush brushDarkSlateGray = new SolidBrush(Color.DarkSlateGray);
        private SolidBrush brushLightSteelBlue = new SolidBrush(Color.LightSteelBlue);

        private Yolov5Net.Scorer.YoloScorer<YoloCocoP5Model_condornn> myYoloScorer = null;
        private string YoloONNXFile;

        public System.Drawing.Bitmap myBitmap { get; set; }

        private List<Pen> pens;
        private Pen penAirField = new Pen(new SolidBrush(Color.LawnGreen), 5);
        private Pen penHorizon = new Pen(new SolidBrush(Color.IndianRed), 5);
        private Pen penTowPlane = new Pen(new SolidBrush(Color.OrangeRed), 5);

        private Rectangle mySimulatorScreen;

        private bool hasYawString = false;
        private PointF yawStringPoint = new Point();
        private PointF horizonRefPoint = new Point();
        private float cosBank;
        private float sinBank;
        private float bank;
        private float pitch;

        private int iAirField = -1;
        private bool bHasAirField = false;
        private PointF airFieldCenter = new PointF();
        private PointF airFieldVector = new PointF();

        private DateTime timeStamp = new DateTime();

        private Font yoloFont = new Font(new FontFamily("Arial"), 10);
        private Font yoloFontSmall = new Font(new FontFamily("Arial"), 7);
        private Font yoloFontBig = new Font(new FontFamily("Arial"), 15, FontStyle.Bold);

        private List<CloudBase> cloudBases = new List<CloudBase>();

        public double dCloudBaseHeight = 0;

        private YoloData myYoloData;



        public YoloViewer()
        {
            InitializeComponent();

            YoloONNXFile = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            YoloONNXFile = YoloONNXFile + @"\condornn.onnx";
            if (File.Exists(YoloONNXFile))
                myYoloScorer = new YoloScorer<YoloCocoP5Model_condornn>(YoloONNXFile);

            pens = new List<Pen>();
            pens.Add(new Pen(Brushes.OrangeRed, 2));
            pens.Add(new Pen(Brushes.GreenYellow, 2));
            pens.Add(new Pen(Brushes.Red, 2));
            pens.Add(new Pen(Brushes.Blue, 2));
            pens.Add(new Pen(Brushes.Cyan, 2));
            pens.Add(new Pen(Brushes.Magenta, 2));


            myBitmap = new Bitmap(pictureBoxYoloViewer.Width, pictureBoxYoloViewer.Height);

            mySimulatorScreen = Screen.AllScreens[0].Bounds;
            mySimulatorScreen.Y = 30;
            mySimulatorScreen.Height -= 70;

        }


        public void Stop()
        {
            bRun = false;
        }


        public void Init(CondorUDPData condorUDPData, YoloData yoloData)
        {
            myCondorUDPDataListener = condorUDPData;
            myYoloData = yoloData;
        }

        private void YoloViewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Properties.Settings.Default.YoloTop = this.Top;
            //Properties.Settings.Default.YoloLeft = this.Left;
        }

        private void YoloViewer_Load(object sender, EventArgs e)
        {
            this.Top = Properties.Settings.Default.YoloTop;
            this.Left = Properties.Settings.Default.YoloLeft;
        }

        private void timerYolo_Tick(object sender, EventArgs e)
        {
            if (!bRun)
                Close();
            if (myCondorUDPDataListener == null)
                return;
            if (!Properties.Settings.Default.YoloRun)
                return;

            if (dCloudBaseHeight == 0)
            {
                dCloudBaseHeight = Properties.Settings.Default.CloudInversionHeight;
            }

            Grab();

            timeStamp = DateTime.Now;
            myCondorUDPData.myClone(myCondorUDPDataListener);
            var yoloPredictions = myYoloScorer.Predict(myBitmap);
            SaveYoloBitmap(yoloPredictions);

            hasYawString = ProcessYawString(yoloPredictions);
            ProcessCloudBases(yoloPredictions);
            ProcessAirfield(yoloPredictions);
            ProcessTowPlane(yoloPredictions);

            BitMapWrapper bmp = new BitMapWrapper(myBitmap);
            bmp.Gray();
            bmp.UnLock();

            AddYoloGraphics(yoloPredictions);

            pictureBoxYoloViewer.Image = myBitmap;
        }

        public void Grab()
        {
            Bitmap bmpScreen = new Bitmap(mySimulatorScreen.Width, mySimulatorScreen.Height);
            using (Graphics g = Graphics.FromImage(bmpScreen))
            {
                g.CopyFromScreen(new Point(0, mySimulatorScreen.Y), Point.Empty, mySimulatorScreen.Size);
            }
            myBitmap = new Bitmap(bmpScreen, pictureBoxYoloViewer.Width, pictureBoxYoloViewer.Height);
        }

        private void SaveYoloBitmap(List<YoloPrediction> yoloPredictions)
        {
            if (!Properties.Settings.Default.SaveYoloBitmap)
                return;

            string pathToYolo = Properties.Settings.Default.CondorFlightPlanPath;
            pathToYolo = pathToYolo.Replace("FlightPlans", "Yolo");
            string path = pathToYolo + Path.DirectorySeparatorChar;
            Directory.CreateDirectory(path);


            string strTime = DateTime.Now.ToString("yyyyMMdd-HHmmss");

            Directory.CreateDirectory(path + "images/train");
            Directory.CreateDirectory(path + "labels/train");

            List<string> annotation = new List<string>();
            annotation.Add("towplane");
            annotation.Add("airfield");
            annotation.Add("yawstring");
            annotation.Add("cloudbase");
            annotation.Add("stripes");
            annotation.Add("wingtip");

            File.WriteAllLines(path + "labels/train/labels.txt", annotation);

            annotation.Clear();

            foreach (var yolo in yoloPredictions)
            {
                double xc = ((double)yolo.Rectangle.Left + 0.5 * (double)yolo.Rectangle.Width) / myBitmap.Width;
                double yc = ((double)yolo.Rectangle.Top + 0.5 * (double)yolo.Rectangle.Height) / myBitmap.Height;
                double wi = (double)(yolo.Rectangle.Width) / myBitmap.Width;
                double hi = (double)(yolo.Rectangle.Height) / myBitmap.Height;
                string strLine = (yolo.Label.Id-1).ToString() + " " +
                    xc.ToString("0.000000") + " " +
                    yc.ToString("0.000000") + " " +
                    wi.ToString("0.000000") + " " +
                    hi.ToString("0.000000");


                if (xc < 0 || xc > 1 || yc < 0 || yc > 1)
                    continue;

                annotation.Add(strLine);
            }



            File.WriteAllLines(path + "labels/train/" + strTime + ".txt", annotation);
            myBitmap.Save(path + "images/train/" + strTime + ".png", ImageFormat.Png);

            Properties.Settings.Default.SaveYoloBitmap = false;
        }

        private PointF transformToHorizonCS(PointF pt)
        {
            float vecX = pt.X - horizonRefPoint.X;
            float vecY = pt.Y - horizonRefPoint.Y;
            PointF ptOut = new PointF(
                vecX * cosBank + vecY * sinBank,
                vecX * sinBank - vecY * cosBank
                );

            return ptOut;
        }

        private PointF transformToYawStringCS(PointF pt)
        {
            float vecX = pt.X - yawStringPoint.X;
            float vecY = pt.Y - yawStringPoint.Y;
            PointF ptOut = new PointF(
                vecX * cosBank + vecY * sinBank,
                vecX * sinBank - vecY * cosBank
                );

            return ptOut;
        }

        private RectangleF TransformToHorizonCS(RectangleF rect)
        {
            
            PointF ptCenter = new PointF((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);
            ptCenter = transformToHorizonCS(ptCenter);
            float fWidth = rect.Width;
            float fHeight = rect.Height * cosBank;

            RectangleF rectTransform = new RectangleF(ptCenter.X - fWidth/2, ptCenter.Y - fHeight / 2, fWidth, fHeight);
            return rectTransform;
        }

        private void ProcessTowPlane(List<YoloPrediction> yoloPredictions)
        {
            foreach (var yoloTowPlane in yoloPredictions)
            {
                if (yoloTowPlane.Label.Id == 1)
                {
                    PointF ptPlane = new PointF(yoloTowPlane.Rectangle.Left + yoloTowPlane.Rectangle.Width / 2,
                        yoloTowPlane.Rectangle.Top + yoloTowPlane.Rectangle.Height / 2);
                    ptPlane = transformToYawStringCS(ptPlane);
                    myYoloData.dTowPlaneX = -Utils.deg2rad(ptPlane.X * 15 / myBitmap.Width);
                    myYoloData.dTowPlaneY = Utils.deg2rad(ptPlane.Y * 15 / myBitmap.Width);


                    List< PointF> ptTips = new List<PointF>();

                    foreach (var yoloWingTip in yoloPredictions)
                    {
                        if (yoloWingTip.Label.Id == 6)
                        {
                            PointF ptWt = new PointF(yoloWingTip.Rectangle.Left + yoloWingTip.Rectangle.Width / 2,
                                yoloWingTip.Rectangle.Top + yoloWingTip.Rectangle.Height / 2
                                );
                            if (
                                yoloTowPlane.Rectangle.Left < ptWt.X &&
                                yoloTowPlane.Rectangle.Right > ptWt.X &&
                                yoloTowPlane.Rectangle.Bottom > ptWt.Y &&
                                yoloTowPlane.Rectangle.Top < ptWt.Y
                                )
                            {
                                ptTips.Add(transformToYawStringCS(ptWt));
                                //ptTips.Add(ptWt);
                            }
                        }
                    }
                    if (ptTips.Count > 1)
                    {
                        bool bSort = true;
                        while (bSort)
                        {
                            bSort = false;
                            for (int i = 1; i < ptTips.Count(); i++)
                            {
                                if (ptTips[i-1].X > ptTips[i].X)
                                {
                                    PointF ptTemp = ptTips[i];
                                    ptTips[i] = ptTips[i-1];
                                    ptTips[i-1] = ptTemp;
                                    bSort = true;
                                }
                            }
                        }

                        myYoloData.dTowLeftWing = ptTips[0];
                        myYoloData.dTowRightWing = ptTips.Last();
                        myYoloData.bTowPlanePresent = true;
                        double  dB;
                        dB = Math.Atan2(ptTips.Last().Y - ptTips[0].Y, ptTips.Last().X - ptTips[0].X);

                        myYoloData.dTowPlaneBank = 1.2f*Convert.ToSingle(dB);
                        double alfa = (ptTips[1].X + ptTips[0].X) * 40 / myBitmap.Width;
                        double beta = (ptTips[1].Y + ptTips[0].Y) * 40 / myBitmap.Width;
                        //myYoloData.dTowPlaneX = -Utils.deg2rad((ptTips[1].X + ptTips[0].X) * 15 / myBitmap.Width);
                        //myYoloData.dTowPlaneY = Utils.deg2rad((ptTips[1].Y + ptTips[0].Y) * 15 / myBitmap.Width);

                        Console.WriteLine("Alfa = " + alfa.ToString("F2") + " Beta = " + beta.ToString("F2"));
                        return;
                    }
                }
            }
            myYoloData.bTowPlanePresent = false;
        }



        private void ProcessAirfield(List<YoloPrediction> yoloPredictions)
        {
            bHasAirField = false;
            iAirField = FindBestAirfield(yoloPredictions);
            if (iAirField < 0)
                return;
            var yolo = yoloPredictions[iAirField];

            DataTable dt = new DataTable();
            dt.Columns.Add("X", typeof(double));
            dt.Columns.Add("Y", typeof(double));


            foreach (var yoloStripes in yoloPredictions)
            {
                if (yoloStripes.Label.Id == 5)
                {
                    if (
                        yoloStripes.Rectangle.Left > yolo.Rectangle.Left &&
                        yoloStripes.Rectangle.Right < yolo.Rectangle.Right &&
                        yoloStripes.Rectangle.Bottom < yolo.Rectangle.Bottom &&
                        yoloStripes.Rectangle.Top > yolo.Rectangle.Top
                        )
                    {
                        DataRow dr = dt.NewRow();
                        dr["X"] = yoloStripes.Rectangle.Left + 0.5* yoloStripes.Rectangle.Width;
                        dr["Y"] = yoloStripes.Rectangle.Top + 0.5 * yoloStripes.Rectangle.Height;
                        dt.Rows.Add(dr);
                    }
                }
            }

            if (dt.Rows.Count == 0)
                return;

            PrincipalComponentAnalysis pca = new PrincipalComponentAnalysis()
            {
                Method = PrincipalComponentMethod.Center,
                ExplainedVariance = 0.95
            };

            pca.Learn(dt.ToJagged());

            airFieldCenter.X = Convert.ToSingle(pca.Means[0]);
            airFieldCenter.Y = Convert.ToSingle(pca.Means[1]);

            airFieldVector.X = Convert.ToSingle(pca.ComponentVectors[0][0]);
            airFieldVector.Y = Convert.ToSingle(pca.ComponentVectors[0][1]);

            bHasAirField = true;

        }

        private int FindBestAirfield(List<YoloPrediction> yoloPredictions)
        {
            int iAirField = -1;
            int iCountStripes = 0;
            for (int i = 0; i < yoloPredictions.Count(); i++ )
            {
                var yolo = yoloPredictions[i];
                if (yolo.Label.Id == 2)
                {
                    int iStripes = 0;
                    foreach (var yoloStripes in yoloPredictions)
                    {
                        if (yoloStripes.Label.Id == 5)
                        {
                            if (
                                yoloStripes.Rectangle.Left > yolo.Rectangle.Left &&
                                yoloStripes.Rectangle.Right < yolo.Rectangle.Right &&
                                yoloStripes.Rectangle.Bottom < yolo.Rectangle.Bottom &&
                                yoloStripes.Rectangle.Top > yolo.Rectangle.Top
                                )
                            {
                                iStripes++;
                            }
                        }
                    }
                    if (iStripes > iCountStripes)
                    {
                        iCountStripes = iStripes;
                        iAirField = i;
                    }
                }
            }
            return iAirField;
        }

        private void ProcessCloudBases(List<YoloPrediction> yoloPredictions)
        {
            foreach (var yolo in yoloPredictions)
            {
                if (yolo.Label.Id == 4)
                {
                    PointF ptCloud = transformToHorizonCS(new PointF(
                    yolo.Rectangle.X + yolo.Rectangle.Width / 2,
                    yolo.Rectangle.Bottom - yolo.Rectangle.Height / 3)
                        );

                    double alfaDeg = ptCloud.Y * 80 / myBitmap.Width;
                    double alfa = Utils.deg2rad(alfaDeg);

                    if (hasYawString)
                    {
                        yolo.Info = "";
                        yolo.InfoIndex = "";

                        if (alfaDeg > 8)
                        {
                            int indexCloud;

                            int iC = CloudBase.BestMatch(myCondorUDPDataListener.time, TransformToHorizonCS(yolo.Rectangle), cloudBases);
                            if (iC > -1)
                            {
                                cloudBases[iC].rectLast = TransformToHorizonCS(yolo.Rectangle);
                                cloudBases[iC].LogicalAdd(alfa, new CondorUDPData(myCondorUDPDataListener));
                                indexCloud = cloudBases[iC].index;
                            }
                            else
                            {
                                indexCount++;
                                indexCloud = indexCount;
                                CloudBase newCloudBase = new CloudBase(indexCount, alfa, myCondorUDPDataListener, TransformToHorizonCS(yolo.Rectangle));
                                cloudBases.Add(newCloudBase);
                            }
                            yolo.InfoIndex = indexCloud.ToString();
                        }

                        if (alfaDeg > 1) 
                        { 
                            double dDist = (dCloudBaseHeight - myCondorUDPDataListener.altitude) / Math.Tan(alfa) / 1000;
                            if (dDist > 0)
                                yolo.Info = dDist.ToString("F2");
                        }
                    }
                }
            }
            if (hasYawString)
            {
                CloudBase.Clean(myCondorUDPDataListener.time, cloudBases);
                if (CloudBase.CalcCloudBaseHeight(cloudBases, out double dH, out double dSmooth))
                {
                    dCloudBaseHeight = (1 - dSmooth) * dCloudBaseHeight + dSmooth * dH;
                }
            }
        }


        private bool ProcessYawString(List<YoloPrediction> yoloPredictions)
        {
            double dDist = double.MaxValue;
            PointF yawStringPointQ = new PointF();
            PointF horizonRefPointQ = new PointF();
            float sinBankQ;
            float cosBankQ;
            foreach (var yolo in yoloPredictions)
            {
                if (yolo.Label.Id == 3)
                {
                    yawStringPointQ.X = yolo.Rectangle.X + yolo.Rectangle.Width / 2;
                    yawStringPointQ.Y = yolo.Rectangle.Bottom;
                    var dT = Convert.ToSingle(Math.Min(1,(timeStamp - myCondorUDPData.timeStamp).TotalMilliseconds / 1000));

                    //bank = 0.1f * bank + 0.9f * (myCondorUDPData.bank + dT * myCondorUDPData.bankrate);
                    //pitch = 0.1f * pitch + 0.9f * (myCondorUDPData.pitch + dT * myCondorUDPData.pitchrate);
                    bank = myCondorUDPData.bank + dT * myCondorUDPData.bankrate;
                    pitch = myCondorUDPData.pitch + dT * myCondorUDPData.pitchrate;
                    sinBankQ = Convert.ToSingle(Math.Sin(bank));
                    cosBankQ = Convert.ToSingle(Math.Cos(bank));

                    var dPitch = Convert.ToSingle(myBitmap.Width / 80 * Utils.rad2deg(pitch));

                    horizonRefPointQ.X = yawStringPointQ.X - sinBankQ * dPitch;
                    horizonRefPointQ.Y = yawStringPointQ.Y + cosBankQ * dPitch;

                    var dDistQ = (horizonRefPoint.X - myBitmap.Width / 2) * (horizonRefPoint.X - myBitmap.Width / 2)
                        + (horizonRefPoint.Y - myBitmap.Height / 2) * (horizonRefPoint.Y - myBitmap.Height / 2) / 20;
                    if (dDistQ < dDist)
                    {
                        dDist = dDistQ;
                        yawStringPoint.X = yawStringPointQ.X;
                        yawStringPoint.Y = yawStringPointQ.Y;
                        horizonRefPoint.X = horizonRefPointQ.X;
                        horizonRefPoint.Y = horizonRefPointQ.Y;

                        sinBank = sinBankQ;
                        cosBank = cosBankQ;
                    }
                }
            }
            return dDist < double.MaxValue;
        }


    public void AddYoloGraphics(List <YoloPrediction> yoloPredictions)
        {
            float crossSize = 1000;

            Graphics gfx = Graphics.FromImage(myBitmap);

            gfx.SmoothingMode =
                    System.Drawing.Drawing2D.SmoothingMode.AntiAlias;


            if (bHasAirField && airFieldVector.Y != 0)
            {
                var t = (yoloPredictions[iAirField].Rectangle.Top - airFieldCenter.Y) / airFieldVector.Y;
                var airfieldXL = airFieldCenter.X + t * airFieldVector.X;
                var airfieldYL = airFieldCenter.Y + t * airFieldVector.Y;
                t = (yoloPredictions[iAirField].Rectangle.Bottom - airFieldCenter.Y) / airFieldVector.Y;
                var airfieldXR = airFieldCenter.X + t * airFieldVector.X;
                var airfieldYR = airFieldCenter.Y + t * airFieldVector.Y;

                gfx.DrawLine(penAirField, airfieldXL, airfieldYL, airfieldXR, airfieldYR);
            }

            int iP = 0;

            gfx.DrawString("AI Estimated Cloud Base: " + dCloudBaseHeight.ToString("F0"), yoloFontBig, Brushes.Blue, 15, 20);

            foreach (var yolo in yoloPredictions)
            {
                int iPen = yolo.Label.Id - 1;
                iP++;
                string text = iP.ToString() + ") " + yolo.Label.Name + ", " + (100 * yolo.Score).ToString("F1") + "%";


                if (yolo.Label.Id != 5)
                {
                    gfx.DrawRectangle(pens[iPen], Rectangle.Ceiling(yolo.Rectangle));
                }

                if (yolo.Label.Id == 1 && myYoloData.bTowPlanePresent)
                {
                    gfx.DrawLine(penTowPlane, myYoloData.dTowLeftWing, myYoloData.dTowRightWing);
                }



                if (yolo.Label.Id == 3)
                {
                    var horXL = horizonRefPoint.X - crossSize * cosBank;
                    var horYL = horizonRefPoint.Y - crossSize * sinBank;
                    var horXR = horizonRefPoint.X + crossSize * cosBank;
                    var horYR = horizonRefPoint.Y + crossSize * sinBank;

                    gfx.DrawLine(penHorizon, horXL, horYL, horXR, horYR);

                    var verXL = horizonRefPoint.X - crossSize * sinBank;
                    var verYL = horizonRefPoint.Y + crossSize * cosBank;
                    var verXR = horizonRefPoint.X + crossSize * sinBank;
                    var verYR = horizonRefPoint.Y - crossSize * cosBank;

                    gfx.DrawLine(penHorizon, verXL, verYL, verXR, verYR);
                }else if (yolo.Label.Id == 4)
                {
                    gfx.DrawString(yolo.Info, yoloFont, pens[iPen].Brush, yolo.Rectangle.X, yolo.Rectangle.Y - 15);
                    gfx.DrawString(yolo.InfoIndex, yoloFontSmall, pens[iPen].Brush, yolo.Rectangle.X, yolo.Rectangle.Bottom +2);
                }
            }
        }
    }


    public class YoloViewerThread
    {
        private CondorUDPData myCondorUDPData = null;
        public YoloViewer myYoloViewer = null;
        public YoloData myYoloData;
        public YoloViewerThread()
        {

        }

        public void Init(CondorUDPData condorUDPData, YoloData yoloData)
        {
            myCondorUDPData = condorUDPData;
            myYoloData = yoloData;
        }

        public void Load()
        {
            Properties.Settings.Default.YoloRun = false;
            myYoloViewer = new YoloViewer();
            myYoloViewer.Init(myCondorUDPData, myYoloData);
            myYoloViewer.ShowDialog();
        }
        public void Run()
        {
            Properties.Settings.Default.YoloRun = true;
        }
        public void ThreadStop()
        {
            if (myYoloViewer != null)
                myYoloViewer.Stop();
        }
    }

    public class CloudBase
    {
        public int index;
        public RectangleF rectLast;
        public double alfaLast;
        public CondorUDPData condorUDPDataLast;

        public List<Tuple<double, CondorUDPData>> captures = new List <Tuple<double, CondorUDPData>>();

        private double alfaThreshold = 8 / 180 * Math.PI;

        private double dTimeStep = 5 / 3600;

        public CloudBase(int indexNew, double alfa, CondorUDPData condorUDPData, RectangleF rect)
        {
            condorUDPDataLast = new CondorUDPData(condorUDPData);
            captures.Add(Tuple.Create(alfa, new CondorUDPData(condorUDPData)));
            alfaLast = alfa;

            rectLast = rect;
            index = indexNew;
        }

        public double IUO(RectangleF rect)
        {
            double xioumin = Math.Max(rectLast.Left, rect.Left);
            double xioumax = Math.Min(rectLast.Right, rect.Right);
            double yioumin = Math.Max(rectLast.Top, rect.Top);// remember in graphics top < bottom
            double yioumax = Math.Min(rectLast.Bottom, rect.Bottom);

            double iou = 2 * (xioumax - xioumin) * (yioumax - yioumin) / (rectLast.Width * rectLast.Height + rect.Width * rect.Height);

            if (xioumin > xioumax)
                iou = 0;
            if (yioumin > yioumax)
                iou = 0;
            return iou;
        }

        public bool CalcCloudBaseHeight(out double height)
        {
            height = 0;
            List<double> heights = new List<double>();

            for (int i = 0; i < captures.Count() ; i++)
            {
                var dDeltaTime = 3600 * (condorUDPDataLast.time - captures[i].Item2.time);
                if (dDeltaTime < 10)
                    continue;
                if (alfaLast < alfaThreshold)
                    continue;

                double dist = Math.Sqrt((condorUDPDataLast.x - captures[i].Item2.x) * (condorUDPDataLast.x - captures[i].Item2.x) +
                    (condorUDPDataLast.y - captures[i].Item2.y) * (condorUDPDataLast.y - captures[i].Item2.y));

                double dTan = Math.Tan(captures[i].Item1) - Math.Tan(alfaLast);
                if (Math.Abs(dTan) < 0.05)
                    continue;

                double dL = (condorUDPDataLast.altitude - captures[i].Item2.altitude - dist * Math.Tan(captures[i].Item1)) / dTan;

                heights.Add(condorUDPDataLast.altitude + dL * Math.Tan(alfaLast));
            }
            if (heights.Count() == 0)
                return false;

            height = Accord.Statistics.Measures.Median(heights.ToArray());

            return true;
        }

        public void LogicalAdd (double alfa, CondorUDPData cud)
        {
            alfaLast = alfa;
            condorUDPDataLast.myClone(cud);

            if (captures.Count() == 0)
            {
                Tuple<double, CondorUDPData> cap = Tuple.Create(alfa, cud);
                captures.Add(cap);
            }
            else
            {
                if (cud.time + dTimeStep < captures.Last().Item2.time)// every 5 sec add
                {
                    Tuple<double, CondorUDPData> cap = Tuple.Create(alfa, cud);
                    captures.Add(cap);
                }
            }
        }


        internal static int BestMatch(float timeRef, RectangleF rect, List<CloudBase> cloudBases)
        {
            int iBestMatch = -1;
            double dBestIUO = 0; // double.MaxValue;
            for (int i = 0; i < cloudBases.Count(); i++)
            {
                var dIUO = cloudBases[i].IUO(rect);
                if (dIUO > dBestIUO /*&& cloudBases[i].condorUDPDataLast.time != timeRef*/)
                {
                    dBestIUO = dIUO;
                    iBestMatch = i;
                }
            }
            return iBestMatch;
        }

        public static void Clean(float timeRef, List<CloudBase> cloudBases)
        {
            for (int i = cloudBases.Count() - 1; i >= 0; i--)
            {
                if (timeRef > cloudBases[i].condorUDPDataLast.time) // anything older than 11 sec, remove
                {
                    cloudBases.RemoveAt(i);
                }
            }
        }

        public static bool CalcCloudBaseHeight(List<CloudBase> cloudBases, out double dNewCloadBaseHeight, out double dSmooth)
        {
            dSmooth = 0;
            dNewCloadBaseHeight = 0;
            int iIndexMax = 0;
            List<double> heights = new List<double>();
            for (int i = 0; i < cloudBases.Count(); i++)
            {
                if (iIndexMax < cloudBases[i].index)
                    iIndexMax = cloudBases[i].index;

                if (cloudBases[i].CalcCloudBaseHeight(out double dHeight))
                {
                    {
                        heights.Add(dHeight);
                    }
                }
            }
            if (heights.Count() == 0)
                return false;

            dNewCloadBaseHeight = Accord.Statistics.Measures.Median(heights.ToArray());
            dSmooth = 0.3;// Math.Min (0.3, 200.0 / iIndexMax);
            return true;
        }
    }

    public class YoloData
    {
        public bool bTowPlanePresent { set; get; }
        public float dTowPlaneX { set; get; }
        public float dTowPlaneY { set; get; }
        public float dTowPlaneBank { set; get; }
        public PointF dTowLeftWing { set; get; }
        public PointF dTowRightWing { set; get; }


    }

}

