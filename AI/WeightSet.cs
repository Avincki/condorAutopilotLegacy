using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondorAutoPilot.AI
{
    public class WeightSet
    {
        public List<float> weights { get; set; }
        public double score { get; internal set; } // 0 is spot on; score should be positive

        public void SetScore (double d)
        {
            score = Math.Max(0, d);
        }

        public void ResetScore()
        {
            score = double.MaxValue;
        }

        public WeightSet()
        {
            weights = new List<float>();
            score = double.MaxValue;
        }
        public WeightSet(int size)
        {
            weights = new List<float>();
            Init(size);
        }

        public void Init(int size)
        {
            weights.Clear();
            for (int i = 0; i < size; i++)
                weights.Add(0);

            score = double.MaxValue;
        }

        public bool BinWriter(BinaryWriter binWriter)
        {
            for (int i = 0; i < weights.Count; i++)
            {
                binWriter.Write(weights[i]);
            }

            return true;
        }
        public bool BinReader(BinaryReader binReader)
        {
            for (int i = 0; i < weights.Count; i++)
            {
                weights[i] = binReader.Read();
            }

            return true;
        }

        public void Set(WeightSet otherSet)
        {
            for (int i = 0; i < weights.Count; i++)
            {
                weights[i] = otherSet.weights[i];
            }
            score = otherSet.score;
        }
        public bool Swap(WeightSet otherSet)
        {
            if (weights.Count != otherSet.weights.Count)
                return false;
            float f;
            double d;
            for (int i = 0; i < weights.Count; i++)
            {
                f = weights[i];
                weights[i] = otherSet.weights[i];
                otherSet.weights[i] = f;
            }
            d = score;
            score = otherSet.score;
            otherSet.score = d;
            return true;
        }
    }
}
