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
        private string movieFileName;

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

            movieFileName = Path.GetFileNameWithoutExtension(pickedFile.Path);
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
            // await ExportFramesPurify();
            await ExportFramesStats();
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

        private void ExportFramesStats_0127(List<Tuple<float, int[]>> exportConfig)
        {
            exportConfig.Add(new Tuple<float, int[]>(22.5f, new int[] {
                0, 0, 0, 0,
                0, 0, 0, -100,
                -100, 0, 1, 0,
                0, 0, -1, 0,
                0, -2, -1, 0,

                0, -1, 0, -2,
                0, -1, 1, -1,
                0, 0, 0, 1,
                0, 100, 0, 0,
                0, 0, 0, -2 }));
            exportConfig.Add(new Tuple<float, int[]>(24.25f, new int[] {
                -100, 0, -100, 0,
                0, 0, 0, -100,
                -100, 0, 1, 0,
                0, 0, -1, 0,
                -100, -2, -1, 0,

                0, -1, 0, -3,
                0, 100, 1, -1,
                0, 0, 0, 1,
                0, 100, 0, -100,
                100, 0, -100, -2 }));
            exportConfig.Add(new Tuple<float, int[]>(25.25f, new int[] {
                -2, 0, -2, 0,
                0, 0, 0, -1,
                -1, 0, 1, 0,
                0, 0, -1, 0,
                -1, -2, -1, 0,

                0, -1, 0, -3,
                0, 1, 1, -1,
                0, 0, 0, 1,
                0, 1, 0, -1,
                1, 0, -1, -2 }));
            exportConfig.Add(new Tuple<float, int[]>(25.5f, new int[] {
                -2, 0, -2, 0,
                0, 0, 0, -1,
                -1, 0, 1, 0,
                0, 0, -1, 0,
                -1, -2, -1, 0,

                0, -1, 0, -3,
                0, 1, 1, -1,
                0, 0, 0, 1,
                0, 1, 0, -1,
                1, 0, -1, -2 }));
            exportConfig.Add(new Tuple<float, int[]>(27.75f, new int[] {
                -1, -100, -1, -100,
                0, -100, 100, -1,
                -1, 0, 1, 0,
                0, 0, -1, -100,
                -1, -2, 100, 0,

                0, -1, 0, -3,
                0, 1, 1, -1,
                -100, 0, 0, 1,
                100, 1, 0, -1,
                1, 0, -1, -2 }));
            exportConfig.Add(new Tuple<float, int[]>(28.5f, new int[] {
                -1, -100, -1, -100,
                0, -100, 100, -1,
                -1, 0, 1, 0,
                0, 0, -1, -100,
                100, -3, 100, -100,

                0, -1, 0, -3,
                0, 1, 1, -1,
                -100, 0, 0, 1,
                100, 1, 0, -1,
                1, 0, -1, -2 }));
            exportConfig.Add(new Tuple<float, int[]>(29.0f, new int[] {
                -1, -3, -1, -2,
                0, -1, 1, -1,
                -1, 0, 1, 0,
                0, 0, -1, -2,
                1, -3, 2, -2,

                0, -1, 0, -3,
                0, 1, 1, -200,
                -1, 0, 0, 1,
                1, 1, 0, -1,
                1, 0, -1, -2 }));
            exportConfig.Add(new Tuple<float, int[]>(29.5f, new int[] {
                -1, -3, -1, -2,
                0, -1, 1, -1,
                -1, 0, 1, 0,
                0, 0, -1, -2,
                1, -3, 2, -2,

                0, -1, 0, -3,
                0, 1, 1, -1,
                -1, 0, 0, 1,
                1, 1, 0, -1,
                1, 0, -1, -2 }));
            exportConfig.Add(new Tuple<float, int[]>(30.75f, new int[] {
                -100, -100, -100, -100,
                0, -100, 100, -100,
                -100, -100, 100, -100,
                0, 0, -100, -100,
                100, -100, 100, -100,

                0, -100, 0, -100,
                0, 100, 100, -100,
                -100, 0, 0, 100,
                100, 100, 0, -100,
                100, 0, -100, -100 }));
            exportConfig.Add(new Tuple<float, int[]>(31.75f, new int[] {
                -2, -3, -2, -2,
                0, -1, 1, -1,
                -1, -100, 2, -100,
                -100, 0, -1, -2,
                1, -3, 2, -2,

                0, -1, 0, -3,
                100, 1, 1, -1,
                -1, 0, 0, 1,
                1, 1, 0, -1,
                1, 100, -1, -2 }));
            exportConfig.Add(new Tuple<float, int[]>(35.5f, new int[] {
                -2, -5, -2, -2,
                0, -1, 1, -1,
                -1, -1, 2, -3,
                -1, 0, -1, -2,
                1, -3, 2, -2,

                -100, -1, -100, -3,
                1, 2, 1, 100,
                -1, -100, 0, 1,
                1, 1, 100, -1,
                1, 1, -1, -2 }));
            exportConfig.Add(new Tuple<float, int[]>(36.0f, new int[] {
                -2, -5, -2, -2,
                0, -2, 1, -2,
                -1, -1, 2, -3,
                -1, 0, -1, -2,
                1, -3, 2, -2,

                -2, -1, -2, -3,
                1, 2, 1, 1,
                -1, -1, 0, 1,
                1, -100, 1, -1,
                1, 1, -1, -2 }));
            exportConfig.Add(new Tuple<float, int[]>(39.0f, new int[] {
                -2, -5, -2, -2,
                0, -2, 1, -2,
                -1, -1, 2, -3,
                -1, 0, -1, -2,
                2, -3, 3, -2,

                -2, -1, -2, -3,
                1, 3, 1, 1,
                -1, -1, 0, 1,
                1, -100, 1, -1,
                2, 1, -1, -2 }));
            exportConfig.Add(new Tuple<float, int[]>(53.5f, new int[] {
                -2, -5, -2, -2,
                -2, -1, -1, -1,
                1, -2, 3, -3,
                -1, -1, -1, -3,
                1, -3, 2, -2,

                -1, -1, -2, -2,
                1, 3, 2, 1,
                -3, -2, -2, -1,
                1, -4, 2, -1,
                2, 2, 1, -2 }));
            exportConfig.Add(new Tuple<float, int[]>(64.5f, new int[] {
                -2, -6, -2, -3,
                -2, -2, 1, -1,
                1, -2, 4, -4,
                -1, -3, -2, -4,
                1, -4, 3, -4,

                -2, 3, 2, -1,
                1, 3, 2, 1,
                -3, -2, -3, 1,
                -1, -4, 2, -2,
                0, 0, 2, 1 }));
            exportConfig.Add(new Tuple<float, int[]>(67.0f, new int[] {
                -3, -6, -2, -3,
                -2, -2, 1, -1,
                1, -2, 4, -4,
                -1, -3, -2, -4,
                1, -4, 3, -4,

                -2, 3, -200, -200,
                1, 3, 2, 1,
                -3, -2, -2, 1,
                -2, -4, 1, -2,
                -100, -100, 2, 1 }));
            exportConfig.Add(new Tuple<float, int[]>(68.0f, new int[] {
                -3, -6, -2, -3,
                -2, -2, 1, -1,
                1, -2, 4, -4,
                -1, -3, -2, -4,
                1, -4, 3, -4,

                -2, 3, -2, -1,
                1, 3, 2, 1,
                -3, -2, -2, 1,
                -2, -4, 1, -2,
                -3, -4, 2, 1 }));
            exportConfig.Add(new Tuple<float, int[]>(74.0f, new int[] {
                -3, -6, -3, -3,
                -2, -2, 1, -2,
                1, -2, 4, -6,
                -1, -3, -2, -4,
                2, -4, 3, -4,

                -2, 3, -1, -1,
                1, 3, 2, 1,
                -3, -2, -2, 1,
                -2, -4, 2, -2,
                -2, -4, 2, 2 }));
            exportConfig.Add(new Tuple<float, int[]>(77.75f, new int[] {
                -3, -6, -2, -3,
                -2, -2, 1, -2,
                1, -1, 4, -6,
                -1, -3, -2, -4,
                2, -4, 3, -4,

                -2, 9, -1, 100,
                1, 9, 2, 7,
                -3, 100, -2, 7,
                -2, 100, 3, 100,
                -2, 100, 3, 8 }));
            exportConfig.Add(new Tuple<float, int[]>(78.25f, new int[] {
                -3, -6, -2, -3,
                -2, -2, 1, -2,
                1, -1, 4, -6,
                -1, -3, -2, -4,
                2, -4, 3, -4,

                -2, 9, -1, 6,
                1, 9, 2, 7,
                -3, 5, -2, 7,
                -2, 3, 3, 5,
                -2, 3, 3, 8 }));
            exportConfig.Add(new Tuple<float, int[]>(82.0f, new int[] {
                -3, -6, -2, -3,
                -2, -2, 1, -2,
                1, -1, 4, -6,
                -1, -3, -2, -4,
                2, -5, 3, -4,

                3, 2, 2, -3,
                1, 9, 2, 7,
                -3, 5, -2, 7,
                -2, 3, 3, 4,
                -2, 3, 3, 8 }));
            exportConfig.Add(new Tuple<float, int[]>(112.75f, new int[] {
                -3, -7, -2, -6,
                -2, -2, 2, -2,
                2, -2, 4, -5,
                -1, -3, 1, -4,
                4, -5, 3, -4,

                2, 2, 1, -5,
                -2, 8, 3, 7,
                -2, 3, -1, 7,
                -1, 3, 3, 4,
                -1, 3, 5, 9 }));
            exportConfig.Add(new Tuple<float, int[]>(119.0f, new int[] {
                -3, -7, -2, -6,
                -2, -2, 2, -2,
                -1, -2, 4, -5,
                -1, 1, 1, -3,
                5, -5, 3, -4,

                1, 2, -100, -5,
                -2, 8, 3, 7,
                -3, 3, -1, 7,
                -2, 2, 2, 2,
                -1, 2, 5, 8 }));
            exportConfig.Add(new Tuple<float, int[]>(327.0f, new int[] {
                -3, -4, -3, -3,
                -1, -5, 2, -2,
                -1, -4, 1, -4,
                1, -3, 2, -4,
                3, 1, 8, -1,

                -3, -7, 2, -11,
                6, -1, 10, -6,
                2, -7, 5, -7,
                -2, -3, 8, -1,
                1, -5, 10, -3 }));
            exportConfig.Add(new Tuple<float, int[]>(327.5f, new int[] {
                -3, -5, -3, -4,
                -1, -5, 2, -2,
                -1, -4, 1, -4,
                1, -3, 2, -4,
                3, 1, 8, -1,

                -3, -7, 2, -11,
                6, -1, 10, -6,
                2, -7, 5, -7,
                -2, -3, 8, -1,
                1, -5, 10, -3 }));
            exportConfig.Add(new Tuple<float, int[]>(329.75f, new int[] {
                -3, -5, -3, -4,
                -1, -6, 2, -3,
                -1, -4, 1, -4,
                1, -3, 2, -4,
                3, 1, 8, -4,

                -3, -7, 2, -10,
                6, -1, 10, -6,
                2, -7, 5, -7,
                -2, -3, 8, -2,
                1, -5, 10, -3 }));
            exportConfig.Add(new Tuple<float, int[]>(336.5f, new int[] {
                -3, -5, -2, -4,
                -1, -6, 2, -3,
                -1, -5, 1, -6,
                1, -3, 2, -4,
                3, 1, 8, -3,

                4, -8, 2, -11,
                6, -2, 10, -7,
                2, -7, 5, -7,
                -3, -3, 9, -2,
                2, -4, 11, -3 }));
            exportConfig.Add(new Tuple<float, int[]>(350.75f, new int[] {
                2, -6, 3, -4,
                -4, -6, 5, -3,
                4, -5, 5, -6,
                1, -3, 2, -4,
                6, 1, 11, -5,

                3, -8, 3, -11,
                6, -3, 11, -8,
                2, -7, 5, -7,
                -3, -2, 8, -3,
                1, -3, 11, -2 }));
            exportConfig.Add(new Tuple<float, int[]>(424.5f, new int[] {
                2, -2, 3, -1,
                4, -5, 6, -2,
                6, -6, 7, 1,
                1, 4, 2, -4,
                11, -4, 11, -1,

                2, -8, 1, -11,
                4, -5, 11, -8,
                2, -5, 4, -6,
                -3, 1, 10, -3,
                1, -4, 12, -2 }));
            exportConfig.Add(new Tuple<float, int[]>(425.5f, new int[] {
                2, -1, 3, 100,
                4, -5, 6, -1,
                6, -6, 8, 1,
                1, 4, 2, -4,
                11, -4, 11, -1,

                2, -8, 1, -11,
                4, -5, 11, -8,
                2, -6, 4, -6,
                -3, 1, 10, -3,
                1, -4, 12, -2 }));
            exportConfig.Add(new Tuple<float, int[]>(537.5f, new int[] {
                0, 1, 0, 1,
                0, 0, 1, 0,
                -1, 0, 4, 0,
                1, 4, 0, 0,
                2, 0, 2, 0,

                2, -8, 1, -9,
                4, -7, 9, -8,
                1, -7, 3, -6,
                -2, 4, 10, 1,
                -1, -4, 13, -1 }));
            exportConfig.Add(new Tuple<float, int[]>(539.25f, new int[] {
                0, 1, 0, 1,
                0, 0, 1, 0,
                -1, -1, 4, -1,
                1, 4, 0, 0,
                3, 0, 3, 0,

                2, -8, 1, -9,
                4, -7, 10, -8,
                1, -7, 3, -6,
                -2, 4, 9, 1,
                -1, -4, 13, -1 }));
            exportConfig.Add(new Tuple<float, int[]>(543.75f, new int[] {
                1, -3, 1, -1,
                0, 0, 1, 0,
                -1, -1, 4, -3,
                1, 4, -100, 100,
                3, 0, 3, 0,

                3, -8, 3, -10,
                4, -6, 10, -8,
                1, -7, 3, -6,
                -2, 4, 9, 1,
                1, -4, 13, -1 }));
            exportConfig.Add(new Tuple<float, int[]>(627.75f, new int[] {
                0, 0, 0, 0,
                0, 0, 1, 0,
                2, 0, 3, 0,
                1, 4, 0, 0,
                3, 1, 4, 0,

                6, -7, 11, -11,
                4, -6, 13, -8,
                1, -7, 9, -7,
                -3, 5, 11, -1,
                2, -3, 16, -1 }));
            exportConfig.Add(new Tuple<float, int[]>(645.25f, new int[] {
                0, -3, -1, 0,
                -1, 0, -1, 1,
                1, -2, 2, -1,
                1, 5, 1, 5,
                5, 1, 5, -2,

                4, -7, 9, -11,
                3, -6, 12, -8,
                -1, -7, 8, -7,
                -3, 5, 11, 1,
                4, -3, 17, -1 }));
            exportConfig.Add(new Tuple<float, int[]>(645.75f, new int[] {
                0, -3, -1, 0,
                -1, 0, -1, 1,
                1, -2, 2, -1,
                1, 5, 1, 5,
                5, 1, 5, -2,

                4, -7, 9, -11,
                3, -6, 12, -8,
                -1, -7, 8, -7,
                -3, 5, 11, 1,
                4, -3, 17, -1 }));
            exportConfig.Add(new Tuple<float, int[]>(647.5f, new int[] {
                0, -3, -1, 0,
                -1, 0, 100, 1,
                1, -2, 2, -1,
                1, 5, 1, 5,
                5, 1, 5, -2,

                5, -7, 9, -11,
                4, -6, 13, -8,
                -1, -7, 8, -7,
                -4, 5, 11, 1,
                4, -3, 17, -1 }));
            exportConfig.Add(new Tuple<float, int[]>(649.25f, new int[] {
                0, -2, -1, 0,
                -1, 0, 1, 1,
                1, -2, 2, -3,
                1, 5, -1, 5,
                5, 1, 5, -2,

                5, -6, 9, -10,
                4, -6, 13, -8,
                -1, -7, 8, -7,
                -4, 6, 11, 1,
                6, -3, 18, -2 }));
            exportConfig.Add(new Tuple<float, int[]>(649.75f, new int[] {
                0, -2, -1, 0,
                -1, 0, 1, 1,
                1, -2, 2, -3,
                1, 5, -1, 5,
                5, 1, 5, -2,

                5, -6, 9, -10,
                4, -6, 13, -8,
                -1, -7, 8, -7,
                -4, 6, 11, 1,
                6, -3, 18, -2 }));
            exportConfig.Add(new Tuple<float, int[]>(663.75f, new int[] {
                -1, -2, 2, 0,
                -1, -2, 1, 1,
                1, -2, 2, -3,
                3, 1, 3, 3,
                6, 1, 6, -2,

                4, -6, 9, -9,
                4, -5, 13, -8,
                -1, -7, 7, -7,
                -4, 3, 12, 1,
                6, -5, 18, -1 }));
            exportConfig.Add(new Tuple<float, int[]>(665.5f, new int[] {
                -1, -2, 2, 0,
                -1, -2, 1, -100,
                -100, -2, 2, -4,
                3, 1, 3, 3,
                6, 1, 6, -2,

                4, -6, 9, -9,
                4, -5, 13, -8,
                -1, -7, 8, -7,
                -4, 3, 12, 1,
                6, -5, 18, -1 }));
            exportConfig.Add(new Tuple<float, int[]>(673.0f, new int[] {
                1, -2, 2, -1,
                -1, -2, 1, -1,
                -2, -2, 2, -4,
                3, 1, 3, 3,
                8, 1, 6, -2,

                4, -8, 9, -9,
                4, -5, 13, -9,
                -1, -7, 8, -8,
                -4, 3, 12, 1,
                6, -3, 18, 1 }));
            exportConfig.Add(new Tuple<float, int[]>(764.0f, new int[] {
                1, -3, 1, -100,
                0, -1, 1, 0,
                0, 0, 3, 1,
                3, 1, 3, 3,
                10, 3, 9, 1,

                3, -7, 9, -10,
                4, -4, 13, -9,
                2, -7, 9, -8,
                -2, 3, 12, 1,
                3, -3, 16, 1 }));
            exportConfig.Add(new Tuple<float, int[]>(775.75f, new int[] {
                -1, -3, 1, -2,
                0, 1, 1, 1,
                1, -3, 3, -2,
                2, -3, 2, -3,
                10, 4, 9, 2,

                2, -7, 8, -9,
                2, -2, 13, -8,
                2, -8, 9, -8,
                -2, 4, 12, 1,
                3, -3, 17, 1 }));
            exportConfig.Add(new Tuple<float, int[]>(784.5f, new int[] {
                -1, -4, -1, 1,
                -1, 1, 1, 1,
                1, -3, 3, 1,
                2, -3, 2, -3,
                11, 1, 10, 1,

                2, -7, 8, -10,
                2, -2, 13, -8,
                2, -8, 9, -8,
                -2, 4, 12, -1,
                3, -3, 16, 1 }));

            // TODO: moar!
        }

        private async Task ExportFramesStats()
        {
            var exportConfig = new List<Tuple<float, int[]>>();
            var dataPrefix = "stat";

            if (movieFileName == "2021-01-27 17-59-44")
            {
                dataPrefix = "stat0127";
                ExportFramesStats_0127(exportConfig);
            }

            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            int imgIdx = 0;

            var textData = new List<string>();
            var prefix = "            ";

            foreach (var item in exportConfig)
            {
                try
                {
                    int timeSec = (int)item.Item1;
                    int timeMSec = (int)Math.Floor(item.Item1 * 1000) % 1000;
                    TimeSpan timeOfFrame = new TimeSpan(0, 0, 0, timeSec, timeMSec);
                    await LoadFrame(timeOfFrame);

                    var fileName = string.Format("{0}-{1}.jpg", dataPrefix, imgIdx);
                    StorageFile exportFile = await storageFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                    imgIdx++;

                    textData.Add(string.Format("{0}fileList.Add(new StatML(\"{1}\",", prefix, fileName));

                    var statDesc = "";
                    for (int idxS = 0; idxS < 20; idxS += 4)
                    {
                        if (idxS > 0) { statDesc += ", "; }
                        statDesc += string.Format("{{ {0}, {1}, {2}, {3} }}", item.Item2[idxS], item.Item2[idxS + 1], item.Item2[idxS + 2], item.Item2[idxS + 3]);
                    }
                    textData.Add(string.Format("{0}    new int[5, 4]{{ {1} }},", prefix, statDesc));

                    statDesc = "";
                    for (int idxS = 20; idxS < 40; idxS += 4)
                    {
                        if (idxS > 20) { statDesc += ", "; }
                        statDesc += string.Format("{{ {0}, {1}, {2}, {3} }}", item.Item2[idxS], item.Item2[idxS + 1], item.Item2[idxS + 2], item.Item2[idxS + 3]);
                    }
                    textData.Add(string.Format("{0}    new int[5, 4]{{ {1} }}));", prefix, statDesc));

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
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            {
                StorageFile textExportFile = await storageFolder.CreateFileAsync("textData-" + dataPrefix + ".txt", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteLinesAsync(textExportFile, textData);
            }

            Slider_ValueChanged(null, null);
        }
    }
}
