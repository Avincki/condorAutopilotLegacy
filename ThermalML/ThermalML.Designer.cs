
namespace CondorAutoPilot.ThermalML
{
    partial class CThermalML
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea4 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend4 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series7 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series8 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.chartCorrelation = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.dataGridView = new System.Windows.Forms.DataGridView();
            this.Index = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Score = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.button1 = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.gMapLiftML = new GMap.NET.WindowsForms.GMapControl();
            this.pictureBoxSection = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.chartCorrelation)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxSection)).BeginInit();
            this.SuspendLayout();
            // 
            // chartCorrelation
            // 
            this.chartCorrelation.BorderlineWidth = 3;
            this.chartCorrelation.BorderSkin.BorderWidth = 3;
            chartArea4.Name = "ChartArea1";
            this.chartCorrelation.ChartAreas.Add(chartArea4);
            legend4.Name = "Legend1";
            this.chartCorrelation.Legends.Add(legend4);
            this.chartCorrelation.Location = new System.Drawing.Point(35, 636);
            this.chartCorrelation.Name = "chartCorrelation";
            series7.ChartArea = "ChartArea1";
            series7.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
            series7.Legend = "Legend1";
            series7.Name = "Correlation";
            series7.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Double;
            series7.YValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Double;
            series8.ChartArea = "ChartArea1";
            series8.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
            series8.Legend = "Legend1";
            series8.Name = "17";
            this.chartCorrelation.Series.Add(series7);
            this.chartCorrelation.Series.Add(series8);
            this.chartCorrelation.Size = new System.Drawing.Size(484, 402);
            this.chartCorrelation.TabIndex = 0;
            this.chartCorrelation.Text = "chart1";
            // 
            // dataGridView
            // 
            this.dataGridView.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Index,
            this.Score});
            this.dataGridView.Location = new System.Drawing.Point(35, 25);
            this.dataGridView.Name = "dataGridView";
            this.dataGridView.Size = new System.Drawing.Size(484, 588);
            this.dataGridView.TabIndex = 1;
            this.dataGridView.DragDrop += new System.Windows.Forms.DragEventHandler(this.dataGridView_DragDrop);
            this.dataGridView.DragOver += new System.Windows.Forms.DragEventHandler(this.dataGridView_DragOver);
            this.dataGridView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.dataGridView_MouseDown);
            this.dataGridView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.dataGridView_MouseMove);
            // 
            // Index
            // 
            this.Index.HeaderText = "Index";
            this.Index.Name = "Index";
            this.Index.Width = 40;
            // 
            // Score
            // 
            this.Score.HeaderText = "Score";
            this.Score.Name = "Score";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(1109, 972);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(159, 28);
            this.button1.TabIndex = 2;
            this.button1.Text = "Calc";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(1162, 1018);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(106, 20);
            this.textBox1.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(1106, 1021);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(50, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Iterations";
            // 
            // gMapLiftML
            // 
            this.gMapLiftML.Bearing = 0F;
            this.gMapLiftML.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.gMapLiftML.CanDragMap = true;
            this.gMapLiftML.EmptyTileColor = System.Drawing.Color.Navy;
            this.gMapLiftML.GrayScaleMode = false;
            this.gMapLiftML.HelperLineOption = GMap.NET.WindowsForms.HelperLineOptions.DontShow;
            this.gMapLiftML.LevelsKeepInMemory = 5;
            this.gMapLiftML.Location = new System.Drawing.Point(538, 25);
            this.gMapLiftML.MarkersEnabled = true;
            this.gMapLiftML.MaxZoom = 2;
            this.gMapLiftML.MinZoom = 2;
            this.gMapLiftML.MouseWheelZoomEnabled = true;
            this.gMapLiftML.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionAndCenter;
            this.gMapLiftML.Name = "gMapLiftML";
            this.gMapLiftML.NegativeMode = false;
            this.gMapLiftML.PolygonsEnabled = true;
            this.gMapLiftML.RetryLoadTile = 0;
            this.gMapLiftML.RoutesEnabled = true;
            this.gMapLiftML.ScaleMode = GMap.NET.WindowsForms.ScaleModes.Integer;
            this.gMapLiftML.SelectedAreaFillColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(65)))), ((int)(((byte)(105)))), ((int)(((byte)(225)))));
            this.gMapLiftML.ShowTileGridLines = false;
            this.gMapLiftML.Size = new System.Drawing.Size(730, 730);
            this.gMapLiftML.TabIndex = 5;
            this.gMapLiftML.Zoom = 0D;
            this.gMapLiftML.MouseClick += new System.Windows.Forms.MouseEventHandler(this.gMapLiftML_MouseClick);
            // 
            // pictureBoxSection
            // 
            this.pictureBoxSection.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pictureBoxSection.Location = new System.Drawing.Point(538, 776);
            this.pictureBoxSection.Name = "pictureBoxSection";
            this.pictureBoxSection.Size = new System.Drawing.Size(500, 260);
            this.pictureBoxSection.TabIndex = 6;
            this.pictureBoxSection.TabStop = false;
            // 
            // CThermalML
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1299, 1061);
            this.Controls.Add(this.pictureBoxSection);
            this.Controls.Add(this.gMapLiftML);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.dataGridView);
            this.Controls.Add(this.chartCorrelation);
            this.Name = "CThermalML";
            this.Text = "ThermalML";
            ((System.ComponentModel.ISupportInitialize)(this.chartCorrelation)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxSection)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataVisualization.Charting.Chart chartCorrelation;
        private System.Windows.Forms.DataGridView dataGridView;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;
        private GMap.NET.WindowsForms.GMapControl gMapLiftML;
        private System.Windows.Forms.PictureBox pictureBoxSection;
        private System.Windows.Forms.DataGridViewTextBoxColumn Index;
        private System.Windows.Forms.DataGridViewTextBoxColumn Score;
    }
}