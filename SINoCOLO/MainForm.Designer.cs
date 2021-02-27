
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
            this.labelCombatMode = new System.Windows.Forms.Label();
            this.comboBoxColoTarget = new System.Windows.Forms.ComboBox();
            this.comboBoxStoryMode = new System.Windows.Forms.ComboBox();
            this.labelStoryMode = new System.Windows.Forms.Label();
            this.checkBoxClicks = new System.Windows.Forms.CheckBox();
            this.labelStatus = new System.Windows.Forms.Label();
            this.numericEventRepeat = new System.Windows.Forms.NumericUpDown();
            this.timerScan = new System.Windows.Forms.Timer(this.components);
            this.buttonDetails = new System.Windows.Forms.Button();
            this.labelScreenshotFailed = new System.Windows.Forms.Label();
            this.textBoxDetails = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxAnalyzed)).BeginInit();
            this.panelStatus.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericEventRepeat)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBoxAnalyzed
            // 
            this.pictureBoxAnalyzed.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.pictureBoxAnalyzed.Location = new System.Drawing.Point(12, 59);
            this.pictureBoxAnalyzed.Name = "pictureBoxAnalyzed";
            this.pictureBoxAnalyzed.Size = new System.Drawing.Size(340, 601);
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
            this.panelStatus.Controls.Add(this.labelCombatMode);
            this.panelStatus.Controls.Add(this.comboBoxColoTarget);
            this.panelStatus.Controls.Add(this.comboBoxStoryMode);
            this.panelStatus.Controls.Add(this.labelStoryMode);
            this.panelStatus.Controls.Add(this.checkBoxClicks);
            this.panelStatus.Controls.Add(this.labelStatus);
            this.panelStatus.Controls.Add(this.numericEventRepeat);
            this.panelStatus.Location = new System.Drawing.Point(12, 12);
            this.panelStatus.Name = "panelStatus";
            this.panelStatus.Size = new System.Drawing.Size(654, 45);
            this.panelStatus.TabIndex = 2;
            this.panelStatus.Click += new System.EventHandler(this.topPanelClick);
            // 
            // labelCombatMode
            // 
            this.labelCombatMode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelCombatMode.AutoSize = true;
            this.labelCombatMode.Location = new System.Drawing.Point(470, 22);
            this.labelCombatMode.Name = "labelCombatMode";
            this.labelCombatMode.Size = new System.Drawing.Size(58, 13);
            this.labelCombatMode.TabIndex = 7;
            this.labelCombatMode.Text = "Targetting:";
            // 
            // comboBoxColoTarget
            // 
            this.comboBoxColoTarget.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxColoTarget.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxColoTarget.FormattingEnabled = true;
            this.comboBoxColoTarget.Location = new System.Drawing.Point(534, 19);
            this.comboBoxColoTarget.Name = "comboBoxColoTarget";
            this.comboBoxColoTarget.Size = new System.Drawing.Size(115, 21);
            this.comboBoxColoTarget.TabIndex = 6;
            this.comboBoxColoTarget.SelectedIndexChanged += new System.EventHandler(this.comboBoxColoTarget_SelectedIndexChanged);
            // 
            // comboBoxStoryMode
            // 
            this.comboBoxStoryMode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxStoryMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxStoryMode.FormattingEnabled = true;
            this.comboBoxStoryMode.Location = new System.Drawing.Point(534, 19);
            this.comboBoxStoryMode.Name = "comboBoxStoryMode";
            this.comboBoxStoryMode.Size = new System.Drawing.Size(115, 21);
            this.comboBoxStoryMode.TabIndex = 4;
            this.comboBoxStoryMode.SelectedIndexChanged += new System.EventHandler(this.comboBoxStoryMode_SelectedIndexChanged);
            // 
            // labelStoryMode
            // 
            this.labelStoryMode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelStoryMode.AutoSize = true;
            this.labelStoryMode.Location = new System.Drawing.Point(494, 22);
            this.labelStoryMode.Name = "labelStoryMode";
            this.labelStoryMode.Size = new System.Drawing.Size(34, 13);
            this.labelStoryMode.TabIndex = 3;
            this.labelStoryMode.Text = "Story:";
            // 
            // checkBoxClicks
            // 
            this.checkBoxClicks.AutoSize = true;
            this.checkBoxClicks.Location = new System.Drawing.Point(6, 21);
            this.checkBoxClicks.Name = "checkBoxClicks";
            this.checkBoxClicks.Size = new System.Drawing.Size(123, 17);
            this.checkBoxClicks.TabIndex = 2;
            this.checkBoxClicks.Text = "Enable mouse clicks";
            this.checkBoxClicks.UseVisualStyleBackColor = true;
            this.checkBoxClicks.CheckedChanged += new System.EventHandler(this.checkBoxClicks_CheckedChanged);
            // 
            // labelStatus
            // 
            this.labelStatus.AutoSize = true;
            this.labelStatus.Location = new System.Drawing.Point(3, 5);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(49, 13);
            this.labelStatus.TabIndex = 1;
            this.labelStatus.Text = "Status: --";
            this.labelStatus.Click += new System.EventHandler(this.topPanelClick);
            // 
            // numericEventRepeat
            // 
            this.numericEventRepeat.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.numericEventRepeat.Location = new System.Drawing.Point(479, 20);
            this.numericEventRepeat.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.numericEventRepeat.Name = "numericEventRepeat";
            this.numericEventRepeat.Size = new System.Drawing.Size(49, 20);
            this.numericEventRepeat.TabIndex = 5;
            this.numericEventRepeat.Value = new decimal(new int[] {
            9,
            0,
            0,
            0});
            this.numericEventRepeat.ValueChanged += new System.EventHandler(this.numericEventRepeat_ValueChanged);
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
            this.buttonDetails.Location = new System.Drawing.Point(12, 666);
            this.buttonDetails.Name = "buttonDetails";
            this.buttonDetails.Size = new System.Drawing.Size(654, 23);
            this.buttonDetails.TabIndex = 3;
            this.buttonDetails.Text = "Hide details";
            this.buttonDetails.UseVisualStyleBackColor = true;
            this.buttonDetails.Click += new System.EventHandler(this.buttonDetails_Click);
            // 
            // labelScreenshotFailed
            // 
            this.labelScreenshotFailed.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.labelScreenshotFailed.Location = new System.Drawing.Point(12, 59);
            this.labelScreenshotFailed.Name = "labelScreenshotFailed";
            this.labelScreenshotFailed.Size = new System.Drawing.Size(340, 601);
            this.labelScreenshotFailed.TabIndex = 4;
            this.labelScreenshotFailed.Text = "(screenshot not available)";
            this.labelScreenshotFailed.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelScreenshotFailed.Visible = false;
            // 
            // textBoxDetails
            // 
            this.textBoxDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxDetails.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.textBoxDetails.Location = new System.Drawing.Point(359, 59);
            this.textBoxDetails.Multiline = true;
            this.textBoxDetails.Name = "textBoxDetails";
            this.textBoxDetails.ReadOnly = true;
            this.textBoxDetails.Size = new System.Drawing.Size(307, 601);
            this.textBoxDetails.TabIndex = 5;
            this.textBoxDetails.WordWrap = false;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(678, 695);
            this.Controls.Add(this.textBoxDetails);
            this.Controls.Add(this.labelScreenshotFailed);
            this.Controls.Add(this.buttonDetails);
            this.Controls.Add(this.pictureBoxAnalyzed);
            this.Controls.Add(this.panelStatus);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(380, 130);
            this.Name = "MainForm";
            this.Text = "SINoCOLO";
            this.Load += new System.EventHandler(this.MainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxAnalyzed)).EndInit();
            this.panelStatus.ResumeLayout(false);
            this.panelStatus.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericEventRepeat)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.PictureBox pictureBoxAnalyzed;
        private System.Windows.Forms.Panel panelStatus;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Timer timerScan;
        private System.Windows.Forms.CheckBox checkBoxClicks;
        private System.Windows.Forms.Button buttonDetails;
        private System.Windows.Forms.Label labelScreenshotFailed;
        private System.Windows.Forms.TextBox textBoxDetails;
        private System.Windows.Forms.ComboBox comboBoxStoryMode;
        private System.Windows.Forms.Label labelStoryMode;
        private System.Windows.Forms.NumericUpDown numericEventRepeat;
        private System.Windows.Forms.ComboBox comboBoxColoTarget;
        private System.Windows.Forms.Label labelCombatMode;
    }
}

