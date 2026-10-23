using CondorAutoPilot.AI;
using GMap.NET.MapProviders;
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
using System.Windows.Forms.DataVisualization.Charting;

namespace CondorAutoPilot.ThermalML
{
    public partial class CThermalML : Form
    {
        private MapCondor myMap = new MapCondor();

        private WeightCollections myWeightCollections = new WeightCollections();
        private FlightPlanData myFlightPlanData = new FlightPlanData();
        private List<CondorUDPData> myCondorUDPDataLog = new List<CondorUDPData>();
        private TerrainMap myTerrainMap;
        private int iIteration = 0;
        private Point3d ptMap = new Point3d();
        //private Series mySeriesCorrelation = new Series();

        public CThermalML(FlightPlanData flightPlanData, TerrainMap terrainMap, List<CondorUDPData> condorUDPDataLog)
        {
            InitializeComponent();

            myFlightPlanData = flightPlanData;
            myTerrainMap = terrainMap;
            myCondorUDPDataLog = condorUDPDataLog;

            myWeightCollections.Init(dataGridView, 20, 12, -1, 1, 0.05f);

            myWeightCollections.InitDataGridView();


            //chartCorrelation.Series.Add(mySeriesCorrelation);
            ChartArea chartArea1 = new ChartArea();
            chartArea1.Name = "Correlation";
            chartCorrelation.ChartAreas.Add(chartArea1);
            chartCorrelation.ChartAreas[0].AxisX.Minimum = -2;
            chartCorrelation.ChartAreas[0].AxisX.Maximum = 2;
            chartCorrelation.ChartAreas[0].AxisY.Minimum = -2;
            chartCorrelation.ChartAreas[0].AxisY.Maximum = 2;

            Series mySeriesFunction = new Series();
            mySeriesFunction.ChartType = SeriesChartType.Point;
            mySeriesFunction.Name = "Function";
            chartCorrelation.Series.Add(mySeriesFunction);


            myMap.Init(ref gMapLiftML, GMapProviders.GoogleTerrainMap, 8);
            myMap.AddFlightPlan(myFlightPlanData, true);


        }

        private void button1_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 50; i++)
            {
                MakeNewSets();
                Calc(iIteration == 0);
                Sort();
                textBox1.Text = myWeightCollections.sets[0].score.ToString();
                textBox1.Refresh();
            }
            FillDataGrid();
            FillChart();

            iIteration++;
        }

        private void FillDataGrid()
        {
            myWeightCollections.FillDataGridView();
        }

        private void FillChart()
        {
            chartCorrelation.Series[0].Points.Clear();
            chartCorrelation.Series[1].Points.Clear();
            chartCorrelation.Series[2].Points.Clear();

            for (int i = 0; i < myCondorUDPDataLog.Count(); i += 25)
            {
                double dValue0 = 0;
                double dValue1 = 0;
                if (myTerrainMap.GetSpecialMLValue(
                    myCondorUDPDataLog[i].x,
                    myCondorUDPDataLog[i].y,
                    myCondorUDPDataLog[i].altitude,
                    myWeightCollections.sets[0],
                    out dValue0
                    ) &&
                    myTerrainMap.GetSpecialMLValue(
                    myCondorUDPDataLog[i].x,
                    myCondorUDPDataLog[i].y,
                    myCondorUDPDataLog[i].altitude,
                    myWeightCollections.sets[19],
                    out dValue1
                    )
                    )
                {
                    chartCorrelation.Series[0].Points.AddXY(dValue0, myCondorUDPDataLog[i].vario / myFlightPlanData.windSpeed);
                    chartCorrelation.Series[1].Points.AddXY(dValue1, myCondorUDPDataLog[i].vario / myFlightPlanData.windSpeed);
                    if (myCondorUDPDataLog[i].vario != 0)
                    {
                        double x = 0;
                    }
                }

                if (myTerrainMap.GetSpecialNode(
                    myCondorUDPDataLog[i].x,
                    myCondorUDPDataLog[i].y,
                    out Node node
                    ))
                {
                    chartCorrelation.Series[2].Points.AddXY(node.functionF, myCondorUDPDataLog[i].vario / myFlightPlanData.windSpeed);
                }
            }
        }

        private void Calc(bool bInit)
        {
            int iStart = 5;
            if (bInit)
                iStart = 0;

            for (int i = iStart; i < myWeightCollections.sets.Count; i++)
            {
                CalcSet(i);
            }

        }

        private void CalcSet(int iSet)
        {
            double dError = 0;
            int iCount = 0;
            for (int i = 0; i < myCondorUDPDataLog.Count(); i += 10)
            {
                double dValue = 0;
                if (myTerrainMap.GetSpecialMLValue(
                    myCondorUDPDataLog[i].x,
                    myCondorUDPDataLog[i].y,
                    myCondorUDPDataLog[i].altitude,
                    myWeightCollections.sets[iSet],
                    out dValue
                    ))
                {
                    double dE = dValue - myCondorUDPDataLog[i].vario / myFlightPlanData.windSpeed;

                    if (dValue * myCondorUDPDataLog[i].vario < 0)
                        dE *= 2;

                    dError += dE * dE * Math.Abs(myCondorUDPDataLog[i].vario / myFlightPlanData.windSpeed);


                    if (myCondorUDPDataLog[i].vario != 0)
                    {
                        double x = 0;
                        iCount++;
                    }

                }
            }
            myWeightCollections.sets[iSet].score = Math.Sqrt(dError) / iCount;
        }

        private void Sort()
        {
            bool bSort = true;
            while (bSort)
            {
                bSort = false;
                for (int i = 0; i < myWeightCollections.sets.Count - 1; i++)
                {
                    if (myWeightCollections.sets[i].score > myWeightCollections.sets[i + 1].score)
                    {
                        myWeightCollections.Swap(i, i + 1);
                        bSort = true;
                    }
                }
            }
        }

        private void MakeNewSets()
        {
            for (int i = 0; i < 5; i++)
            {
                myWeightCollections.MutateFrom(i, i + 5);
                if (i < 5)
                    myWeightCollections.MateWith(i, i + 1, i + 10);
                else
                    myWeightCollections.MateWith(0, 2, i + 10);

                myWeightCollections.Randomize(i + 15);
            }
        }

        private void gMapLiftML_MouseClick(object sender, MouseEventArgs e)
        {
            double lat = gMapLiftML.FromLocalToLatLng(e.X, e.Y).Lat;
            double lng = gMapLiftML.FromLocalToLatLng(e.X, e.Y).Lng;
            ptMap = new Point3d(Convert.ToSingle(lat), Convert.ToSingle(lng));
            //for (int i = 0; i < 5; i++)
            //{
                ptMap.Z = 2000;
                myMap.AddMLTerrainMap(myTerrainMap, ptMap, myWeightCollections.sets[0]);
            MakeSection();
            //    System.Threading.Thread.Sleep(500);
            //}


        }

        private void MakeSection()
        {
            Bitmap bitmapSection = new Bitmap(pictureBoxSection.Width, pictureBoxSection.Height);
            Graphics gfx = Graphics.FromImage(bitmapSection);

            gfx.SmoothingMode =
                    System.Drawing.Drawing2D.SmoothingMode.AntiAlias;



            gfx.FillRectangle(new SolidBrush(Color.White), 0, 0, bitmapSection.Width, bitmapSection.Height);

            for (float x = -5000; x <= 5000; x += 50)
            {
                Point3d pt = ptMap.add(x, myFlightPlanData.vecWind);
                for (float z = 500; z <= 5500; z += 50)
                {
                    PointF[] ptScreen = new PointF[4];
                    ptScreen[0].X = (1 + (x - 25) / 5000) * bitmapSection.Width / 2;
                    ptScreen[0].Y = (1 - (z - 25) / 5000) * bitmapSection.Height;
                    ptScreen[1].X = (1 + (x + 25) / 5000) * bitmapSection.Width / 2;
                    ptScreen[1].Y = (1 - (z - 25) / 5000) * bitmapSection.Height;
                    ptScreen[2].X = (1 + (x + 25) / 5000) * bitmapSection.Width / 2;
                    ptScreen[2].Y = (1 - (z + 25) / 5000) * bitmapSection.Height;
                    ptScreen[3].X = (1 + (x - 25) / 5000) * bitmapSection.Width / 2;
                    ptScreen[3].Y = (1 - (z + 25) / 5000) * bitmapSection.Height;
                    SolidBrush brush;
                    pt.Z = z;
                    myTerrainMap.GetHeight(pt, out float fHeight);
                    if (fHeight > pt.Z)
                    {
                        brush = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
                    }
                    else
                    {
                        if (!myTerrainMap.GetSpecialMLValue(pt.X, pt.Y, z, myWeightCollections.sets[0], out double dValue))
                        {
                            brush = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
                        }
                        else
                        {
                            int iColorRed = Convert.ToInt32(Utils.interpolate(dValue, -0.5, 0, 0.5, 255));
                            int iColorGreen = Convert.ToInt32(Utils.interpolate(dValue, -0.5, 0, 0, 255));
                            if (dValue > 0)
                                iColorGreen = Convert.ToInt32(Utils.interpolate(dValue, 0, 255, 0.5, 0));
                            int iColorBlue = Convert.ToInt32(Utils.interpolate(dValue, -0.5, 255, 0.5, 0));
                            brush = new SolidBrush(Color.FromArgb(120, iColorRed, iColorGreen, iColorBlue));
                        }
                    }
                    gfx.FillPolygon(brush, ptScreen);
                }
            }

            pictureBoxSection.Image = bitmapSection;

        }

        private void dataGridView_MouseMove(object sender, MouseEventArgs e)
        {
            myWeightCollections.dataGridView_MouseMove(sender, e);
        }

        private void dataGridView_MouseDown(object sender, MouseEventArgs e)
        {
            myWeightCollections.dataGridView_MouseDown(sender, e);
        }

        private void dataGridView_DragDrop(object sender, DragEventArgs e)
        {
            myWeightCollections.dataGridView_DragDrop(sender, e);
        }

        private void dataGridView_DragOver(object sender, DragEventArgs e)
        {
            myWeightCollections.dataGridView_DragOver(sender, e);
        }
    }
}

    //    Chart chart = new Chart();
    //    Series series = new Series();
    //    ChartArea chartArea1 = new ChartArea();
    //    chartArea1.Name = "ChartArea1";
    // chart.ChartAreas.Add(chartArea1);
    // series.BorderWidth = 2;
    // series.BorderDashStyle = ChartDashStyle.Solid;
    // series.ChartType = SeriesChartType.Line;
    // series.Color = Color.Green;
    // for (int i = 0; i<values.Count; i++)
    // {
    //     series.Points.AddXY(i, values[i]);
    // }
    //chart.BorderlineColor = Color.Red;
    // chart.BorderlineWidth = 1;
    // chart.Series.Add(series);
    // chart.Titles.Add(title);
    // chart.Invalidate();
    // chart.Palette = ChartColorPalette.Fire;
    // chartArea1.AxisY.Minimum = values.Min();
    // chartArea1.AxisY.Maximum = values.Max();
    // AddChartInRuntime(chart);


