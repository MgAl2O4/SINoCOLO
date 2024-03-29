﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace SINoVision
{
    public struct FastPixelHSV
    {
        private byte HuePack;
        private byte SaturationPack;
        public byte Value;
        public byte Monochrome;

        public byte RawR;
        public byte RawG;
        public byte RawB;
        public byte HasHSL;

        public FastPixelHSV(byte colorR, byte colorG, byte colorB)
        {
            HuePack = 0;
            SaturationPack = 0;
            Value = 0;
            Monochrome = 0;
            RawR = colorR;
            RawG = colorG;
            RawB = colorB;
            HasHSL = 0;
        }

        public FastPixelHSV(bool patternMatch)
        {
            HuePack = 0;
            SaturationPack = 0;
            Value = 0;
            Monochrome = patternMatch ? (byte)255 : (byte)0;
            RawR = Monochrome;
            RawG = Monochrome;
            RawB = Monochrome;
            HasHSL = 1;
        }

        public int GetHue()
        {
            return ((int)(SaturationPack & 0x80) << 1) | HuePack;
        }

        public int GetSaturation()
        {
            return SaturationPack & 0x7f;
        }

        public int GetValue()
        {
            return Value;
        }

        public int GetMonochrome()
        {
            return Monochrome;
        }

        public void SetHSV(int hue, int saturation, int value)
        {
            HuePack = (byte)(hue & 0xff);
            SaturationPack = (byte)((saturation & 0x7f) | ((hue & 0x100) >> 1));
            Value = (byte)value;

            ScreenshotUtilities.HsvToRgb(hue, saturation, value, out int colorR, out int colorG, out int colorB);
            Monochrome = (byte)Math.Round((0.2125 * colorR) + (0.7154 * colorG) + (0.0721 * colorB));
            RawR = (byte)colorR;
            RawG = (byte)colorG;
            RawB = (byte)colorB;
            HasHSL = 1;
        }

        public void ExpandHSV()
        {
            float LinearR = (RawR / 255f);
            float LinearG = (RawG / 255f);
            float LinearB = (RawB / 255f);

            float MinRG = (LinearR < LinearG) ? LinearR : LinearG;
            float MaxRG = (LinearR > LinearG) ? LinearR : LinearG;
            float MinV = (MinRG < LinearB) ? MinRG : LinearB;
            float MaxV = (MaxRG > LinearB) ? MaxRG : LinearB;
            float DeltaV = MaxV - MinV;

            float H = 0;
            float S = 0;
            float L = (float)((MaxV + MinV) / 2.0f);

            if (DeltaV != 0)
            {
                if (L < 0.5f)
                {
                    S = (float)(DeltaV / (MaxV + MinV));
                }
                else
                {
                    S = (float)(DeltaV / (2.0f - MaxV - MinV));
                }

                if (LinearR == MaxV)
                {
                    H = (LinearG - LinearB) / DeltaV;
                }
                else if (LinearG == MaxV)
                {
                    H = 2f + (LinearB - LinearR) / DeltaV;
                }
                else if (LinearB == MaxV)
                {
                    H = 4f + (LinearR - LinearG) / DeltaV;
                }
            }

            int HueV = (int)(H * 60f);
            if (HueV < 0) HueV += 360;
            int SaturationV = (int)(S * 100f);
            int LightV = (int)(L * 100f);

            HuePack = (byte)(HueV & 0xff);
            SaturationPack = (byte)((SaturationV & 0x7f) | ((HueV & 0x100) >> 1));
            Value = (byte)LightV;
            Monochrome = (byte)((0.2125 * RawR) + (0.7154 * RawG) + (0.0721 * RawB));
            HasHSL = 1;
        }

        public override string ToString()
        {
            return "H:" + GetHue() + ", S:" + GetSaturation() + ", V:" + GetValue() + ", M:" + GetMonochrome();
        }
    };

    public abstract class FastPixelMatch
    {
        public abstract bool IsMatching(FastPixelHSV pixel);
    }

    public class FastPixelMatchHSV : FastPixelMatch
    {
        private short HueMin;
        private short HueMax;
        private byte SaturationMin;
        private byte SaturationMax;
        private byte ValueMin;
        private byte ValueMax;

        public FastPixelMatchHSV(short hueMin, short hueMax, byte saturationMin, byte saturationMax, byte valueMin, byte valueMax)
        {
            HueMin = hueMin;
            HueMax = hueMax;
            SaturationMin = saturationMin;
            SaturationMax = saturationMax;
            ValueMin = valueMin;
            ValueMax = valueMax;
        }

        public override bool IsMatching(FastPixelHSV pixel)
        {
            int Saturation = pixel.GetSaturation();
            if ((Saturation < SaturationMin) || (Saturation > SaturationMax) ||
                 (pixel.Value < ValueMin) || (pixel.Value > ValueMax))
            {
                return false;
            }

            int Hue = pixel.GetHue();
            if (HueMin < 0 && Hue > 200) { Hue -= 360; }

            return (Hue >= HueMin) && (Hue <= HueMax);
        }

        public override string ToString()
        {
            return "Hue:" + HueMin + ".." + HueMax +
                ", Saturation:" + SaturationMin + ".." + SaturationMax +
                ", Value:" + ValueMin + ".." + ValueMax;
        }
    }

    public class FastPixelMatchMono : FastPixelMatch
    {
        private byte MonoMin;
        private byte MonoMax;

        public FastPixelMatchMono(byte monoMin, byte monoMax)
        {
            MonoMin = monoMin;
            MonoMax = monoMax;
        }

        public override bool IsMatching(FastPixelHSV pixel)
        {
            return (pixel.Monochrome >= MonoMin) && (pixel.Monochrome <= MonoMax);
        }

        public override string ToString()
        {
            return "Mono:" + MonoMin + ".." + MonoMax;
        }
    }

    public class FastPixelMatchHueMono : FastPixelMatch
    {
        private short HueMin;
        private short HueMax;
        private byte MonoMin;
        private byte MonoMax;

        public FastPixelMatchHueMono(short hueMin, short hueMax, byte monoMin, byte monoMax)
        {
            HueMin = hueMin;
            HueMax = hueMax;
            MonoMin = monoMin;
            MonoMax = monoMax;
        }

        public override bool IsMatching(FastPixelHSV pixel)
        {
            if ((pixel.Monochrome >= MonoMin) && (pixel.Monochrome <= MonoMax))
            {
                int Hue = pixel.GetHue();
                return (Hue >= HueMin) && (Hue <= HueMax);
            }

            return false;
        }

        public override string ToString()
        {
            return "Hue:" + HueMin + ".." + HueMax + ", Mono:" + MonoMin + ".." + MonoMax;
        }
    }

    public class FastBitmapHSV
    {
        public FastPixelHSV[] Pixels;
        public int Width;
        public int Height;

        public FastBitmapHSV(int width, int height)
        {
            Width = width;
            Height = height;
            Pixels = new FastPixelHSV[width * height];
        }

        public FastPixelHSV GetPixel(int X, int Y)
        {
            int idx = X + (Y * Width);
            if (Pixels[idx].HasHSL == 0)
            {
                Pixels[idx].ExpandHSV();
            }

            return Pixels[idx];
        }

        public void SetPixel(int X, int Y, FastPixelHSV pixel)
        {
            Pixels[X + (Y * Width)] = pixel;
        }

        public void SetPixel(int Idx, FastPixelHSV pixel)
        {
            Pixels[Idx] = pixel;
        }

        public override string ToString()
        {
            return "FastBitmap " + Width + "x" + Height;
        }
    }

    public class ScreenshotUtilities
    {
        public static FastBitmapHSV ConvertToFastBitmap(Bitmap image, int forcedWidth = -1, int forcedHeight = -1)
        {
            int useWidth = (forcedWidth > 0) ? forcedWidth : image.Width;
            int useHeight = (forcedHeight > 0) ? forcedHeight : image.Height;
            FastBitmapHSV result = new FastBitmapHSV(useWidth, useHeight);

            unsafe
            {
                BitmapData bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
                int bytesPerPixel = Image.GetPixelFormatSize(image.PixelFormat) / 8;
                int bytesPerRow = Math.Min(bitmapData.Width, result.Width) * bytesPerPixel;
                int convertHeight = Math.Min(bitmapData.Height, result.Height);

                for (int IdxY = 0; IdxY < convertHeight; IdxY++)
                {
                    byte* pixels = (byte*)bitmapData.Scan0 + (IdxY * bitmapData.Stride);
                    int IdxPixel = IdxY * result.Width;
                    for (int IdxByte = 0; IdxByte < bytesPerRow; IdxByte += bytesPerPixel)
                    {
                        result.Pixels[IdxPixel] = new FastPixelHSV(pixels[IdxByte + 2], pixels[IdxByte + 1], pixels[IdxByte]);
                        IdxPixel++;
                    }
                }

                image.UnlockBits(bitmapData);
            }

            return result;
        }

        public static FastBitmapHSV ConvertToFastBitmap(byte[] PixelsBGRA, int width, int height)
        {
            FastBitmapHSV result = new FastBitmapHSV(width, height);

            int numPx = width * height;
            for (int IdxPx = 0; IdxPx < numPx; IdxPx++)
            {
                int IdxByte = IdxPx * 4;
                result.Pixels[IdxPx] = new FastPixelHSV(PixelsBGRA[IdxByte + 2], PixelsBGRA[IdxByte + 1], PixelsBGRA[IdxByte]);
            }

            return result;
        }

        public static Bitmap CreateBitmapWithShapes(FastBitmapHSV bitmap, List<Rectangle> listBounds)
        {
            Bitmap bmp = new Bitmap(bitmap.Width, bitmap.Height);
            unsafe
            {
                BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);
                int bytesPerPixel = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
                int bytesPerRow = bitmapData.Width * bytesPerPixel;

                for (int IdxY = 0; IdxY < bmp.Height; IdxY++)
                {
                    byte* pixels = (byte*)bitmapData.Scan0 + (IdxY * bitmapData.Stride);
                    for (int IdxByte = 0; IdxByte < bytesPerRow; IdxByte += bytesPerPixel)
                    {
                        FastPixelHSV writePx = bitmap.GetPixel(IdxByte / bytesPerPixel, IdxY);
                        Color writeColor = Color.FromArgb(writePx.Monochrome, writePx.Monochrome, writePx.Monochrome);
                        //Color writeColor = GetColorFromHSV(writePx);
                        pixels[IdxByte + 3] = writeColor.A;
                        pixels[IdxByte + 2] = writeColor.R;
                        pixels[IdxByte + 1] = writeColor.G;
                        pixels[IdxByte + 0] = writeColor.B;
                    }
                }

                bmp.UnlockBits(bitmapData);
            }

            using (Graphics gBmp = Graphics.FromImage(bmp))
            {
                if (listBounds.Count > 0)
                {
                    Pen boundsPen = new Pen(Color.Cyan);
                    gBmp.DrawRectangles(boundsPen, listBounds.ToArray());
                }
            }

            return bmp;
        }

        public static void SaveBitmapWithShapes(FastBitmapHSV bitmap, List<Rectangle> listBounds, string fileName)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            using (Bitmap bmp = CreateBitmapWithShapes(bitmap, listBounds))
            {
                bmp.Save(fileName, ImageFormat.Png);
            }
        }

        public static Color GetColorFromHSV(FastPixelHSV pixel)
        {
            int R = 0, G = 0, B = 0;
            HsvToRgb(pixel.GetHue(), pixel.GetSaturation() / 100.0, pixel.GetValue() / 100.0, out R, out G, out B);
            return Color.FromArgb(R, G, B);
        }

        public static void HsvToRgb(double h, double S, double V, out int r, out int g, out int b)
        {
            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            r = Math.Max(0, Math.Min(255, ((int)(R * 255.0))));
            g = Math.Max(0, Math.Min(255, ((int)(G * 255.0))));
            b = Math.Max(0, Math.Min(255, ((int)(B * 255.0))));
        }

        public static List<int> TraceLineSegments(FastBitmapHSV bitmap, int posX, int posY, int incX, int incY, int traceLen,
            FastPixelMatch colorMatch, int minSegSize, int segLimit, bool bDebugMode = false)
        {
            FastPixelHSV[] streakBuffer = new FastPixelHSV[minSegSize];
            int bufferIdx = 0;
            for (int Idx = 0; Idx < streakBuffer.Length; Idx++) { streakBuffer[Idx] = new FastPixelHSV(255, 255, 255); }

            List<int> result = new List<int>();
            bool bWasMatch = false;

            if (bDebugMode) { Console.WriteLine("TraceLineSegments [" + posX + ", " + posY + "] -> [" + (posX + (incX * traceLen)) + ", " + (posY + (incY * traceLen)) + "]"); }

            for (int stepIdx = 0; stepIdx < traceLen; stepIdx++)
            {
                int scanX = posX + (stepIdx * incX);
                int scanY = posY + (stepIdx * incY);
                FastPixelHSV testPx = bitmap.GetPixel(scanX, scanY);

                streakBuffer[bufferIdx] = testPx;
                bufferIdx = (bufferIdx + 1) % minSegSize;

                bool bBufferMatching = true;
                for (int Idx = 0; Idx < streakBuffer.Length; Idx++)
                {
                    bBufferMatching = bBufferMatching && colorMatch.IsMatching(streakBuffer[Idx]);
                }

                if (bDebugMode) { Console.WriteLine("  [" + scanX + ", " + scanY + "] " + testPx + " => match:" + colorMatch.IsMatching(testPx) + ", buffer:" + bBufferMatching); }

                if (bBufferMatching != bWasMatch)
                {
                    bWasMatch = bBufferMatching;

                    int segPos = bBufferMatching ?
                        (incX != 0) ? (scanX - (incX * minSegSize)) : (scanY - (incY * minSegSize)) :
                        (incX != 0) ? scanX : scanY;

                    result.Add(segPos);
                    if (bDebugMode) { Console.WriteLine("  >> mark segment:" + segPos); }

                    if (result.Count >= segLimit && segLimit > 0)
                    {
                        break;
                    }
                }
            }

            return result;
        }

        public static bool TraceLine(FastBitmapHSV bitmap, int posX, int posY, int incX, int incY, int traceLen, FastPixelMatch colorMatch, out Point posHit, bool bDebugMode = false)
        {
            if (bDebugMode) { Console.WriteLine("TraceLine [" + posX + ", " + posY + "] -> [" + (posX + (incX * traceLen)) + ", " + (posY + (incY * traceLen)) + "]"); }

            for (int stepIdx = 0; stepIdx < traceLen; stepIdx++)
            {
                int scanX = posX + (stepIdx * incX);
                int scanY = posY + (stepIdx * incY);
                FastPixelHSV testPx = bitmap.GetPixel(scanX, scanY);
                bool bIsMatching = colorMatch.IsMatching(testPx);

                if (bDebugMode) { Console.WriteLine("  [" + scanX + ", " + scanY + "] " + testPx + " => match:" + bIsMatching); }

                if (bIsMatching)
                {
                    posHit = new Point(scanX, scanY);
                    return true;
                }
            }

            if (bDebugMode) { Console.WriteLine("  >> failed"); }
            posHit = new Point(posX + (traceLen * incX), posY + (traceLen * incY));
            return false;
        }

        public static List<Point> TraceSpansV(FastBitmapHSV bitmap, Rectangle box, FastPixelMatch colorMatch, int minSize, bool bDebugMode = false)
        {
            List<Point> result = new List<Point>();
            int lastY = -1;
            bool bHasMatch = false;
            for (int IdxY = box.Top; IdxY <= box.Bottom; IdxY++)
            {
                bHasMatch = false;
                for (int IdxX = box.Left; IdxX <= box.Right; IdxX++)
                {
                    FastPixelHSV testPx = bitmap.GetPixel(IdxX, IdxY);
                    bHasMatch = colorMatch.IsMatching(testPx);
                    if (bHasMatch)
                    {
                        if (bDebugMode) { Console.WriteLine("[" + IdxX + ", " + IdxY + "] " + testPx + " => match!"); }
                        break;
                    }
                }

                if (lastY == -1 && bHasMatch)
                {
                    lastY = IdxY;
                }
                else if (lastY >= 0 && !bHasMatch)
                {
                    int spanSize = IdxY - lastY;
                    if (spanSize > minSize)
                    {
                        if (bDebugMode) { Console.WriteLine(">> adding span: " + lastY + ", size:" + spanSize); }
                        result.Add(new Point(lastY, spanSize));
                    }

                    lastY = -1;
                }
            }

            if (lastY >= 0 && bHasMatch)
            {
                int spanSize = box.Bottom - lastY + 1;
                if (spanSize > minSize)
                {
                    if (bDebugMode) { Console.WriteLine(">> adding span: " + lastY + ", size:" + spanSize); }
                    result.Add(new Point(lastY, spanSize));
                }
            }

            return result;
        }

        public static List<Point> TraceSpansH(FastBitmapHSV bitmap, Rectangle box, FastPixelMatch colorMatch, int minSize, bool bDebugMode = false)
        {
            List<Point> result = new List<Point>();
            int lastX = -1;
            bool bHasMatch = false;
            for (int IdxX = box.Left; IdxX <= box.Right; IdxX++)
            {
                bHasMatch = false;
                for (int IdxY = box.Top; IdxY <= box.Bottom; IdxY++)
                {
                    FastPixelHSV testPx = bitmap.GetPixel(IdxX, IdxY);
                    bHasMatch = colorMatch.IsMatching(testPx);
                    if (bHasMatch)
                    {
                        if (bDebugMode) { Console.WriteLine("[" + IdxX + ", " + IdxY + "] " + testPx + " => match!"); }
                        break;
                    }
                }

                if (lastX == -1 && bHasMatch)
                {
                    lastX = IdxX;
                }
                else if (lastX >= 0 && !bHasMatch)
                {
                    int spanSize = IdxX - lastX;
                    if (spanSize > minSize)
                    {
                        if (bDebugMode) { Console.WriteLine(">> adding span: " + lastX + ", size:" + spanSize); }
                        result.Add(new Point(lastX, spanSize));
                    }

                    lastX = -1;
                }
            }

            if (lastX >= 0 && bHasMatch)
            {
                int spanSize = box.Right - lastX + 1;
                if (spanSize > minSize)
                {
                    if (bDebugMode) { Console.WriteLine(">> adding span: " + lastX + ", size:" + spanSize); }
                    result.Add(new Point(lastX, spanSize));
                }
            }

            return result;
        }

        public static Point TraceBoundsH(FastBitmapHSV bitmap, Rectangle box, FastPixelMatch colorMatch, int maxGapSize, bool bDebugMode = false)
        {
            int boxCenter = (box.Right + box.Left) / 2;

            int minX = -1;
            int gapStart = -1;
            bool bPrevMatch = false;
            for (int IdxX = box.Left; IdxX < boxCenter; IdxX++)
            {
                bool bHasMatch = false;
                for (int IdxY = box.Top; IdxY <= box.Bottom; IdxY++)
                {
                    FastPixelHSV testPx = bitmap.GetPixel(IdxX, IdxY);
                    bHasMatch = colorMatch.IsMatching(testPx);
                    if (bHasMatch)
                    {
                        if (bDebugMode) { Console.WriteLine("[" + IdxX + ", " + IdxY + "] " + testPx + " => match!"); }
                        break;
                    }
                }

                if (bHasMatch)
                {
                    int gapSize = IdxX - gapStart;
                    if ((gapSize > maxGapSize && gapStart > 0) || (minX < 0))
                    {
                        minX = IdxX;
                        gapStart = -1;
                    }

                    if (bDebugMode) { Console.WriteLine(">> gapSize:" + gapSize + ", gapStart:" + gapStart + ", bPrevMatch:" + bPrevMatch + " => minX:" + minX); }
                }
                else
                {
                    if (bPrevMatch)
                    {
                        gapStart = IdxX;
                        if (bDebugMode) { Console.WriteLine(">> gapStart:" + gapStart); }
                    }
                }

                bPrevMatch = bHasMatch;
            }

            if (minX >= 0)
            {
                int maxX = -1;
                gapStart = -1;
                bPrevMatch = false;
                for (int IdxX = box.Right; IdxX > boxCenter; IdxX--)
                {
                    bool bHasMatch = false;
                    for (int IdxY = box.Top; IdxY <= box.Bottom; IdxY++)
                    {
                        FastPixelHSV testPx = bitmap.GetPixel(IdxX, IdxY);
                        bHasMatch = colorMatch.IsMatching(testPx);
                        if (bHasMatch)
                        {
                            if (bDebugMode) { Console.WriteLine("[" + IdxX + ", " + IdxY + "] " + testPx + " => match!"); }
                            break;
                        }
                    }

                    if (bHasMatch)
                    {
                        int gapSize = gapStart - IdxX;
                        if ((gapSize > maxGapSize && gapStart > 0) || (maxX < 0))
                        {
                            maxX = IdxX;
                            gapStart = -1;
                        }

                        if (bDebugMode) { Console.WriteLine(">> gapSize:" + gapSize + ", gapStart:" + gapStart + ", bPrevMatch:" + bPrevMatch + " => maxX:" + maxX); }
                    }
                    else
                    {
                        if (bPrevMatch)
                        {
                            gapStart = IdxX;
                            if (bDebugMode) { Console.WriteLine(">> gapStart:" + gapStart); }
                        }
                    }

                    bPrevMatch = bHasMatch;
                }

                if (maxX > minX)
                {
                    return new Point(minX, maxX - minX);
                }
                else
                {
                    if (bDebugMode) { Console.WriteLine(">> TraceBoundsH: no match on right side!"); }
                }
            }
            else
            {
                if (bDebugMode) { Console.WriteLine(">> TraceBoundsH: no match on left side!"); }
            }

            return new Point();
        }

        public static float CountFillPct(FastBitmapHSV bitmap, Rectangle box, FastPixelMatch colorMatch)
        {
            int totalPixels = (box.Width + 1) * (box.Height + 1);
            int matchPixels = 0;
            for (int IdxY = box.Top; IdxY <= box.Bottom; IdxY++)
            {
                for (int IdxX = box.Left; IdxX <= box.Right; IdxX++)
                {
                    FastPixelHSV testPx = bitmap.GetPixel(IdxX, IdxY);
                    matchPixels += colorMatch.IsMatching(testPx) ? 1 : 0;
                }
            }

            return (float)matchPixels / totalPixels;
        }

        public static FastPixelHSV GetAverageColor(FastBitmapHSV bitmap, Rectangle bounds)
        {
            float hueAcc = 0.0f;
            float satAcc = 0.0f;
            float valAcc = 0.0f;
            float scale = 1.0f / bounds.Width;

            for (int idx = 0; idx < bounds.Width; idx++)
            {
                FastPixelHSV testPx = bitmap.GetPixel(bounds.X + idx, bounds.Y);
                hueAcc += testPx.GetHue();
                satAcc += testPx.GetSaturation();
                valAcc += testPx.GetValue();
            }

            FastPixelHSV avgPx = new FastPixelHSV();
            avgPx.SetHSV((int)(hueAcc * scale), (int)(satAcc * scale), (int)(valAcc * scale));
            return avgPx;
        }

        public static void FindColorRange(FastBitmapHSV bitmap, Rectangle box, out int minMono, out int maxMono)
        {
            minMono = 255;
            maxMono = 0;
            for (int IdxY = box.Top; IdxY <= box.Bottom; IdxY++)
            {
                for (int IdxX = box.Left; IdxX <= box.Right; IdxX++)
                {
                    FastPixelHSV testPx = bitmap.GetPixel(IdxX, IdxY);
                    minMono = Math.Min(minMono, testPx.Monochrome);
                    maxMono = Math.Max(maxMono, testPx.Monochrome);
                }
            }
        }

        public static bool CreateFloodFillBitmap(FastBitmapHSV srcBitmap, Point floodOrigin, Size floodExtent, FastPixelMatch colorMatch,
            out FastBitmapHSV floodBitmap, out Rectangle floodBounds, bool bDebugMode = false)
        {
            List<Point> floodPoints = new List<Point>();
            int minX = floodOrigin.X;
            int maxX = floodOrigin.X;
            int minY = floodOrigin.Y;
            int maxY = floodOrigin.Y;
            Rectangle boundRect = new Rectangle(floodOrigin.X - floodExtent.Width, floodOrigin.Y - floodExtent.Height, floodExtent.Width * 2, floodExtent.Height * 2);

            Stack<Point> openList = new Stack<Point>();
            openList.Push(floodOrigin);

            while (openList.Count > 0)
            {
                Point testPoint = openList.Pop();
                if (floodPoints.Contains(testPoint))
                {
                    continue;
                }

                FastPixelHSV testPx = srcBitmap.GetPixel(testPoint.X, testPoint.Y);
                if (bDebugMode) { Console.WriteLine("[" + testPoint.X + ", " + testPoint.Y + "] " + testPx + ", match:" + colorMatch.IsMatching(testPx) + ", inBounds:" + boundRect.Contains(testPoint)); }

                if (colorMatch.IsMatching(testPx) && boundRect.Contains(testPoint))
                {
                    floodPoints.Add(testPoint);

                    minX = Math.Min(minX, testPoint.X);
                    maxX = Math.Max(maxX, testPoint.X);
                    minY = Math.Min(minY, testPoint.Y);
                    maxY = Math.Max(maxY, testPoint.Y);

                    openList.Push(new Point(testPoint.X - 1, testPoint.Y));
                    openList.Push(new Point(testPoint.X + 1, testPoint.Y));
                    openList.Push(new Point(testPoint.X, testPoint.Y - 1));
                    openList.Push(new Point(testPoint.X, testPoint.Y + 1));
                    openList.Push(new Point(testPoint.X - 1, testPoint.Y - 1));
                    openList.Push(new Point(testPoint.X + 1, testPoint.Y - 1));
                    openList.Push(new Point(testPoint.X - 1, testPoint.Y + 1));
                    openList.Push(new Point(testPoint.X + 1, testPoint.Y + 1));
                }
            }

            floodBounds = new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
            if (floodPoints.Count > 0)
            {
                floodBitmap = new FastBitmapHSV(floodBounds.Width, floodBounds.Height);
                int numPx = floodBounds.Width * floodBounds.Height;

                for (int Idx = 0; Idx < numPx; Idx++)
                {
                    floodBitmap.SetPixel(Idx, new FastPixelHSV(false));
                }

                foreach (Point p in floodPoints)
                {
                    int Idx = (p.X - minX) + ((p.Y - minY) * floodBounds.Width);
                    floodBitmap.SetPixel(Idx, new FastPixelHSV(true));
                }
            }
            else
            {
                floodBitmap = null;
            }

            return (floodBitmap != null);
        }

    }
}
