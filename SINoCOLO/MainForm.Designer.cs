
namespace SINoCOLO
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.pictureBoxAnalyzed = new System.Windows.Forms.PictureBox();
            this.panelStatus = new System.Windows.Forms.Panel();
            this.labelScreenScaling = new System.Windows.Forms.Label();
            this.checkBoxClicks = new System.Windows.Forms.CheckBox();
            this.labelStatus = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.timerScan = new System.Windows.Forms.Timer(this.components);
            this.buttonDetails = new System.Windows.Forms.Button();
            this.labelScreenshotFailed = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxAnalyzed)).BeginInit();
            this.panelStatus.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBoxAnalyzed
            // 
            this.pictureBoxAnalyzed.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxAnalyzed.Location = new System.Drawing.Point(12, 59);
            this.pictureBoxAnalyzed.Name = "pictureBoxAnalyzed";
            this.pictureBoxAnalyzed.Size = new System.Drawing.Size(463, 862);
            this.pictureBoxAnalyzed.TabIndex = 0;
            this.pictureBoxAnalyzed.TabStop = false;
            this.pictureBoxAnalyzed.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBoxAnalyzed_MouseDown);
            this.pictureBoxAnalyzed.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBoxAnalyzed_MouseMove);
            this.pictureBoxAnalyzed.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBoxAnalyzed_MouseUp);
            // 
            // panelStatus
            // 
            this.panelStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelStatus.Controls.Add(this.labelScreenScaling);
            this.panelStatus.Controls.Add(this.checkBoxClicks);
            this.panelStatus.Controls.Add(this.labelStatus);
            this.panelStatus.Controls.Add(this.label1);
            this.panelStatus.Location = new System.Drawing.Point(12, 12);
            this.panelStatus.Name = "panelStatus";
            this.panelStatus.Size = new System.Drawing.Size(463, 45);
            this.panelStatus.TabIndex = 2;
            this.panelStatus.Click += new System.EventHandler(this.topPanelClick);
            // 
            // labelScreenScaling
            // 
            this.labelScreenScaling.AutoSize = true;
            this.labelScreenScaling.Location = new System.Drawing.Point(287, 24);
            this.labelScreenScaling.Name = "labelScreenScaling";
            this.labelScreenScaling.Size = new System.Drawing.Size(171, 13);
            this.labelScreenScaling.TabIndex = 3;
            this.labelScreenScaling.Text = "Warning! Screen scaling detected!";
            this.labelScreenScaling.Visible = false;
            // 
            // checkBoxClicks
            // 
            this.checkBoxClicks.AutoSize = true;
            this.checkBoxClicks.Location = new System.Drawing.Point(52, 23);
            this.checkBoxClicks.Name = "checkBoxClicks";
            this.checkBoxClicks.Size = new System.Drawing.Size(123, 17);
            this.checkBoxClicks.TabIndex = 2;
            this.checkBoxClicks.Text = "Enable mouse clicks";
            this.checkBoxClicks.UseVisualStyleBackColor = true;
            // 
            // labelStatus
            // 
            this.labelStatus.AutoSize = true;
            this.labelStatus.Location = new System.Drawing.Point(49, 5);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(53, 13);
            this.labelStatus.TabIndex = 1;
            this.labelStatus.Text = "Unknown";
            this.labelStatus.Click += new System.EventHandler(this.topPanelClick);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 5);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(40, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Status:";
            this.label1.Click += new System.EventHandler(this.topPanelClick);
            // 
            // timerScan
            // 
            this.timerScan.Enabled = true;
            this.timerScan.Tick += new System.EventHandler(this.timerScan_Tick);
            // 
            // buttonDetails
            // 
            this.buttonDetails.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonDetails.Location = new System.Drawing.Point(12, 927);
            this.buttonDetails.Name = "buttonDetails";
            this.buttonDetails.Size = new System.Drawing.Size(463, 23);
            this.buttonDetails.TabIndex = 3;
            this.buttonDetails.Text = "Hide details";
            this.buttonDetails.UseVisualStyleBackColor = true;
            this.buttonDetails.Click += new System.EventHandler(this.buttonDetails_Click);
            // 
            // labelScreenshotFailed
            // 
            this.labelScreenshotFailed.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelScreenshotFailed.Location = new System.Drawing.Point(12, 59);
            this.labelScreenshotFailed.Name = "labelScreenshotFailed";
            this.labelScreenshotFailed.Size = new System.Drawing.Size(463, 862);
            this.labelScreenshotFailed.TabIndex = 4;
            this.labelScreenshotFailed.Text = "(screenshot not available)";
            this.labelScreenshotFailed.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelScreenshotFailed.Visible = false;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(487, 956);
            this.Controls.Add(this.labelScreenshotFailed);
            this.Controls.Add(this.buttonDetails);
            this.Controls.Add(this.pictureBoxAnalyzed);
            this.Controls.Add(this.panelStatus);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "SINoCOLO";
            this.Load += new System.EventHandler(this.MainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxAnalyzed)).EndInit();
            this.panelStatus.ResumeLayout(false);
            this.panelStatus.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.PictureBox pictureBoxAnalyzed;
        private System.Windows.Forms.Panel panelStatus;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Timer timerScan;
        private System.Windows.Forms.CheckBox checkBoxClicks;
        private System.Windows.Forms.Button buttonDetails;
        private System.Windows.Forms.Label labelScreenshotFailed;
        private System.Windows.Forms.Label labelScreenScaling;
    }
}

