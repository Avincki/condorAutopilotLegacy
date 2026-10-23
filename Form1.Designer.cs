namespace CondorAutoPilot
{
    partial class Form1
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.buttonStartStop = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.pictureGimBall = new System.Windows.Forms.PictureBox();
            this.pictureTopView = new System.Windows.Forms.PictureBox();
            this.textBoxIP = new System.Windows.Forms.TextBox();
            this.gMapSmall = new GMap.NET.WindowsForms.GMapControl();
            this.gMapLarge = new GMap.NET.WindowsForms.GMapControl();
            this.buttonGetFlightPlan = new System.Windows.Forms.Button();
            this.textBoxMemory = new System.Windows.Forms.TextBox();
            this.richTextBox = new System.Windows.Forms.RichTextBox();
            this.pictureBoxPhoto = new System.Windows.Forms.PictureBox();
            this.textBoxInfo = new System.Windows.Forms.TextBox();
            this.buttonPlanner = new System.Windows.Forms.Button();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.textBoxPhotoInfo = new System.Windows.Forms.TextBox();
            this.buttonML_Lift = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureGimBall)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureTopView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPhoto)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonStartStop
            // 
            this.buttonStartStop.Location = new System.Drawing.Point(285, 1010);
            this.buttonStartStop.Name = "buttonStartStop";
            this.buttonStartStop.Size = new System.Drawing.Size(235, 20);
            this.buttonStartStop.TabIndex = 1;
            this.buttonStartStop.Text = "Start/Stop";
            this.buttonStartStop.UseVisualStyleBackColor = true;
            this.buttonStartStop.Click += new System.EventHandler(this.buttonStartStop_Click);
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // pictureGimBall
            // 
            this.pictureGimBall.BackColor = System.Drawing.Color.Silver;
            this.pictureGimBall.Location = new System.Drawing.Point(20, 15);
            this.pictureGimBall.Name = "pictureGimBall";
            this.pictureGimBall.Size = new System.Drawing.Size(1020, 460);
            this.pictureGimBall.TabIndex = 5;
            this.pictureGimBall.TabStop = false;
            // 
            // pictureTopView
            // 
            this.pictureTopView.BackColor = System.Drawing.Color.Silver;
            this.pictureTopView.Location = new System.Drawing.Point(20, 495);
            this.pictureTopView.Name = "pictureTopView";
            this.pictureTopView.Size = new System.Drawing.Size(500, 500);
            this.pictureTopView.TabIndex = 6;
            this.pictureTopView.TabStop = false;
            // 
            // textBoxIP
            // 
            this.textBoxIP.Location = new System.Drawing.Point(1060, 1010);
            this.textBoxIP.Name = "textBoxIP";
            this.textBoxIP.Size = new System.Drawing.Size(260, 20);
            this.textBoxIP.TabIndex = 7;
            // 
            // gMapSmall
            // 
            this.gMapSmall.Bearing = 0F;
            this.gMapSmall.CanDragMap = true;
            this.gMapSmall.EmptyTileColor = System.Drawing.Color.Navy;
            this.gMapSmall.GrayScaleMode = false;
            this.gMapSmall.HelperLineOption = GMap.NET.WindowsForms.HelperLineOptions.DontShow;
            this.gMapSmall.LevelsKeepInMemory = 5;
            this.gMapSmall.Location = new System.Drawing.Point(1640, 15);
            this.gMapSmall.MarkersEnabled = true;
            this.gMapSmall.MaxZoom = 2;
            this.gMapSmall.MinZoom = 2;
            this.gMapSmall.MouseWheelZoomEnabled = true;
            this.gMapSmall.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionAndCenter;
            this.gMapSmall.Name = "gMapSmall";
            this.gMapSmall.NegativeMode = false;
            this.gMapSmall.PolygonsEnabled = true;
            this.gMapSmall.RetryLoadTile = 0;
            this.gMapSmall.RoutesEnabled = true;
            this.gMapSmall.ScaleMode = GMap.NET.WindowsForms.ScaleModes.Integer;
            this.gMapSmall.SelectedAreaFillColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(65)))), ((int)(((byte)(105)))), ((int)(((byte)(225)))));
            this.gMapSmall.ShowTileGridLines = false;
            this.gMapSmall.Size = new System.Drawing.Size(250, 250);
            this.gMapSmall.TabIndex = 10;
            this.gMapSmall.Zoom = 0D;
            // 
            // gMapLarge
            // 
            this.gMapLarge.Bearing = 0F;
            this.gMapLarge.CanDragMap = true;
            this.gMapLarge.EmptyTileColor = System.Drawing.Color.Navy;
            this.gMapLarge.GrayScaleMode = false;
            this.gMapLarge.HelperLineOption = GMap.NET.WindowsForms.HelperLineOptions.DontShow;
            this.gMapLarge.LevelsKeepInMemory = 5;
            this.gMapLarge.Location = new System.Drawing.Point(1060, 285);
            this.gMapLarge.MarkersEnabled = true;
            this.gMapLarge.MaxZoom = 2;
            this.gMapLarge.MinZoom = 2;
            this.gMapLarge.MouseWheelZoomEnabled = true;
            this.gMapLarge.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionAndCenter;
            this.gMapLarge.Name = "gMapLarge";
            this.gMapLarge.NegativeMode = false;
            this.gMapLarge.PolygonsEnabled = true;
            this.gMapLarge.RetryLoadTile = 0;
            this.gMapLarge.RoutesEnabled = true;
            this.gMapLarge.ScaleMode = GMap.NET.WindowsForms.ScaleModes.Integer;
            this.gMapLarge.SelectedAreaFillColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(65)))), ((int)(((byte)(105)))), ((int)(((byte)(225)))));
            this.gMapLarge.ShowTileGridLines = false;
            this.gMapLarge.Size = new System.Drawing.Size(830, 710);
            this.gMapLarge.TabIndex = 11;
            this.gMapLarge.Zoom = 0D;
            this.gMapLarge.MouseClick += new System.Windows.Forms.MouseEventHandler(this.myMouseClick);
            // 
            // buttonGetFlightPlan
            // 
            this.buttonGetFlightPlan.Location = new System.Drawing.Point(20, 1010);
            this.buttonGetFlightPlan.Name = "buttonGetFlightPlan";
            this.buttonGetFlightPlan.Size = new System.Drawing.Size(235, 20);
            this.buttonGetFlightPlan.TabIndex = 12;
            this.buttonGetFlightPlan.Text = "Get Flight Plan";
            this.buttonGetFlightPlan.UseVisualStyleBackColor = true;
            this.buttonGetFlightPlan.Click += new System.EventHandler(this.buttonGetFlightPlan_Click);
            // 
            // textBoxMemory
            // 
            this.textBoxMemory.Location = new System.Drawing.Point(1345, 1010);
            this.textBoxMemory.Name = "textBoxMemory";
            this.textBoxMemory.Size = new System.Drawing.Size(260, 20);
            this.textBoxMemory.TabIndex = 13;
            // 
            // richTextBox
            // 
            this.richTextBox.Location = new System.Drawing.Point(1060, 15);
            this.richTextBox.Name = "richTextBox";
            this.richTextBox.Size = new System.Drawing.Size(250, 250);
            this.richTextBox.TabIndex = 17;
            this.richTextBox.Text = "";
            // 
            // pictureBoxPhoto
            // 
            this.pictureBoxPhoto.BackColor = System.Drawing.Color.Transparent;
            this.pictureBoxPhoto.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBoxPhoto.Location = new System.Drawing.Point(540, 495);
            this.pictureBoxPhoto.Name = "pictureBoxPhoto";
            this.pictureBoxPhoto.Size = new System.Drawing.Size(500, 381);
            this.pictureBoxPhoto.TabIndex = 18;
            this.pictureBoxPhoto.TabStop = false;
            // 
            // textBoxInfo
            // 
            this.textBoxInfo.BackColor = System.Drawing.Color.Black;
            this.textBoxInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 21.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxInfo.ForeColor = System.Drawing.Color.Silver;
            this.textBoxInfo.Location = new System.Drawing.Point(540, 955);
            this.textBoxInfo.Name = "textBoxInfo";
            this.textBoxInfo.Size = new System.Drawing.Size(500, 40);
            this.textBoxInfo.TabIndex = 19;
            this.textBoxInfo.Text = "Info";
            this.textBoxInfo.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // buttonPlanner
            // 
            this.buttonPlanner.Location = new System.Drawing.Point(1630, 1009);
            this.buttonPlanner.Name = "buttonPlanner";
            this.buttonPlanner.Size = new System.Drawing.Size(85, 21);
            this.buttonPlanner.TabIndex = 20;
            this.buttonPlanner.Text = "Planner";
            this.buttonPlanner.UseVisualStyleBackColor = true;
            this.buttonPlanner.Click += new System.EventHandler(this.buttonPlanner_Click);
            // 
            // richTextBox1
            // 
            this.richTextBox1.Location = new System.Drawing.Point(1350, 15);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(250, 250);
            this.richTextBox1.TabIndex = 21;
            this.richTextBox1.Text = "";
            // 
            // textBoxPhotoInfo
            // 
            this.textBoxPhotoInfo.BackColor = System.Drawing.Color.Black;
            this.textBoxPhotoInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 21.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxPhotoInfo.ForeColor = System.Drawing.Color.Silver;
            this.textBoxPhotoInfo.Location = new System.Drawing.Point(540, 897);
            this.textBoxPhotoInfo.Name = "textBoxPhotoInfo";
            this.textBoxPhotoInfo.Size = new System.Drawing.Size(500, 40);
            this.textBoxPhotoInfo.TabIndex = 22;
            this.textBoxPhotoInfo.Text = "Photo";
            this.textBoxPhotoInfo.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // buttonML_Lift
            // 
            this.buttonML_Lift.Location = new System.Drawing.Point(1732, 1009);
            this.buttonML_Lift.Name = "buttonML_Lift";
            this.buttonML_Lift.Size = new System.Drawing.Size(85, 21);
            this.buttonML_Lift.TabIndex = 23;
            this.buttonML_Lift.Text = "ML Lift";
            this.buttonML_Lift.UseVisualStyleBackColor = true;
            this.buttonML_Lift.Click += new System.EventHandler(this.buttonML_Lift_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.BackgroundImage = global::CondorAutoPilot.Properties.Resources._21348;
            this.ClientSize = new System.Drawing.Size(1904, 1041);
            this.Controls.Add(this.buttonML_Lift);
            this.Controls.Add(this.textBoxPhotoInfo);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.buttonPlanner);
            this.Controls.Add(this.textBoxInfo);
            this.Controls.Add(this.pictureBoxPhoto);
            this.Controls.Add(this.richTextBox);
            this.Controls.Add(this.textBoxMemory);
            this.Controls.Add(this.buttonGetFlightPlan);
            this.Controls.Add(this.gMapLarge);
            this.Controls.Add(this.gMapSmall);
            this.Controls.Add(this.textBoxIP);
            this.Controls.Add(this.pictureTopView);
            this.Controls.Add(this.pictureGimBall);
            this.Controls.Add(this.buttonStartStop);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "Condor GimBall V3.0";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureGimBall)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureTopView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPhoto)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button buttonStartStop;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.PictureBox pictureGimBall;
        private System.Windows.Forms.PictureBox pictureTopView;
        private System.Windows.Forms.TextBox textBoxIP;
        private GMap.NET.WindowsForms.GMapControl gMapSmall;
        private GMap.NET.WindowsForms.GMapControl gMapLarge;
        private System.Windows.Forms.Button buttonGetFlightPlan;
        private System.Windows.Forms.TextBox textBoxMemory;
        private System.Windows.Forms.RichTextBox richTextBox;
        private System.Windows.Forms.PictureBox pictureBoxPhoto;
        private System.Windows.Forms.TextBox textBoxInfo;
        private System.Windows.Forms.Button buttonPlanner;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.TextBox textBoxPhotoInfo;
        private System.Windows.Forms.Button buttonML_Lift;
    }
}

