using CondorAutoPilot.AI;
using GMap.NET;
using GMap.NET.WindowsForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.ML;
using Microsoft.ML.Data;
//using Microsoft.ML.Transforms.Onnx;

namespace CondorAutoPilot
{
    public class ML_Updraft
    {
        MLContext myMlContext = new MLContext();
        PredictionEngine<OnnxInput, OnnxOutput> myOnnxPredictionEngine = null;

        const int INPUT_FIELDS_COUNT = 12;
        const float fHeightNorm = 4000;

        public class OnnxInput
        {
            [VectorType(1, INPUT_FIELDS_COUNT)]
            [ColumnName("dense_input")]
            public float[] denseInput { get; set; }
        }

        public class OnnxOutput
        {
            [ColumnName("dense_3")]
            public float[] denseOutput { get; set; }
        }
        private ITransformer GetPredictionPipeline()
        {

            //data columns for input
            var inputColumns = new string[] { "dense_input" };
            //data columns for output 
            var outputColumns = new string[] { "dense_3" };

            string strUpdraftONNXFile = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            strUpdraftONNXFile = strUpdraftONNXFile + @"\updraft.onnx";


            //creates a pipeline with the given model starting with the input columns and resulting in the output columns
            var onnxPredictionPipeline = myMlContext.Transforms.ApplyOnnxModel(
                outputColumnNames: outputColumns,
                inputColumnNames: inputColumns,
                strUpdraftONNXFile);

            var emptyDv = myMlContext.Data.LoadFromEnumerable(new OnnxInput[] { });

            return onnxPredictionPipeline.Fit(emptyDv);
        }

        public bool Initialize()
        {
            var onnxPredictionPipeline = GetPredictionPipeline();

            //Create prediction engine based on the input and output variables
            myOnnxPredictionEngine = myMlContext.Model.CreatePredictionEngine<OnnxInput, OnnxOutput>(onnxPredictionPipeline);

            return true;
        }

        public bool Calc(float fPlaneHeight, Node node, out double fPrediction)
        {

            float[] dist = new float[] { -500, -250, 0, 250, 500, 1000, 1500, 2500, 5000, 10000 };

            OnnxInput inputdata = new OnnxInput();
            inputdata.denseInput = new float[INPUT_FIELDS_COUNT];
            inputdata.denseInput[0] = fPlaneHeight / fHeightNorm;
            inputdata.denseInput[1] = node.functionF;
            for (int j = 0; j < dist.Length; j++)
            {

                if (j == 2)
                    inputdata.denseInput[2 + j] = node.yolo[j] /= fHeightNorm;
                else
                    inputdata.denseInput[2 + j] = (node.height - node.yolo[j]) / fHeightNorm;
            }


            var prediction = myOnnxPredictionEngine.Predict(inputdata);
            if (prediction.denseOutput.Count() == 0)
            {
                fPrediction = 0;
                return false;
            }
            fPrediction = prediction.denseOutput[0] * 10;
            return true;
        }


    }
    public class Node
    {
        public short height;
        private float normalx;
        private float normaly;
        public float weight;
        public float function;
        public float functionF;
        public bool boundary;
        public float[] yolo;


        public Node()
        {
            height = 0;
            weight = 0;
            function = 0;
            functionF = 0;
            yolo = new float[10];
            boundary = true;
        }

        public Vector3d getNormal()
        {
            Vector3d vecOut = new Vector3d(normalx, normaly, 1);
            vecOut.normalize();

            return vecOut;
        }

        public void setNormal(Vector3d vec)
        {
            normalx = vec.x / vec.z;
            normaly = vec.y / vec.z;
        }

        public float safeHeight()
        {
            float fSafeHeight = Convert.ToSingle(height + 75 / Math.Max(0.15, getNormal().z));
            if (function < 0)
                fSafeHeight -= Convert.ToSingle(200 * function);
            return fSafeHeight;

        }

    }

    public class TRN3
    {
        private float fTRNSize = 64 * 90;

        private int iTRNSize = 64 * 3 + 1;
        public Node[,] Data { get; set; }
        public float RefX()
            {return  NX * fTRNSize; }
        public float RefY()
            {return  NY * fTRNSize; }
        public int NX { get; set; }
        public int NY { get; set; }

        private int nTRN3Size = 64 * 3 + 1;
        public void ToUTM(int i, int j, out float UTMX, out float UTMY)
        {
            UTMX = i * 30 + RefX();
            UTMY = j * 30 + RefY();
        }

        public TRN3()
        {
            Data = new Node[64 * 3 + 1,64 * 3 + 1];

            Parallel.For(0, iTRNSize, (i) =>
            {
                for (int j = 0; j < iTRNSize; j++)
                {
                    Data[i, j] = new Node();
                }
            });
        }

        public bool FillML_UpdraftData(TerrainMap terrainMap, FlightPlanData flightPlanData)
        {
            float[] dist = new float[] { -500, -250, 0, 250, 500, 1000, 1500, 2500, 5000, 10000 };
            Parallel.For(0, iTRNSize, (i) =>
            {
                for (int j = 0; j < iTRNSize; j++)
                {
                    ToUTM(i, j, out float UTMX, out float UTMY);
                    for (int k = 0; k < dist.Length; k++)
                    {
                        float x = UTMX - dist[k] * flightPlanData.vecWind.x;
                        float y = UTMY - dist[k] * flightPlanData.vecWind.y;
                        if (terrainMap.GetHeight(x, y, out float fHeight, true))
                        {
                            Data[i, j].yolo[k] = fHeight;
                        }
                     }
                }
            });
            return false;
        }

        public bool FillWithHeightMap(string TrnLandScape, int nx, int ny)
        {

            string strTRN3Path = Path.GetTempPath() +
                "CondorAutoPilot\\Landscapes\\" +
                TrnLandScape + "\\HeightMaps\\";


            string strFile = "h" + nx.ToString("D2") + ny.ToString("D2") + ".tr3";
            try
            {
                BinaryReader binReader =
                    new BinaryReader(File.Open(strTRN3Path + strFile, FileMode.Open), Encoding.ASCII);
                try
                {

                    for (int k = 0; k < nTRN3Size; k++)
                    {
                        for (int l = 0; l < nTRN3Size; l++)
                        {
                            Data[k, l].height = binReader.ReadInt16();
                        }
                    }

                }
                catch (IOException ioexp)
                {
                    //                    myTextBox.Text = ioexp.ToString();
                    //                    myTextBox.Refresh();
                    binReader.Close();
                    Console.WriteLine(strFile + " -> Failed");
                    return false;
                }
                binReader.Close();
                //Properties.Settings.Default.InfoWindow = "1;" + strFile;

                Console.WriteLine(strFile + " -> OK");

            }
            catch (IOException ioexp)
            {
                //                myTextBox.Text = ioexp.ToString();
                //                myTextBox.Refresh();
                return false;
            }
            return true;

        }


        public void CalculateUpWindWeights(ref Vector3d vecWind)
        {
            for (int j = 0; j < nTRN3Size; j++)
            {
                for (int i = 0; i < nTRN3Size; i++)
                {
                    float dDot = -vecWind.dotProduct(Data[i,j].getNormal());
                    if (dDot > 0.2)
                        dDot = 0.16f+dDot * dDot;
                    //else if (dDot < 0)
                    //    dDot = -0.2f;
                    //else
                    //    dDot = 0;

                    Data[i, j].weight = dDot;
                    Data[i, j].function = 0;
                    Data[i, j].functionF = 0;
                }
            }
        }

        public void CalculateNormals()
        {
            for (int n3y = 0; n3y < nTRN3Size; n3y++)
            {
                for (int n3x = 0; n3x < nTRN3Size; n3x++)
                {
                    int i_p, i_n, j_p, j_n;
                    float d_x = 0;
                    float d_y = 0;

                    if (n3x == 0)
                        i_n = 0;
                    else
                    {
                        i_n = n3x - 1;
                        d_x += 15;
                    }
                    if (n3x == nTRN3Size - 1)
                        i_p = n3x;
                    else
                    {
                        i_p = n3x + 1;
                        d_x += 15;
                    }

                    if (n3y == 0)
                        j_n = 0;
                    else
                    {
                        j_n = n3y - 1;
                        d_y += 15;
                    }
                    if (n3y == nTRN3Size - 1)
                        j_p = n3y;
                    else
                    {
                        j_p = n3y + 1;
                        d_y += 15;
                    }

                    Vector3d vecX = new Vector3d(
                        new Point3d(-d_x, 0, Data[i_n,n3y].height),
                        new Point3d(d_x, 0, Data[i_p,n3y].height)
                        );
                    Vector3d vecY = new Vector3d(
                        new Point3d(0, -d_y, Data[n3x,j_n].height),
                        new Point3d(0, d_y, Data[n3x,j_p].height)
                        );

                    Vector3d venN = vecX.crossProduct(vecY);
                    venN.normalize();
                    if (venN.z < 0)
                        venN.negate();
                    Data[i_p, j_p].setNormal(venN);
                }

            }
        }
        private void SolveNode(int n3x, int n3y, ref double dLastAccuracy)
        {
            double k = 1;
            double dT = 0.1;

            double df, dfin, dfip, dfjp, dfjn;
            df = Data[n3x,n3y].function;

            if (n3x == 0)
            {
                dfin = df;
            }
            else
            {
                dfin = Data[n3x - 1,n3y].function;
            }

            if (n3x == nTRN3Size - 1)
            {
                dfip = df;
            }
            else
            {
                dfip = Data[n3x + 1,n3y].function;
            }

            if (n3y == 0)
            {
                dfjn = df;
            }
            else
            {
                dfjn = Data[n3x,n3y - 1].function;
            }

            if (n3y == nTRN3Size - 1)
            {
                dfjp = df;
            }
            else
            {
                dfjp = Data[n3x,n3y + 1].function;
            }
            double dFunction =
                Data[n3x,n3y].weight
                - 4.2 * k * df
                + k * dfin
                + k * dfip
                + k * dfjn
                + k * dfjp
                ;

            dLastAccuracy = Math.Max(dLastAccuracy, Math.Abs(dFunction));

            Data[n3x,n3y].function += Convert.ToSingle(dT * dFunction);

        }


 
        private void SolveNodeCoarse(int n3x, int n3y, ref double dLastAccuracy)
        {
            double k = 1;
            double dT = 0.1;

            double df, dfin, dfip, dfjp, dfjn;
            df = Data[n3x,n3y].function;


            if (n3x <= 1)
            {
                dfin = df;
            }
            else
            {
                dfin = Data[n3x - 3,n3y].function;
            }

            if (n3x >= nTRN3Size - 3)
            {
                dfip = df;
            }
            else
            {
                dfip = Data[n3x + 3,n3y].function;
            }

            if (n3y <= 1)
            {
                dfjn = df;
            }
            else
            {
                dfjn = Data[n3x,n3y - 3].function;
            }

            if (n3y >= nTRN3Size - 3)
            {
                dfjp = df;
            }
            else
            {
                dfjp = Data[n3x,n3y + 3].function;
            }
            double dFunction =
                Data[n3x,n3y].weight
                - 4.3 * k * df
                + k * dfin
                + k * dfip
                + k * dfjn
                + k * dfjp
                ;

            dLastAccuracy = Math.Max(dLastAccuracy, Math.Abs(dFunction));

            Data[n3x,n3y].function += Convert.ToSingle(dT * dFunction);
            Data[Math.Max(0, n3x - 1),n3y].function = Data[n3x,n3y].function;
            Data[Math.Min(nTRN3Size - 2, n3x + 1),n3y].function = Data[n3x,n3y].function;
            Data[n3x,Math.Max(0, n3y - 1)].function = Data[n3x,n3y].function;
            Data[n3x,Math.Min(nTRN3Size - 2, n3y + 1)].function = Data[n3x,n3y].function;

        }

        public void SolveFunction()
        {
            double dLastAccuracy = double.MaxValue;
            int iter = 0;

            while (dLastAccuracy > 0.001 && iter < 100)
            {
                dLastAccuracy = 0;
                iter++;

                for (int n3y = 0; n3y < nTRN3Size; n3y++)
                {
                    for (int n3x = 0; n3x < nTRN3Size; n3x++)
                    {
                        SolveNode(n3x, n3y, ref dLastAccuracy);
                    }
                }

                for (int n3y = nTRN3Size - 1; n3y >= 0; n3y--)
                {
                    for (int n3x = nTRN3Size - 1; n3x >= 0; n3x--)
                    {
                        SolveNode(n3x, n3y, ref dLastAccuracy);
                    }
                }
            }


        }
        public void SolveFunctionCoarse()
        {
            double dLastAccuracy = double.MaxValue;
            int iter = 0;

            while (dLastAccuracy > 0.001 && iter < 100)
            {
                dLastAccuracy = 0;
                iter++;
                int nMax = 0;
                for (int n3y = 1; n3y < nTRN3Size - 1; n3y += 3)
                {
                    nMax = Math.Max(nMax, n3y);
                    for (int n3x = 1; n3x < nTRN3Size - 1; n3x += 3)
                    {
                        SolveNodeCoarse(n3x, n3y, ref dLastAccuracy);
                    }
                    //int nJ = (nTRN3Size - 1) / 3;
                    //Parallel.For(0, nJ, (j) =>
                    //{
                    //    int n3x = 1 + j * 3;
                    //    if (n3x < nTRN3Size - 1)
                    //        SolveNodeCoarse(n3x, n3y, ref dLastAccuracy);
                    //});
                }

                for (int n3y = nMax; n3y >= 1; n3y -= 3)
                {
                    for (int n3x = nMax; n3x >= 1; n3x -= 3)
                    {
                        SolveNodeCoarse(n3x, n3y, ref dLastAccuracy);
                    }
                    //int nJ = (nTRN3Size - 1) / 3;
                    //Parallel.For(0, nJ, (j) =>
                    //{
                    //    int n3x = nMax - j * 3;
                    //    if (n3x >= 1)
                    //        SolveNodeCoarse(n3x, n3y, ref dLastAccuracy);
                    //});

                }
            }
            for (int n3y = 1; n3y < nTRN3Size - 1; n3y += 3)
            {
                for (int n3x = 1; n3x < nTRN3Size - 1; n3x += 3)
                {
                    for (int i = -1; i < 2; i++)
                    {
                        for (int j = -1; j < 2; j++)
                        {
                            Data[n3x + i, n3y + j].function = Data[n3x, n3y].function;
                        }
                    }
                }
            }


        }
    }
    public class TerrainMap
    {
        public int TrnSizeX { get; set; }
        public int TrnSizeY { get; set; }
        public float TrnDX { get; set; }
        public float TrnDY { get; set; }
        public float TrnDZ { get; set; }
        public float TrnRefX { get; set; }
        public float TrnRefY { get; set; }
        public int TrnUTMX { get; set; }
        public Char TrnUTMY { get; set; }
        public string TrnLandScape { get; set; }

        public FlightPathPoint AntiCollisionDirectionTarget { get; set; }
        public bool bNeedAvoidance { get; set; }
        public bool bFreeLeft { get; set; }
        public bool bFreeRight { get; set; }
        public bool bRidgeLeft { get; set; }
        public bool bRidgeRight { get; set; }
        public bool bRidge { get; set; }

        public Vector3d vecRidge { get; set; }

        public float dClearanceReferenceHeight { get; set; }

        public double dWeightLeft { get; set; }
        public double dWeightRight { get; set; }
        public double dWeightRidgeLeft { get; set; }
        public double dWeightRidgeRight { get; set; }

        public float dHeightFor100Km { get; set; }
        public float dHeightFor75Km { get; set; }
        public float dHeightFor40Km { get; set; }
        public float dHeightFor10Km { get; set; }



        public double myDistanceForFinalApproach { get; set; }
        public double myDistanceLeftToGo { get; set; }

        public Vector3d vecWind = new Vector3d();
        public FlightPathPoint CurrentFlightPoint { get; set; }

        public FlightPathPoint UpDraftTarget { get; set; }
        public FlightPathPoint UpDraftPotentialTarget { get; set; }
        public bool HasUpDraftPotentialTarget { get; set; }
        public bool IsAboveUpDraftPotentialTarget { get; set; }


        public bool IsInFinalApproach { get; set; }
        public double NeedActionToMeetFinalDestination;

        internal TextBox myTextBox;

        private ML_Updraft myML_Updraft = new ML_Updraft();

        private List<TRN3> TRNmatrices = new List<TRN3>();
        private List<List<short>> TRNIndexList = new List<List<short>>();

        public int NumberOfTRNMatrices()
        {
            return TRNmatrices.Count;
        }


        private int nTRN3Size = 64 * 3 + 1;
        private float fTRNSize = 64 * 90;
        private bool bInitialized = false;


        public double dWeightLeftSmoothed = 0;
        public double dWeightRightSmoothed = 0;


        public float dSideOffsetForUpDraft { get; set; }

        float dTanFlightAngle = Convert.ToSingle(Math.Tan(Utils.deg2rad(2.4)));
        float dTanSafeFlightAngle = Convert.ToSingle(Math.Tan(Utils.deg2rad(2.7)));
        double dHeightAboveFinalTarget = 350;


        public bool InitMap(FlightPlanData flightPlan, TextBox textBoxPhoto)
        {
            myML_Updraft.Initialize();
            UpDraftPotentialTarget = new FlightPathPoint();
            HasUpDraftPotentialTarget = false;

            myTextBox = textBoxPhoto;

            vecRidge = new Vector3d();
            dClearanceReferenceHeight = 0;

            dSideOffsetForUpDraft = 0;
            IsInFinalApproach = false;
            if (flightPlan.StrLandscape.Equals(""))
                return false;
            vecWind = flightPlan.vecWind;

            TrnLandScape = flightPlan.StrLandscape;
            string strTRNFile = Properties.Settings.Default.CondorApplicationPath + 
                "\\Landscapes\\" +
                TrnLandScape + "\\" +
                TrnLandScape + ".trn";

            BinaryReader binReader =
                new BinaryReader(File.Open(strTRNFile, FileMode.Open), Encoding.ASCII);
            try
            {
                TrnSizeX = binReader.ReadInt32();
                TrnSizeY = binReader.ReadInt32();
                TrnDX = binReader.ReadSingle();
                TrnDY = binReader.ReadSingle();
                TrnDZ = binReader.ReadSingle();
                TrnRefX = binReader.ReadSingle();
                TrnRefY = binReader.ReadSingle();
                TrnUTMX = binReader.ReadInt32();
                TrnUTMY = binReader.ReadChar();
                binReader.Close();

            }
            catch (IOException ioexp)
            {
                string strMessage = $"\nError:  {ioexp.Message}";
                binReader.Close();
                return false;
            }
            bInitialized = Init_V2(ref textBoxPhoto);


            Form1.naviCon64.init(this);

            return bInitialized;
        }

        
        private bool Init_V2(ref TextBox textBoxPhoto)
        {
            AntiCollisionDirectionTarget = new FlightPathPoint();
            UpDraftTarget = new FlightPathPoint();

            string strTRN3Path = Path.GetTempPath() +
                "CondorAutoPilot\\Landscapes\\" +
                TrnLandScape + "\\HeightMaps\\";

            int numberX = TrnSizeX / 64;
            int numberY = TrnSizeY / 64;

            TRNIndexList.Clear();

            for (int i = 0; i < numberX; i++)
            {
                List<short> setI = new List<short>();
                for (int j = 0; j < numberY; j++)
                {
                    setI.Add(-1);
                }
                TRNIndexList.Add(setI);
            }


            return true;
        }

        public void InitFlightPathRange(double dRange, FlightPlanData flightPlanData, TextBox textBoxInfo)
        {
            int numberX = TrnSizeX / 64;
            int numberY = TrnSizeY / 64;
            textBoxInfo.Text = "Initializing 1/3";
            textBoxInfo.Refresh();

            int iTotal = numberX * numberY;
            for (int nx = 0; nx < numberX; nx++)
            {
                //Parallel.For(0, numberY, (ny) =>
                //{

                for (int ny = 0; ny < numberY; ny++)
                {
                    int n = numberY * nx + ny;
                    Properties.Settings.Default.InfoWindow = "1;" + n.ToString() + ";"+ iTotal.ToString() ;
                    if (IsInFlightPathRange(nx, ny, dRange, flightPlanData.Markers))
                    {
                        TRN3 trn3 = new TRN3();
                        trn3.NX = nx;
                        trn3.NY = ny;
                        TRNmatrices.Add(trn3);
                        TRNIndexList[nx][ny] = Convert.ToInt16(TRNmatrices.Count() - 1);


                        //if (trn3.FillWithHeightMap(TrnLandScape, nx, ny))
                        //{
                        //    trn3.CalculateNormals();
                        //    trn3.CalculateUpWindWeights(ref vecWind);
                        //    TRNmatrices.Add(trn3);
                        //    TRNIndexList[nx][ny] = Convert.ToInt16(TRNmatrices.Count() - 1);
                        //}


                    }
                }//);
            }

            int nProcessed = 0;

            Parallel.For(0, TRNmatrices.Count(), (iTRN) =>
            {
                nProcessed++;

                int nx, ny;
                nx = TRNmatrices[iTRN].NX;
                ny = TRNmatrices[iTRN].NY;

                if (TRNmatrices[iTRN].FillWithHeightMap(TrnLandScape, nx, ny))
                {
                    TRNmatrices[iTRN].CalculateNormals();
                    TRNmatrices[iTRN].CalculateUpWindWeights(ref vecWind);

                }
                else
                {
                    //ToDo set 
                    TRNIndexList[nx][ny] = -1;
                }


                Properties.Settings.Default.InfoWindow = "2;" + nProcessed.ToString() + ";" + TRNmatrices.Count().ToString();


            });


            textBoxInfo.Text = "Initializing 2/3";
            textBoxInfo.Refresh();

            nProcessed = 0;

            Parallel.For(0, TRNmatrices.Count(), (iTRN) =>
            {
                nProcessed++;
                TRNmatrices[iTRN].SolveFunctionCoarse();

                string msgconsole = nProcessed.ToString() + "  " + iTRN.ToString() + "/" + TRNmatrices.Count().ToString();
                Console.WriteLine(msgconsole);

                Properties.Settings.Default.InfoWindow = "3;" + nProcessed.ToString() + ";" + TRNmatrices.Count().ToString();


            });

            textBoxInfo.Text = "Initializing 3/3";
            textBoxInfo.Refresh();

            nProcessed = 0;

            Parallel.For(0, TRNmatrices.Count(), (iTRN) =>
            {
                nProcessed++;
                TRNmatrices[iTRN].FillML_UpdraftData(this, flightPlanData);

                string msgconsole = nProcessed.ToString() + "  " + iTRN.ToString() + "/" + TRNmatrices.Count().ToString();
                Console.WriteLine(msgconsole);

                Properties.Settings.Default.InfoWindow = "4;" + nProcessed.ToString() + ";" + TRNmatrices.Count().ToString();


            });


            textBoxInfo.Text = "Initializing Done";
            textBoxInfo.Refresh();



        }
        public bool IsInFlightPathRange(int nx, int ny, double dRange, List<FlightPlanMarker> markers)
        {
            for (int i = 1; i < markers.Count(); i++)
            {
                Point3d pt1 = markers[i - 1].getPoint();
                Point3d pt2 = markers[i].getPoint();
                Vector3d vec = new Vector3d(pt1, pt2);
                double dLen = vec.length();
                vec.normalize();
                Vector3d vecPerp = vec.crossProduct(Vector3d.WorldZ());

                for (int j = 0; j < 2; j++)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        Point3d pt = new Point3d((nx + j) * fTRNSize, (ny + k) * fTRNSize, 0);
                        Vector3d vecRange = new Vector3d(pt1, pt);
                        var dx = vecRange.dotProduct(vec);
                        var dy = vecRange.dotProduct(vecPerp);
                        if (dx > -dRange && dx < dLen + dRange && dy > -dRange && dy < dRange)
                            return true;
                    }
                }
            }

            return false;
        }

        

        private bool ReadTRN3_V2(int nx, int ny)
        {

            string strTRN3Path = Path.GetTempPath() +
                "CondorAutoPilot\\Landscapes\\" +
                TrnLandScape + "\\HeightMaps\\";


            string strFile = "h" + nx.ToString("D2") + ny.ToString("D2") + ".tr3";
            Properties.Settings.Default.InfoWindow = "0;" + strFile;
            try
            {
                BinaryReader binReader =
                    new BinaryReader(File.Open(strTRN3Path + strFile, FileMode.Open), Encoding.ASCII);
                try
                {
                    TRN3 trn3 = new TRN3();
                    trn3.NX = nx;
                    trn3.NY = ny;

                    for (int k = 0; k < nTRN3Size; k++)
                    {
                        List<Node> setK = new List<Node>();
                        for (int l = 0; l < nTRN3Size; l++)
                        {
                            //Node node = new Node();
                            //node.height = binReader.ReadInt16();
                            trn3.Data[k, l].height = binReader.ReadInt16(); ;
                            //                        setK.Add(node);
                        }
                        //                    trn3.Data.Add(setK);
                    }
                    trn3.CalculateNormals();
                    trn3.CalculateUpWindWeights(ref vecWind);
                    trn3.SolveFunctionCoarse();

                    TRNmatrices.Add(trn3);
                    TRNIndexList[nx][ny] = Convert.ToInt16(TRNmatrices.Count() - 1);
                }
                catch (IOException ioexp)
                {
//                    myTextBox.Text = ioexp.ToString();
//                    myTextBox.Refresh();
                    binReader.Close();
                    return false;
                }
                binReader.Close();

            }
            catch (IOException ioexp)
            {
//                myTextBox.Text = ioexp.ToString();
//                myTextBox.Refresh();
                return false;
            }

            return true;
        }


        public bool IdTR3(float UTMX, float UTMY, out int nx, out int ny, bool bSafe = false)
        {
            nx = Convert.ToInt32(Math.Floor(UTMX / fTRNSize));
            ny = Convert.ToInt32(Math.Floor(UTMY / fTRNSize));

            if (nx >= TrnSizeX / 64 || ny >= TrnSizeY / 64 || nx < 0 || ny < 0)
            {
                return false;
            }

            if(TRNIndexList[nx][ny] == -1)
            {
                if (bSafe)
                {
                    return false;
                }
                else if (!ReadTRN3_V2(nx, ny))
                {
                    return false;
                }
            }
            return true;

        }

        public bool GetNodeData(float UTMX, float UTMY, out Node node)
        {
            
            if (!IdTR3(UTMX, UTMY, out int nx, out int ny))
            {
                node = new Node();
                return false;
            }
                

            float D3X = UTMX - nx * fTRNSize;
            float D3Y = UTMY - ny * fTRNSize;

            int n3x = Convert.ToInt32(Math.Round(D3X / 30));
            int n3y = Convert.ToInt32(Math.Round(D3Y / 30));

            if (n3x >= nTRN3Size || n3y >= nTRN3Size)
            {
                node = new Node();
                return false;
            }

            if (TRNIndexList[nx][ny] < 0)
            {
                node = new Node();
                return false;
            }


            node = TRNmatrices[TRNIndexList[nx][ny]].Data[n3x,n3y];
            return true;

        }
        public bool GetSafeNodeData(float UTMX, float UTMY, out Node node)
        {
            if (!IdTR3(UTMX, UTMY, out int nx, out int ny))
            {
                node = new Node();
                return false;
            }

            float D3X = UTMX - nx * fTRNSize;
            float D3Y = UTMY - ny * fTRNSize;

            int n3x = Convert.ToInt32(Math.Round(D3X / 30));
            int n3y = Convert.ToInt32(Math.Round(D3Y / 30));

            if (n3x >= nTRN3Size || n3y >= nTRN3Size)
            {
                node = new Node();
                return false;
            }

            if (TRNIndexList[nx][ny] < 0)
            {
                node = new Node();
                return false;
            }

            node = TRNmatrices[TRNIndexList[nx][ny]].Data[n3x,n3y];
            return true;

        }
        public bool GetNodeDataRef(float UTMX, float UTMY, ref Node node)
        {
            if (!IdTR3(UTMX, UTMY, out int nx, out int ny))
                return false;

            float D3X = UTMX - nx * fTRNSize;
            float D3Y = UTMY - ny * fTRNSize;

            int n3x = Convert.ToInt32(Math.Round(D3X / 30));
            int n3y = Convert.ToInt32(Math.Round(D3Y / 30));

            if (n3x >= nTRN3Size || n3y >= nTRN3Size)
            {
                return false;
            }

            if (TRNIndexList[nx][ny] < 0)
            {
                node = new Node();
                return false;
            }

            node = TRNmatrices[TRNIndexList[nx][ny]].Data[n3x,n3y];
            return true;

        }

        public bool GetNodeDataCoarse(Point3d pt, out Node node)
        {
            return GetNodeDataCoarse(pt.X, pt.Y, out node);
        }

        public bool GetNodeDataCoarse(float UTMX, float UTMY, out Node node)
        {
            if (!IdTR3(UTMX, UTMY, out int nx, out int ny))
            {
                node = new Node();
                return false;
            }


            float D3X = UTMX - nx * fTRNSize;
            float D3Y = UTMY - ny * fTRNSize;

            int n3x = Convert.ToInt32(Math.Round(D3X / 90));
            int n3y = Convert.ToInt32(Math.Round(D3Y / 90));
            n3x = 3 * n3x + 1;
            n3y = 3 * n3y + 1;

            if (n3x >= nTRN3Size || n3y >= nTRN3Size)
            {
                node = new Node();
                return false;
            }

            if (TRNIndexList[nx][ny] < 0)
            {
                node = new Node();
                return false;
            }

            node = TRNmatrices[TRNIndexList[nx][ny]].Data[n3x,n3y];
            return true;

        }
        public bool GetSpecialNode(Point3d pt, out Node node)
        {
            return GetSpecialNode(pt.X, pt.Y, out node);
        }

        public bool GetSpecialNode(float UTMX, float UTMY, out Node node)
        {
            if (!GetNodeData(UTMX, UTMY, out node))
                return false;

            float dR = 3000;

            float heightM, heightP;
            if (GetSafeHeight(UTMX - vecWind.x * dR, UTMY - vecWind.y * dR, out heightM) 
                &&
                GetSafeHeight(UTMX + vecWind.x * dR, UTMY + vecWind.y * dR, out heightP)
                )
            {
                float fact = 1.0f*((2 * node.height - heightM - heightP) / dR + 1);
                if (fact < -0.5)
                    fact = -0.5f;

                node.functionF = node.function * fact + 0.2f*(fact-1);
            }
            else
            {
                node.functionF = node.function;
            }


            return true;
        }

        public bool GetSpecialMLValue(float UTMX, float UTMY, float dHPlane, WeightSet weightSet, out double dValue)
        {
            dValue = 0;
            if (!GetNodeData(UTMX, UTMY, out Node node))
                return false;

            float dR1 = 100 + weightSet.weights[3] * 10000;
            float dR2 = 100 + weightSet.weights[6] * 10000;
            float dR3 = 100 + weightSet.weights[9] * 10000;
            float fact1, fact2, fact3;

            float heightM1, heightP1;
            float heightM2, heightP2;
            float heightM3, heightP3;
            if (
                GetSafeHeight(UTMX - vecWind.x * dR1, UTMY - vecWind.y * dR1, out heightM1)
                &&
                GetSafeHeight(UTMX + vecWind.x * dR1, UTMY + vecWind.y * dR1, out heightP1)
                &&
                GetSafeHeight(UTMX - vecWind.x * dR2, UTMY - vecWind.y * dR2, out heightM2)
                &&
                GetSafeHeight(UTMX + vecWind.x * dR2, UTMY + vecWind.y * dR2, out heightP2)
                &&
                GetSafeHeight(UTMX - vecWind.x * dR3, UTMY - vecWind.y * dR3, out heightM3)
                &&
                GetSafeHeight(UTMX + vecWind.x * dR3, UTMY + vecWind.y * dR3, out heightP3)
                )
            {
                fact1 = weightSet.weights[0] * node.function + ((weightSet.weights[1] + weightSet.weights[2]) * node.height - weightSet.weights[1] * heightM1 - weightSet.weights[2] * heightP1) / dR1;
                fact2 = weightSet.weights[10] * node.function + ((weightSet.weights[4] + weightSet.weights[5]) * node.height - weightSet.weights[4] * heightM2 - weightSet.weights[5] * heightP2) / dR2;
                fact3 = weightSet.weights[11] * node.function + ((weightSet.weights[7] + weightSet.weights[8]) * node.height - weightSet.weights[7] * heightM3 - weightSet.weights[8] * heightP3) / dR3;

                double deltaHeight = dHPlane - node.height;

                if (deltaHeight < 1000)
                    dValue = Utils.interpolate(deltaHeight, 500, fact1, 1000, fact2);
                else
                    dValue = Utils.interpolate(deltaHeight, 1000, fact2, 2500, fact3);


            }
            else
            {
                dValue = node.function;
            }


            return true;
        }


        public bool GetHeight(Point3d pt, out float fHeight)
        {
            return GetHeight(pt.X, pt.Y, out fHeight);
        }

        public bool GetHeight(float UTMX, float UTMY, out float fHeight, bool bDoNotLoadTRN = false)
        {
            fHeight = 0;
            if (!IdTR3(UTMX, UTMY, out int nx, out int ny, bDoNotLoadTRN))
                return false;

            float D3X = UTMX - nx * fTRNSize;
            float D3Y = UTMY - ny * fTRNSize;

            int n3x = Convert.ToInt32(Math.Round(D3X / 30));
            int n3y = Convert.ToInt32(Math.Round(D3Y / 30));

            if (n3x >= nTRN3Size || n3y >= nTRN3Size)
            {
                fHeight = 0;
                return false;
            }

            fHeight = TRNmatrices[TRNIndexList[nx][ny]].Data[n3x,n3y].height;
            return true;
        }

        public bool GetSafeHeight(float UTMX, float UTMY, out float fHeight)
        {
            fHeight = 0;
            if (!IdTR3(UTMX, UTMY, out int nx, out int ny))
                return false;

            float D3X = UTMX - nx * fTRNSize;
            float D3Y = UTMY - ny * fTRNSize;

            int n3x = Convert.ToInt32(Math.Round(D3X / 30));
            int n3y = Convert.ToInt32(Math.Round(D3Y / 30));

            if (n3x >= nTRN3Size || n3y >= nTRN3Size)
            {
                fHeight = 0;
                return false;
            }

            if (TRNIndexList[nx][ny] < 0)
            {
                fHeight = 0;
                return false;
            }

            fHeight = TRNmatrices[TRNIndexList[nx][ny]].Data[n3x,n3y].height;

            if(GetNodeDataCoarse(UTMX, UTMY, out Node node))
            {
                if (node.function < 0)
                    fHeight -= Convert.ToSingle(300 * node.function);
            }
            return true;
        }



        public List<Tuple<double, Point3d[]>> getArea(float UTMX, float UTMY, FlightPathPoint flightPoint, int iSize, int iSpacing)
        {
            float DX = Convert.ToSingle(Math.Round(UTMX / 30)) * 30;
            float DY = Convert.ToSingle(Math.Round(UTMY / 30)) * 30;

            float dSpacingX = iSpacing * 30;
            float dSpacingY = iSpacing * 30;

            if (flightPoint.vec.x > 0)
            {
                dSpacingX = -dSpacingX;
            }
            if (flightPoint.vec.y > 0)
                dSpacingY = -dSpacingY;

            List<Tuple<double,Point3d[]>> result = new List<Tuple<double, Point3d[]>> ();
            for (int i = -iSize; i < iSize+1; i++)
            {
                
                for (int j = -iSize; j < iSize + 1; j++)
                {
                    Point3d[] resultLine = new Point3d[4];
                    float fHeight;
                    float dsx, dsy;
                    dsx = DX + i * dSpacingX;
                    dsy = DY + j * dSpacingY;
                    if (!GetSafeHeight(dsx, dsy, out fHeight))
                        continue;
                    resultLine[0] = new Point3d(dsx, dsy, fHeight);
                    dsx = DX + (1+i) * dSpacingX;
                    dsy = DY + j * dSpacingY;
                    if (!GetSafeHeight(dsx, dsy, out fHeight))
                        continue;
                    resultLine[1] = new Point3d(dsx, dsy, fHeight);
                    dsx = DX + (1+i) * dSpacingX;
                    dsy = DY + (1+j) * dSpacingY;
                    if (!GetSafeHeight(dsx, dsy, out fHeight))
                        continue;
                    resultLine[2] = new Point3d(dsx, dsy, fHeight);
                    dsx = DX + i * dSpacingX;
                    dsy = DY + (j+1) * dSpacingY;
                    if (!GetSafeHeight(dsx, dsy, out fHeight))
                        continue;
                    resultLine[3] = new Point3d(dsx, dsy, fHeight);

                    Vector3d vec13 = new Vector3d(resultLine[1], resultLine[3]);
                    Vector3d vec02 = new Vector3d(resultLine[0], resultLine[2]);
                    Vector3d vecNormal = vec02.crossProduct(vec13);
                    Point3d ptCenter = resultLine[0].add(0.5f, vec02);



                    Vector3d vecView = new Vector3d(flightPoint.getPosition(), ptCenter);
                    int n = 1;
                    if (vecNormal.z > 0)
                        n = -1;

                    if (n * vecView.dotProduct(vecNormal) < 0)
                        continue;

                    Tuple<double, Point3d[]> tuple = new Tuple<double, Point3d[]>(vecView.length(), resultLine);
                    result.Add(tuple);

                }
            }

            // Sort
            bool bNeedSort = true;
            while (bNeedSort)
            {
                bNeedSort = false;
                for (int i = 0; i < result.Count-1; i++)
                {
                    if (result[i].Item1 < result[i + 1].Item1)
                    {
                        Tuple<double, Point3d[]> tupletemp;
                        tupletemp = result[i];
                        result[i] = result[i + 1];
                        result[i + 1] = tupletemp;
                        bNeedSort = true;
                    }

                }
            }

            return result;
        }






        public void CalculateMinHeightForEndTarget(ref FlightPlanData flightPlan, FlightPathPoint flightPoint, out float dHeightMin)
        {
            NeedActionToMeetFinalDestination = 0;
            double dLengthStep = 100;//meter
            GetHeight(flightPlan.Markers.Last().X, flightPlan.Markers.Last().Y, out float dHeightStart);
            flightPlan.Markers.Last().Z = dHeightStart;
            double dHeightIntegrated = dHeightStart + dHeightAboveFinalTarget;
            IsInFinalApproach = false;
            myDistanceLeftToGo = 0;

            for (int i = flightPlan.Markers.Count-1; i > 0; i--)
            {
                double dirx = flightPlan.Markers[i - 1].X - flightPlan.Markers[i].X;
                double diry = flightPlan.Markers[i - 1].Y - flightPlan.Markers[i].Y;

                double dLengthSegment = Math.Sqrt(dirx * dirx + diry * diry);
                if (dLengthSegment == 0)
                    continue;
                dirx /= dLengthSegment;
                diry /= dLengthSegment;

                double dLSeg = 0;

                for (double dL = 0; dL < dLengthSegment; dL += dLengthStep)
                {
                    dHeightIntegrated += dLengthStep * dTanFlightAngle;
                    myDistanceLeftToGo += dLengthStep;
                    dLSeg += dLengthStep;

                    float dPosX = flightPlan.Markers[i].X + Convert.ToSingle(dirx * dL);
                    float dPosY = flightPlan.Markers[i].Y + Convert.ToSingle(diry * dL);


                    if (GetHeight(dPosX, dPosY, out float dHeightPos))
                    {
                        if (dHeightPos + 250 > dHeightIntegrated)
                            dHeightIntegrated = dHeightPos + 250;
                    }
                    if (dHeightIntegrated > 6000 || myDistanceLeftToGo > flightPlan.RemainingDistance)// bailout
                    {
                        dHeightMin = Convert.ToSingle(dHeightIntegrated);
                        IsInFinalApproach = myDistanceLeftToGo < myDistanceForFinalApproach;
                        NeedActionToMeetFinalDestination = 0;
                        return;
                    }
                }
                myDistanceLeftToGo += dLengthSegment - dLSeg;


            }

            dHeightMin = Convert.ToSingle(dHeightIntegrated);
            IsInFinalApproach = myDistanceLeftToGo < myDistanceForFinalApproach;
            NeedActionToMeetFinalDestination = dHeightMin  - flightPoint.z;

            return;
        }


        public void CalculateDistanceForFinalApproach(ref FlightPlanData flightPlan)
        {
            double dLengthStep = 100;//meter
            if (flightPlan.Markers.Count == 0)
                return;
            GetHeight(flightPlan.Markers.Last().X, flightPlan.Markers.Last().Y, out float dHeightStart);
            flightPlan.Markers.Last().Z = dHeightStart;
            double dHeightIntegrated = dHeightStart + dHeightAboveFinalTarget;
            myDistanceForFinalApproach = 0;

            for (int i = flightPlan.Markers.Count - 1; i > 0; i--)
            {
                double dirx = flightPlan.Markers[i - 1].X - flightPlan.Markers[i].X;
                double diry = flightPlan.Markers[i - 1].Y - flightPlan.Markers[i].Y;

                double dLengthSegment = Math.Sqrt(dirx * dirx + diry * diry);
                if (dLengthSegment == 0)
                    continue;
                dirx /= dLengthSegment;
                diry /= dLengthSegment;

                double dLSeg = 0;
                for (double dL = 0; dL < dLengthSegment; dL += dLengthStep)
                {
                    dLSeg += dLengthStep;
                    myDistanceForFinalApproach += dLengthStep;
                    dHeightIntegrated += dLengthStep * dTanFlightAngle;

                    float dPosX = flightPlan.Markers[i].X + Convert.ToSingle(dirx * dL);
                    float dPosY = flightPlan.Markers[i].Y + Convert.ToSingle(diry * dL);


                    if (GetHeight(dPosX, dPosY, out float dHeightPos))
                    {
                        if (dHeightPos + 250 > dHeightIntegrated)
                            dHeightIntegrated = dHeightPos + 250;
                    }

                    if (dHeightIntegrated > 4000)// bailout
                    {
                        return;
                    }
                }

                myDistanceForFinalApproach += dLengthSegment - dLSeg;
                dHeightIntegrated += (dLengthSegment - dLSeg) * dTanFlightAngle;
            }
            return;
        }

        public bool CalcRidgeDirection(FlightPathPoint flightPoint, Vector3d vecWind)
        {
            vecRidge.setNull();

            for (float dX = -1500; dX < 1500; dX += 90)
            {
                for (float dY = -1500; dY < 1500; dY += 90)
                {
                    float ptX = flightPoint.x + dX;
                    float ptY = flightPoint.y + dY;
                    //Node node = new Node();

                    if (!GetSafeNodeData(ptX, ptY, out Node node))
                        continue;

                    //float fact = Math.Min(1 - (Math.Abs(dX / 1400)), 1 - (Math.Abs(dY / 1400)));

                    Vector3d vecN = node.getNormal();
                    vecRidge.add(vecN);
                }
            }
            if (vecWind.dotProduct(vecRidge) < 0)
                vecRidge.negate();

            vecRidge.add(vecWind);

            vecRidge.normalizeXY();
            return true;
        }

        public bool CheckForRidge_V2(FlightPathPoint flightPoint)
        {
            float dSafetyHeight = 150;

            dClearanceReferenceHeight = 0;

            float dWeightRidgeLeft = 0;
            float dWeightRidgeRight = 0;

            for (int iSide = -1; iSide < 2; iSide += 2)
            {
                float dWeight = 0;
                for (float dX = -500; dX < 1500; dX += 90)
                {
                    for (float dY = 20; dY < 800; dY += 90)
                    {
                        float ptX = flightPoint.x + dX * flightPoint.vec.x - iSide * dY * flightPoint.vec.y;
                        float ptY = flightPoint.y + dX * flightPoint.vec.y + iSide * dY * flightPoint.vec.x;

                        if (!GetNodeData(ptX, ptY, out Node node))
                            continue;

                        if (dClearanceReferenceHeight < node.height)
                            dClearanceReferenceHeight = node.height;

                        if (node.height + dSafetyHeight < flightPoint.z)
                            continue;

                        dWeight += node.height + dSafetyHeight - flightPoint.z;

                        Vector3d vecNor = node.getNormal();

                        if (flightPoint.getWind().dotProduct(vecNor) < 0)
                        {
                            vecNor.negate();
                            vecRidge.add(vecNor);
                        }
                        else
                        {
                            vecRidge.add(vecNor);
                        }

                    }
                }

                if (iSide == -1)
                    dWeightRidgeLeft = dWeight;
                else
                    dWeightRidgeRight = dWeight;
            }

            if (dWeightRidgeLeft == 0 && dWeightRidgeRight == 0)
            {
                bRidgeLeft = false;
                bRidgeRight = false;
            }
            else if (dWeightRidgeLeft > dWeightRidgeRight)
            {
                bRidgeLeft = true;
                bRidgeRight = false;
            }
            else
            {
                bRidgeLeft = false;
                bRidgeRight = true;
            }
            bRidge = bRidgeLeft || bRidgeRight;
            return bRidge;
        }



        //public void CalculateNeededHeightForDistance(
        //    ref FlightPlanData flightPlan,
        //    FlightPathPoint flightPoint
        //    )
        //{
        //    dHeightFor100Km = 0;
        //    dHeightFor75Km = 0;
        //    dHeightFor40Km = 0;
        //    dHeightFor10Km = 0;

        //    float dSafetyHeight = 150;
        //    float dTS100 = 100 * dTanSafeFlightAngle;
        //    float dT100 = 100 * dTanFlightAngle;

        //    int iDist = Convert.ToInt32(flightPlan.RemainingDistance / 100);
        //    int iStart = Math.Min(Math.Max(flightPlan.Path100mInterval.Count - iDist + 50, 0), flightPlan.Path100mInterval.Count-1);// stay 5km away
        //    int iEnd100 = Math.Min(iStart + 1000, flightPlan.Path100mInterval.Count - 1);
        //    int iEnd75 = Math.Min(iStart + 750, flightPlan.Path100mInterval.Count - 1);
        //    int iEnd40 = Math.Min(iStart + 400, flightPlan.Path100mInterval.Count - 1);
        //    int iEnd10 = Math.Min(iStart + 100, flightPlan.Path100mInterval.Count - 1);

        //    for (int i = iEnd100; i >= iStart; i--)
        //    {
        //        if (flightPlan.Path100mInterval[i].ptOnPath.Z + dSafetyHeight > dHeightFor100Km)
        //            dHeightFor100Km = flightPlan.Path100mInterval[i].ptOnPath.Z + dSafetyHeight;
        //        dHeightFor100Km += dT100;

        //        if (i < iEnd75)
        //        {
        //            if (flightPlan.Path100mInterval[i].ptOnPath.Z + dSafetyHeight > dHeightFor75Km)
        //                dHeightFor75Km = flightPlan.Path100mInterval[i].ptOnPath.Z + dSafetyHeight;
        //            dHeightFor75Km += dT100;
        //        }

        //        if (i < iEnd40)
        //        {
        //            if (flightPlan.Path100mInterval[i].ptOnPath.Z + dSafetyHeight > dHeightFor40Km)
        //                dHeightFor40Km = flightPlan.Path100mInterval[i].ptOnPath.Z + dSafetyHeight;
        //            dHeightFor40Km += dTS100;
        //        }

        //        if (i < iEnd10)
        //        {
        //            if (flightPlan.Path100mInterval[i].ptOnPath.Z + dSafetyHeight > dHeightFor10Km)
        //                dHeightFor10Km = flightPlan.Path100mInterval[i].ptOnPath.Z + dSafetyHeight;
        //            dHeightFor10Km += dTS100;
        //        }
        //    }


        //    float dxpre = flightPoint.x - flightPlan.Path100mInterval[iStart].ptOnPath.X;
        //    float dypre = flightPoint.y - flightPlan.Path100mInterval[iStart].ptOnPath.Y;
        //    float dlpre = Convert.ToSingle(Math.Sqrt(dxpre * dxpre + dypre * dypre));
        //    if (dlpre == 0)
        //        return;

        //    dxpre /= dlpre;
        //    dypre /= dlpre;

        //    float dPos = 0;
        //    while (dPos < dlpre)
        //    {
        //        dPos += 100;
        //        if (GetHeight(
        //            flightPlan.Path100mInterval[iStart].ptOnPath.X + dPos * dxpre,
        //            flightPlan.Path100mInterval[iStart].ptOnPath.Y + dPos * dypre,
        //            out float dHeight
        //            ))
        //        {
        //            dHeight += dSafetyHeight;
        //            if (dHeight > dHeightFor100Km)
        //                dHeightFor100Km = dHeight;
        //            dHeightFor100Km += dT100;

        //            if (dHeight > dHeightFor75Km)
        //                dHeightFor75Km = dHeight;
        //            dHeightFor75Km += dT100;

        //            if (dHeight > dHeightFor40Km)
        //                dHeightFor40Km = dHeight;
        //            dHeightFor40Km += dTS100;

        //            if (dHeight > dHeightFor10Km)
        //                dHeightFor10Km = dHeight;
        //            dHeightFor10Km += dTS100;
 
        //        }
        //    }
        //}

        //public void Solve(float UTMX, float UTMY)
        //{
        //    if (!IdTR3(UTMX, UTMY, out int nx, out int ny))
        //        return;

        //    Solve(nx, ny, 0.001, 100);
        //}

        private void SolveNode(int trnIndex, int n3x, int n3y, ref double dLastAccuracy)
        {
            double k = 1;
            double dT = 0.1;

            double df, dfin, dfip, dfjp, dfjn;
            df = TRNmatrices[trnIndex].Data[n3x,n3y].function;

            if (n3x == 0)
            {
                TRNmatrices[trnIndex].ToUTM(n3x - 1, n3y, out float UTMX, out float UTMY);
                Node node = new Node();
                if (GetNodeData(UTMX, UTMY, out node))
                {
                    dfin = node.function;
                }
                else
                {
                    dfin = df;
                }
            }
            else
            {
                dfin = TRNmatrices[trnIndex].Data[n3x - 1,n3y].function;
            }

            if (n3x == nTRN3Size - 1)
            {
                TRNmatrices[trnIndex].ToUTM(n3x + 1, n3y, out float UTMX, out float UTMY);
                Node node = new Node();
                if (GetNodeData(UTMX, UTMY, out node))
                {
                    dfip = node.function;
                }
                else
                {
                    dfip = df;
                }
            }
            else
            {
                dfip = TRNmatrices[trnIndex].Data[n3x + 1,n3y].function;
            }

            if (n3y == 0)
            {
                TRNmatrices[trnIndex].ToUTM(n3x, n3y - 1, out float UTMX, out float UTMY);
                Node node = new Node();
                if (GetNodeData(UTMX, UTMY, out node))
                {
                    dfjn = node.function;
                }
                else
                {
                    dfjn = df;
                }
            }
            else
            {
                dfjn = TRNmatrices[trnIndex].Data[n3x,n3y - 1].function;
            }

            if (n3y == nTRN3Size - 1)
            {
                TRNmatrices[trnIndex].ToUTM(n3x, n3y + 1, out float UTMX, out float UTMY);
                Node node = new Node();
                if (GetNodeData(UTMX, UTMY, out node))
                {
                    dfjp = node.function;
                }
                else
                {
                    dfjp = df;
                }
            }
            else
            {
                dfjp = TRNmatrices[trnIndex].Data[n3x,n3y + 1].function;
            }
            double dFunction =
                TRNmatrices[trnIndex].Data[n3x,n3y].weight
                - 4.5 * k * df
                + k * dfin
                + k * dfip
                + k * dfjn
                + k * dfjp
                ;

            dLastAccuracy = Math.Max(dLastAccuracy, Math.Abs(dFunction));

            TRNmatrices[trnIndex].Data[n3x,n3y].function += Convert.ToSingle(dT * dFunction);

        }



        public void FillMap(ref GMapControl mapControl, int iOverlay, ref int lastTRNIndexDrawn)
        {


            double functionMax = 0;

           // mapControl.Overlays[iOverlay].Polygons.Clear();
            for (int n3t = lastTRNIndexDrawn; n3t < TRNmatrices.Count(); n3t++)
            {
                for (int n3y = 1; n3y < nTRN3Size; n3y += 3)
                {
                    for (int n3x = 1; n3x < nTRN3Size; n3x += 3)
                    {
                        double function = TRNmatrices[n3t].Data[n3x,n3y].function;
                        if (function > 0.2)
                        {
                            functionMax = Math.Max(functionMax, function);
                            List<PointLatLng> poly = new List<PointLatLng>();
                            float dLat;
                            float dLong;
                            TRNmatrices[n3t].ToUTM(n3x, n3y, out float x, out float y);
                            Form1.naviCon64.calcLatLon(x - 45, y + 45, out dLat, out dLong);
                            poly.Add(new PointLatLng(dLat, dLong));
                            Form1.naviCon64.calcLatLon(x + 45, y + 45, out dLat, out dLong);
                            poly.Add(new PointLatLng(dLat, dLong));
                            Form1.naviCon64.calcLatLon(x + 45, y - 45, out dLat, out dLong);
                            poly.Add(new PointLatLng(dLat, dLong));
                            Form1.naviCon64.calcLatLon(x - 45, y - 45, out dLat, out dLong);
                            poly.Add(new PointLatLng(dLat, dLong));



                            GMapPolygon polyMap = new GMapPolygon(poly, "");
                            if (function < 0.6)
                            {
                                int iColor = Convert.ToInt32(Math.Min((function) * 400, 255));
                                polyMap.Fill = new SolidBrush(Color.FromArgb(100, 0 , iColor, 255 - iColor));
                            }
                            else
                            {
                                int iColor = Convert.ToInt32(Math.Min((function - 0.6) * 400, 255));
                                polyMap.Fill = new SolidBrush(Color.FromArgb(100 + iColor/2, iColor, 255 - iColor, 0));
                            }
                            polyMap.Stroke = new Pen(Color.Transparent, 1);
                            mapControl.Overlays[iOverlay].Polygons.Add(polyMap);


                        }
                    }
                }
            }
            lastTRNIndexDrawn = TRNmatrices.Count();
        }

        public void FillMLLocalMap(ref GMapControl mapControl, int iOverlay, Point3d ptCenter, WeightSet weightSet)
        {
            float fx = Convert.ToSingle(Math.Round(ptCenter.X / 90) * 90);
            float fy = Convert.ToSingle(Math.Round(ptCenter.Y / 90) * 90);

            float dRadius = 5000;
            for (float dX = -dRadius; dX < dRadius; dX += 90)
            {
                for (float dY = -dRadius; dY < dRadius; dY += 90)
                {
                    double dR = Math.Sqrt(dX * dX + dY * dY);
                    if (dR > dRadius - 70)
                        continue;
                    float x = fx + dX;
                    float y = fy + dY;

                    //Node node = new Node();

                    if (!GetSpecialMLValue(x, y, ptCenter.Z, weightSet, out double dValue))
                        continue;

                    if (!GetNodeData(x, y, out Node node))
                        continue;

                    List<PointLatLng> poly = new List<PointLatLng>();
                    float dLat;
                    float dLong;

                    Form1.naviCon64.calcLatLon(x - 45, y + 45, out dLat, out dLong);
                    poly.Add(new PointLatLng(dLat, dLong));
                    Form1.naviCon64.calcLatLon(x + 45, y + 45, out dLat, out dLong);
                    poly.Add(new PointLatLng(dLat, dLong));
                    Form1.naviCon64.calcLatLon(x + 45, y - 45, out dLat, out dLong);
                    poly.Add(new PointLatLng(dLat, dLong));
                    Form1.naviCon64.calcLatLon(x - 45, y - 45, out dLat, out dLong);
                    poly.Add(new PointLatLng(dLat, dLong));

                    GMapPolygon polyMap = new GMapPolygon(poly, "");


                    int iColorRed = Convert.ToInt32(Utils.interpolate(dValue, -0.5, 0, 0.5, 255));
                    int iColorGreen = Convert.ToInt32(Utils.interpolate(dValue, -0.5, 0, 0, 255));
                    if (dValue> 0)
                        iColorGreen = Convert.ToInt32(Utils.interpolate(dValue, 0, 255, 0.5, 0));
                    int iColorBlue = Convert.ToInt32(Utils.interpolate(dValue, -0.5, 255, 0.5, 0));

                    if (node.height > ptCenter.Z)
                        polyMap.Fill = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
                    else
                        polyMap.Fill = new SolidBrush(Color.FromArgb(120, iColorRed, iColorGreen, iColorBlue));
                    polyMap.Stroke = new Pen(Color.Transparent, 1);
                    mapControl.Overlays[iOverlay].Polygons.Add(polyMap);

                }
            }
        }

        public void FillLocalMap(ref GMapControl mapControl, int iOverlay, FlightPathPoint flightPoint)
        {
            float fx = Convert.ToSingle(Math.Round(flightPoint.x/90)*90);
            float fy = Convert.ToSingle(Math.Round(flightPoint.y/90)*90);

            float dRadius = 5000;
            for (float dX = -dRadius; dX < dRadius; dX += 90)
            {
                for (float dY = -dRadius; dY < dRadius; dY += 90)
                {
                    double dR = Math.Sqrt(dX * dX + dY * dY);
                    if (dR > dRadius - 70)
                        continue;
                    float x = fx + dX;
                    float y = fy + dY;

                    //Node node = new Node();

                    if (!GetSpecialNode(x, y, out Node node))
 //                       if (!GetNodeDataCoarse(x, y, out Node node))
                            continue;
                    double prediction;
                    myML_Updraft.Calc(flightPoint.z, node, out prediction);
//                  prediction = node.functionF;

                    List<PointLatLng> poly = new List<PointLatLng>();
                    float dLat;
                    float dLong;

                    Form1.naviCon64.calcLatLon(x - 45, y + 45, out dLat, out dLong);
                    poly.Add(new PointLatLng(dLat, dLong));
                    Form1.naviCon64.calcLatLon(x + 45, y + 45, out dLat, out dLong);
                    poly.Add(new PointLatLng(dLat, dLong));
                    Form1.naviCon64.calcLatLon(x + 45, y - 45, out dLat, out dLong);
                    poly.Add(new PointLatLng(dLat, dLong));
                    Form1.naviCon64.calcLatLon(x - 45, y - 45, out dLat, out dLong);
                    poly.Add(new PointLatLng(dLat, dLong));

                    GMapPolygon polyMap = new GMapPolygon(poly, "");
                    if (flightPoint.z - dR / 30 - 50 < node.height)
                    {
                        polyMap.Fill = new SolidBrush(Color.FromArgb(140, 0, 0, 0));
                    }
                    else if (prediction < 0.6 && prediction > 0.2)
                    {
                        int iColor = Convert.ToInt32(Math.Min((prediction) * 400, 255));
                        if (iColor < 0) iColor = 0;
                        polyMap.Fill = new SolidBrush(Color.FromArgb(100, 0, iColor, 255 - iColor));
                    }
                    else if (prediction > 0.6)
                    {
                        int iColor = Convert.ToInt32(Math.Min((prediction - 0.6) * 200, 255));
                        if (iColor < 0) iColor = 0;
                        polyMap.Fill = new SolidBrush(Color.FromArgb(100 + iColor / 2, iColor, 255 - iColor, 0));
                    }
                    else
                        continue;
                    polyMap.Stroke = new Pen(Color.Transparent, 1);
                    mapControl.Overlays[iOverlay].Polygons.Add(polyMap);

                }
             }

        }


        public void FillRangesMap(ref GMapControl mapControl, int iOverlay, FlightPathPoint flightPoint)
        {
            float fx = Convert.ToSingle(Math.Round(flightPoint.x / 90) * 90);
            float fy = Convert.ToSingle(Math.Round(flightPoint.y / 90) * 90);

            float dRadius = 5000;
            for (int i = 1; i < 5; i++)
            {
                switch (i)
                {
                    case 1:
                        dRadius = 5000;
                        break;
                    case 2:
                        dRadius = 10000;
                        break;
                    case 3:
                        dRadius = 25000;
                        break;
                    case 4:
                        dRadius = 50000;
                        break;
                }
                List<PointLatLng> pointsWay = new List<PointLatLng>();

                float dR = dRadius;

                for (int j = 0; j <= 360; j += 10)
                {
                    double dA = Utils.deg2rad(j);
                    float dlat, dlon;
                    float x, y;
                    x = fx + Convert.ToSingle(dR * Math.Cos(dA));
                    y = fy + Convert.ToSingle(dR * Math.Sin(dA));
                    Form1.naviCon64.calcLatLon(x, y, out dlat, out dlon);
                    pointsWay.Add(new PointLatLng(dlat, dlon));
                }
                GMapRoute route = new GMapRoute(pointsWay, "Ranges");
                route.Stroke = new Pen(Color.White, 2);
                mapControl.Overlays[iOverlay].Routes.Add(route);
            }

        }


        public bool getScanAccessibility(FlightPathPoint flightPoint, FlightPathPoint flightPointTarget, float dSafetyHeight = 25)
        {
            Vector3d vecDir = new Vector3d(flightPoint.getPosition(), flightPointTarget.getPosition());
            float dist = vecDir.length();
            vecDir.normalize();
            float safetyHeight = dSafetyHeight;
            if (flightPoint.getWind().dotProduct(flightPoint.getSpeedDirection()) < 0)
                safetyHeight = dSafetyHeight / 2;

            for (float pos = 30; pos < dist; pos += 30)
            {
                float dx = flightPoint.x + vecDir.x * pos;
                float dy = flightPoint.y + vecDir.y * pos;
                if (!GetSafeHeight(dx, dy, out float fHeight))
                    continue;
                if (flightPoint.z < fHeight + safetyHeight)
                    return false;


            }

            return true;
        }

        //public void makeTargetSafe(FlightPathPoint flightPoint, FlightPathPoint flightPointTarget)
        //{
        //    if (true)
        //        return;

        //    if (!bRidge)
        //        return;
        //    if (getScanAccessibility(flightPoint, flightPointTarget))
        //        return;
        //    Vector3d vecDir = new Vector3d(flightPoint.getPosition(), flightPointTarget.getPosition());
        //    float dist = vecDir.length();
        //    vecDir.normalize();
        //    if (bRidgeLeft)
        //    {
        //        flightPointTarget.Add(flightPointTarget, vecDir, 0, dist);
        //    }
        //    if (bRidgeRight)
        //    {
        //        flightPointTarget.Add(flightPointTarget, vecDir, 0, -dist);
        //    }

        //}




        //public void FillMapV2(ref GMapControl mapControl, int iOverlay)
        //{
        //    Node node = new Node();
        //    mapControl.Overlays[iOverlay].Clear();

        //    for (float dX = -3000; dX <= 3000; dX += 30)
        //    {
        //        for (float dY = -3000; dY <= 3000; dY += 30)
        //        {
        //            float posX = CurrentFlightPoint.x + dX;
        //            float posY = CurrentFlightPoint.y + dY;
        //            if (GetNodeDataRef(posX, posY, ref node))
        //            {
        //                List<PointLatLng> poly = new List<PointLatLng>();
        //                float dLat;
        //                float dLong;
        //                Form1.naviCon64.calcLatLon(posX - 15, posY + 15, out dLat, out dLong);
        //                poly.Add(new PointLatLng(dLat, dLong));
        //                Form1.naviCon64.calcLatLon(posX + 15, posY + 15, out dLat, out dLong);
        //                poly.Add(new PointLatLng(dLat, dLong));
        //                Form1.naviCon64.calcLatLon(posX + 15, posY - 15, out dLat, out dLong);
        //                poly.Add(new PointLatLng(dLat, dLong));
        //                Form1.naviCon64.calcLatLon(posX - 15, posY - 15, out dLat, out dLong);
        //                poly.Add(new PointLatLng(dLat, dLong));

        //                GMapPolygon polyMap = new GMapPolygon(poly, "");

        //                if (node.boundary)
        //                {
        //                    polyMap.Fill = new SolidBrush(Color.FromArgb(150, 0, 0, 0));
        //                }
        //                else if (node.functionF < 0)
        //                {
        //                    int iColor = Convert.ToInt32(Math.Min(node.functionF * 255, 255));
        //                    polyMap.Fill = new SolidBrush(Color.FromArgb(100, iColor, 255 - iColor, 0));
        //                }
        //                else
        //                {
        //                    int iColor = Convert.ToInt32(Math.Min(-node.functionF * 255, 255));
        //                    polyMap.Fill = new SolidBrush(Color.FromArgb(100, 0, iColor, 255 - iColor));

        //                }
        //                polyMap.Stroke = new Pen(Color.Transparent, 1);
        //                mapControl.Overlays[iOverlay].Polygons.Add(polyMap);


        //            }
        //        }
        //    }
        //}

        //public bool CheckUpDraftPotential(ref FlightPlanData flightPlanData, FlightPathPoint flightPoint)
        //{
        //    flightPlanData.GetPathIndexOn(200, out int iPos);
        //    MaxUpdraftIndex = Math.Max(iPos, MaxUpdraftIndex);
        //    int iPosEnd = Math.Min(MaxUpdraftIndex + 120, flightPlanData.Path100mInterval.Count);// 12 km forward

        //    float dT100 = 100 * dTanFlightAngle;

        //    float dPosHeight = flightPoint.z - dT100*5;

        //    UpDraftPotentialTarget = new FlightPathPoint();
        //    double dMaxSideFunction = 0;
        //    double dMaxFunction = 0;
        //    double dMaxFunctionClose = 0;

        //    int iStart = Math.Max(MaxUpdraftIndex - 3, 0);


        //    for (int i = iStart; i < iPosEnd; i++)
        //    {
        //        dPosHeight -= dT100;

        //        if ((flightPlanData.Path100mInterval[i].ptOnPath.Z - 100) > dPosHeight)
        //            continue;


        //        for (int iSide = -1; iSide < 2; iSide += 2)
        //        {
        //            for (float dPos = 100; dPos < 2000; dPos += 90)
        //            {
        //                float ptX = flightPlanData.Path100mInterval[i].ptOnPath.X + iSide * dPos * flightPlanData.Path100mInterval[i].vecDir.y;
        //                float ptY = flightPlanData.Path100mInterval[i].ptOnPath.Y - iSide * dPos * flightPlanData.Path100mInterval[i].vecDir.x;
        //                Node node = new Node();

        //                if (GetNodeDataCoarse(ptX, ptY, out node))
        //                {
        //                    if (node.height < dPosHeight)
        //                    {
        //                        double dFact = Math.Abs(dPos) / 5000;
        //                        if (i > MaxUpdraftIndex)
        //                        {
        //                            if (dMaxSideFunction < (node.function - dFact))
        //                            {
        //                                dMaxFunction = node.function;
        //                                dMaxSideFunction = node.function - dFact;
        //                                UpDraftPotentialTarget.x = ptX;
        //                                UpDraftPotentialTarget.y = ptY;
        //                                UpDraftPotentialTarget.z = node.height;
        //                                UpDraftPotentialTarget.dPosFunction = dPos;
        //                            }
        //                        }
        //                        if (i < MaxUpdraftIndex + 10)
        //                        {
        //                            if (dMaxFunctionClose < node.function)
        //                            {
        //                                dMaxFunctionClose = node.function;
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    HasUpDraftPotentialTarget = dMaxFunction > 0.9;
        //    IsAboveUpDraftPotentialTarget = dMaxFunctionClose > 0.85;
        //    return HasUpDraftPotentialTarget;
        //}



        //public bool CheckUpDraftLocationV3(FlightPathPoint flightPoint, ref FlightPlanData myFlightPlanData)
        //{
        //    myFlightPlanData.GetPathIndexOnMinimum(500, out int iPos5);
        //    Vector3d vecDir = new Vector3d(flightPoint.getPosition(),
        //        myFlightPlanData.Path100mInterval[iPos5].ptOnPath.add(10000, myFlightPlanData.Path100mInterval[iPos5].vecDir));
        //    vecDir.normalize();
        //    Vector3d vecPerp = new Vector3d(-vecDir.y, vecDir.x, 0);

        //    if (myFlightPlanData.ParallelDistanceToNextWayPoint(flightPoint.x, flightPoint.y) < 3000)
        //    {
        //        dSideOffsetForUpDraft = 0;
        //        UpDraftTarget = myFlightPlanData.getNextMarkerPosition(true);
        //        return true;
        //    }
        //    else
        //    {

        //        float yMaxPos = 0;
        //        float yOffset = 90 * 25;
        //        double functionMaxPos = 0;
        //        for (float y = -yOffset; y <= yOffset; y += 90)
        //        {
        //            double yFunctionmax = 0;
        //            for (float x = 2 * Math.Abs(y); x < 8000; x += 90)
        //            {
        //                Point3d pt = flightPoint.getPosition();
        //                //pt.x = myFlightPlanData.Path100mInterval[iPos2].ptOnPath.x;
        //                //pt.y = myFlightPlanData.Path100mInterval[iPos2].ptOnPath.y;
        //                pt = pt.add(x, vecDir);
        //                pt = pt.add(y, vecPerp);

        //                Node node = new Node();
        //                GetNodeDataCoarse(pt.X, pt.Y, out node);
        //                if (node.height < flightPoint.z + 200 && node.height > flightPoint.z - 3000 && (node.function > 0.6 || node.function < 0.1))
        //                {
        //                    double dd = node.function;
        //                    if (node.function < 0.1)
        //                        dd = -0.2;

        //                    yFunctionmax += dd * (14000 - x + 2 * Math.Abs(y)) / 14000 * (node.height - (flightPoint.z - 3000)) / 3000;
        //                }
        //            }
        //            yFunctionmax *= 4000 / (4000 - 0.8f * Math.Abs(y));
        //            if (yFunctionmax > functionMaxPos)
        //            {
        //                functionMaxPos = yFunctionmax;
        //                yMaxPos = y;
        //            }
        //        }

        //        dSideOffsetForUpDraft = 0.2f * yMaxPos + 0.8f * dSideOffsetForUpDraft;
        //    }

        //    FlightPathPoint ptFlight = new FlightPathPoint(flightPoint);
        //    //ptFlight.x = myFlightPlanData.Path100mInterval[iPos15].ptOnPath.x;
        //    //ptFlight.y = myFlightPlanData.Path100mInterval[iPos15].ptOnPath.y;
        //    //           dSideOffsetForUpDraft = yMaxPos;
        //    UpDraftTarget.Add(ptFlight, vecDir, 800 + 60*flightPoint.speed, dSideOffsetForUpDraft);

        //    //           FlightPathPoint flightPoint5000 = new FlightPathPoint(myFlightPlanData.Path100mInterval[iPos50].ptOnPath);
        //    //FlightPathPoint UpDraftTargetTemp = new FlightPathPoint(UpDraftTarget.getPosition());
        //    //UpDraftTargetTemp.z += 500;
        //    if (
        //        //                !getScanAccessibility(UpDraftTargetTemp, flightPoint5000) ||
        //        !getScanAccessibility(flightPoint, UpDraftTarget)
        //        )
        //    {
        //        dSideOffsetForUpDraft = 0;
        //        UpDraftTarget.x = myFlightPlanData.Path100mInterval[iPos5].ptOnPath.X;
        //        UpDraftTarget.y = myFlightPlanData.Path100mInterval[iPos5].ptOnPath.Y;
        //    }


        //    return true;
        //}




        //public void ProcessTerrainMap(FlightPlanData flightPlanData, TextBox textBox)
        //{
        //    for (int i = 0; i < flightPlanData.Path100mInterval.Count; i++)
        //    {
        //        Properties.Settings.Default.InfoWindow = "4;" + (i + 1).ToString() + ";" + flightPlanData.Path100mInterval.Count.ToString();
        //        textBox.Text = (i + 1).ToString() + "/" + flightPlanData.Path100mInterval.Count.ToString();
        //        textBox.Refresh();

        //        flightPlanData.Path100mInterval[i].dMaxFunction = 0;
        //        GetHeight(
        //           flightPlanData.Path100mInterval[i].ptOnPath.X,
        //           flightPlanData.Path100mInterval[i].ptOnPath.Y,
        //           out flightPlanData.Path100mInterval[i].ptOnPath.Z
        //            );
        //        flightPlanData.Path100mInterval[i].dMaxPos = 0;
        //        flightPlanData.Path100mInterval[i].ptMaxFunction = flightPlanData.Path100mInterval[i].ptOnPath;

        //        for (float dPos = -2500; dPos < 2500; dPos += 90)
        //        {
        //            float ptX = flightPlanData.Path100mInterval[i].ptOnPath.X + dPos * flightPlanData.Path100mInterval[i].vecDir.y;
        //            float ptY = flightPlanData.Path100mInterval[i].ptOnPath.Y - dPos * flightPlanData.Path100mInterval[i].vecDir.x;
        //            Node node = new Node();
        //            if (GetNodeDataCoarse(ptX, ptY, out node))
        //            {
        //                //double dFact = Math.Abs(dPos) / 3000;
        //                if (node.function > 0.6 && flightPlanData.Path100mInterval[i].dMaxFunction < node.function)
        //                {
        //                    flightPlanData.Path100mInterval[i].dMaxFunction = node.function;
        //                    flightPlanData.Path100mInterval[i].dMaxPos = dPos;
        //                    flightPlanData.Path100mInterval[i].ptMaxFunction = new Point3d(ptX, ptY, node.height);
        //                }
        //            }
        //        }
        //    }

        //    for (int i = 0; i < flightPlanData.Path100mInterval.Count - 500; i++)
        //    {
        //        flightPlanData.Path100mInterval[i].dIntegration = 0;
        //        for (int j = 1; j < 500; j++)
        //        {
        //            flightPlanData.Path100mInterval[i].dIntegration += flightPlanData.Path100mInterval[i + j].dMaxFunction;
        //        }
        //    }


        //}

        //public void getAlternativeAccessibility(FlightPathPoint flightPoint, FlightPathPoint flightPointTarget, out FlightPathPoint flightPointAlternative)
        //{
        //    flightPointAlternative = new FlightPathPoint(flightPointTarget);

        //    if (getScanAccessibility(flightPoint, flightPointTarget))
        //    {
        //        flightPointAlternative = flightPointTarget;
        //        return;
        //    }
        //    Vector3d vecDir = new Vector3d(flightPoint.getPosition(), flightPointTarget.getPosition());
        //    vecDir.z = 0;

        //    for (float posy = 1; posy < 10; posy += 1)
        //    {
        //        for (int isign = -1; isign < 3; isign += 2)
        //        flightPointAlternative.Add(flightPoint, vecDir, 0.5f, isign* posy);
        //        if (getScanAccessibility(flightPoint, flightPointAlternative, 50)
        //            &&
        //            getScanAccessibility( flightPointAlternative, flightPointTarget, 100)
        //            )
        //        {
        //            return;
        //        }
        //    }


        //    return;
        //}



        //public void IterationProcess()
        //{
        //    Node node = new Node() ;
        //    while (true)
        //    {
        //        double dAccuracy = 0;
        //        for (float dX = -3000; dX <= 3000; dX += 30)
        //        {
        //            for (float dY = -3000; dY <= 3000; dY += 30)
        //            {
        //                float posX = CurrentFlightPoint.x + dX;
        //                float posY = CurrentFlightPoint.y + dY;
        //                if (GetNodeDataRef(posX, posY, ref node))
        //                {
        //                    bool wasBoundary = node.boundary;
        //                    node.boundary = node.height > CurrentFlightPoint.z;
        //                    if (wasBoundary && !node.boundary)
        //                        node.functionF = node.function;
        //                }
        //            }
        //        }
        //        for (int iter = 0; iter < 5; iter++)
        //        {
        //            for (float dX = -3000; dX <= 3000; dX += 30)
        //            {
        //                for (float dY = -3000; dY <= 3000; dY += 30)
        //                {
        //                    float posX = CurrentFlightPoint.x + dX;
        //                    float posY = CurrentFlightPoint.y + dY;
        //                    SolveNode(posX, posY, ref dAccuracy);
        //                }
        //            }
        //            for (float dX = 3000; dX >= -3000; dX -= 30)
        //            {
        //                for (float dY = 3000; dY >= -3000; dY -= 30)
        //                {
        //                    float posX = CurrentFlightPoint.x + dX;
        //                    float posY = CurrentFlightPoint.y + dY;
        //                    SolveNode(posX, posY, ref dAccuracy);
        //                }
        //            }
        //        }
        //    }
        //}
        //private void SolveNode(float posX, float posY, ref double dLastAccuracy)
        //{
        //    double k = 1;
        //    double dT = 0.1;

        //    double dBoundary = 0.1;


        //    Node node = new Node();
        //    Node nodein = new Node();
        //    Node nodeip = new Node();
        //    Node nodejn = new Node();
        //    Node nodejp = new Node();

        //    if (!GetNodeDataRef(posX, posY, ref node))
        //        return;


        //    if (!GetNodeDataRef(posX - 30, posY, ref nodein))
        //    {
        //        nodein = node;
        //    }

        //    if (!GetNodeDataRef(posX + 30, posY, ref nodeip))
        //    {
        //        nodeip = node;
        //    }

        //    if (!GetNodeDataRef(posX, posY - 30, ref nodejn))
        //    {
        //        nodejn = node;
        //    }

        //    if (!GetNodeDataRef(posX, posY + 30, ref nodejp))
        //    {
        //        nodejp = node;
        //    }

        //    double dFunction =
        //        node.weight
        //        - 4.1 * k * node.function
        //        + k * nodein.function
        //        + k * nodeip.function
        //        + k * nodejn.function
        //        + k * nodejp.function
        //        ;

        //    dLastAccuracy = Math.Max(dLastAccuracy, Math.Abs(dFunction));

        //    node.function += dT * dFunction;

        //    if (!node.boundary)
        //    {
        //        double dFunctionF =
        //            node.weight - 0.1 * k * node.function;

        //        if (nodein.boundary)
        //        {
        //            dFunctionF -= k * dBoundary;
        //        }
        //        else
        //        {
        //            dFunctionF += k * (nodein.functionF - node.functionF);
        //        }

        //        if (nodeip.boundary)
        //        {
        //            dFunctionF -= k * dBoundary;
        //        }
        //        else
        //        {
        //            dFunctionF += k * (nodeip.functionF - node.functionF);
        //        }

        //        if (nodejn.boundary)
        //        {
        //            dFunctionF -= k * dBoundary;
        //        }
        //        else
        //        {
        //            dFunctionF += k * (nodejn.functionF - node.functionF);
        //        }

        //        if (nodejp.boundary)
        //        {
        //            dFunctionF -= k * dBoundary;
        //        }
        //        else
        //        {
        //            dFunctionF += k * (nodejp.functionF - node.functionF);
        //        }

        //        node.functionF += dT * dFunctionF;

        //    }


        //}

        //public bool GetNormal(float UTMX, float UTMY, out Vector3d vecNormal, out float dHeight)
        //{
        //    float DX = UTMX;
        //    float DY = UTMY;

        //    vecNormal = new Vector3d();
        //    dHeight = 0;


        //    if (!IdTR3(UTMX, UTMY, out int nx, out int ny))
        //        return false;


        //    float D3X = DX - nx * fTRNSize;
        //    float D3Y = DY - ny * fTRNSize;

        //    int n3x = Convert.ToInt32(Math.Round(D3X / 30));
        //    int n3y = Convert.ToInt32(Math.Round(D3Y / 30));

        //    if (n3x >= nTRN3Size || n3y >= nTRN3Size)
        //    {
        //        return false;
        //    }

        //    int i_p, i_n, j_p, j_n;
        //    float d_x = 0;
        //    float d_y = 0;

        //    if (n3x == 0)
        //        i_n = 0;
        //    else
        //    {
        //        i_n = n3x - 1;
        //        d_x += 15;
        //    }
        //    if (n3x == nTRN3Size - 1)
        //        i_p = n3x;
        //    else
        //    {
        //        i_p = n3x + 1;
        //        d_x += 15;
        //    }

        //    if (n3y == 0)
        //        j_n = 0;
        //    else
        //    {
        //        j_n = n3y - 1;
        //        d_y += 15;
        //    }
        //    if (n3y == nTRN3Size - 1)
        //        j_p = n3y;
        //    else
        //    {
        //        j_p = n3y + 1;
        //        d_y += 15;
        //    }

        //    Vector3d vecX = new Vector3d(
        //        new Point3d(-d_x, 0, TRNmatrices[TRNIndexList[nx][ny]].Data[i_n][n3y].height),
        //        new Point3d(d_x, 0, TRNmatrices[TRNIndexList[nx][ny]].Data[i_p][n3y].height)
        //        );
        //    Vector3d vecY = new Vector3d(
        //        new Point3d(0, -d_y, TRNmatrices[TRNIndexList[nx][ny]].Data[n3x][j_n].height),
        //        new Point3d(0, d_y, TRNmatrices[TRNIndexList[nx][ny]].Data[n3x][j_p].height)
        //        );

        //    vecNormal = vecX.crossProduct(vecY);
        //    vecNormal.normalize();
        //    if (vecNormal.z < 0)
        //        vecNormal.negate();

        //    dHeight = TRNmatrices[TRNIndexList[nx][ny]].Data[n3x][n3y].height;


        //    return true;
        //}


        //public bool getScan(FlightPathPoint flightPoint, double dAngleDeg, float start, float end, out float nearest, out float weight)
        //{
        //    float dHeightSafety = 50;
        //    nearest = 10000;
        //    weight = 0;

        //    double dAngle = Utils.deg2rad(dAngleDeg);
        //    double dc = Math.Cos(dAngle);
        //    double ds = Math.Sin(dAngle);

        //    double vx = flightPoint.vec.x;
        //    double vy = flightPoint.vec.x;
        //    double dl = Math.Sqrt(vx * vx + vy * vy);
        //    if (dl == 0)
        //        return false;

        //    float dirx = Convert.ToSingle(vx * dc - vy * ds);
        //    float diry = Convert.ToSingle(vy * dc + vx * ds);
        //    for (float pos = start; pos < (end + 15); pos += 30)
        //    {
        //        float dx = flightPoint.x + dirx * pos;
        //        float dy = flightPoint.y + diry * pos;
        //        if (!GetHeight(dx, dy, out float fHeight))
        //            continue;
        //        float w = fHeight + dHeightSafety - flightPoint.x;
        //        if (w > 0)
        //        {
        //            if (nearest == 10000)
        //                nearest = pos;
        //            weight += w;
        //        }

        //    }

        //    return true;
        //}

        //public int CheckForRidge(FlightPathPoint flightPoint)
        //{
        //    float dRadius = 400;
        //    float dWeight = 0;
        //    float dHeightSafety = 200;


        //    for (int i = 0; i < 360; i += 30)
        //    {
        //        float alfa = Utils.deg2rad(i);
        //        float dCos = dRadius*Convert.ToSingle(Math.Cos(alfa));
        //        float dSin = dRadius*Convert.ToSingle(Math.Sin(alfa));
        //        float x = flightPoint.x + dCos * flightPoint.vec.x;
        //        float y = flightPoint.y + dSin * flightPoint.vec.y;
        //        if (!GetHeight(x, y, out float dHeight))
        //            continue;
        //        float dDelta = flightPoint.z - (dHeight + dHeightSafety);
        //        if (dDelta > 0)
        //        {
        //            dWeight += dSin * dDelta;
        //        }
        //    }

        //    if (dWeight == 0)
        //        return 0;
        //    if (dWeight < 0)
        //        return -1;
        //    else
        //        return 1;
        //}
        //private void Solve(int nx, int ny, double dAccuracyLimit, int maxIterations)
        //{
        //    double dLastAccuracy = double.MaxValue;
        //    int iter = 0;
        //    int trnIndex = TRNIndexList[nx][ny];
        //    if (trnIndex < 0)
        //        return;

        //    while (dLastAccuracy > dAccuracyLimit && iter < maxIterations)
        //    {
        //        dLastAccuracy = 0;
        //        iter++;

        //        for (int n3y = 0; n3y < TRNmatrices[trnIndex].nTRN3Size; n3y++)
        //        {
        //            for (int n3x = 0; n3x < TRNmatrices[trnIndex].nTRN3Size; n3x++)
        //            {
        //                SolveNode(trnIndex, n3x, n3y, ref dLastAccuracy);
        //            }
        //        }

        //        for (int n3y = TRNmatrices[trnIndex].nTRN3Size-1; n3y >= 0; n3y--)
        //        {
        //            for (int n3x = TRNmatrices[trnIndex].nTRN3Size - 1; n3x >= 0; n3x--)
        //            {
        //                SolveNode(trnIndex, n3x, n3y, ref dLastAccuracy);
        //            }
        //        }

        //    }
        //}

        //public bool CheckCollision_V2(FlightPathPoint flightPoint, bool bInThermal)
        //{
        //    float dSafetyHeight = 100;

        //    if (flightPoint.getWind().dotProduct(flightPoint.getSpeedDirection()) < 0)
        //        dSafetyHeight = 50;



        //    dWeightLeft = 0;
        //    dWeightRight = 0;
        //    float dForwardSafetyDistance = 35 * flightPoint.speed;

        //    for (int iSide = -1; iSide < 2; iSide += 2)
        //    {
        //        double dWeight = 0;
        //        //float ptX = flightPoint.x + dForwardSafetyDistance * flightPoint.vec.x + iSide * 0.25f * dForwardSafetyDistance * flightPoint.vec.y;
        //        //float ptY = flightPoint.y + dForwardSafetyDistance * flightPoint.vec.y - iSide * 0.25f * dForwardSafetyDistance * flightPoint.vec.x;
        //        ////if (GetHeight(ptX, ptY, out float dHeight))
        //        //{
        //        //    if (dHeight + dSafetyHeight > flightPoint.z)
        //        //        dForwardSafetyDistance /= 2;
        //        //}



        //        for (float dX = 0; dX < dForwardSafetyDistance; dX += 30)
        //        {
        //            for (float dY = 50; dY < 170; dY += 30)
        //            {
        //                FlightPathPoint flightPoint0 = new FlightPathPoint(flightPoint);
        //                flightPoint0.Add(flightPoint, dX, iSide * dY);

        //                if (!GetSafeHeight(flightPoint0.x, flightPoint0.y, out float dHeight))
        //                    continue;

        //                if (dHeight + dSafetyHeight < flightPoint.z)
        //                    continue;

        //                double dx = dX / dForwardSafetyDistance;
        //                double dy = (dY - 50) / 170;
        //                dWeight += Math.Max(1 - (dx * dx + dy * dy), 0);
        //            }
        //        }

        //        if (iSide == -1)
        //            dWeightLeft = dWeight;
        //        else
        //            dWeightRight = dWeight;
        //    }
        //    dWeightLeftSmoothed = 0.9 * dWeightLeftSmoothed + 0.1 * dWeightLeft;
        //    dWeightRightSmoothed = 0.9 * dWeightRightSmoothed + 0.1 * dWeightRight;

        //    if (dWeightLeft == 0 && dWeightRight == 0)
        //    {
        //        bFreeLeft = true;
        //        bFreeRight = true;
        //        bNeedAvoidance = false;
        //        dWeightLeftSmoothed = 0;
        //        dWeightRightSmoothed = 0;
        //    }
        //    else if (dWeightLeftSmoothed > dWeightRightSmoothed)
        //    {
        //        bFreeLeft = false;
        //        bFreeRight = true;
        //        for (int i = 6; i > Convert.ToInt32(bInThermal)*3; i--)
        //        {
        //            double dAlfa = i * Math.PI / 20;
        //            float dCos = Convert.ToSingle(dForwardSafetyDistance * Math.Cos(dAlfa));
        //            float dSin = Convert.ToSingle(dForwardSafetyDistance * Math.Sin(dAlfa));
        //            AntiCollisionDirectionTarget.Add(flightPoint, dCos, dSin);
        //            FlightPathPoint flightPointStart = new FlightPathPoint(flightPoint);
        //            flightPointStart.Add(flightPoint, 0.1f * dCos, 0.1f * dSin);

        //            if (!getScanAccessibility(flightPointStart, AntiCollisionDirectionTarget)
        //                || dWeightLeftSmoothed > 20)
        //            {
        //                dAlfa = (i + 1) * Math.PI / 20;
        //                dCos = Convert.ToSingle(500 * Math.Cos(dAlfa));
        //                dSin = Convert.ToSingle(500 * Math.Sin(dAlfa));
        //                AntiCollisionDirectionTarget.Add(flightPoint, dCos, dSin);
        //                break;
        //            }
        //        }

        //        bNeedAvoidance = true;
        //    }
        //    else
        //    {
        //       bFreeLeft = true;
        //       bFreeRight = false;
        //       for (int i = 6; i > Convert.ToInt32(bInThermal) * 3; i--)
        //        {
        //            double dAlfa = i * Math.PI / 20;
        //            float dCos = Convert.ToSingle(dForwardSafetyDistance * Math.Cos(dAlfa));
        //            float dSin = Convert.ToSingle(dForwardSafetyDistance * Math.Sin(dAlfa));
        //            AntiCollisionDirectionTarget.Add(flightPoint, dCos, -dSin);
        //            FlightPathPoint flightPointStart = new FlightPathPoint(flightPoint);
        //            flightPointStart.Add(flightPoint, 0.1f * dCos, -0.1f * dSin);
        //            if (!getScanAccessibility(flightPointStart, AntiCollisionDirectionTarget)
        //                || dWeightRightSmoothed > 20)
        //            {
        //                dAlfa = (i+1)*Math.PI / 20;
        //                dCos = Convert.ToSingle(500 * Math.Cos(dAlfa));
        //                dSin = Convert.ToSingle(500 * Math.Sin(dAlfa));
        //                AntiCollisionDirectionTarget.Add(flightPoint, dCos, -dSin);
        //                break;
        //            }
        //        }
        //        bNeedAvoidance = true;
        //    }
        //    dWeightLeft = dWeightLeftSmoothed;
        //    dWeightRight = dWeightRightSmoothed;

        //    return bNeedAvoidance;
        //}


        //public bool CheckCollision_V3(FlightPathPoint flightPoint)
        //{
        //    float dSafetyHeight = 70;

        //    double dRadius = flightPoint.speed * 30; //30 sec
        //    double dAngleStep = 50 / dRadius;

        //    dWeightLeft = 0;
        //    dWeightRight = 0;
        //    dWeightRidgeLeft = 0;
        //    dWeightRidgeRight = 0;

        //    for (int iSide = -1; iSide < 2; iSide += 2)
        //    {
        //        double dWeight = 0;
        //        double dWeightRidge = 0;
        //        float dForwardSafetyDistance = 25 * flightPoint.speed;
        //        double ptX = flightPoint.x + iSide * dRadius * flightPoint.vec.y;
        //        double ptY = flightPoint.y - iSide * dRadius * flightPoint.vec.x;


        //        for (double dAlfa = 0; dAlfa < Math.PI / 2; dAlfa += dAngleStep)
        //        {
        //            double dCos = Math.Cos(dAlfa);
        //            double dSin = Math.Sin(dAlfa);

        //            for (double dR = dRadius; dR >= dRadius - 300; dR -= 30)
        //            {
        //                double dX = dR * dSin;
        //                double dY = dR * dCos;

        //                float ptLX = Convert.ToSingle(ptX + dX * flightPoint.vec.x - iSide * dY * flightPoint.vec.y);
        //                float ptLY = Convert.ToSingle(ptY + dX * flightPoint.vec.y + iSide * dY * flightPoint.vec.x);

        //                if (!GetHeight(ptLX, ptLY, out float dHeight))
        //                    continue;

        //                if (dHeight + 70 > flightPoint.z)
        //                {
        //                    dWeight += dHeight + 70 - flightPoint.z;
        //                }
        //                if (dHeight + 500 > flightPoint.z)
        //                {
        //                    dWeightRidge += dHeight + 500 - flightPoint.z;
        //                }

        //            }
        //        }

        //        if (iSide == -1)
        //        {
        //            dWeightLeft = dWeight;
        //            dWeightRidgeLeft = dWeightRidge;

        //        }
        //        else
        //        {
        //            dWeightRight = dWeight;
        //            dWeightRidgeRight = dWeightRidge;

        //        }
        //    }
        //    if (dWeightLeft > dWeightRight && dWeightRight > 0)
        //    {
        //        bFreeLeft = false;
        //        bFreeRight = true;
        //        AntiCollisionDirectionTarget.x = flightPoint.x - 250 * flightPoint.vec.y;
        //        AntiCollisionDirectionTarget.y = flightPoint.y + 250 * flightPoint.vec.x;
        //    }
        //    else if (dWeightRight > dWeightLeft && dWeightLeft > 0)
        //    {
        //        bFreeLeft = true;
        //        bFreeRight = false;
        //        AntiCollisionDirectionTarget.x = flightPoint.x + 250 * flightPoint.vec.y;
        //        AntiCollisionDirectionTarget.y = flightPoint.y - 250 * flightPoint.vec.x;
        //    }
        //    else if (dWeightLeft == 0 && dWeightRight == 0)
        //    {
        //        bFreeLeft = true;
        //        bFreeRight = true;
        //    }

        //    bNeedAvoidance = !bFreeLeft || !bFreeRight;
        //    return bNeedAvoidance;
        //}

    }
}
