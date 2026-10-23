using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CondorAutoPilot.InfoWindow
{
    public partial class InfoWindow : Form
    {
        public InfoWindow()
        {
            InitializeComponent();
        }

        private void timerInfoWindow_Tick(object sender, EventArgs e)
        {
            if (!Properties.Settings.Default.InfoWindowRun)
            {
                Close();
            }

            if (Properties.Settings.Default.InfoWindow.Length == 0)
                return;

            var strSplit = Properties.Settings.Default.InfoWindow.Split(';');
            if (strSplit.Length < 2)
                return;

            int iChoice = Convert.ToInt32(strSplit[0]);
            int i1, i2;
            switch (iChoice)
            {
                case 0:
                    textBoxInfoWindow.Text = strSplit[1];
                    textBoxInfoWindow.Refresh();
                    break;
                case 1:
                    textBoxInfoWindow.Text = "Initializing TRN files";
                    textBoxInfoWindow.Refresh();

                    if (strSplit.Length < 3)
                        return;
                    i1 = Convert.ToInt32(strSplit[1]);
                    i2 = Convert.ToInt32(strSplit[2]);
                    progressBar1.Maximum = i2;
                    progressBar1.Value = i1;
                    progressBar1.Refresh();
                    break;
                case 2:
                    textBoxInfoWindow.Text = "Reading TRN files";
                    textBoxInfoWindow.Refresh();

                    if (strSplit.Length < 3)
                        return;
                    i1 = Convert.ToInt32(strSplit[1]);
                    i2 = Convert.ToInt32(strSplit[2]);
                    progressBar2.Maximum = i2;
                    progressBar2.Value = i1;
                    progressBar2.Refresh();

                    progressBar1.Value = progressBar1.Maximum;
                    progressBar1.Refresh();

                    break;
                case 3:
                    textBoxInfoWindow.Text = "Calculating TRN files";
                    textBoxInfoWindow.Refresh();

                    if (strSplit.Length < 3)
                        return;
                    i1 = Convert.ToInt32(strSplit[1]);
                    i2 = Convert.ToInt32(strSplit[2]);
                    progressBar3.Maximum = i2;
                    progressBar3.Value = i1;
                    progressBar3.Refresh();

                    progressBar2.Value = progressBar2.Maximum;
                    progressBar2.Refresh();

                    break;
                case 4:
                    textBoxInfoWindow.Text = "Calculating TRN Yolo";
                    textBoxInfoWindow.Refresh();

                    if (strSplit.Length < 3)
                        return;
                    i1 = Convert.ToInt32(strSplit[1]);
                    i2 = Convert.ToInt32(strSplit[2]);
                    progressBar4.Maximum = i2;
                    progressBar4.Value = i1;
                    progressBar4.Refresh();

                    progressBar3.Value = progressBar3.Maximum;
                    progressBar3.Refresh();

                    break;
            }



        }

        private void InfoWindow_Load(object sender, EventArgs e)
        {
            this.Top = Properties.Settings.Default.FormTop + (Properties.Settings.Default.FormHeight - this.Height) / 2;
            this.Left = Properties.Settings.Default.FormLeft + (Properties.Settings.Default.FormWidth - this.Width) / 2;

        }
    }

    public class InfoWindowThread
    {
        public InfoWindow myInfoWindowcs = null;
        public InfoWindowThread()
        {

        }


        public void Load()
        {
            myInfoWindowcs = new InfoWindow();
            myInfoWindowcs.ShowDialog();
        }
        public void Run()
        {
            Properties.Settings.Default.ControlRun = true;
        }
        public void ThreadStop()
        {
            if (myInfoWindowcs != null)
                myInfoWindowcs.Close();
        }
    }

}
