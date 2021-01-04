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

            /*var listBounds = new List<Rectangle>();
            foreach (var path in Directory.EnumerateFiles(samplesFolder + "test-purify-pvp/"))
            {
                var srcScreenshot = LoadTestScreenshot("test-purify-pvp/" + Path.GetFileName(path));
                var fastBitmap = ScreenshotUtilities.ConvertToFastBitmap(srcScreenshot);
                scanners[1].Process(fastBitmap);
                ScreenshotUtilities.SaveBitmapWithShapes(fastBitmap, listBounds, samplesFolder + Path.GetFileName(path));
            }*/

            //GatherMLDataWeapon();
            //GatherMLDataDemon();
            //GatherMLDataPurify();
            //GatherMLDataButtons();
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

        private Bitmap LoadTestScreenshot(string sampleName)
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

            // fixed crop, too lazy to calc
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
                if (!File.Exists(scaledPath))
                {
                    srcBitmap.Save(scaledPath);
                }
            }

            return srcBitmap;
        }

        private struct WeapML
        {
            public string fileName;
            public string[] effects;
            public ScannerCombatBase.EWeaponType[] WeaponTypes;
            public ScannerCombatBase.EElementType[] ElementTypes;
            public ScannerCombatBase.EElementType BoostElement;

            public WeapML(string path, string weaponCode, string elemCode, string effectCode, string meta = null)
            {
                fileName = path;
                WeaponTypes = new ScannerCombatBase.EWeaponType[5];
                ElementTypes = new ScannerCombatBase.EElementType[5];
                BoostElement = ScannerCombatBase.EElementType.Unknown;
                effects = effectCode.Split(',');

                for (int idx = 0; idx < 5; idx++)
                {
                    WeaponTypes[idx] = GetWeaponTypeCode(weaponCode[idx]);
                    ElementTypes[idx] = GetWeaponElementCode(elemCode[idx]);
                }

                if (meta == "boost:wind") { BoostElement = ScannerCombatBase.EElementType.Wind; }
                else if (meta == "boost:water") { BoostElement = ScannerCombatBase.EElementType.Water; }
                else if (meta == "boost:fire") { BoostElement = ScannerCombatBase.EElementType.Fire; }
            }

            private ScannerCombatBase.EWeaponType GetWeaponTypeCode(char c)
            {
                return (c == 'I' || c == 'i') ? ScannerCombatBase.EWeaponType.Instrument :
                    (c == 'T' || c == 't') ? ScannerCombatBase.EWeaponType.Tome :
                    (c == 'S' || c == 's') ? ScannerCombatBase.EWeaponType.Staff :
                    (c == 'O' || c == 'o') ? ScannerCombatBase.EWeaponType.Orb :
                    ScannerCombatBase.EWeaponType.Unknown;
            }

            private ScannerCombatBase.EElementType GetWeaponElementCode(char c)
            {
                return (c == 'R' || c == 'r') ? ScannerCombatBase.EElementType.Fire :
                    (c == 'B' || c == 'b') ? ScannerCombatBase.EElementType.Water :
                    (c == 'G' || c == 'g') ? ScannerCombatBase.EElementType.Wind :
                    ScannerCombatBase.EElementType.Unknown;
            }
        };

        private struct PurifyML
        {
            public string fileName;
            public ScannerColoPurify.ESlotType[] SlotTypes;

            public PurifyML(string path, string slotCode)
            {
                fileName = path;
                SlotTypes = new ScannerColoPurify.ESlotType[8];
                for (int idx = 0; idx < SlotTypes.Length; idx++)
                {
                    SlotTypes[idx] = GetSlotTypeCode(slotCode[idx]);
                }
            }

            private ScannerColoPurify.ESlotType GetSlotTypeCode(char c)
            {
                return (c == 'L' || c == 'l') ? ScannerColoPurify.ESlotType.Locked :
                    (c == 'M' || c == 'm') ? ScannerColoPurify.ESlotType.LockedBig :
                    (c == 'B' || c == 'b') ? ScannerColoPurify.ESlotType.Big :
                    (c == 'S' || c == 's') ? ScannerColoPurify.ESlotType.Small :
                    ScannerColoPurify.ESlotType.None;
            }
        };

        private struct ButtonML
        {
            public string fileName;
            public ScannerMessageBox.EButtonType[] ButtonSlots;

            public ButtonML(string path, string typeCode)
            {
                fileName = path;
                ButtonSlots = new ScannerMessageBox.EButtonType[6];

                string[] tokens = typeCode.Split(new char[] { ':', ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int idx = 0; idx < tokens.Length; idx += 2)
                {
                    var buttonPos = ScannerMessageBox.EButtonPos.Unknown;
                    var buttonType = ScannerMessageBox.EButtonType.Unknown;

                    if (tokens[idx] == "reportRetry") { buttonPos = ScannerMessageBox.EButtonPos.CombatReportRetry; } 
                    else if (tokens[idx] == "reportOk") { buttonPos = ScannerMessageBox.EButtonPos.CombatReportOk; }
                    else if (tokens[idx] == "center") { buttonPos = ScannerMessageBox.EButtonPos.Center; }
                    else if (tokens[idx] == "centerL") { buttonPos = ScannerMessageBox.EButtonPos.CenterTwoLeft; }
                    else if (tokens[idx] == "centerR") { buttonPos = ScannerMessageBox.EButtonPos.CenterTwoRight; }

                    if (tokens[idx + 1] == "retry") { buttonType = ScannerMessageBox.EButtonType.Retry; }
                    else if (tokens[idx + 1] == "ok") { buttonType = ScannerMessageBox.EButtonType.Ok; }
                    else if (tokens[idx + 1] == "cancel") { buttonType = ScannerMessageBox.EButtonType.Cancel; }
                    else if (tokens[idx + 1] == "close") { buttonType = ScannerMessageBox.EButtonType.Close; }
                    else if (tokens[idx + 1] == "next") { buttonType = ScannerMessageBox.EButtonType.Next; }

                    ButtonSlots[(int)buttonPos] = buttonType;
                }
            }
        }

        private void GatherMLDataWeapon()
        { 
            List<WeapML> fileList = new List<WeapML>();
            fileList.Add(new WeapML("real-source1.jpg", "iiiit", "rgrgb", "mdef,atk,def,matk,matk"));
            fileList.Add(new WeapML("real-source2.jpg", "iiiti", "grgbb", "atk,def,matk,matk,patk"));
            fileList.Add(new WeapML("real-source3.jpg", "iiiti", "grgbb", "atk,def,matk,matk,patk"));
            fileList.Add(new WeapML("real-source4.jpg", "iiiti", "grgbb", "atk,def,matk,matk,patk"));
            fileList.Add(new WeapML("real-source5.jpg", ".tisi", ".rgbb", "x,def,matk,matk,patk"));
            fileList.Add(new WeapML("real-source6.jpg", "iitis", "rgbbg", "def,matk,matk,patk,heal"));
            fileList.Add(new WeapML("real-source7.jpg", "iitis", "rgbbg", "def,matk,matk,patk,heal"));
            fileList.Add(new WeapML("real-source8.jpg", ".itis", ".gbbg", "x,matk,matk,patk,heal"));
            fileList.Add(new WeapML("real-source9.jpg", ".itis", ".gbbg", "x,matk,matk,patk,heal"));
            fileList.Add(new WeapML("real-source12.jpg", "itisi", "gbbgg", "matk,matk,patk,heal,def"));
            fileList.Add(new WeapML("real-source13.jpg", "itisi", "gbbgg", "matk,matk,patk,heal,def"));
            fileList.Add(new WeapML("real-source14.jpg", "itisi", "gbbgg", "matk,matk,patk,heal,def"));
            fileList.Add(new WeapML("real-source15.jpg", "itisi", "gbbgg", "matk,matk,patk,heal,def"));
            fileList.Add(new WeapML("real-source16.jpg", "itisi", "gbbgg", "matk,matk,patk,heal,def"));
            fileList.Add(new WeapML("real-source17.jpg", "itisi", "gbbgg", "matk,matk,patk,heal,def"));
            fileList.Add(new WeapML("real-source18.jpg", "itisi", "gbbgg", "matk,matk,patk,heal,def"));
            fileList.Add(new WeapML("real-source19.jpg", "itisi", "gbbgg", "matk,matk,patk,heal,def"));
            fileList.Add(new WeapML("real-source20.jpg", "itisi", "gbbgg", "matk,matk,patk,heal,def"));
            fileList.Add(new WeapML("real-source21.jpg", "itisi", "gbbgg", "matk,matk,patk,heal,def"));
            fileList.Add(new WeapML("real-source23.jpg", "itist", "gbbgg", "matk,matk,patk,heal,pdef+patk"));
            fileList.Add(new WeapML("real-source24.jpg", "itist", "gbbgg", "matk,matk,patk,heal,pdef+patk"));
            fileList.Add(new WeapML("real-source25.jpg", "iti.t", "gbb.g", "matk,matk,patk,x,pdef+patk"));
            fileList.Add(new WeapML("real-source26.jpg", "iti.t", "gbb.g", "matk,matk,patk,x,pdef+patk"));
            fileList.Add(new WeapML("real-source27.jpg", "iti.t", "gbb.g", "matk,matk,patk,x,pdef+patk"));
            fileList.Add(new WeapML("real-source28.jpg", "ititi", "gbbgr", "matk,matk,patk,pdef+patk,atk"));
            fileList.Add(new WeapML("real-source30.jpg", "ititi", "gbbgr", "matk,matk,patk,pdef+patk,atk"));
            fileList.Add(new WeapML("real-source31.jpg", "ititi", "gbbgr", "matk,matk,patk,pdef+patk,atk"));
            fileList.Add(new WeapML("real-source32.jpg", "ititi", "gbbgr", "matk,matk,patk,pdef+patk,atk"));
            fileList.Add(new WeapML("real-source33.jpg", "ititi", "gbbgr", "matk,matk,patk,pdef+patk,atk"));
            fileList.Add(new WeapML("real-source35.jpg", "ititi", "gbbgg", "matk,matk,patk,pdef+patk,atk"));
            fileList.Add(new WeapML("real-source36.jpg", "ititi", "gbbgg", "matk,matk,patk,pdef+patk,atk"));
            fileList.Add(new WeapML("real-source37.jpg", "i.iti", "g.bgg", "matk,x,patk,pdef+patk,atk"));
            fileList.Add(new WeapML("real-source38.jpg", "i.iti", "g.bgg", "matk,x,patk,pdef+patk,atk"));
            fileList.Add(new WeapML("real-source39.jpg", "iitis", "gbggb", "matk,patk,pdef+patk,atk,heal"));
            fileList.Add(new WeapML("real-source40.jpg", "iitis", "gbggb", "matk,patk,pdef+patk,atk,heal"));
            fileList.Add(new WeapML("real-source41.jpg", "iitis", "gbggb", "matk,patk,pdef+patk,atk,heal"));
            fileList.Add(new WeapML("real-source42.jpg", "iitis", "gbggb", "matk,patk,pdef+patk,atk,heal"));
            fileList.Add(new WeapML("real-source43.jpg", "iitis", "gbggb", "matk,patk,pdef+patk,atk,heal"));
            fileList.Add(new WeapML("real-source44.jpg", "iiti.", "gbgg.", "matk,patk,pdef+patk,atk,x"));
            fileList.Add(new WeapML("real-source45.jpg", "iitit", "gbggr", "matk,patk,pdef+patk,atk,atk"));
            fileList.Add(new WeapML("real-source46.jpg", "iitit", "gbggr", "matk,patk,pdef+patk,atk,atk"));
            fileList.Add(new WeapML("real-source47.jpg", "iitit", "gbggr", "matk,patk,pdef+patk,atk,atk"));
            fileList.Add(new WeapML("real-source48.jpg", "iitit", "gbggr", "matk,patk,pdef+patk,atk,atk"));
            fileList.Add(new WeapML("real-source49.jpg", "iitit", "gbggr", "matk,patk,pdef+patk,atk,atk"));
            fileList.Add(new WeapML("real-source50.jpg", "iitit", "gbggr", "matk,patk,pdef+patk,atk,atk"));
            fileList.Add(new WeapML("real-source51.jpg", "ii.it", "gb.gr", "matk,patk,x,atk,atk"));
            fileList.Add(new WeapML("real-source53.jpg", "iiiti", "gbgrg", "matk,patk,atk,atk,patk"));
            fileList.Add(new WeapML("real-source54.jpg", "iiit.", "gbgr.", "matk,patk,atk,atk,x"));
            fileList.Add(new WeapML("real-source55.jpg", "iiit.", "gbgr.", "matk,patk,atk,atk,x"));
            fileList.Add(new WeapML("real-source56.jpg", "iiiti", "gbgrb", "matk,patk,atk,atk,def"));
            fileList.Add(new WeapML("real-source57.jpg", "iiiti", "gbgrb", "matk,patk,atk,atk,def"));
            fileList.Add(new WeapML("real-source58.jpg", "iiiti", "gbgrb", "matk,patk,atk,atk,def"));
            fileList.Add(new WeapML("real-source59.jpg", "iiiti", "gbgrb", "matk,patk,atk,atk,def"));
            fileList.Add(new WeapML("real-source60.jpg", "iiiti", "gbgrb", "matk,patk,atk,atk,def"));
            fileList.Add(new WeapML("real-source61.jpg", "iiiti", "gbgrb", "matk,patk,atk,atk,def"));
            fileList.Add(new WeapML("real-source62.jpg", "ii.ti", "gb.rr", "matk,patk,x,atk,def"));
            fileList.Add(new WeapML("real-source63.jpg", "iitit", "gbrrr", "matk,patk,atk,def,matk"));
            fileList.Add(new WeapML("real-source64.jpg", "iitit", "gbrrr", "matk,patk,atk,def,matk"));
            fileList.Add(new WeapML("real-source65.jpg", "iitit", "gbrrr", "matk,patk,atk,def,matk"));
            fileList.Add(new WeapML("real-source66.jpg", "iitit", "gbrrr", "matk,patk,atk,def,matk"));
            fileList.Add(new WeapML("real-source67.jpg", "iiti.", "gbrr.", "matk,patk,atk,def,x"));
            fileList.Add(new WeapML("real-source68.jpg", "iiti.", "gbrr.", "matk,patk,atk,def,x"));
            // TODO: more?

            fileList.Add(new WeapML("image-elemboost1-scaled.jpg", "i.iii", "b.gbr", "patk,x,matk,def,def", "boost:wind"));
            fileList.Add(new WeapML("image-elemboost2-scaled.jpg", "iiiit", "bgbrb", "patk,matk,def,def,atk", "boost:wind"));
            fileList.Add(new WeapML("image-elemboost3-scaled.jpg", "iiiit", "bgbrb", "patk,matk,def,def,atk", "boost:wind"));
            fileList.Add(new WeapML("image-elemboost4-scaled.jpg", "iiiit", "bgbrb", "patk,matk,def,def,atk", "boost:wind"));
            fileList.Add(new WeapML("image-elemboost5-scaled.jpg", "iiiit", "bgbrb", "patk,matk,def,def,atk", "boost:wind"));
            fileList.Add(new WeapML("image-elemboost6-scaled.jpg", "iiiit", "bgbrb", "patk,matk,def,def,atk", "boost:wind"));
            fileList.Add(new WeapML("image-elemboost7-scaled.jpg", "iiiit", "bgbrb", "patk,matk,def,def,atk", "boost:wind"));
            fileList.Add(new WeapML("image-elemboost8-scaled.jpg", "iiii.", "bgbr.", "patk,matk,def,def,x", "boost:wind"));
            fileList.Add(new WeapML("image-elemboost9-scaled.jpg", "iiii.", "bgbr.", "patk,matk,def,def,x", "boost:wind"));
            fileList.Add(new WeapML("image-elemboost10-scaled.jpg", "iiii.", "bgbr.", "patk,matk,def,def,x", "boost:wind"));
            fileList.Add(new WeapML("image-elemboost11-scaled.jpg", "iiii.", "bgbr.", "patk,matk,def,def,x", "boost:wind"));
            fileList.Add(new WeapML("image-summon1-scaled.jpg", "i....", "r....", "def,x,x,x,x"));
            fileList.Add(new WeapML("image-summon2-scaled.jpg", "i....", "r....", "def,x,x,x,x"));
            fileList.Add(new WeapML("image-summon3-scaled.jpg", ".....", ".....", "x,x,x,x,x"));
            fileList.Add(new WeapML("image-summon4-scaled.jpg", "ittit", "rbrrg", "mdef,matk,atk,atk,pdef+patk"));
            fileList.Add(new WeapML("real-orb1.jpg", "itsto", "ggbgb", "atk,def,heal,pdef+patk,matk"));
            fileList.Add(new WeapML("real-orb2.jpg", "tstoi", "gbgbr", "def,heal,pdef+patk,matk,mdef"));
            fileList.Add(new WeapML("real-orb3.jpg", "tstoi", "gbgbr", "def,heal,pdef+patk,matk,mdef"));
            fileList.Add(new WeapML("real-orb4.jpg", "tstoi", "gbgbr", "def,heal,pdef+patk,matk,mdef"));
            fileList.Add(new WeapML("real-orb6.jpg", "tsois", "gbbrg", "def,heal,matk,mdef,heal"));
            fileList.Add(new WeapML("real-orb7.jpg", "tsois", "gbbrg", "def,heal,matk,mdef,heal"));
            fileList.Add(new WeapML("real-orb8.jpg", "tsois", "gbbrg", "def,heal,matk,mdef,heal"));
            fileList.Add(new WeapML("real-orb9.jpg", "tsois", "gbbrg", "def,heal,matk,mdef,heal"));
            fileList.Add(new WeapML("real-orb10.jpg", "tsois", "gbbrg", "def,heal,matk,mdef,heal"));

            var mapEffects = new Dictionary<string, int>();
            mapEffects.Add("heal", 0);
            mapEffects.Add("patk+", 0);
            mapEffects.Add("patk-", 0);
            mapEffects.Add("matk+", 0);
            mapEffects.Add("matk-", 0);
            mapEffects.Add("atk+", 0);
            mapEffects.Add("atk-", 0);
            mapEffects.Add("pdef+", 0);
            mapEffects.Add("pdef-", 0);
            mapEffects.Add("mdef+", 0);
            mapEffects.Add("mdef-", 0);
            mapEffects.Add("def+", 0);
            mapEffects.Add("def-", 0);

            var combatScanner = scanners[scanners.Count - 1] as ScannerCombat;
            var orgDebugLevel = combatScanner.DebugLevel;
            combatScanner.DebugLevel = ScannerBase.EDebugLevel.None;

            string jsonDesc = "{\"dataset\":[";
            foreach (var fileData in fileList)
            {
                var srcScreenshot = LoadTestScreenshot("train-weapons/" + fileData.fileName);
                var fastBitmap = ScreenshotUtilities.ConvertToFastBitmap(srcScreenshot);

                for (int idx = 0; idx < 5; idx++)
                {
                    if (fileData.WeaponTypes[idx] != ScannerCombatBase.EWeaponType.Unknown)
                    {
                        if (fileData.fileName == "image-elemboost1-scaled.jpg" && idx == 2)
                        {
                            int a = 1;

                        }

                        var values = combatScanner.ExtractActionSlotWeaponData(fastBitmap, idx);
                        var elem = combatScanner.ScanElementType(fastBitmap, idx);
                        if (elem != fileData.ElementTypes[idx])
                        {
                            Console.WriteLine("Element type scan mismatch! image:{0}, slot:{1} => has:{2}, expected:{3}",
                                fileData.fileName, idx, elem, fileData.ElementTypes[idx]);
                        }

                        jsonDesc += "\n{\"input\":[";
                        jsonDesc += string.Join(",", values);
                        jsonDesc += "], \"output\":";
                        jsonDesc += (int)fileData.WeaponTypes[idx];
                        jsonDesc += "},";
                    }

                    string effectCode = fileData.effects[idx];
                    if (fileData.WeaponTypes[idx] == ScannerCombatBase.EWeaponType.Instrument) { effectCode += "+"; }
                    else if (fileData.WeaponTypes[idx] == ScannerCombatBase.EWeaponType.Tome) { effectCode += "-"; }

                    if (!mapEffects.ContainsKey(effectCode)) { mapEffects.Add(effectCode, 0); }
                    mapEffects[effectCode] += 1;
                }
            }

            jsonDesc = jsonDesc.Remove(jsonDesc.Length - 1, 1);
            jsonDesc += "\n]}";

            string savePath = @"D:\temp\recording\sino-ml-weapons.json";
            File.WriteAllText(savePath, jsonDesc);

            combatScanner.DebugLevel = orgDebugLevel;
            Console.WriteLine("Effect icons in training data:");
            foreach (var kvp in mapEffects)
            {
                if (kvp.Key != "x")
                {
                    Console.WriteLine("  {0}: {1} {2}", kvp.Key, kvp.Value, kvp.Value == 0 ? " << MISSING!" : "");
                }
            }
        }

        private void GatherMLDataDemon()
        {
            var fileMap = new Dictionary<string, bool>();
            fileMap.Add("real-source1-scaled.jpg", false);
            fileMap.Add("real-source2-scaled.jpg", false);
            fileMap.Add("real-source3-scaled.jpg", false);
            fileMap.Add("real-source4-scaled.jpg", true);
            fileMap.Add("real-source5-scaled.jpg", true);
            fileMap.Add("real-source6-scaled.jpg", true);
            fileMap.Add("real-source7-scaled.jpg", true);
            fileMap.Add("real-source8-scaled.jpg", true);
            fileMap.Add("real-source9-scaled.jpg", true);
            fileMap.Add("real-source11-scaled.jpg", true);
            fileMap.Add("real-source12-scaled.jpg", true);
            fileMap.Add("real-source13-scaled.jpg", true);
            fileMap.Add("real-source14-scaled.jpg", false);
            fileMap.Add("real-source15-scaled.jpg", false);
            fileMap.Add("real-source16-scaled.jpg", false);
            fileMap.Add("real-source17-scaled.jpg", false);
            fileMap.Add("real-source18-scaled.jpg", false);
            fileMap.Add("real-source19-scaled.jpg", false);
            fileMap.Add("real-source21-scaled.jpg", false);
            fileMap.Add("real-source22-scaled.jpg", false);
            fileMap.Add("real-source23-scaled.jpg", false);
            fileMap.Add("real-source24-scaled.jpg", false);
            fileMap.Add("real-source25-scaled.jpg", false);
            fileMap.Add("real-source26-scaled.jpg", false);
            fileMap.Add("real-source27-scaled.jpg", false);
            fileMap.Add("real-source28-scaled.jpg", false);

            fileMap.Add("real-demon1.jpg", true);
            fileMap.Add("real-demon2.jpg", true);
            fileMap.Add("real-demon3.jpg", true);
            fileMap.Add("real-demon5.jpg", true);
            fileMap.Add("real-demon6.jpg", true);
            fileMap.Add("real-demon7.jpg", true);
            fileMap.Add("real-demon8.jpg", true);
            fileMap.Add("real-demon9.jpg", true);
            fileMap.Add("real-demon10.jpg", true);
            fileMap.Add("real-demon11.jpg", true);
            fileMap.Add("real-demon12.jpg", true);
            fileMap.Add("real-demon13.jpg", true);
            fileMap.Add("real-demon14.jpg", true);
            fileMap.Add("real-demon15.jpg", true);
            fileMap.Add("real-demon16.jpg", true);
            fileMap.Add("real-nodemon1.jpg", false);
            fileMap.Add("real-nodemon2.jpg", false);
            fileMap.Add("real-nodemon3.jpg", false);
            fileMap.Add("real-nodemon4.jpg", false);
            fileMap.Add("real-nodemon5.jpg", false);
            fileMap.Add("real-nodemon6.jpg", false);
            fileMap.Add("real-nodemon7.jpg", false);
            fileMap.Add("real-nodemon8.jpg", false);
            fileMap.Add("real-nodemon9.jpg", false);
            fileMap.Add("real-nodemon10.jpg", false);
            fileMap.Add("real-nodemon11.jpg", false);
            fileMap.Add("real-nodemon12.jpg", false);
            fileMap.Add("real-nodemon13.jpg", false);


            string jsonDesc = "{\"dataset\":[";
            foreach (var kvp in fileMap)
            {
                var srcScreenshot = LoadTestScreenshot("train-demon/" + kvp.Key);
                var fastBitmap = ScreenshotUtilities.ConvertToFastBitmap(srcScreenshot);

                var combatScanner = scanners[0] as ScannerColoCombat;
                for (int idx = 0; idx < 2; idx++)
                {
                    var values = combatScanner.ExtractDemonCounterData(fastBitmap, idx);

                    jsonDesc += "\n{\"input\":[";
                    jsonDesc += string.Join(",", values);
                    jsonDesc += "], \"output\":";
                    jsonDesc += kvp.Value ? 1 : 0;
                    jsonDesc += "},";
                }
            }

            jsonDesc = jsonDesc.Remove(jsonDesc.Length - 1, 1);
            jsonDesc += "\n]}";

            string savePath = @"D:\temp\recording\sino-ml-demon.json";
            File.WriteAllText(savePath, jsonDesc);
        }

        private void GatherMLDataPurify()
        {
            List<PurifyML> fileList = new List<PurifyML>();
            fileList.Add(new PurifyML("purify-3-scaled.jpg", ".SSSSSxS"));
            fileList.Add(new PurifyML("purify-6-scaled.jpg", ".S.LSSxS"));
            fileList.Add(new PurifyML("purify-7-scaled.jpg", ".S..LSxS"));
            fileList.Add(new PurifyML("purify-8-scaled.jpg", ".S..xLxS"));
            fileList.Add(new PurifyML("purify-9-scaled.jpg", ".S..xxxS"));
            fileList.Add(new PurifyML("purify-10-scaled.jpg", ".S..xxxL"));
            fileList.Add(new PurifyML("purify-10-scaled.jpg", ".L..xxxx"));
            fileList.Add(new PurifyML("purify-11-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-12-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-13-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-14-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-17-scaled.jpg", "..LSxSxB"));
            fileList.Add(new PurifyML("purify-18-scaled.jpg", "...LxSxB"));
            fileList.Add(new PurifyML("purify-19-scaled.jpg", "....xLxB"));
            fileList.Add(new PurifyML("purify-20-scaled.jpg", "....xxxB"));
            fileList.Add(new PurifyML("purify-21-scaled.jpg", "....xxxM"));
            fileList.Add(new PurifyML("purify-22-scaled.jpg", "....xxxM"));
            fileList.Add(new PurifyML("purify-23-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-24-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-27-scaled.jpg", ".SSSSSSx"));
            fileList.Add(new PurifyML("purify-28-scaled.jpg", ".LSSSSSx"));
            fileList.Add(new PurifyML("purify-29-scaled.jpg", "..LSSSSx"));
            fileList.Add(new PurifyML("purify-30-scaled.jpg", "...SSSSx"));
            fileList.Add(new PurifyML("purify-31-scaled.jpg", "...LSSSx"));
            fileList.Add(new PurifyML("purify-32-scaled.jpg", "....xSSx"));
            fileList.Add(new PurifyML("purify-33-scaled.jpg", "....xLSx"));
            fileList.Add(new PurifyML("purify-34-scaled.jpg", "....xxSx"));
            fileList.Add(new PurifyML("purify-35-scaled.jpg", "....xxLx"));
            fileList.Add(new PurifyML("purify-36-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-37-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-40-scaled.jpg", ".SSSSSBB"));
            fileList.Add(new PurifyML("purify-41-scaled.jpg", ".LSSSSBB"));
            fileList.Add(new PurifyML("purify-42-scaled.jpg", "..SSSSBB"));
            fileList.Add(new PurifyML("purify-43-scaled.jpg", "..LSSSBB"));
            fileList.Add(new PurifyML("purify-44-scaled.jpg", "...LSSBB"));
            fileList.Add(new PurifyML("purify-45-scaled.jpg", "....LSBB"));
            fileList.Add(new PurifyML("purify-46-scaled.jpg", "....xLBB"));
            fileList.Add(new PurifyML("purify-47-scaled.jpg", "....xxMB"));
            fileList.Add(new PurifyML("purify-48-scaled.jpg", "....xxMM"));
            fileList.Add(new PurifyML("purify-49-scaled.jpg", "....xxxM"));
            fileList.Add(new PurifyML("purify-50-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-54-scaled.jpg", ".SBSSSSx"));
            fileList.Add(new PurifyML("purify-55-scaled.jpg", ".SMSSSSx"));
            fileList.Add(new PurifyML("purify-56-scaled.jpg", ".S.SSSSx"));
            fileList.Add(new PurifyML("purify-57-scaled.jpg", ".S.LSSSx"));
            fileList.Add(new PurifyML("purify-58-scaled.jpg", ".S..SSSx"));
            fileList.Add(new PurifyML("purify-59-scaled.jpg", ".S..xSSx"));
            fileList.Add(new PurifyML("purify-60-scaled.jpg", ".S..xSSx"));
            fileList.Add(new PurifyML("purify-61-scaled.jpg", ".S..xLSx"));
            fileList.Add(new PurifyML("purify-62-scaled.jpg", ".S..xxSx"));
            fileList.Add(new PurifyML("purify-63-scaled.jpg", ".L..xxxx"));
            fileList.Add(new PurifyML("purify-64-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-65-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-69-scaled.jpg", "S..MSSSS"));
            fileList.Add(new PurifyML("purify-70-scaled.jpg", "S...SSSS"));
            fileList.Add(new PurifyML("purify-71-scaled.jpg", "S...SSSS"));
            fileList.Add(new PurifyML("purify-72-scaled.jpg", "S...xSSS"));
            fileList.Add(new PurifyML("purify-73-scaled.jpg", "S...xLSS"));
            fileList.Add(new PurifyML("purify-74-scaled.jpg", "S...xxLS"));
            fileList.Add(new PurifyML("purify-75-scaled.jpg", "S...xxxS"));
            fileList.Add(new PurifyML("purify-76-scaled.jpg", "S...xxxS"));
            fileList.Add(new PurifyML("purify-77-scaled.jpg", "S...xxxS"));
            fileList.Add(new PurifyML("purify-78-scaled.jpg", "S...xxxS"));
            fileList.Add(new PurifyML("purify-79-scaled.jpg", "S...xxxS"));
            fileList.Add(new PurifyML("purify-80-scaled.jpg", "S...xxxS"));
            fileList.Add(new PurifyML("purify-81-scaled.jpg", "S...xxxS"));
            fileList.Add(new PurifyML("purify-86-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-87-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-90-scaled.jpg", "S..SxxSS"));
            fileList.Add(new PurifyML("purify-91-scaled.jpg", "S..LxxSS"));
            fileList.Add(new PurifyML("purify-92-scaled.jpg", "S...xxLS"));
            fileList.Add(new PurifyML("purify-93-scaled.jpg", "S...xxxL"));
            fileList.Add(new PurifyML("purify-94-scaled.jpg", "L...xxxx"));
            fileList.Add(new PurifyML("purify-95-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-98-scaled.jpg", "S.S.SxxS"));
            fileList.Add(new PurifyML("purify-99-scaled.jpg", "..S.SxxS"));
            fileList.Add(new PurifyML("purify-100-scaled.jpg", "..L.SxxS"));
            fileList.Add(new PurifyML("purify-101-scaled.jpg", "....LxxS"));
            fileList.Add(new PurifyML("purify-102-scaled.jpg", "....xxxL"));
            fileList.Add(new PurifyML("purify-103-scaled.jpg", "....xxxL"));
            fileList.Add(new PurifyML("purify-104-scaled.jpg", "....xxxL"));
            fileList.Add(new PurifyML("purify-105-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-106-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-109-scaled.jpg", "..SSSSSS"));
            fileList.Add(new PurifyML("purify-110-scaled.jpg", "..SSSSSS"));
            fileList.Add(new PurifyML("purify-111-scaled.jpg", "..SSSSSS"));
            fileList.Add(new PurifyML("purify-112-scaled.jpg", "..SSSSSS"));
            fileList.Add(new PurifyML("purify-113-scaled.jpg", "..SSSSSL"));
            fileList.Add(new PurifyML("purify-114-scaled.jpg", "..SSSSSx"));
            fileList.Add(new PurifyML("purify-115-scaled.jpg", "..LSSSSx"));
            fileList.Add(new PurifyML("purify-116-scaled.jpg", "...LSSSx"));
            fileList.Add(new PurifyML("purify-117-scaled.jpg", "....LSSx"));
            fileList.Add(new PurifyML("purify-118-scaled.jpg", "....xSSx"));
            fileList.Add(new PurifyML("purify-119-scaled.jpg", "....xLSx"));
            fileList.Add(new PurifyML("purify-120-scaled.jpg", "....xxSx"));
            fileList.Add(new PurifyML("purify-121-scaled.jpg", "....xxLx"));
            fileList.Add(new PurifyML("purify-122-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-126-scaled.jpg", "SSBSSSxS"));
            fileList.Add(new PurifyML("purify-127-scaled.jpg", "SSBSSSxS"));
            fileList.Add(new PurifyML("purify-128-scaled.jpg", "SSBSSSxS"));
            fileList.Add(new PurifyML("purify-129-scaled.jpg", "SSBSSSxS"));
            fileList.Add(new PurifyML("purify-130-scaled.jpg", "SSBSSSxS"));
            fileList.Add(new PurifyML("purify-147-scaled.jpg", "LSSSSSSS"));
            fileList.Add(new PurifyML("purify-148-scaled.jpg", ".LSSSSSS"));
            fileList.Add(new PurifyML("purify-149-scaled.jpg", "..SSSSSS"));
            fileList.Add(new PurifyML("purify-150-scaled.jpg", "..LSSSSS"));
            fileList.Add(new PurifyML("purify-151-scaled.jpg", "...LSSSS"));
            fileList.Add(new PurifyML("purify-152-scaled.jpg", "....SSSS"));
            fileList.Add(new PurifyML("purify-153-scaled.jpg", "....xSSS"));
            fileList.Add(new PurifyML("purify-154-scaled.jpg", "....xLSS"));
            fileList.Add(new PurifyML("purify-155-scaled.jpg", "....xxLS"));
            fileList.Add(new PurifyML("purify-156-scaled.jpg", "....xxxS"));
            fileList.Add(new PurifyML("purify-157-scaled.jpg", "....xxxL"));
            fileList.Add(new PurifyML("purify-158-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-159-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-162-scaled.jpg", "SSSSxBxS"));
            fileList.Add(new PurifyML("purify-163-scaled.jpg", "SSSSxBxL"));
            fileList.Add(new PurifyML("purify-164-scaled.jpg", ".SSSxBxx"));
            fileList.Add(new PurifyML("purify-165-scaled.jpg", "..SSxBxx"));
            fileList.Add(new PurifyML("purify-166-scaled.jpg", "..LSxBxx"));
            fileList.Add(new PurifyML("purify-167-scaled.jpg", "...LxBxx"));
            fileList.Add(new PurifyML("purify-168-scaled.jpg", "....xBxx"));
            fileList.Add(new PurifyML("purify-169-scaled.jpg", "....xBxx"));
            fileList.Add(new PurifyML("purify-170-scaled.jpg", "....xBxx"));
            fileList.Add(new PurifyML("purify-171-scaled.jpg", "....xBxx"));
            fileList.Add(new PurifyML("purify-176-scaled.jpg", "....xMxx"));
            fileList.Add(new PurifyML("purify-177-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-178-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-179-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-182-scaled.jpg", "SSSSSLSS"));
            fileList.Add(new PurifyML("purify-183-scaled.jpg", "SSSSSxLS"));
            fileList.Add(new PurifyML("purify-184-scaled.jpg", "SSSSSxxS"));
            fileList.Add(new PurifyML("purify-185-scaled.jpg", "SSSSSxxL"));
            fileList.Add(new PurifyML("purify-186-scaled.jpg", "SSSSSxxx"));
            fileList.Add(new PurifyML("purify-187-scaled.jpg", ".LSSSxxx"));
            fileList.Add(new PurifyML("purify-188-scaled.jpg", "..SSSxxx"));
            fileList.Add(new PurifyML("purify-189-scaled.jpg", "..LSSxxx"));
            fileList.Add(new PurifyML("purify-190-scaled.jpg", "...LSxxx"));
            fileList.Add(new PurifyML("purify-191-scaled.jpg", "....Sxxx"));
            fileList.Add(new PurifyML("purify-192-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-193-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-196-scaled.jpg", "S...LSSx"));
            fileList.Add(new PurifyML("purify-197-scaled.jpg", "S...xLSx"));
            fileList.Add(new PurifyML("purify-198-scaled.jpg", "S...xLSx"));
            fileList.Add(new PurifyML("purify-199-scaled.jpg", "S...xxLx"));
            fileList.Add(new PurifyML("purify-200-scaled.jpg", "S...xxxx"));
            fileList.Add(new PurifyML("purify-201-scaled.jpg", "L...xxxx"));
            fileList.Add(new PurifyML("purify-202-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-203-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-207-scaled.jpg", ".L..xSBS"));
            fileList.Add(new PurifyML("purify-208-scaled.jpg", "....xLBS"));
            fileList.Add(new PurifyML("purify-209-scaled.jpg", "....xLMS"));
            /*fileList.Add(new PurifyML("purify-200-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-201-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-202-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-203-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-204-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-205-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-206-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-207-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-208-scaled.jpg", "....xxxx"));
            fileList.Add(new PurifyML("purify-209-scaled.jpg", "....xxxx"));*/
            fileList.Add(new PurifyML("colo2-purify1.jpg", "..B.xxxx"));
            fileList.Add(new PurifyML("colo2-purify2.jpg", "..B.xxxx"));
            fileList.Add(new PurifyML("colo2-purify3.jpg", "...SSBxx"));
            fileList.Add(new PurifyML("colo2-purify4.jpg", "....xBxx"));
            fileList.Add(new PurifyML("colo2-purify5.jpg", "....xBxx"));
            fileList.Add(new PurifyML("colo2-purify6.jpg", "....xBxx"));
            fileList.Add(new PurifyML("colo2-purify7.jpg", "....xBxx"));
            fileList.Add(new PurifyML("colo2-purify8.jpg", "....xBxx"));
            fileList.Add(new PurifyML("colo2-purify10.jpg", "SS..xxSS"));
            fileList.Add(new PurifyML("colo2-purify11.jpg", ".LS.SxSx"));
            fileList.Add(new PurifyML("colo2-purify12.jpg", "SS..xBxx"));
            fileList.Add(new PurifyML("colo2-purify13.jpg", "...LBxxx"));
            fileList.Add(new PurifyML("colo2-purify18.jpg", "BSS.BSSS"));
            fileList.Add(new PurifyML("colo2-purify20.jpg", "B...xSSS"));
            fileList.Add(new PurifyML("colo2-purify22.jpg", "..SSxBSx"));
            fileList.Add(new PurifyML("colo2-purify23.jpg", ".SSSSSSx"));

            string jsonDesc = "{\"dataset\":[";
            foreach (var fileData in fileList)
            {
                var srcScreenshot = LoadTestScreenshot("train-purify-pvp/" + fileData.fileName);
                var fastBitmap = ScreenshotUtilities.ConvertToFastBitmap(srcScreenshot);

                var combatScanner = scanners[1] as ScannerColoPurify;
                for (int idx = 0; idx < 8; idx++)
                {
                    var values = combatScanner.ExtractActionSlotData(fastBitmap, idx);

                    jsonDesc += "\n{\"input\":[";
                    jsonDesc += string.Join(",", values);
                    jsonDesc += "], \"output\":";
                    jsonDesc += (int)fileData.SlotTypes[idx];
                    jsonDesc += "},";
                }
            }

            jsonDesc = jsonDesc.Remove(jsonDesc.Length - 1, 1);
            jsonDesc += "\n]}";

            string savePath = @"D:\temp\recording\sino-ml-purify.json";
            File.WriteAllText(savePath, jsonDesc);
        }

        private void GatherMLDataButtons()
        {
            List<ButtonML> fileList = new List<ButtonML>();
            fileList.Add(new ButtonML("real-buttons.jpg", "reportRetry:retry, reportOk:ok"));
            fileList.Add(new ButtonML("real-buttonNext1.jpg", "reportRetry:next, reportOk:ok"));
            fileList.Add(new ButtonML("real-buttonNext2.jpg", "reportRetry:next, reportOk:ok"));
            fileList.Add(new ButtonML("real-source4.jpg", "reportRetry:retry, reportOk:ok"));
            fileList.Add(new ButtonML("real-source5.jpg", "reportRetry:retry, reportOk:ok"));
            fileList.Add(new ButtonML("real-source6.jpg", "reportRetry:retry, reportOk:ok"));
            fileList.Add(new ButtonML("real-source7.jpg", "reportRetry:retry, reportOk:ok"));
            fileList.Add(new ButtonML("real-msg1.jpg", "center:ok"));
            fileList.Add(new ButtonML("real-msg2.jpg", "center:ok"));
            fileList.Add(new ButtonML("real-msgClose.jpg", "center:close"));
            fileList.Add(new ButtonML("real-msgClose2.jpg", "center:close"));
            fileList.Add(new ButtonML("real-msgOkCancel.jpg", "centerL:cancel, centerR:ok"));

            string jsonDesc = "{\"dataset\":[";
            foreach (var fileData in fileList)
            {
                var srcScreenshot = LoadTestScreenshot("train-smol/" + fileData.fileName);
                var fastBitmap = ScreenshotUtilities.ConvertToFastBitmap(srcScreenshot);

                var buttonsScanner = scanners[2] as ScannerMessageBox;
                for (int idx = 0; idx < fileData.ButtonSlots.Length; idx++)
                {
                    if (fileData.ButtonSlots[idx] == ScannerMessageBox.EButtonType.Unknown)
                    {
                        continue;
                    }

                    var values = buttonsScanner.ExtractButtonData(fastBitmap, idx);
                    jsonDesc += "\n{\"input\":[";
                    jsonDesc += string.Join(",", values);
                    jsonDesc += "], \"output\":";
                    jsonDesc += (int)fileData.ButtonSlots[idx];
                    jsonDesc += "},";
                }
            }

            jsonDesc = jsonDesc.Remove(jsonDesc.Length - 1, 1);
            jsonDesc += "\n]}";

            string savePath = @"D:\temp\recording\sino-ml-buttons.json";
            File.WriteAllText(savePath, jsonDesc);
        }
    }
}
