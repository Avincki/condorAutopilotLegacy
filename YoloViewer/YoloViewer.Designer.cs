
namespace CondorAutoPilot.YoloViewer
{
    partial class YoloViewer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(YoloViewer));
            this.pictureBoxYoloViewer = new System.Windows.Forms.PictureBox();
            this.timerYolo = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxYoloViewer)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBoxYoloViewer
            // 
            this.pictureBoxYoloViewer.Location = new System.Drawing.Point(16, 14);
            this.pictureBoxYoloViewer.Name = "pictureBoxYoloViewer";
            this.pictureBoxYoloViewer.Size = new System.Drawing.Size(1230, 720);
            this.pictureBoxYoloViewer.TabIndex = 0;
            this.pictureBoxYoloViewer.TabStop = false;
            // 
            // timerYolo
            // 
            this.timerYolo.Enabled = true;
            this.timerYolo.Interval = 250;
            this.timerYolo.Tick += new System.EventHandler(this.timerYolo_Tick);
            // 
            // YoloViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::CondorAutoPilot.Properties.Resources._21348;
            this.ClientSize = new System.Drawing.Size(1259, 747);
            this.Controls.Add(this.pictureBoxYoloViewer);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "YoloViewer";
            this.Text = "YoloViewer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.YoloViewer_FormClosing);
            this.Load += new System.EventHandler(this.YoloViewer_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxYoloViewer)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBoxYoloViewer;
        private System.Windows.Forms.Timer timerYolo;
    }
}