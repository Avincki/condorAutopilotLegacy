using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace CondorAutoPilot.ControlViewer
{
    public partial class ControlViewer : Form
    {
        private List<CondorUDPData> myCondorUDPDataLog = null;
        private bool bRun = true;

        private SolidBrush brushWhite = new SolidBrush(Color.White);
        private SolidBrush brushYellow = new SolidBrush(Color.Yellow);
        private SolidBrush brushYellowish = new SolidBrush(Color.FromArgb(255,215,0));
        private SolidBrush brushBlack = new SolidBrush(Color.Black);
        private SolidBrush brushGreen = new SolidBrush(Color.Green);
        private SolidBrush brushCyan = new SolidBrush(Color.Cyan);
        private SolidBrush brushRed = new SolidBrush(Color.Red);
        private SolidBrush brushDarkSlateGray = new SolidBrush(Color.DarkSlateGray);
        private SolidBrush brushLightSteelBlue = new SolidBrush(Color.LightSteelBlue);

        public ControlViewer()
        {
            InitializeComponent();
        }

        public void Stop()
        {
            bRun = false;
            //Properties.Settings.Default.
        }

        public void Init(List<CondorUDPData> condorUDPDataLog)
        {
            myCondorUDPDataLog = condorUDPDataLog;

            chartSpeed.ChartAreas[0].AxisX.Minimum = 0;
            chartSpeed.ChartAreas[0].AxisX.Maximum = 100;
            chartSpeed.ChartAreas[0].AxisY.Minimum = 75;
            chartSpeed.ChartAreas[0].AxisY.Maximum = 275;
            chartSpeed.ChartAreas[0].AxisY.Interval = 25;

            chartHeading.ChartAreas[0].AxisX.Minimum = 0;
            chartHeading.ChartAreas[0].AxisX.Maximum = 100;
            chartHeading.ChartAreas[0].AxisY.Minimum = 0;
            chartHeading.ChartAreas[0].AxisY.Maximum = 360;
            chartHeading.ChartAreas[0].AxisY.Interval = 45;

            chartRange.ChartAreas[0].AxisX.Minimum = 0;
            chartRange.ChartAreas[0].AxisX.Maximum = 100;
            chartRange.ChartAreas[0].AxisY.Minimum = 0;
            chartRange.ChartAreas[0].AxisY.Maximum = 100000;

        }

        private double ToCompasHeading(double dHeading)
        {
            double dOut = dHeading - 90;
            if (dOut < 0)
                dOut += 360;

            return dOut;
        }

        private void FillChart()
        {
            chartSpeed.Series[0].Points.Clear();
            chartSpeed.Series[1].Points.Clear();

            chartHeading.Series[0].Points.Clear();
            chartHeading.Series[1].Points.Clear();

            chartRange.Series[0].Points.Clear();
            chartRange.Series[1].Points.Clear();

            int iRange = 0;
            iRange = Math.Min(101, myCondorUDPDataLog.Count);

            for (int i = 1; i < iRange; i++)
            {
                var condorUDPData = myCondorUDPDataLog[myCondorUDPDataLog.Count - i];
                chartSpeed.Series[0].Points.AddXY(i - 1, condorUDPData.targetSpeed * 3.6);
                chartSpeed.Series[1].Points.AddXY(i - 1, condorUDPData.getSpeed() * 3.6);
                chartHeading.Series[0].Points.AddXY(i - 1, ToCompasHeading(condorUDPData.targetHeading));
                chartHeading.Series[1].Points.AddXY(i - 1, ToCompasHeading(condorUDPData.getHeading()));

                chartRange.Series[0].Points.AddXY(i - 1, condorUDPData.targetRange);
                chartRange.Series[1].Points.AddXY(i - 1, condorUDPData.range);
            }
            for (int i = 1; i < chartHeading.Series[0].Points.Count(); i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (Math.Abs(chartHeading.Series[j].Points[i - 1].YValues[0] -
                        chartHeading.Series[j].Points[i].YValues[0]) > 180)
                        chartHeading.Series[j].Points[i].Color = Color.Transparent;
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!bRun)
                Close();
            if (myCondorUDPDataLog == null)
                return;
            if (!Properties.Settings.Default.ControlRun)
                return;


            FillChart();
            updateStick();
        }

        private PointF addVectors(PointF pt, float dX, float dY)
        {
            SizeF vec = new SizeF(dX , dY);
            PointF ptOut = PointF.Add(pt, vec);
            return ptOut;
        }


        private void updateStick()
        {


            Bitmap bitmapBoxStick = new Bitmap(pictureBoxStick.Width, pictureBoxStick.Height);
            Graphics gfx = Graphics.FromImage(bitmapBoxStick);

            gfx.SmoothingMode =
                    System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            gfx.FillRectangle(brushDarkSlateGray, 0, 0, pictureBoxStick.Width, pictureBoxStick.Height);

            Pen pen = new Pen(Color.White, 1);
            int nLines = 6;
            for (int i = 0; i <= nLines; i++)
            {
                gfx.DrawLine(pen, 10 + (pictureBoxStick.Width - 20) * i / nLines, 0, 10 + (pictureBoxStick.Width - 20) * i / nLines, pictureBoxStick.Height);
                gfx.DrawLine(pen, 0, 10 + (pictureBoxStick.Height - 20) * i / nLines, pictureBoxStick.Width, 10 + (pictureBoxStick.Height - 20) * i / nLines);
            }

            if (myCondorUDPDataLog == null || myCondorUDPDataLog.Count() == 0)
                return;

            PointF ptCenter = new PointF(pictureBoxStick.Width / 2, pictureBoxStick.Height / 2);

            if (myCondorUDPDataLog.Last().stickRoll != 0)
            {
                float offset = 20;
                if (myCondorUDPDataLog.Last().stickRoll < 0)
                    offset = -20;
                PointF[] curveArrow = new PointF[5];
                curveArrow[0] = addVectors(ptCenter, 0, 20);
                curveArrow[1] = addVectors(ptCenter, myCondorUDPDataLog.Last().stickRoll + offset, 20);
                curveArrow[2] = addVectors(ptCenter, myCondorUDPDataLog.Last().stickRoll + 2 * offset, 0);
                curveArrow[3] = addVectors(ptCenter, myCondorUDPDataLog.Last().stickRoll + offset, -20);
                curveArrow[4] = addVectors(ptCenter, 0, -20);
                gfx.FillPolygon(brushRed, curveArrow);
            }

            if (myCondorUDPDataLog.Last().stickPitch != 0)
            {
                float offset = 20;
                if (myCondorUDPDataLog.Last().stickPitch < 0)
                    offset = -20;
                PointF[] curveArrow = new PointF[5];
                curveArrow[0] = addVectors(ptCenter, 20, 0);
                curveArrow[1] = addVectors(ptCenter, 20, myCondorUDPDataLog.Last().stickPitch + offset);
                curveArrow[2] = addVectors(ptCenter, 0, myCondorUDPDataLog.Last().stickPitch + 2 * offset);
                curveArrow[3] = addVectors(ptCenter, -20, myCondorUDPDataLog.Last().stickPitch + offset);
                curveArrow[4] = addVectors(ptCenter, -20, 0);
                gfx.FillPolygon(brushYellowish, curveArrow);
            }

            gfx.FillEllipse(brushLightSteelBlue, ptCenter.X - 40, ptCenter.Y - 40, 80, 80);

            if (
                !double.IsNaN(myCondorUDPDataLog.Last().targetTurn) &&
                !double.IsNaN(myCondorUDPDataLog.Last().targetPitch) &&
                !double.IsNaN(myCondorUDPDataLog.Last().targetBank)
                )
            {
                float cosBank = Convert.ToSingle(Math.Cos(myCondorUDPDataLog.Last().targetBank));
                float sinBank = Convert.ToSingle(Math.Sin(myCondorUDPDataLog.Last().targetBank));
                float centX = ptCenter.X + 200 * myCondorUDPDataLog.Last().targetTurn;
                float centY = ptCenter.Y - 100 * myCondorUDPDataLog.Last().targetPitch;
                Pen penGreen = new Pen(new SolidBrush(Color.LawnGreen), 5);
                gfx.DrawLine(penGreen,
                    centX + 200 * cosBank, centY - 200 * sinBank,
                    centX - 200 * cosBank, centY + 200 * sinBank
                    );
                gfx.DrawLine(penGreen,
                    centX + 50 * sinBank, centY + 50 * cosBank,
                    centX - 50 * sinBank, centY - 50 * cosBank
                    );
            }


            pictureBoxStick.Image = bitmapBoxStick;
        }

        private void ControlViewer_Load(object sender, EventArgs e)
        {
            this.Top = Properties.Settings.Default.ControlTop;
            this.Left = Properties.Settings.Default.ControlLeft;
        }

        private void ControlViewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Properties.Settings.Default.ControlTop = this.Top;
            //Properties.Settings.Default.ControlLeft = this.Left;
        }
    }

    public class ControlViewerThread
    {
        private List<CondorUDPData> myCondorUDPDataLog = null;
        public ControlViewer myControlViewer = null;
        public ControlViewerThread()
        {

        }

        public void Init(List<CondorUDPData> condorUDPDataLog)
        {
            myCondorUDPDataLog = condorUDPDataLog;
        }

        public void Load()
        {
            Properties.Settings.Default.ControlRun = false;
            myControlViewer = new ControlViewer();
            myControlViewer.Init(myCondorUDPDataLog);
            myControlViewer.ShowDialog();
        }
        public void Run()
        {
            Properties.Settings.Default.ControlRun = true;
        }
        public void ThreadStop()
        {
            if (myControlViewer != null)
                myControlViewer.Stop();
        }
    }

}
