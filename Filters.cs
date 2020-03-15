using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;



namespace WindowsFormsApp1
{
    static class GlobalVars
    {
        public static string FilterType = "";
    }
    abstract class Filters
    {
        public int R1 = 0, G1 = 0, B1 = 0;
        public int AVG = 0;
        protected abstract Color calculateNewPixelColor(Bitmap sourceImage, int x, int y);
        public int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
        public Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            if (GlobalVars.FilterType == "GrayScale")
            {
                int size = sourceImage.Height * sourceImage.Width;
                for (int i = 0; i < sourceImage.Width; i++)
                    for (int j = 0; j < sourceImage.Height; j++)
                    {
                        R1 += sourceImage.GetPixel(i, j).R;
                        G1 += sourceImage.GetPixel(i, j).G;
                        B1 += sourceImage.GetPixel(i, j).B;
                    }
                R1 /= size;
                G1 /= size;
                B1 /= size;
                AVG = (R1 + G1 + B1) / 3;
            }
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);
            for (int i = 0; i < sourceImage.Width - 1; i++)
            {
                worker.ReportProgress((int)((float)i / resultImage.Width * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height - 1; j++)
                {
                    resultImage.SetPixel(i, j, calculateNewPixelColor(sourceImage, i, j));
                }
            }
            return resultImage;
        }
    }
    class InvertFilter : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            Color resultColor = Color.FromArgb(255 - sourceColor.R,
                                               255 - sourceColor.G,
                                               255 - sourceColor.B);
            return resultColor;
        }
    }

    class GrayScaleFilter : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            int intensity = Convert.ToInt32(sourceColor.R * 0.299) + 
                            Convert.ToInt32(sourceColor.G * 0.587) +
                            Convert.ToInt32(sourceColor.B * 0.114);
            Color resultColor = Color.FromArgb(intensity, intensity, intensity);
            return resultColor;
        }
    }

    class Waves : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int xx = Convert.ToInt32(x + 20 * Math.Sin((2 * Math.PI * y) / 60));
            int newx = Clamp(xx, 0, sourceImage.Width - 1);
            Color resultColor = sourceImage.GetPixel(newx, y);
            return resultColor;
        }
    }

    class Mediana : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            if (x == 0 || y == 0 || x == 1 || y == 1 || x == 2 || y == 2 ||
                x == sourceImage.Width || x == sourceImage.Width - 1 || 
                x == sourceImage.Width - 2 || x == sourceImage.Width - 3 ||
                y == sourceImage.Height || y == sourceImage.Height - 1 || 
                y == sourceImage.Height - 2 || y == sourceImage.Height - 3)
            {
                Color defaultColor = sourceImage.GetPixel(x, y);
                return defaultColor;
            }
            else
            {
                List<int> valuesR = new List<int>();
                List<int> valuesG = new List<int>();
                List<int> valuesB = new List<int>();

                for (int _x = x - 3; _x <= x + 3; _x++)
                {
                    for (int _y = y - 3; _y <= y + 3; _y++)
                    {
                        valuesR.Add(sourceImage.GetPixel(_x, _y).R);
                        valuesG.Add(sourceImage.GetPixel(_x, _y).G);
                        valuesB.Add(sourceImage.GetPixel(_x, _y).B);
                    }
                }

                valuesB.Sort();
                valuesG.Sort();
                valuesR.Sort();
                int newR = valuesR[valuesR.Count / 2];
                int newG = valuesG[valuesG.Count / 2];
                int newB = valuesB[valuesB.Count / 2];
                Color resultColor = Color.FromArgb(newR, newG, newB);
                return resultColor;
            }
        }
    }

    class Dilation : Filters
    {
        float[,] kernel = null;
        public Dilation()
        {
            kernel = new float[3, 3];
            kernel[0, 0] = 0.0f; kernel[0, 1] = 1.0f; kernel[0, 2] = 0.0f;
            kernel[1, 0] = 1.0f; kernel[1, 1] = 1.0f; kernel[1, 2] = 1.0f;
            kernel[2, 0] = 0.0f; kernel[2, 1] = 1.0f; kernel[2, 2] = 0.0f;
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;

            Color resultColor = Color.Black;

            byte max = 0;
            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color color = sourceImage.GetPixel(idX, idY);
                    int intensity = color.R;
                    if (color.R != color.G || color.R != color.B || color.G != color.B)
                    {
                        intensity = (int)(0.36 * color.R + 0.53 * color.G + 0.11 * color.R);
                    }
                    if (kernel[k + radiusX, l + radiusY] > 0 && intensity > max)
                    {
                        max = (byte)intensity;
                        resultColor = color;
                    }
                }
            return resultColor;
        }
    }

    class Erosion : Filters
    {
        float[,] kernel = null;
        public Erosion()
        {
            kernel = new float[3, 3];
            kernel[0, 0] = 0.0f; kernel[0, 1] = 1.0f; kernel[0, 2] = 0.0f;
            kernel[1, 0] = 1.0f; kernel[1, 1] = 1.0f; kernel[1, 2] = 1.0f;
            kernel[2, 0] = 0.0f; kernel[2, 1] = 1.0f; kernel[2, 2] = 0.0f;
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int radiusX = kernel.GetLength(0) / 2; // кол-во строк
            int radiusY = kernel.GetLength(1) / 2; // кол-во столбцов

            Color resultColor = Color.White;

            byte min = 255;
            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color color = sourceImage.GetPixel(idX, idY);
                    int intensity = color.R;
                    if (color.R != color.G || color.R != color.B || color.G != color.B)
                    {
                        intensity = (int)(0.36 * color.R + 0.53 * color.G + 0.11 * color.R);
                    }
                    if (kernel[k + radiusX, l + radiusY] > 0 && intensity < min)
                    {
                        min = (byte)intensity;
                        resultColor = color;
                    }
                }
            return resultColor;
        }
    }

    // NIKITA FILTERS
    class Transfer : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            if (x - 50 < 0)
            {
                return Color.FromArgb(255, 255, 255);
            }
            else
            {
                return sourceImage.GetPixel(x - 50, y);
            }
        }
    }

    class Rotation : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int centerX = sourceImage.Width / 2;
            int centerY = sourceImage.Height / 2;
            int x_color, y_color;
            x_color = (int)((x - centerX) * Math.Cos(Math.PI / 4) - (y - centerY) * Math.Sin(Math.PI / 4) + x);
            y_color = (int)((x - centerX) * Math.Sin(Math.PI / 4) + (y - centerY) * Math.Cos(Math.PI / 4) + y);
            if (x_color < 0 || y_color < 0 || x_color > sourceImage.Width - 1 || y_color > sourceImage.Height - 1)
            {
                return Color.FromArgb(255, 255, 255);
            }
            else
            {
                return sourceImage.GetPixel(x_color, y_color);
            }
        }
    }


    class LinearGist : Filters
    {
        int y_min;
        int y_max;
        public LinearGist(Bitmap sourceImage)
        {
            int min = 255, max = 0;
            for (int i = 0; i < sourceImage.Width; i++)
            {
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    if ((sourceImage.GetPixel(i, j)).A < min)
                    {
                        min = (sourceImage.GetPixel(i, j)).A;
                    }
                    if ((sourceImage.GetPixel(i, j)).A > max)
                    {
                        max = (sourceImage.GetPixel(i, j)).A;
                    }
                }
            }
            y_min = min;
            y_max = max;
        }
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            int new_intens;
            if (y_max - y_min == 0)
            {
                new_intens = 255;
            }
            else
            {
                new_intens = (int)((Clamp(sourceColor.A - y_min, 0, 255) * ((float)255 / (y_max - y_min))));
            }
            Color resultColor = Color.FromArgb(new_intens, sourceColor.R, sourceColor.G, sourceColor.B);
            return resultColor;
        }
    }


    class MatrixFilter : Filters
    {
        protected float[,] kernel = null;
        protected MatrixFilter() { }
        public MatrixFilter(float[,] kernel)
        {
            this.kernel = kernel;
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;
            float resultR = 0;
            float resultG = 0;
            float resultB = 0;
            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR += neighborColor.R * kernel[k + radiusX, l + radiusY];
                    resultG += neighborColor.G * kernel[k + radiusX, l + radiusY];
                    resultB += neighborColor.B * kernel[k + radiusX, l + radiusY];
                }
            return Color.FromArgb(
                Clamp((int)resultR, 0, 255),
                Clamp((int)resultG, 0, 255),
                Clamp((int)resultB, 0, 255)
                );
        }
    }

    class BlurFilter : MatrixFilter
    {
        public BlurFilter()
        {
            int sizeX = 3;
            int sizeY = 3;
            kernel = new float[sizeX, sizeY];
            for (int i = 0; i < sizeX; i++)
                for (int j = 0; j < sizeY; j++)
                    kernel[i, j] = 1.0f / (float)(sizeX * sizeY);
        }
    }
    
    class GaussianFilter : MatrixFilter
    {
        public GaussianFilter()
        {
            CreateGaussianKernel(3, 2);
        }

        public void CreateGaussianKernel(int radius, float sigma)
        {
            int size = 2 * radius + 1;
            kernel = new float[size, size];
            float norm = 0;
            for (int i = -radius; i <= radius; i++) 
                for (int j = -radius; j <= radius; j++)
                {
                    kernel[i + radius, j + radius] = (float)(Math.Exp(-(i * i + j * j) / (2 * sigma * sigma)));
                    norm += kernel[i + radius, j + radius];
                }
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    kernel[i, j] /= norm;
        }
    }

    class Sharpness : MatrixFilter
    {
        public Sharpness()
        {
            int sizeX = 3;
            int sizeY = 3;
            kernel = new float[sizeX, sizeY];
            for (int i = 0; i < sizeX; i++)
                for (int j = 0; j < sizeY; j++)
                    kernel[i, j] = -1;
            kernel[1, 1] = 9;
        }
    }

    class MotionBlur : MatrixFilter
    {
        public MotionBlur()
        {
            int sizeX = 3;
            int sizeY = 3;
            kernel = new float[sizeX, sizeY];
            for (int i = 0; i < sizeX; i++)
                for (int j = 0; j < sizeY; j++)
                {
                    kernel[i, j] = 0;
                    if (i == j)
                        kernel[i, j] = (float)1 / (float)3;
                }
        }
    }

    // NIKITA FILTERS

    class EdgesHighlighting : MatrixFilter
    {
        public EdgesHighlighting()
        {
            int sizeX = 3;
            int sizeY = 3;
            kernel = new float[sizeX, sizeY];
            for (int i = 0; i < sizeX; i++)
            {
                for (int j = 0; j < sizeY; j++)
                {
                    if (i == 0)
                    {
                        kernel[i, j] = -1;
                    }
                    if (i == 1)
                    {
                        kernel[i, j] = 0;
                    }
                    if (i == 2)
                    {
                        kernel[i, j] = 1;
                    }
                }
            }
        }
    }
}
