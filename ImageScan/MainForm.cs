using SINoVision;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace SINoCOLO
{
    public partial class MainForm : Form
    {
        private string samplesFolder = @"D:\Projects\Git\SINoCOLO\samples\";
        private List<ScannerBase> scanners = new List<ScannerBase>();
        

        public MainForm()
        {
            InitializeComponent();
            CollectFileNames();

            scanners.Add(new ScannerColoCombat());
            scanners.Add(new ScannerColoPurify());
            scanners.Add(new ScannerMessageBox());
            scanners.Add(new ScannerCombat());

            foreach (var scanner in scanners)
            {
                scanner.DebugLevel = ScannerBase.EDebugLevel.Verbose;
            }

            //GatherMLDataWeapon();
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

            comboBoxFileName.SelectedIndex = 0;
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
                    debugBounds.AddRange(scanner.debugShapes);

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

        private Bitmap LoadTestScreenshot(string sampleName)
        {
            string samplePath = samplesFolder + sampleName;
            return File.Exists(samplePath) ? new Bitmap(samplePath) : null;
        }

        private struct WeapML
        {
            public string fileName;
            public ScannerCombatBase.EWeaponType[] WeaponTypes;

            public WeapML(string path, string weaponCode)
            {
                fileName = path;
                WeaponTypes = new ScannerCombatBase.EWeaponType[5];
                for (int idx = 0; idx < 5; idx++)
                {
                    WeaponTypes[idx] = GetWeaponTypeCode(weaponCode[idx]);
                }
            }

            private ScannerCombatBase.EWeaponType GetWeaponTypeCode(char c)
            {
                return (c == 'I' || c == 'i') ? ScannerCombatBase.EWeaponType.Instrument :
                    (c == 'T' || c == 't') ? ScannerCombatBase.EWeaponType.Tome :
                    (c == 'S' || c == 's') ? ScannerCombatBase.EWeaponType.Staff :
                    ScannerCombatBase.EWeaponType.Unknown;
            }
        };

        private void GatherMLDataWeapon()
        {
            List<WeapML> fileList = new List<WeapML>();
            fileList.Add(new WeapML("real-source1.jpg", "iiiit"));
            fileList.Add(new WeapML("real-source2.jpg", "iiiti"));
            fileList.Add(new WeapML("real-source3.jpg", "iiiti"));
            fileList.Add(new WeapML("real-source4.jpg", "iiiti"));
            fileList.Add(new WeapML("real-source5.jpg", ".iiti"));
            fileList.Add(new WeapML("real-source6.jpg", "iitis"));
            fileList.Add(new WeapML("real-source7.jpg", "iitis"));
            fileList.Add(new WeapML("real-source8.jpg", ".itis"));
            fileList.Add(new WeapML("real-source9.jpg", ".itis"));
            fileList.Add(new WeapML("real-source10.jpg", ".itis"));
            fileList.Add(new WeapML("real-source12.jpg", "itist"));
            fileList.Add(new WeapML("real-source13.jpg", "itist"));
            fileList.Add(new WeapML("real-source14.jpg", "itist"));
            fileList.Add(new WeapML("real-source15.jpg", "itist"));
            fileList.Add(new WeapML("real-source16.jpg", "itist"));
            fileList.Add(new WeapML("real-source17.jpg", "itist"));
            fileList.Add(new WeapML("real-source18.jpg", "itist"));
            fileList.Add(new WeapML("real-source19.jpg", "itist"));
            fileList.Add(new WeapML("real-source20.jpg", "itist"));
            fileList.Add(new WeapML("real-source21.jpg", "itist"));
            fileList.Add(new WeapML("real-source22.jpg", "itis."));
            fileList.Add(new WeapML("real-source23.jpg", "itist"));
            fileList.Add(new WeapML("real-source24.jpg", "itist"));
            fileList.Add(new WeapML("real-source25.jpg", "iti.t"));
            fileList.Add(new WeapML("real-source26.jpg", "iti.t"));
            fileList.Add(new WeapML("real-source27.jpg", "iti.t"));
            fileList.Add(new WeapML("real-source28.jpg", "ititi"));
            fileList.Add(new WeapML("real-source29.jpg", "ititi"));
            fileList.Add(new WeapML("real-source30.jpg", "ititi"));
            fileList.Add(new WeapML("real-source31.jpg", "ititi"));
            fileList.Add(new WeapML("real-source32.jpg", "ititi"));
            fileList.Add(new WeapML("real-source33.jpg", "ititi"));

            string jsonDesc = "{\"dataset\":[";
            foreach (var fileData in fileList)
            {
                var srcScreenshot = LoadTestScreenshot(fileData.fileName);
                var fastBitmap = ScreenshotUtilities.ConvertToFastBitmap(srcScreenshot);

                var combatScanner = scanners[0] as ScannerColoCombat;
                for (int idx = 0; idx < 5; idx++)
                {
                    var values = combatScanner.ExtractActionSlotWeaponData(fastBitmap, idx);

                    jsonDesc += "\n{\"input\":[";
                    jsonDesc += string.Join(",", values);
                    jsonDesc += "], \"output\":";
                    jsonDesc += (int)fileData.WeaponTypes[idx];
                    jsonDesc += "},";
                }
            }

            jsonDesc = jsonDesc.Remove(jsonDesc.Length - 1, 1);
            jsonDesc += "\n]}";

            string savePath = @"D:\temp\recording\sino-ml.json";
            File.WriteAllText(savePath, jsonDesc);
        }
    }
}
