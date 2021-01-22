using SINoVision;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace SINoCOLO
{
    public partial class MainForm : Form
    {
        private string samplesFolder = @"..\..\..\samples\";
        private string jsonFolder = @"..\..\..\ML\data\";
        private List<ScannerBase> scanners = new List<ScannerBase>();        

        public MainForm()
        {
            InitializeComponent();
            CollectFileNames();

            scanners.Add(new ScannerColoCombat());
            scanners.Add(new ScannerColoPurify());
            scanners.Add(new ScannerTitleScreen());
            scanners.Add(new ScannerMessageBox());
            scanners.Add(new ScannerCombat());
            scanners.Add(new ScannerPurify());

            foreach (var scanner in scanners)
            {
                scanner.DebugLevel = ScannerBase.EDebugLevel.Verbose;
            }

            MLDataExporter MLexport = new MLDataExporter
            {
                LoadScreenshot = (path) => LoadTestScreenshot(path, false),
                exportPath = jsonFolder
            };
            MLexport.DoTheThing();
        }

        private void CollectFileNames()
        {
            foreach (var path in Directory.EnumerateFiles(samplesFolder))
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName.StartsWith("image-") || fileName.StartsWith("real-"))
                {
                    comboBoxFileName.Items.Add(fileName);
                }
            }

            comboBoxFileName.SelectedIndex = (comboBoxFileName.Items.Count > 0) ? 0 : -1;
        }

        private void buttonLoad_Click(object sender, EventArgs e)
        {
            var srcScreenshot = LoadTestScreenshot(comboBoxFileName.Text + ".jpg");
            pictureBoxSrc.Image = srcScreenshot;

            var fastBitmap = ScreenshotUtilities.ConvertToFastBitmap(srcScreenshot);
            var debugBounds = new List<Rectangle>();

            foreach (var scanner in scanners)
            {
                var resultOb = scanner.Process(fastBitmap);
                if (resultOb != null)
                {
                    Console.WriteLine("{0} found!\n\n{1}", scanner.ScannerName, resultOb);
                    break;
                }
            }

            var processedScreenshot = ScreenshotUtilities.CreateBitmapWithShapes(fastBitmap, debugBounds);
            pictureBoxAnalyzed.Image = processedScreenshot;

            var savePath = samplesFolder + "analyzed.png";
            if (File.Exists(savePath)) { File.Delete(savePath); }
            processedScreenshot.Save(savePath, ImageFormat.Png);
        }

        private Bitmap LoadTestScreenshot(string sampleName, bool canResave = true)
        {
            string samplePath = samplesFolder + sampleName;
            if (!File.Exists(samplePath))
            {
                return null;
            }

            Bitmap srcBitmap = new Bitmap(samplePath);
            Size desiredSize = new Size(338, 600);
            if (srcBitmap.Size == desiredSize)
            {
                return srcBitmap;
            }

            // fixed crop with source size:462x864, too lazy to calc
            Rectangle cropRect_462_864 = new Rectangle(2, 45, 458, 814);

            Rectangle cropRect = cropRect_462_864;
            Bitmap croppedBitmap = srcBitmap.Clone(cropRect, srcBitmap.PixelFormat);
            srcBitmap.Dispose();
            srcBitmap = croppedBitmap;

            if (srcBitmap.Size != desiredSize)
            {
                Bitmap scaledBitmap = new Bitmap(desiredSize.Width, desiredSize.Height);
                using (Graphics g = Graphics.FromImage(scaledBitmap))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(srcBitmap, 0, 0, desiredSize.Width, desiredSize.Height);
                }

                srcBitmap.Dispose();
                srcBitmap = scaledBitmap;

                string scaledPath = samplesFolder + Path.GetFileNameWithoutExtension(sampleName) + "-scaled.jpg";
                if (!File.Exists(scaledPath) && canResave)
                {
                    srcBitmap.Save(scaledPath);
                }
            }

            return srcBitmap;
        }
    }
}
