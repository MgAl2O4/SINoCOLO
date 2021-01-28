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

        public MainPage()
        {
            this.InitializeComponent();

            scanners.Add(new ScannerColoCombat());
            scanners.Add(new ScannerColoPurify());
            scanners.Add(new ScannerMessageBox());
            scanners.Add(new ScannerTitleScreen());
            scanners.Add(new ScannerCombat());
            scanners.Add(new ScannerPurify());
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

            previewImage.Width = frameWidth;
            previewImage.Height = frameHeight;
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

        private async void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
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
            await LoadFrame(timeOfFrame);
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
            int pixelW = _bitmap.PixelWidth;
            int pixelH = _bitmap.PixelHeight;
            byte[] usePixels = null;

            BitmapBounds cropBounds = new BitmapBounds() { X = 0, Y = 0, Width = 0, Height = 0 };
            if (pixelW == 462 && pixelH == 864)
            {
                cropBounds = new BitmapBounds() { X = 1, Y = 46, Width = 458, Height = 813 };
            }
            else if (pixelW == 440 && pixelH == 822)
            {
                cropBounds = new BitmapBounds() { X = 2, Y = 42, Width = 436, Height = 778 };
            }

            if (cropBounds.Width > 0)
            { 
                var scaledW = 338;
                var scaledH = 600;

                try
                {
                    // transform: crop
                    InMemoryRandomAccessStream streamCrop = new InMemoryRandomAccessStream();
                    {
                        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, streamCrop);
                        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)pixelW, (uint)pixelH, 96, 96, pixels.ToArray());
                        encoder.BitmapTransform.Bounds = cropBounds;
                        await encoder.FlushAsync();
                    }

                    InMemoryRandomAccessStream streamSize = new InMemoryRandomAccessStream();
                    {
                        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(streamCrop);
                        var pixelsProvider = await decoder.GetPixelDataAsync();
                        var inPixels = pixelsProvider.DetachPixelData();

                        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, streamSize);
                        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)cropBounds.Width, (uint)cropBounds.Height, 96, 96, inPixels);
                        encoder.BitmapTransform.ScaledHeight = (uint)scaledH;
                        encoder.BitmapTransform.ScaledWidth = (uint)scaledW;
                        encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;
                        await encoder.FlushAsync();
                    }

                    {
                        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(streamSize);
                        var pixelsProvider = await decoder.GetPixelDataAsync();
                        pixelW = (int)scaledW;
                        pixelH = (int)scaledH;
                        usePixels = pixelsProvider.DetachPixelData();
                    }

                    streamCrop.Dispose();
                    streamSize.Dispose();
                }
                catch (Exception ex)
                {
                }
            }

            try
            {
                byte[] pixelData = (usePixels != null) ? usePixels : pixels.ToArray();
                var analyzeBitmap = ScreenshotUtilities.ConvertToFastBitmap(pixelData, pixelW, pixelH);

                foreach (var scanner in scanners)
                {
                    object resultOb = scanner.Process(analyzeBitmap);
                    if (resultOb != null)
                    {
                        textScanResults.Text = scanner.ScannerName + " found!\n\n" + resultOb;
                        break;
                    }
                }

                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                StorageFile exportFile = await storageFolder.CreateFileAsync("image-test.jpg", CreationCollisionOption.ReplaceExisting);

                using (IRandomAccessStream stream = await exportFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                    byte[] bytes = pixels.ToArray();
                    encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                            BitmapAlphaMode.Ignore,
                                            (uint)pixelW,
                                            (uint)pixelH,
                                            200,
                                            200,
                                            pixelData);

                    await encoder.FlushAsync();
                }
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

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await ExportFramesPurify();
        }

        private async Task ExportFramesPurify()
        { 
            var purifyTime = new List<Tuple<float, float>>();
            purifyTime.Add(new Tuple<float, float>(73.0f, 92.5f));
            purifyTime.Add(new Tuple<float, float>(157.25f, 180.25f));
            purifyTime.Add(new Tuple<float, float>(249.25f, 278.5f));

            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            int imgIdx = 0;

            foreach (var time2 in purifyTime)
            {
                try
                {
                    float fromTime = time2.Item1;
                    float toTime = time2.Item2;

                    for (float itTime = fromTime; itTime <= toTime; itTime += 0.25f)
                    {
                        int timeSec = (int)itTime;
                        int timeMSec = (int)Math.Floor(itTime * 1000) % 1000;
                        TimeSpan timeOfFrame = new TimeSpan(0, 0, 0, timeSec, timeMSec);
                        await LoadFrame(timeOfFrame);

                        StorageFile exportFile = await storageFolder.CreateFileAsync("purify-" + imgIdx + ".jpg", CreationCollisionOption.ReplaceExisting);
                        imgIdx++;

                        try
                        {
                            var _bitmap = new RenderTargetBitmap();
                            await _bitmap.RenderAsync(previewImage);

                            var pixels = await _bitmap.GetPixelsAsync();
                            using (IRandomAccessStream stream = await exportFile.OpenAsync(FileAccessMode.ReadWrite))
                            {
                                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
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
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            Slider_ValueChanged(null, null);
        }
    }
}
