
namespace SINoCOLO
{
    partial class WindowFlashForm
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
            this.panelTransparent = new System.Windows.Forms.Panel();
            this.timerFlash = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // panelTransparent
            // 
            this.panelTransparent.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelTransparent.BackColor = System.Drawing.Color.Green;
            this.panelTransparent.Location = new System.Drawing.Point(12, 12);
            this.panelTransparent.Name = "panelTransparent";
            this.panelTransparent.Size = new System.Drawing.Size(514, 575);
            this.panelTransparent.TabIndex = 0;
            // 
            // timerFlash
            // 
            this.timerFlash.Enabled = true;
            this.timerFlash.Interval = 250;
            this.timerFlash.Tick += new System.EventHandler(this.timerFlash_Tick);
            // 
            // WindowFlashForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Red;
            this.ClientSize = new System.Drawing.Size(538, 599);
            this.Controls.Add(this.panelTransparent);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "WindowFlashForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.TransparencyKey = System.Drawing.Color.Green;
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelTransparent;
        private System.Windows.Forms.Timer timerFlash;
    }
}