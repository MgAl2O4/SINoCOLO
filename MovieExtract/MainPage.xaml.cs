using SINoVision;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Editing;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SINoCOLO
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MediaComposition movieComp = null;
        private uint frameWidth = 0;
        private uint frameHeight = 0;
        private bool ignoreTimeSync = false;
        private ImageStream frameStream = null;

        private List<ScannerBase> scanners = new List<ScannerBase>();
        private ScannerColoPurify scannerPurify = null;
        private FastBitmapHSV cachedBitmap = null;

        public MainPage()
        {
            this.InitializeComponent();

            scanners.Add(new ScannerColoCombat());
            scanners.Add(new ScannerColoPurify());
            scanners.Add(new ScannerMessageBox());

            scannerPurify = scanners[1] as ScannerColoPurify;
        }

        private async Task LoadMovieData()
        {
            //pick mp4 file
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            picker.FileTypeFilter.Add(".mp4");
            StorageFile pickedFile = await picker.PickSingleFileAsync();
            if (pickedFile == null)
            {
                return;
            }
            ///

            //Get video resolution
            List<string> encodingPropertiesToRetrieve = new List<string>();
            encodingPropertiesToRetrieve.Add("System.Video.FrameHeight");
            encodingPropertiesToRetrieve.Add("System.Video.FrameWidth");
            IDictionary<string, object> encodingProperties = await pickedFile.Properties.RetrievePropertiesAsync(encodingPropertiesToRetrieve);
            frameHeight = (uint)encodingProperties["System.Video.FrameHeight"];
            frameWidth = (uint)encodingProperties["System.Video.FrameWidth"];
            ///

            //Use Windows.Media.Editing to get ImageStream
            var clip = await MediaClip.CreateFromFileAsync(pickedFile);
            movieComp = new MediaComposition();
            movieComp.Clips.Add(clip);

            timeSlider.Maximum = clip.OriginalDuration.TotalSeconds;
            timeSlider.Value = 0;
            Slider_ValueChanged(null, null);
        }

        private async Task LoadFrame(TimeSpan timeOfFrame)
        {
            frameStream = await movieComp.GetThumbnailAsync(timeOfFrame, (int)frameWidth, (int)frameHeight, VideoFramePrecision.NearestFrame);
            ///

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.SetSource(frameStream);

            previewImage.Source = bitmapImage;

            await AnalyzeFrame();
        }

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!ignoreTimeSync)
            {
                ignoreTimeSync = true;
                timeText.Text = timeSlider.Value.ToString();
                ignoreTimeSync = false;
            }

            int timeSec = (int)timeSlider.Value;
            int timeMSec = (int)Math.Floor(timeSlider.Value * 1000) % 1000;
            TimeSpan timeOfFrame = new TimeSpan(0, 0, 0, timeSec, timeMSec);
            LoadFrame(timeOfFrame);
        }

        private void timeText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(timeText.Text, out double timeValue))
            {
                if (!ignoreTimeSync)
                {
                    ignoreTimeSync = true;
                    timeSlider.Value = timeValue;
                    ignoreTimeSync = false;
                }
            }
        }

        private async void ClickSaveFrame(object sender, RoutedEventArgs e)
        {
            if (frameStream != null)
            {
                await SaveCurrentFrame();
            }
        }

        private async Task SaveCurrentFrame()
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeChoices.Add("JPEG files", new List<string>() { ".jpg" });
            picker.SuggestedFileName = "image-" + timeText.Text.Replace(".", "m");

            StorageFile pickedFile = await picker.PickSaveFileAsync();
            if (pickedFile == null)
            {
                return;
            }

            try
            {
                var _bitmap = new RenderTargetBitmap();
                await _bitmap.RenderAsync(previewImage);

                var pixels = await _bitmap.GetPixelsAsync();
                using (IRandomAccessStream stream = await pickedFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await
                    BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                    byte[] bytes = pixels.ToArray();
                    encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                            BitmapAlphaMode.Ignore,
                                            (uint)_bitmap.PixelWidth,
                                            (uint)_bitmap.PixelHeight,
                                            200,
                                            200,
                                            bytes);

                    await encoder.FlushAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task AnalyzeFrame()
        {
            textScanResults.Text = "";

            var _bitmap = new RenderTargetBitmap();
            await _bitmap.RenderAsync(previewImage);

            var pixels = await _bitmap.GetPixelsAsync();
            try
            {
                byte[] pixelData = pixels.ToArray();
                int pixelW = _bitmap.PixelWidth;
                int pixelH = _bitmap.PixelHeight;

                var analyzeBitmap = ScreenshotUtilities.ConvertToFastBitmap(pixelData, _bitmap.PixelWidth, _bitmap.PixelHeight);
                cachedBitmap = analyzeBitmap;

                foreach (var scanner in scanners)
                {
                    object resultOb = scanner.Process(analyzeBitmap);
                    if (resultOb != null)
                    {
                        textScanResults.Text = scanner.ScannerName + " found!\n\n" + resultOb;
                        break;
                    }
                }

                var showBitmapPixels = new byte[pixelW * pixelH * 4];
                int writeIdx = 0;
                for (int idxY = 0; idxY < analyzeBitmap.Height; idxY++)
                {
                    for (int idxX = 0; idxX < analyzeBitmap.Width; idxX++)
                    {
                        FastPixelHSV testPx = analyzeBitmap.GetPixel(idxX, idxY);
                        showBitmapPixels[writeIdx + 0] = (byte)testPx.GetMonochrome();      // B
                        showBitmapPixels[writeIdx + 1] = (byte)testPx.GetMonochrome();      // G
                        showBitmapPixels[writeIdx + 2] = (byte)testPx.GetMonochrome();      // R
                        showBitmapPixels[writeIdx + 3] = 255;                               // A

                        writeIdx += 4;
                    }
                }

                var showBitmap = new WriteableBitmap(pixelW, pixelH);
                using (Stream stream = showBitmap.PixelBuffer.AsStream())
                {
                    await stream.WriteAsync(showBitmapPixels, 0, showBitmapPixels.Length);
                }
                processedImage.Source = showBitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async void ClickLoadMovie(object sender, RoutedEventArgs e)
        {
            await LoadMovieData();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadMovieData();
        }

        private Dictionary<double, int[]> BuildPurifyFrameMap_Purify()
        {
            //colo2 movie
            var mapPatterns = new Dictionary<double, int[]>();
            mapPatterns.Add(183.50, new int[] { 1, 0, 0, 1, 0, 0, 0, 2 });
            mapPatterns.Add(183.75, new int[] { 1, 0, 0, 0, 0, 0, 0, 2 });
            mapPatterns.Add(184.00, new int[] { 1, 0, 0, 0, 0, 0, 0, 2 });
            mapPatterns.Add(184.25, new int[] { 1, 0, 0, 0, 0, 0, 0, 2 });
            mapPatterns.Add(184.50, new int[] { 1, 0, 0, 0, 0, 0, 0, 2 });
            mapPatterns.Add(184.75, new int[] { 1, 0, 0, 0, 0, 0, 0, 2 });
            mapPatterns.Add(185.00, new int[] { 1, 0, 0, 0, 0, 0, 0, 2 });
            mapPatterns.Add(185.25, new int[] { 1, 0, 0, 0, 0, 0, 0, 2 });
            mapPatterns.Add(185.50, new int[] { 1, 0, 0, 0, 0, 0, 0, 2 });
            mapPatterns.Add(185.75, new int[] { 1, 0, 0, 0, 0, 0, 0, 2 });
            mapPatterns.Add(186.00, new int[] { 1, 0, 0, 0, 0, 0, 0, 2 });
            mapPatterns.Add(186.25, new int[] { 1, 0, 0, 0, 0, 0, 0, 2 });
            mapPatterns.Add(186.50, new int[] { 1, 0, 0, 0, 0, 0, 0, 2 });
            mapPatterns.Add(186.75, new int[] { 1, 0, 0, 0, 0, 0, 0, 2 });
            mapPatterns.Add(187.00, new int[] { 1, 0, 0, 0, 0, 0, 0, 2 });
            mapPatterns.Add(187.25, new int[] { 1, 0, 0, 0, 0, 0, 0, 2 });
            mapPatterns.Add(187.50, new int[] { 1, 0, 0, 0, 0, 0, 0, 2 });
            mapPatterns.Add(187.75, new int[] { 1, 0, 0, 0, 0, 0, 0, 3 });
            mapPatterns.Add(188.00, new int[] { 3, 0, 0, 0, 0, 0, 0, 3 });
            mapPatterns.Add(188.25, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 });
            mapPatterns.Add(188.50, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 });
            mapPatterns.Add(188.75, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 });

            mapPatterns.Add(189.25, new int[] { 1, 1, 3, 1, 1, 0, 0, 1 });
            mapPatterns.Add(189.50, new int[] { 1, 1, 0, 3, 1, 0, 0, 1 });
            mapPatterns.Add(189.75, new int[] { 1, 1, 0, 0, 3, 0, 0, 1 });
            mapPatterns.Add(190.00, new int[] { 1, 1, 0, 0, 0, 0, 0, 1 });
            mapPatterns.Add(190.25, new int[] { 1, 1, 0, 0, 0, 0, 0, 3 });
            mapPatterns.Add(190.50, new int[] { 3, 1, 0, 0, 0, 0, 0, 3 });
            mapPatterns.Add(190.75, new int[] { 3, 3, 0, 0, 0, 0, 0, 3 });

            mapPatterns.Add(192.00, new int[] { 1, 0, 1, 1, 0, 1, 0, 0 });
            mapPatterns.Add(192.25, new int[] { 1, 0, 1, 1, 0, 1, 0, 0 });
            mapPatterns.Add(192.50, new int[] { 0, 0, 1, 1, 0, 1, 0, 0 });
            mapPatterns.Add(192.75, new int[] { 0, 0, 3, 1, 0, 1, 0, 0 });
            mapPatterns.Add(193.00, new int[] { 0, 0, 3, 1, 0, 1, 0, 0 });
            mapPatterns.Add(193.25, new int[] { 0, 0, 0, 3, 0, 1, 0, 0 });
            mapPatterns.Add(193.50, new int[] { 0, 0, 0, 0, 0, 3, 0, 0 });
            mapPatterns.Add(193.75, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 });
            mapPatterns.Add(194.00, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 });

            mapPatterns.Add(194.25, new int[] { 0, 1, 0, 2, 0, 1, 0, 1 });
            mapPatterns.Add(194.50, new int[] { 0, 1, 0, 2, 0, 1, 0, 1 });
            mapPatterns.Add(194.75, new int[] { 0, 1, 0, 2, 0, 1, 0, 3 });
            mapPatterns.Add(195.00, new int[] { 0, 1, 0, 2, 0, 1, 0, 0 });
            mapPatterns.Add(195.25, new int[] { 0, 1, 0, 2, 0, 3, 0, 0 });
            mapPatterns.Add(195.50, new int[] { 0, 1, 0, 2, 0, 3, 0, 0 });
            mapPatterns.Add(195.75, new int[] { 0, 1, 0, 2, 0, 0, 0, 0 });
            mapPatterns.Add(196.00, new int[] { 0, 3, 0, 2, 0, 0, 0, 0 });
            mapPatterns.Add(196.25, new int[] { 0, 3, 0, 2, 0, 0, 0, 0 });
            mapPatterns.Add(196.50, new int[] { 0, 0, 0, 3, 0, 0, 0, 0 });
            mapPatterns.Add(196.75, new int[] { 0, 0, 0, 3, 0, 0, 0, 0 });
            mapPatterns.Add(197.00, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 });

            mapPatterns.Add(197.75, new int[] { 1, 1, 3, 1, 1, 2, 1, 1 });
            mapPatterns.Add(198.00, new int[] { 1, 1, 0, 3, 1, 2, 1, 1 });
            mapPatterns.Add(198.25, new int[] { 1, 1, 0, 0, 0, 2, 1, 1 });
            mapPatterns.Add(198.50, new int[] { 1, 1, 0, 0, 0, 2, 1, 1 });
            mapPatterns.Add(198.75, new int[] { 1, 1, 0, 0, 0, 2, 3, 1 });
            mapPatterns.Add(199.00, new int[] { 1, 1, 0, 0, 0, 2, 0, 3 });
            mapPatterns.Add(199.25, new int[] { 0, 1, 0, 0, 0, 2, 0, 0 });
            mapPatterns.Add(199.50, new int[] { 0, 1, 0, 0, 0, 2, 0, 0 });
            mapPatterns.Add(199.75, new int[] { 0, 3, 0, 0, 0, 2, 0, 0 });
            mapPatterns.Add(200.00, new int[] { 0, 0, 0, 0, 0, 2, 0, 0 });
            mapPatterns.Add(200.25, new int[] { 0, 0, 0, 0, 0, 3, 0, 0 });
            mapPatterns.Add(200.50, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 });
            mapPatterns.Add(200.75, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 });

            mapPatterns.Add(201.25, new int[] { 1, 0, 1, 0, 1, 1, 1, 1 });
            mapPatterns.Add(201.50, new int[] { 1, 0, 1, 0, 1, 1, 1, 3 });
            mapPatterns.Add(201.75, new int[] { 1, 0, 3, 0, 1, 1, 1, 0 });
            mapPatterns.Add(202.00, new int[] { 1, 0, 3, 0, 3, 1, 1, 0 });
            mapPatterns.Add(202.25, new int[] { 1, 0, 0, 0, 3, 3, 1, 0 });
            mapPatterns.Add(202.50, new int[] { 1, 0, 0, 0, 0, 3, 3, 0 });
            mapPatterns.Add(202.75, new int[] { 1, 0, 0, 0, 0, 0, 3, 0 });
            mapPatterns.Add(203.00, new int[] { 3, 0, 0, 0, 0, 0, 0, 0 });
            mapPatterns.Add(203.25, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 });
            mapPatterns.Add(203.50, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 });

            mapPatterns.Add(654.50, new int[] { 1, 1, 0, 0, 1, 2, 1, 1 });
            mapPatterns.Add(654.75, new int[] { 1, 1, 0, 0, 1, 2, 1, 1 });
            mapPatterns.Add(655.00, new int[] { 1, 1, 0, 0, 1, 2, 1, 1 });
            mapPatterns.Add(655.25, new int[] { 1, 1, 0, 0, 1, 2, 1, 1 });
            mapPatterns.Add(655.50, new int[] { 1, 1, 0, 0, 1, 2, 1, 1 });
            mapPatterns.Add(655.75, new int[] { 1, 1, 0, 0, 1, 2, 1, 1 });
            mapPatterns.Add(656.00, new int[] { 1, 1, 0, 0, 1, 2, 1, 1 });
            mapPatterns.Add(656.25, new int[] { 1, 1, 0, 0, 3, 3, 1, 1 });
            mapPatterns.Add(656.50, new int[] { 1, 1, 0, 0, 0, 3, 1, 1 });
            mapPatterns.Add(656.75, new int[] { 1, 1, 0, 0, 0, 0, 3, 1 });
            mapPatterns.Add(657.00, new int[] { 1, 1, 0, 0, 0, 0, 0, 1 });
            mapPatterns.Add(657.25, new int[] { 3, 1, 0, 0, 0, 0, 0, 1 });
            mapPatterns.Add(657.50, new int[] { 0, 3, 0, 0, 0, 0, 0, 1 });
            mapPatterns.Add(657.75, new int[] { 0, 0, 0, 0, 0, 0, 0, 1 });
            mapPatterns.Add(658.00, new int[] { 0, 0, 0, 0, 0, 0, 0, 1 });
            mapPatterns.Add(658.25, new int[] { 0, 0, 0, 0, 0, 0, 0, 3 });
            mapPatterns.Add(658.50, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 });
            mapPatterns.Add(658.75, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 });
            mapPatterns.Add(659.00, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 });
            mapPatterns.Add(659.25, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 });

            mapPatterns.Add(757.00, new int[] { 1, 1, 0, 0, 0, 3, 0, 1 });
            mapPatterns.Add(757.25, new int[] { 1, 1, 0, 0, 0, 0, 0, 1 });
            mapPatterns.Add(757.50, new int[] { 1, 1, 0, 0, 0, 0, 0, 3 });
            mapPatterns.Add(757.75, new int[] { 3, 1, 0, 0, 0, 0, 0, 3 });
            mapPatterns.Add(758.00, new int[] { 0, 3, 0, 0, 0, 0, 0, 0 });
            mapPatterns.Add(758.25, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 });
            mapPatterns.Add(758.50, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 });
            mapPatterns.Add(758.75, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 });

            mapPatterns.Add(759.25, new int[] { 1, 2, 1, 1, 1, 3, 3, 0 });
            mapPatterns.Add(759.50, new int[] { 1, 2, 1, 1, 1, 0, 3, 0 });
            mapPatterns.Add(759.75, new int[] { 1, 2, 3, 1, 1, 0, 0, 0 });
            mapPatterns.Add(760.00, new int[] { 1, 2, 0, 1, 1, 0, 0, 0 });
            mapPatterns.Add(760.25, new int[] { 1, 2, 0, 1, 3, 0, 0, 0 });
            mapPatterns.Add(760.50, new int[] { 1, 2, 0, 1, 0, 0, 0, 0 });
            mapPatterns.Add(760.75, new int[] { 1, 3, 0, 1, 0, 0, 0, 0 });
            mapPatterns.Add(761.00, new int[] { 1, 0, 0, 1, 0, 0, 0, 0 });
            mapPatterns.Add(761.25, new int[] { 1, 0, 0, 3, 0, 0, 0, 0 });
            mapPatterns.Add(761.50, new int[] { 1, 0, 0, 3, 0, 0, 0, 0 });
            mapPatterns.Add(761.75, new int[] { 1, 0, 0, 0, 0, 0, 0, 0 });
            mapPatterns.Add(762.00, new int[] { 1, 0, 0, 0, 0, 0, 0, 0 });
            mapPatterns.Add(762.25, new int[] { 3, 0, 0, 0, 0, 0, 0, 0 });
            mapPatterns.Add(762.50, new int[] { 3, 0, 0, 0, 0, 0, 0, 0 });
            mapPatterns.Add(762.75, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 });
            mapPatterns.Add(763.00, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 });
            // more after this if needed

            return mapPatterns;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var frameMap = BuildPurifyFrameMap_Purify();
            var jsonLines = new List<string>();

            foreach (var kvp in frameMap)
            {
                try
                {
                    int timeSec = (int)kvp.Key;
                    int timeMSec = (int)Math.Floor(kvp.Key * 1000) % 1000;
                    TimeSpan timeOfFrame = new TimeSpan(0, 0, 0, timeSec, timeMSec);
                    await LoadFrame(timeOfFrame);                    

                    int[] MLClasses = kvp.Value;
                    for (int idx = 0; idx < MLClasses.Length; idx++)
                    {
                        float[] inputV = scannerPurify.ExtractActionSlotData(cachedBitmap, idx);
                        int outputV = (MLClasses[idx] < 0) ? 0 : MLClasses[idx];
                        string inDesc = string.Join(',', inputV);

                        var textLine = "{\"desc\":\"" + kvp.Key + "_" + idx + "\",\"output\":" + outputV + ",\"input\":[" + inDesc + "]}";
                        jsonLines.Add(textLine);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            StorageFile jsonFile = await storageFolder.CreateFileAsync("sino-ml-purify.json", CreationCollisionOption.OpenIfExists);

            string jsonDesc = "{\"dataset\":[\n" + string.Join(",\n", jsonLines) + "]}";
            await FileIO.WriteTextAsync(jsonFile, jsonDesc);

            Slider_ValueChanged(null, null);
        }
    }
}
