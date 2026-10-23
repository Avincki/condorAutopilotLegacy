using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CondorAutoPilot.AI
{
    public class WeightCollections
    {
        public List<WeightSet> sets { get; set; }
        public List<float> rangeMax { get; set; }
        public List<float> rangeMin { get; set; }
        public List<float> deviation { get; set; }
        public string name { get; set; }
        public List<string> weigthNames { get; set; }

        public int setSize { get; set; }

        internal Random myRandom = new Random();

        private DataGridView myDataGridView;


        public WeightCollections()
        {
            sets = new List<WeightSet>();
            rangeMax = new List<float>();
            rangeMin = new List<float>();
            deviation = new List<float>();
            weigthNames = new List<string>();
            name = "unknown";
            setSize = 0;
        }

        public void Init (DataGridView dataGridView, int nSets, int size, float dMin, float dMax, float dDev)
        {
            setSize = size;
            myDataGridView = dataGridView;

            rangeMax.Clear();
            rangeMin.Clear();
            deviation.Clear();
            weigthNames.Clear();
            for (int i = 0; i < setSize; i++)
            {
                rangeMax.Add(dMax);
                rangeMin.Add(dMin);
                deviation.Add(dDev);
                weigthNames.Add("");
            }

            for (int i = 0; i < nSets; i++)
            {
                WeightSet set = new WeightSet(setSize);
                sets.Add(set);
                Randomize(i);
            }

            

        }

        public bool WriteFile()
        {
            string strWeightFile = Properties.Settings.Default.CondorFlightPlanPath +
                "\\ML_Weights\\" +
                name + ".mlw";

            BinaryWriter binWriter =
                new BinaryWriter(File.Open(strWeightFile, FileMode.OpenOrCreate));
            try
            {
                binWriter.Write(setSize);
                for (int i = 0; i < setSize; i++)
                {
                    binWriter.Write(weigthNames[i]);
                    binWriter.Write(rangeMax[i]);
                    binWriter.Write(rangeMin[i]);
                    binWriter.Write(deviation[i]);
                }

                binWriter.Write(sets.Count);
                for (int i = 0; i < sets.Count; i++)
                {
                    sets[i].BinWriter(binWriter);
                }

                binWriter.Close();

            }
            catch (IOException ioexp)
            {
                string strMessage = $"\nError:  {ioexp.Message}";
                binWriter.Close();
                return false;
            }

            return true;
        }
        public bool ReadFile()
        {
            string strWeightFile = Properties.Settings.Default.CondorFlightPlanPath +
                "\\ML_Weights\\" +
                name + ".mlw";

            BinaryReader binReader =
                new BinaryReader(File.Open(strWeightFile, FileMode.Open));
            try
            {
                setSize = binReader.Read();
                for (int i = 0; i < setSize; i++)
                {
                    weigthNames[i] = binReader.ReadString();
                    rangeMax[i] = binReader.Read();
                    rangeMin[i] = binReader.Read();
                    deviation[i] = binReader.Read();
                }

                int nSets = binReader.Read();
                for (int i = 0; i < nSets; i++)
                {
                    sets[i].BinReader(binReader);
                }


                binReader.Close();

            }
            catch (IOException ioexp)
            {
                string strMessage = $"\nError:  {ioexp.Message}";
                binReader.Close();
                return false;
            }

            return true;

        }

        private float NextRandom()
        {
            return Convert.ToSingle(myRandom.NextDouble());
        }

        public bool Swap (int i, int j)
        {
            if (i < 0 || j < 0 || i >= sets.Count || j >= sets.Count)
                return false;
            return sets[i].Swap(sets[j]);
        }

        public bool MutateFrom(int iSource, int iTarget)
        {
            if (iSource < 0 || iTarget < 0 || iSource >= sets.Count || iTarget >= sets.Count)
                return false;

            for (int k = 0; k < sets[iSource].weights.Count; k++)
            {
                float d = sets[iSource].weights[k] + deviation[k] * (2* NextRandom() - 1);
                sets[iTarget].weights[k] = Math.Min(rangeMax[k], Math.Max(rangeMin[k], d));
            }
            sets[iTarget].ResetScore();
            return true;
        }
        public bool MateWith(int iSourceM, int iSourceF, int iTarget)
        {
            if (iSourceM < 0 || iSourceF < 0 || iTarget < 0 || iSourceM >= sets.Count || iSourceF >= sets.Count || iTarget >= sets.Count)
                return false;

            for (int k = 0; k < sets[iSourceM].weights.Count; k++)
            {
                float d = sets[iSourceM].weights[k] + (sets[iSourceM].weights[k] - sets[iSourceF].weights[k]) * NextRandom();
                sets[iTarget].weights[k] = Math.Min(rangeMax[k], Math.Max(rangeMin[k], d));
            }
            sets[iTarget].ResetScore();
            return true;
        }
        public bool Reverse(int iSource, int iTarget)
        {
            if (iSource < 0 || iTarget < 0 || iSource >= sets.Count || iTarget >= sets.Count)
                return false;

            for (int k = 0; k < sets[iSource].weights.Count; k++)
            {
                float d = 2*sets[iSource].weights[k] - sets[iTarget].weights[k];
                sets[iTarget].weights[k] = Math.Min(rangeMax[k], Math.Max(rangeMin[k], d));
            }
            sets[iTarget].ResetScore();
            return true;
        }
        public bool Randomize(int iTarget)
        {
            if (iTarget < 0 || iTarget >= sets.Count)
                return false;

            for (int k = 0; k < sets[iTarget].weights.Count; k++)
            {
                float d = rangeMin[k] + (rangeMax[k] - rangeMin[k]) * NextRandom();
                sets[iTarget].weights[k] = d;
            }
            sets[iTarget].ResetScore();
            return true;
        }


        /// <summary>
        /// ////////////////////////  myDataGridView  ///////////////////////
        /// </summary>
        private Rectangle dragBoxFromMouseDown;
        private int rowIndexFromMouseDown;
        private int rowIndexOfItemUnderMouseToDrop;



        public void InitDataGridView()
        {
            myDataGridView.AllowDrop = true;
            myDataGridView.Columns.Clear();
            myDataGridView.Columns.Add("Index", "index");
            myDataGridView.Columns.Add("Score", "score");

            for (int i = 0; i < setSize; i++)
            {
                string str1, str2;
                str1 = "W" + (i + 1).ToString();
                str2 = "w" + (i + 1).ToString();
                myDataGridView.Columns.Add(str1, str2);
            }

        }
        public void FillDataGridView()
        {
            myDataGridView.DataSource = null;
            myDataGridView.Rows.Clear();
            myDataGridView.Refresh();


            for (int i = 0; i < sets.Count; i++)
            {
                myDataGridView.Rows.Add(new object[] { (i+1).ToString(),
                    sets[i].score.ToString()
                });

                for (int j = 0; j < setSize; j++)
                {
                    myDataGridView.Rows[i].Cells[j + 2].Value = sets[i].weights[j].ToString();
                }

            }

        }

        public void dataGridView_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                // If the mouse moves outside the rectangle, start the drag.
                if (dragBoxFromMouseDown != Rectangle.Empty &&
                    !dragBoxFromMouseDown.Contains(e.X, e.Y))
                {

                    // Proceed with the drag and drop, passing in the list item.                    
                    DragDropEffects dropEffect = myDataGridView.DoDragDrop(
                    myDataGridView.Rows[rowIndexFromMouseDown],
                    DragDropEffects.Move);
                }
            }
        }

        public void dataGridView_MouseDown(object sender, MouseEventArgs e)
        {
            // Get the index of the item the mouse is below.
            rowIndexFromMouseDown = myDataGridView.HitTest(e.X, e.Y).RowIndex;
            if (rowIndexFromMouseDown != -1)
            {
                // Remember the point where the mouse down occurred. 
                // The DragSize indicates the size that the mouse can move 
                // before a drag event should be started.                
                Size dragSize = SystemInformation.DragSize;

                // Create a rectangle using the DragSize, with the mouse position being
                // at the center of the rectangle.
                dragBoxFromMouseDown = new Rectangle(new Point(e.X - (dragSize.Width / 2),
                                                               e.Y - (dragSize.Height / 2)),
                                    dragSize);
            }
            else
                // Reset the rectangle if the mouse is not over an item in the ListBox.
                dragBoxFromMouseDown = Rectangle.Empty;
        }

        public void dataGridView_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        public void dataGridView_DragDrop(object sender, DragEventArgs e)
        {
            // The mouse locations are relative to the screen, so they must be 
            // converted to client coordinates.
            Point clientPoint = myDataGridView.PointToClient(new Point(e.X, e.Y));

            // Get the row index of the item the mouse is below. 
            rowIndexOfItemUnderMouseToDrop =
                myDataGridView.HitTest(clientPoint.X, clientPoint.Y).RowIndex;

            // If the drag operation was a move then remove and insert the row.
            if (e.Effect == DragDropEffects.Move)
            {
                DataGridViewRow rowToMove = e.Data.GetData(
                    typeof(DataGridViewRow)) as DataGridViewRow;
                myDataGridView.Rows.RemoveAt(rowIndexFromMouseDown);
                myDataGridView.Rows.Insert(rowIndexOfItemUnderMouseToDrop, rowToMove);

            }
        }

    }
}
