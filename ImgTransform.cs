using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Numerics;
using Accord.Math;

namespace Diplom
{
    internal class ImgTransform
    {
        BitmapSource _bitmap;
        int _height;
        int _width;
        PixelColores _arr;
        double[,] Y, Cb, Cr;
        int count_of_kanal;

        BackgroundWorker _backgroundWorker;
        public ImgTransform(BitmapSource bitmap, BackgroundWorker backgroundWorker)
        {
            _bitmap = bitmap;
            _height = _bitmap.PixelHeight;
            _width = _bitmap.PixelWidth;

            if (_bitmap.Format != PixelFormats.Bgra32)
                _bitmap = new FormatConvertedBitmap(_bitmap, PixelFormats.Bgra32, null, 0);
            count_of_kanal = 3;

            _arr = new PixelColores(GetPixels(_bitmap));

            _backgroundWorker = backgroundWorker;
        }
        
        public BitmapSource GetImage()
        {
            int nStride = (_bitmap.PixelWidth * _bitmap.Format.BitsPerPixel + 7) / 8;
            WriteableBitmap wb = new WriteableBitmap(_bitmap);
            Int32Rect rect = new Int32Rect(0, 0, _width, _height);
            byte[] pixelsToWrite = new byte[_width * _height * wb.Format.BitsPerPixel / 8];

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    int pixelOffset = (x + y * _width) * wb.Format.BitsPerPixel / 8;
                    pixelsToWrite[pixelOffset] = _arr[x, y].Blue;
                    pixelsToWrite[pixelOffset + 1] = _arr[x, y].Green;
                    pixelsToWrite[pixelOffset + 2] = _arr[x, y].Red;
                    pixelsToWrite[pixelOffset + 3] = _arr[x, y].Alpha;
                }

                wb.WritePixels(rect, pixelsToWrite, nStride, 0);
            }
            return wb;
        }
        public int GetCountPixels()
        {
            return _height * _width;
        }
        public BitmapSource GetImageGray()
        {
            return new WriteableBitmap(new FormatConvertedBitmap(_bitmap, PixelFormats.Gray16, null, 0));
        }
        private void Create_YCbCr()
        {
            Y = new double[_width, _height];
            Cb = new double[_width, _height];
            Cr = new double[_width, _height];
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    Y[x,y]  = ((0.299 * _arr[x, y].Red) + (0.587 * _arr[x, y].Green) + (0.114 * _arr[x, y].Blue));
                    Cb[x,y] = (128 - (0.168736 * _arr[x, y].Red) - (0.331264 * _arr[x, y].Green) + (0.5 * _arr[x, y].Blue));
                    Cr[x,y] = (128 + (0.5 * _arr[x, y].Red) - (0.418688 * _arr[x, y].Green) - (0.081312 * _arr[x, y].Blue));
                }
            }
        }
        private void YCbCr_to_Bgra()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    PixelColor pixelColor = new PixelColor();
                    pixelColor.Red = (byte)ToByte(Math.Round(Y[x, y] + 1.402 * (Cr[x, y] - 128)));
                    pixelColor.Green = (byte)ToByte(Math.Round(Y[x, y] - 0.34414 * (Cb[x, y] - 128) - 0.71414 * (Cr[x, y] - 128)));
                    pixelColor.Blue = (byte)ToByte(Math.Round(Y[x, y] + 1.772 * (Cb[x, y] - 128)));
                    pixelColor.Alpha = _arr[x, y].Alpha;
                    _arr[x, y] = pixelColor;
                }
            }
        }
        private void Y_to_Bgra()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    PixelColor pixelColor = new PixelColor();
                    pixelColor.Red = (byte)ToByte(Math.Round(Y[x, y]));
                    pixelColor.Green = (byte)ToByte(Math.Round(Y[x, y]));
                    pixelColor.Blue = (byte)ToByte(Math.Round(Y[x, y]));
                    pixelColor.Alpha = _arr[x, y].Alpha;
                    _arr[x, y] = pixelColor;
                }
            }
        }

        private void log1fftshift_to_Y(Complex[,] complices, int n, double k_brightness)
        {
            for (int x = 0; x < _width/2; x++)
            {
                for (int y = 0; y < _height/2; y++)
                {
                    Y[x + _width / 2, y + _height / 2] = Math.Log(1 + Math.Abs(complices[x, y].Real)) * 256*k_brightness;
                    Y[-x + _width / 2, y + _height / 2] = Math.Log(1 + Math.Abs(complices[n-1 - x, y].Real)) * 256*k_brightness;
                    Y[x + _width / 2, -y + _height / 2] = Math.Log(1 + Math.Abs(complices[x, n-1 - y].Real)) * 256*k_brightness;
                    Y[-x + _width / 2, -y + _height / 2] = Math.Log(1 + Math.Abs(complices[n-1 - x, n-1 - y].Real)) * 256*k_brightness;
                }
            }
        }

        public void Fourier_freq(double k_brightness)
        {
            Create_YCbCr();

            _backgroundWorker.ReportProgress(-1);
            _backgroundWorker.ReportProgress(-2);

            int n = Math.Max(_width, _height);
            for (int i = 1; ; i <<= 1)
            {
                if (i >= n)
                {
                    n = i;
                    break;
                }
            }

            Complex[,] complices = new Complex[n, n];
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    complices[x, y] = Y[x, y];
                }
            }

            Accord.Math.FourierTransform.FFT2(complices, FourierTransform.Direction.Forward);
            log1fftshift_to_Y(complices, n, k_brightness);

            Y_to_Bgra();
        }
        public void change_freq(int r, double koef, int frequences)
        {
            Create_YCbCr();

            _backgroundWorker.ReportProgress(-1);
            _backgroundWorker.ReportProgress(-2);

            int n = Math.Max(_width, _height);
            for (int i = 1; ; i <<= 1)
            {
                if (i >= n)
                {
                    n = i;
                    break;
                }
            }

            Complex[,] complices = new Complex[n, n];
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    complices[x, y] = Y[x, y];
                }
            }

            Accord.Math.FourierTransform.FFT2(complices, FourierTransform.Direction.Forward);

            _backgroundWorker.ReportProgress(-3);

            for (int x = 0; x < n / 2; x++)
            {
                for (int y = 0; y < n / 2; y++)
                {
                    if (frequences == 0 && (x < r || y < r))
                    {
                        complices[x, y] *= koef;
                        complices[n - 1 - x, y] *= koef;
                        complices[x, n - 1 - y] *= koef;
                        complices[n - 1 - x, n - 1 - y] *= koef;
                    }
                    if (frequences == 1 && (x > r || y > r))
                    {
                        complices[x, y] *= koef;
                        complices[n - 1 - x, y] *= koef;
                        complices[x, n - 1 - y] *= koef;
                        complices[n - 1 - x, n - 1 - y] *= koef;
                    }
                }
            }

            _backgroundWorker.ReportProgress(-4);

            Accord.Math.FourierTransform.FFT2(complices, FourierTransform.Direction.Backward);

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    Y[x, y] = complices[x, y].Real;
                }
            }

            YCbCr_to_Bgra();
        }

        public void Blur_Box(int k)
        {
            double[,] Mask = new double[k, k];
            for (int i = 0; i < k; i++)
            {
                for (int j = 0; j < k; j++)
                {
                    Mask[i, j] = 1 / Math.Pow(k, 2);
                }
            }
            PixelColores pixelColores = new PixelColores(_width, _height);
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    pixelColores[x, y] = CalcPixel(Mask, k, x, y);
                }
                if (x % 5 == 0) _backgroundWorker.ReportProgress((x + 1) * _height);
            }
            
            _arr = pixelColores;
        }
        int n_mask_gauss;
        double[,] Mask_Gauss;
        public void SetMask(double sgm)
        {
            n_mask_gauss = (int)(Math.Round(3 * sgm) * 2 + 1);//size of mask
            double m_sum = 0;
            Mask_Gauss = new double[n_mask_gauss, n_mask_gauss];
            for (int x = 0; x < n_mask_gauss; x++)
            {
                for (int y = 0; y < n_mask_gauss; y++)
                {
                    Mask_Gauss[x, y] = Math.Pow(Math.E, ((-Math.Pow(x - n_mask_gauss / 2, 2) - Math.Pow(y - n_mask_gauss / 2, 2)) / (2 * Math.Pow(sgm, 2)))) / (2 * Math.PI * Math.Pow(sgm, 2));
                    m_sum += Mask_Gauss[x, y];
                }
            }
            for (int x = 0; x < n_mask_gauss; x++)
            {
                for (int y = 0; y < n_mask_gauss; y++)
                {
                    Mask_Gauss[x, y] /= m_sum;
                }
            }
        }
        public void Blur_Gauss()
        {
            PixelColores pixelColores = new PixelColores(_width, _height);
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    PixelColor pixelColor = CalcPixel(Mask_Gauss, n_mask_gauss, x,y);
                    pixelColores[x, y] = pixelColor;

                }
                if(x%5==0) _backgroundWorker.ReportProgress((x + 1) * _height);                    
            }
            
            _arr = pixelColores;
        }
        private PixelColor CalcPixel(double[,] Mask, int n, int x, int y)//mask for every pixel
        {
            PixelColor ResPixelColor = new PixelColor();
            for (int kan = 0; kan < count_of_kanal; kan++)
            {
                double pix_value = 0;
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        pix_value += Mask[i, j] * _arr[x - n / 2 + i, y - n / 2 + j][kan];
                    }
                }
                ResPixelColor[kan] = ToByte(pix_value);
            }
            if( count_of_kanal == 3) ResPixelColor.Alpha = 255;
            return ResPixelColor;
        }
        public void Blur_Median()
        {
            PixelColores pixelColores = new PixelColores(_width, _height);
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    PixelColor pixelColor = new PixelColor();
                    for (int kan = 0; kan < 4; kan++)
                    {
                        byte[] bytes = new byte[] { _arr[x - 1, y - 1][kan], _arr[x - 1, y][kan] , _arr[x - 1, y + 1][kan]
                            , _arr[x, y - 1][kan] , _arr[x, y][kan] , _arr[x, y + 1][kan]
                            , _arr[x + 1, y - 1][kan] , _arr[x + 1, y][kan] , _arr[x + 1, y + 1][kan] };
                        Array.Sort(bytes);
                        pixelColor[kan] = bytes[4];
                    }
                    pixelColores[x, y] = pixelColor;
                }
                if (x % 5 == 0) _backgroundWorker.ReportProgress((x + 1) * _height);
            }

            _arr = pixelColores;
        }
        public void Sharp_Filter1()
        {
            int k = 3;
            double[,] Mask = new double[,]
            {
                { -0.1, -0.2, -0.1 },
                { -0.2, 2.2, -0.2 },
                { -0.1, -0.2, -0.1 }
            };
            PixelColores pixelColores = new PixelColores(_width, _height);
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    pixelColores[x, y] = CalcPixel(Mask, k, x, y);
                }
                if (x % 5 == 0) _backgroundWorker.ReportProgress((x + 1) * _height);
            }
            _arr = pixelColores;
        }
        public void Sharp_Filter2()
        {
            int k = 3;
            double[,] Mask = new double[,]
            {
                { 0, -1, 0 },
                { -1, 5, -1 },
                { 0, -1, 0 }
            };
            PixelColores pixelColores = new PixelColores(_width, _height);
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    pixelColores[x, y] = CalcPixel(Mask, k, x, y);
                }
                if (x % 5 == 0) _backgroundWorker.ReportProgress((x + 1) * _height);
            }
            _arr = pixelColores;
        }
        public void Embossibg()
        {
            int k = 3;
            double[,] Mask = new double[,]
            {
                { 0, 1, 0 },
                { 1, 0, -1},
                { 0, -1, 0 }
            };
            PixelColores pixelColores = new PixelColores(_width, _height);
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    pixelColores[x, y] = CalcPixel(Mask, k, x, y);
                }
                if (x % 5 == 0) _backgroundWorker.ReportProgress((x + 1) * _height);
            }
            _arr = pixelColores;
        }

        private PixelColores CalcLaplasian()
        {
            int k = 3;
            double[,] Mask = new double[,]
                {
                { 0, -1, 0 },
                { -1, 4, -1 },
                { 0, -1, 0 }
                };
            PixelColores pixelColores = new PixelColores(_width, _height);
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    pixelColores[x, y] = CalcPixel(Mask, k, x, y);
                }
                if (x % 5 == 0) _backgroundWorker.ReportProgress((x + 1) * _height);
            }
            return pixelColores;
        }
        public void Laplasian()
        {
            _arr = CalcLaplasian();
        }
        public void AddLaplasian()
        {
            PixelColores laplasian = CalcLaplasian();
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    PixelColor pixelColor = new PixelColor();
                    for (int k = 0; k < count_of_kanal; k++)
                    {
                        pixelColor[k] = ToByte(_arr[x, y][k] + laplasian[x, y][k]);
                    }
                    if (count_of_kanal == 3) pixelColor.Alpha = 255;
                    _arr[x, y] = pixelColor;
                }
                if (x % 5 == 0) _backgroundWorker.ReportProgress((x + 1) * _height);
            }
        }

        public byte ToByte(double x)
        {
            x = Math.Round(x);
            return (x < 0) ? (byte)0 : ((x > 255) ? (byte)255 : (byte)x);
        }
        public PixelColor[,] GetPixels(BitmapSource source)
        {
            if (source.Format != PixelFormats.Bgra32)
                source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

            int width = source.PixelWidth;
            int height = source.PixelHeight;
            PixelColor[,] result = new PixelColor[width, height];

            source.CopyPixels(result, width * 4, 0, false);
            return result;
        }
    }

    public class PixelColores
    {
        public PixelColores(int length0, int length1)
        {
            pixels = new PixelColor[length0,length1];
        }
        public PixelColores(PixelColor[,] pixelColor)
        {
            pixels = new PixelColor[pixelColor.GetLength(0), pixelColor.GetLength(1)];
            for (int i = 0; i < pixelColor.GetLength(0); i++)
            {
                for (int j = 0; j < pixelColor.GetLength(1); j++)
                {
                    pixels[i, j] = pixelColor[i, j];
                }
            }
        }
        public PixelColor[,] pixels;
        public PixelColor this[int x, int y]
        {
            get
            {
                if (x < 0) x = 0;
                if (y < 0) y = 0;
                if (x >= pixels.GetLength(0)) x = pixels.GetLength(0) - 1;
                if (y >= pixels.GetLength(1)) y = pixels.GetLength(1) - 1;
                PixelColor pixelColor = pixels[x,y];
                return pixelColor;
            }
            set
            {
                if (x < 0) x = 0;
                if (y < 0) y = 0;
                if (x >= pixels.GetLength(0)) x = pixels.GetLength(0) - 1;
                if (y >= pixels.GetLength(1)) y = pixels.GetLength(1) - 1;
                PixelColor pixelColor = value;
                pixels[x, y] = pixelColor;
            }
        }
    }
    public struct PixelColor
    {
        public byte Blue;
        public byte Green;
        public byte Red;
        public byte Alpha;

        public byte this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return Blue;
                    case 1:
                        return Green;
                    case 2:
                        return Red;
                    case 3:
                        return Alpha;
                    default: throw new Exception("Unknown Pixel's Property Name");
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        Blue = value;
                        break;
                    case 1:
                        Green = value;
                        break;
                    case 2:
                        Red = value;
                        break;
                    case 3:
                        Alpha = value;
                        break;
                }
            }
        }
    }
    public static class BitmapSourceHelper
    {
#if UNSAFE
  public unsafe static void CopyPixels(this BitmapSource source, PixelColor[,] pixels, int stride, int offset)
  {
    fixed(PixelColor* buffer = &pixels[0, 0])
      source.CopyPixels(
        new Int32Rect(0, 0, source.PixelWidth, source.PixelHeight),
        (IntPtr)(buffer + offset),
        pixels.GetLength(0) * pixels.GetLength(1) * sizeof(PixelColor),
        stride);
  }
#else
        public static void CopyPixels(this BitmapSource source, PixelColor[,] pixels, int stride, int offset, bool qwe)
        {
            var height = source.PixelHeight;
            var width = source.PixelWidth;
            var pixelBytes = new byte[height * width * 4];
            source.CopyPixels(pixelBytes, stride, 0);
            int y0 = offset / width;
            int x0 = offset - width * y0;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixels[x + x0, y + y0] = new PixelColor
                    {
                        Blue = pixelBytes[(y * width + x) * 4 + 0],
                        Green = pixelBytes[(y * width + x) * 4 + 1],
                        Red = pixelBytes[(y * width + x) * 4 + 2],
                        Alpha = pixelBytes[(y * width + x) * 4 + 3],
                    };
        }
#endif
    }
}
