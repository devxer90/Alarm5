using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace AlarmPP.Web.Services
{
    /// Утилиты для безшовной склейки и рисования без повторного overlap.
    public static class SeamlessStitching
    {
        /// Мягко «сшивает» два изображения по вертикальному стыку.
        //public static void BlendVerticalSeam(
        //    Graphics g,
        //    Bitmap leftBmp, Rectangle srcLeft, Rectangle destLeft,
        //    Bitmap rightBmp, Rectangle srcRight, Rectangle destRight,
        //    int blendW,
        //    float gamma = 1.0f)
        //{
        //    if (g == null || leftBmp == null || rightBmp == null) return;
        //    if (blendW <= 0) return;

        //    int top = Math.Max(destLeft.Y, destRight.Y);
        //    int bottom = Math.Min(destLeft.Bottom, destRight.Bottom);
        //    int h = bottom - top;
        //    if (h <= 0) return;

        //    int srcYL = srcLeft.Y + (top - destLeft.Y);
        //    int srcYR = srcRight.Y + (top - destRight.Y);

        //    var lBandSrc = new Rectangle(Math.Max(srcLeft.X, srcLeft.Right - blendW), srcYL,
        //                                 Math.Min(blendW, srcLeft.Width),
        //                                 Math.Min(h, srcLeft.Bottom - srcYL));
        //    var rBandSrc = new Rectangle(srcRight.X, srcYR,
        //                                 Math.Min(blendW, srcRight.Width),
        //                                 Math.Min(h, srcRight.Bottom - srcYR));

        //    if (lBandSrc.Width <= 0 || lBandSrc.Height <= 0 ||
        //        rBandSrc.Width <= 0 || rBandSrc.Height <= 0) return;

        //    int outX = destRight.X - blendW;
        //    var outRect = new Rectangle(outX, top, 2 * blendW, Math.Min(lBandSrc.Height, rBandSrc.Height));

        //    using var lBand = leftBmp.Clone(lBandSrc, PixelFormat.Format24bppRgb);
        //    using var rBand = rightBmp.Clone(rBandSrc, PixelFormat.Format24bppRgb);
        //    using var band = new Bitmap(outRect.Width, outRect.Height, PixelFormat.Format24bppRgb);

        //    var ld = lBand.LockBits(new Rectangle(0, 0, lBand.Width, lBand.Height),
        //                            ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        //    var rd = rBand.LockBits(new Rectangle(0, 0, rBand.Width, rBand.Height),
        //                            ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        //    var bd = band.LockBits(new Rectangle(0, 0, band.Width, band.Height),
        //                           ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

        //    unsafe
        //    {
        //        byte* lp = (byte*)ld.Scan0;
        //        byte* rp = (byte*)rd.Scan0;
        //        byte* bp = (byte*)bd.Scan0;
        //        int ls = ld.Stride, rs = rd.Stride, bs = bd.Stride;

        //        for (int y = 0; y < band.Height; y++)
        //        {
        //            byte* lrow = lp + y * ls;
        //            byte* rrow = rp + y * rs;
        //            byte* brow = bp + y * bs;

        //            for (int x = 0; x < band.Width; x++)
        //            {
        //                float t = (band.Width <= 1) ? 1f : (x / (float)(band.Width - 1));
        //                if (gamma != 1f) t = (float)Math.Pow(t, gamma);
        //                float s = 1f - t;

        //                int lx = Math.Min(lBand.Width - 1, lBand.Width - 1 - x);
        //                int rx = Math.Min(rBand.Width - 1, x);

        //                int lo = 3 * lx, ro = 3 * rx;
        //                brow[3 * x + 0] = (byte)Math.Clamp(s * lrow[lo + 0] + t * rrow[ro + 0], 0, 255);
        //                brow[3 * x + 1] = (byte)Math.Clamp(s * lrow[lo + 1] + t * rrow[ro + 1], 0, 255);
        //                brow[3 * x + 2] = (byte)Math.Clamp(s * lrow[lo + 2] + t * rrow[ro + 2], 0, 255);
        //            }
        //        }
        //    }

        //    lBand.UnlockBits(ld);
        //    rBand.UnlockBits(rd);
        //    band.UnlockBits(bd);

        //    g.DrawImage(band, outRect);
        //}
        public static void BlendVerticalSeam(
    Graphics g,
    Bitmap leftBmp, Rectangle srcLeft, Rectangle destLeft,
    Bitmap rightBmp, Rectangle srcRight, Rectangle destRight,
    int blendW,
    float gamma = 1.0f)
        {
            if (g == null || leftBmp == null || rightBmp == null) return;
            if (blendW <= 0) return;

            // Пересечение по Y на холсте
            int top = Math.Max(destLeft.Y, destRight.Y);
            int bottom = Math.Min(destLeft.Bottom, destRight.Bottom);
            int h = bottom - top;
            if (h <= 0) return;

            // Синхронизируем Y источников с уcтановленным top на холсте
            int srcYL = srcLeft.Y + (top - destLeft.Y);
            int srcYR = srcRight.Y + (top - destRight.Y);

            // Фактические полосы-источники: правая кромка слева, левая кромка справа
            var lBandSrc = new Rectangle(
                x: Math.Max(srcLeft.X, srcLeft.Right - blendW),
                y: srcYL,
                width: Math.Min(blendW, srcLeft.Width),
                height: Math.Min(h, srcLeft.Bottom - srcYL)
            );
            var rBandSrc = new Rectangle(
                x: srcRight.X,
                y: srcYR,
                width: Math.Min(blendW, srcRight.Width),
                height: Math.Min(h, srcRight.Bottom - srcYR)
            );

            if (lBandSrc.Width <= 0 || lBandSrc.Height <= 0 ||
                rBandSrc.Width <= 0 || rBandSrc.Height <= 0) return;

            // Реальная ширина смеси = сумма фактических полос (а не 2*blendW «в лоб»!)
            int bw = lBandSrc.Width + rBandSrc.Width;
            if (bw <= 0) return;

            int outX = destRight.X - lBandSrc.Width;          // половина смеси уходит на левую колонку
            var outRect = new Rectangle(outX, top, bw, Math.Min(lBandSrc.Height, rBandSrc.Height));

            using var lBand = leftBmp.Clone(lBandSrc, PixelFormat.Format24bppRgb);
            using var rBand = rightBmp.Clone(rBandSrc, PixelFormat.Format24bppRgb);
            using var band = new Bitmap(outRect.Width, outRect.Height, PixelFormat.Format24bppRgb);

            BitmapData ld = null, rd = null, bd = null;
            try
            {
                ld = lBand.LockBits(new Rectangle(0, 0, lBand.Width, lBand.Height),
                                    ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                rd = rBand.LockBits(new Rectangle(0, 0, rBand.Width, rBand.Height),
                                    ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                bd = band.LockBits(new Rectangle(0, 0, band.Width, band.Height),
                                   ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                unsafe
                {
                    byte* lp = (byte*)ld.Scan0;
                    byte* rp = (byte*)rd.Scan0;
                    byte* bp = (byte*)bd.Scan0;
                    int ls = ld.Stride, rs = rd.Stride, bs = bd.Stride;

                    int lw = lBand.Width;
                    int rw = rBand.Width;

                    for (int y = 0; y < band.Height; y++)
                    {
                        byte* lrow = lp + y * ls;
                        byte* rrow = rp + y * rs;
                        byte* brow = bp + y * bs;

                        for (int x = 0; x < bw; x++)
                        {
                            // x в [0..bw-1]; нормализуем 0..1
                            float t = (bw <= 1) ? 1f : (x / (float)(bw - 1));
                            if (gamma != 1f) t = (float)Math.Pow(t, gamma);
                            float s = 1f - t;

                            // индекс для левой полосы берём "с конца" (край) и клампим
                            int lxIdx = Math.Max(0, Math.Min(lw - 1, lw - 1 - x));
                            // индекс для правой полосы растёт слева направо и клампим
                            int rxIdx = Math.Max(0, Math.Min(rw - 1, x - (bw - rw)));

                            // если x попал полностью в зону левой/правой — корректно клампим индексы
                            if (x < lw)
                                rxIdx = 0; // доходим до контакта — правая полоса ещё не началась
                            if (x >= bw - rw)
                                lxIdx = 0; // уход направо — левая полоса закончилась

                            int lo = 3 * lxIdx;
                            int ro = 3 * Math.Max(0, Math.Min(rw - 1, rxIdx));

                            // B,G,R
                            brow[3 * x + 0] = (byte)Math.Clamp(s * lrow[lo + 0] + t * rrow[ro + 0], 0, 255);
                            brow[3 * x + 1] = (byte)Math.Clamp(s * lrow[lo + 1] + t * rrow[ro + 1], 0, 255);
                            brow[3 * x + 2] = (byte)Math.Clamp(s * lrow[lo + 2] + t * rrow[ro + 2], 0, 255);
                        }
                    }
                }
            }
            finally
            {
                if (ld != null) lBand.UnlockBits(ld);
                if (rd != null) rBand.UnlockBits(rd);
                if (bd != null) band.UnlockBits(bd);
            }

            g.DrawImage(band, outRect);
        }

        /// Рисует тайл так, чтобы overlapX/overlapY не рисовались дважды (устраняет шов «в корне»).
        public static void DrawTileWithoutOverlap(
            Graphics g, Bitmap tile,
            int col, int row,
            int tileW, int tileH,
            int overlapX, int overlapY)
        {
            int srcX = (col > 0) ? overlapX : 0;
            int srcY = (row > 0) ? overlapY : 0;
            int srcW = tileW - srcX;
            int srcH = tileH - srcY;
            if (srcW <= 0 || srcH <= 0) return;

            int stepX = tileW - overlapX;
            int stepY = tileH - overlapY;

            int destX = col * stepX;
            int destY = row * stepY;

            var src = new Rectangle(srcX, srcY, srcW, srcH);
            var dst = new Rectangle(destX, destY, srcW, srcH);

            g.DrawImage(tile, dst, src, GraphicsUnit.Pixel);
        }
    }
}
