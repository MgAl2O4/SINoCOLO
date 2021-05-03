using MgAl2O4.Utils;
using System;
using System.Windows.Forms;

namespace SINoCOLO
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool bUpdatePending = GithubUpdater.FindAndApplyUpdates();
            if (bUpdatePending)
            {
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
