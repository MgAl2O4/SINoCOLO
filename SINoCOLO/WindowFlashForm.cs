using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SINoCOLO
{
    public partial class WindowFlashForm : Form
    {
        private int counter = 6;

        public WindowFlashForm()
        {
            InitializeComponent();
        }

        private void timerFlash_Tick(object sender, EventArgs e)
        {
            counter--;
            if (counter <= 0)
            {
                Close();
            }
            else
            {
                Opacity = (counter % 2);
            }
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }
    }
}
