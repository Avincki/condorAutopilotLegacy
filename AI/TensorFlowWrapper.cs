using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TensorFlow;
using System.IO;

namespace CondorAutoPilot.AI
{
    public class TensorFlowWrapper
    {
        private TFGraph myGraph = new TFGraph();
        private TFSession mySession = new TFSession();
        private TFSession.Runner myRunner;

        static long[] shape = new long[] { 10 };
        static int size = 10 * sizeof(float);

        // Empty input and output tensors
        TFTensor my_input = new TFTensor(TFDataType.Float, shape, size);
        TFTensor[] my_output = new TFTensor[1];


        public void init() { 
            string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string strWorkPath = System.IO.Path.GetDirectoryName(strExeFilePath);
            string strFileTensorModel = strWorkPath + @"\model";
            myGraph.Import(File.ReadAllBytes(strFileTensorModel));
            mySession = new TFSession(myGraph);

            myRunner = mySession.GetRunner();

            myRunner.AddInput(myGraph["input"][0], my_input);
            myRunner.Fetch(myGraph["output"][0]);
        }

        public float calc(
            float v0,
            float v1,
            float v2,
            float v3,
            float v4,
            float v5,
            float v6,
            float v7,
            float v8,
            float v9
            )
        {
            float[] myInput = new float[10];
            myInput[0] = v0;
            myInput[1] = v1;
            myInput[2] = v2;
            myInput[3] = v3;
            myInput[4] = v4;
            myInput[5] = v5;
            myInput[6] = v6;
            myInput[7] = v7;
            myInput[8] = v8;
            myInput[9] = v9;

            var result = new float[1, 1];

            my_input.SetValue(myInput);

            // Run the model
            myRunner.Run(my_output, null);

            // Fetch the result from output into `result`
            my_output[0].GetValue(result);


            return 1;
        }
    }
}
