using OpenCvSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;

public static class CvBitmap
{
    // Bitmap(24bpp) -> Mat(CV_8UC3, BGR)
    public static Mat Bitmap24bppToMatBgr(Bitmap bmp)
    {
        // гарантируем 24bppRgb
        if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
        {
            using var tmp = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(tmp))
                g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);

            return Bitmap24bppToMatBgr(tmp);
        }

        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

        try
        {
            // ВАЖНО: FromPixelData использует память bitmap -> делаем Clone перед UnlockBits
            using var header = Mat.FromPixelData(
                bmp.Height,
                bmp.Width,
                MatType.CV_8UC3,
                data.Scan0,
                data.Stride
            );

            return header.Clone();
        }
        finally
        {
            bmp.UnlockBits(data);
        }
    }


    // Mat(CV_8UC3 BGR) -> Bitmap(24bpp)
    public static Bitmap MatBgrToBitmap24bpp(Mat matBgr)
    {
        if (matBgr.Empty()) throw new ArgumentException("mat is empty");
        if (matBgr.Type() != MatType.CV_8UC3)
        {
            using var tmp = new Mat();
            matBgr.ConvertTo(tmp, MatType.CV_8UC3);
            return MatBgrToBitmap24bpp(tmp);
        }

        var bmp = new Bitmap(matBgr.Cols, matBgr.Rows, PixelFormat.Format24bppRgb);
        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var data = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

        try
        {
            unsafe
            {
                byte* src = (byte*)matBgr.DataPointer;
                byte* dst = (byte*)data.Scan0;

                int srcStep = (int)matBgr.Step();
                int dstStep = data.Stride;
                int rowBytes = matBgr.Cols * 3;

                for (int y = 0; y < matBgr.Rows; y++)
                {
                    Buffer.MemoryCopy(src + y * srcStep, dst + y * dstStep, dstStep, rowBytes);
                }
            }
        }
        finally
        {
            bmp.UnlockBits(data);
        }

        return bmp;
    }
}
