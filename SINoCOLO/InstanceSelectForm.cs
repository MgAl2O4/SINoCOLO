using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SINoCOLO
{
    public partial class InstanceSelectForm : Form
    {
        public ScreenReader screenReader;
        private Form activeFlashForm;

        private class ListItemInstance
        {
            public int idx;
            public IntPtr handle;
            public Rectangle rect;
            public string desc;

            public override string ToString()
            {
                return desc;
            }
        }

        public InstanceSelectForm()
        {
            InitializeComponent();
        }

        private void InstanceSelectForm_Load(object sender, EventArgs e)
        {
            UpdateListItems();
        }

        private void UpdateListItems()
        {
            var games = screenReader.GetAvailableGames();
            var validHandles = new List<IntPtr>();
            var existingHandles = new List<IntPtr>();

            for (int idx = 0; idx < games.Count; idx++)
            {
                validHandles.Add(games[idx].windowMain.Handle);
            }

            for (int idx = listBox1.Items.Count - 1; idx >= 0; idx--)
            {
                var item = listBox1.Items[idx] as ListItemInstance;
                if (item == null || !validHandles.Contains(item.handle))
                {
                    listBox1.Items.RemoveAt(idx);
                }
                else
                {
                    existingHandles.Add(item.handle);
                }
            }

            for (int idx = 0; idx < games.Count; idx++)
            {
                var alreadyAdded = existingHandles.Contains(games[idx].windowMain.Handle);
                if (!alreadyAdded)
                {
                    var item = new ListItemInstance();
                    item.idx = idx;
                    item.handle = games[idx].windowMain.Handle;
                    item.rect = games[idx].rectMain;
                    item.desc = string.Format("[0x{0:x}] {1}", games[idx].windowMain.Handle.ToInt64(), games[idx].windowTitle);

                    listBox1.Items.Add(item);
                }
            }
        }

        private void timerRefresh_Tick(object sender, EventArgs e)
        {
            UpdateListItems();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var item = listBox1.SelectedItem as ListItemInstance;
            if (item != null)
            {
                if (activeFlashForm != null)
                {
                    activeFlashForm.Close();
                }

                WindowFlashForm flashForm = new WindowFlashForm();
                flashForm.Show();
                flashForm.Bounds = item.rect;
                activeFlashForm = flashForm;
            }
        }

        private void buttonSelect_Click(object sender, EventArgs e)
        {
            var item = listBox1.SelectedItem as ListItemInstance;
            if (item != null)
            {
                screenReader.SetSelectedWindow(item.handle);
                Close();
            }
        }
    }
}
