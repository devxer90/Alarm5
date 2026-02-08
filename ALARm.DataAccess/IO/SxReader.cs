using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;

namespace AlarmPP.IO
{
    /// <summary>
    /// Читатель кадров *.s7 (или совместимых «сырых» 8bpp) с мета-строкой.
    /// Совместим с C# 7.3 / .NET Framework: никаких using-declaration, local functions, Span и т.п.
    /// </summary>
    public static class SxReader
    {
        public interface IFrameSource
        {
            // Возвращает Bitmap + timestamp(ns) для конкретного файла и индекса кадра
            (Bitmap bmp, long tsNs) Read(string path, int frameIndex);
        }

        public sealed class SxFrameSource : IFrameSource
        {
            public (Bitmap bmp, long tsNs) Read(string path, int frameIndex)
            {
                var bmp = SxReader.ReadFrameBitmap(path, frameIndex);
                var tsNs = SxReader.ReadTimestampNs(path, frameIndex);
                return (bmp, tsNs);
            }
        }

        /// <summary>Затирать первую строку кадра (там лежит мета-строка)</summary>
        public const bool ZeroMetaRow = true;

        private enum Endian { Big, Little }

        private sealed class FileInfoRec
        {
            public ushort Cols;            // ширина
            public ushort Rows;            // высота
            public byte Bpp;             // ожидаем 8
            public int HeaderSkip;      // доп.байт перед BPP (0 или 1)
            public long BaseUnixMs;      // базовое время из имени файла
            public Endian HeaderEndian;    // порядок байт в заголовке
            public Endian MetaEndian;      // порядок байт в мета-строке кадра
            public int FramePrefixBytes;// префикс перед пикселями (обычно 0)
        }

        private static readonly ConcurrentDictionary<string, FileInfoRec> _infoCache =
            new ConcurrentDictionary<string, FileInfoRec>(StringComparer.OrdinalIgnoreCase);

        // --------------------------- публичные методы ---------------------------

        /// <summary>Читает время кадра (наносекунды, отсчёт от эпохи UNIX).</summary>
        public static long ReadTimestampNs(string path, int frameIndex)
        {
            var fi = GetFileInfo(path);
            if (fi.Cols == 0 || fi.Rows == 0) return 0;

            long headerSize = 4 + fi.HeaderSkip + 1; // cols(2)+rows(2)+[skip]+bpp(1)
            long pixels = (long)fi.Cols * fi.Rows * (fi.Bpp / 8);
            long frameSize = fi.FramePrefixBytes + pixels;
            long frameOff = headerSize + frameIndex * frameSize;

            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var br = new BinaryReader(fs))
                {
                    fs.Seek(frameOff, SeekOrigin.Begin);

                    // читаем первые 16 байт (мета)
                    byte[] meta = br.ReadBytes(16);
                    if (meta.Length < 16) return 0;

                    // meta: [0..7] = сигнатура, [8..11] = TC, [12..15] = TS (ms from base)
                    uint tsMsFromBase = (fi.MetaEndian == Endian.Big)
                        ? ReadUInt32BE(meta, 12)
                        : ReadUInt32LE(meta, 12);

                    long absMs = fi.BaseUnixMs + tsMsFromBase;
                    return absMs * 1_000_000L;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[SxReader] ReadTimestampNs error: " + ex.Message);
                return 0;
            }
        }

        /// <summary>Читает кадр как 8bpp Bitmap. При сбое возвращает заглушку с текстом ошибки.</summary>
        public static Bitmap ReadFrameBitmap(string path, int frameIndex)
        {
            var fi = GetFileInfo(path);
            if (fi.Cols <= 0 || fi.Rows <= 0 || fi.Bpp != 8)
            {
                return MakeErrorBitmap("Header invalid (cols/rows/bpp).");
            }

            long headerSize = 4 + fi.HeaderSkip + 1;
            long pixels = (long)fi.Cols * fi.Rows * (fi.Bpp / 8);
            long frameSize = fi.FramePrefixBytes + pixels;
            long frameOff = headerSize + frameIndex * frameSize;

            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var br = new BinaryReader(fs))
                {
                    fs.Seek(frameOff, SeekOrigin.Begin);

                    // пропускаем префикс, если есть
                    if (fi.FramePrefixBytes > 0)
                    {
                        int skipped = br.Read(new byte[fi.FramePrefixBytes], 0, fi.FramePrefixBytes);
                        if (skipped < fi.FramePrefixBytes)
                            return MakeErrorBitmap("Frame prefix truncated.");
                    }

                    // читаем пиксели
                    byte[] buf = br.ReadBytes((int)pixels);
                    if (buf.Length < pixels)
                        return MakeErrorBitmap("Frame truncated.");

                    // затрём первую строку (мета) — чтобы не светилась
                    if (ZeroMetaRow && fi.Cols > 0 && buf.Length >= fi.Cols)
                    {
                        for (int i = 0; i < fi.Cols; i++) buf[i] = 0;
                    }

                    // формируем 8bpp bitmap
                    var bmp = new Bitmap(fi.Cols, fi.Rows, PixelFormat.Format8bppIndexed);

                    // палитра «градации серого»
                    ColorPalette pal = bmp.Palette;
                    for (int i = 0; i < 256; i++)
                        pal.Entries[i] = Color.FromArgb(i, i, i);
                    bmp.Palette = pal;

                    // копируем построчно с учётом stride
                    var rect = new Rectangle(0, 0, fi.Cols, fi.Rows);
                    BitmapData data = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
                    try
                    {
                        int stride = data.Stride;
                        int width = fi.Cols;
                        int height = fi.Rows;

                        int srcOffset = 0;
                        for (int y = 0; y < height; y++)
                        {
                            IntPtr rowPtr = new IntPtr(data.Scan0.ToInt64() + y * (long)stride);
                            System.Runtime.InteropServices.Marshal.Copy(buf, srcOffset, rowPtr, width);
                            srcOffset += width;
                        }
                    }
                    finally
                    {
                        bmp.UnlockBits(data);
                    }

                    return bmp;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[SxReader] ReadFrameBitmap error: " + ex.Message);
                return MakeErrorBitmap(ex.Message);
            }
        }

        // --------------------------- внутренности ---------------------------

        private static FileInfoRec GetFileInfo(string path)
        {
            return _infoCache.GetOrAdd(path, delegate (string p)
            {
                var rec = new FileInfoRec();
                rec.Cols = 0;
                rec.Rows = 0;
                rec.Bpp = 8;
                rec.HeaderSkip = 0;
                rec.FramePrefixBytes = 0;
                rec.HeaderEndian = Endian.Big;
                rec.MetaEndian = Endian.Big;
                rec.BaseUnixMs = 0;

                // 1) читаем заголовок (с запасом)
                try
                {
                    using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var br = new BinaryReader(fs))
                    {
                        byte[] head = br.ReadBytes(16);
                        if (head.Length < 5)
                            throw new EndOfStreamException("Header too short (<5).");

                        // попробуем четыре варианта: (BE/LE) × (skip 0/1)
                        ushort cols = 0, rows = 0;
                        byte bpp = 0;
                        bool ok = false;
                        Endian chosenEnd = Endian.Big;
                        int chosenSkip = 0;

                        for (int variant = 0; variant < 4; variant++)
                        {
                            Endian e = (variant == 0 || variant == 2) ? Endian.Big : Endian.Little;
                            int skip = (variant >= 2) ? 1 : 0;

                            if (head.Length < 4 + 1 + skip) continue;

                            cols = (e == Endian.Big) ? ReadUInt16BE(head, 0) : ReadUInt16LE(head, 0);
                            rows = (e == Endian.Big) ? ReadUInt16BE(head, 2) : ReadUInt16LE(head, 2);
                            bpp = head[4 + skip];

                            if (cols > 0 && rows > 0)
                            {
                                chosenEnd = e;
                                chosenSkip = skip;
                                ok = true;
                                if (bpp == 8) break; // идеальный вариант
                            }
                        }

                        if (!ok)
                            throw new InvalidDataException("Cannot parse cols/rows in header.");

                        if (bpp != 8) bpp = 8; // принудительно 8bpp

                        rec.HeaderEndian = chosenEnd;
                        rec.HeaderSkip = chosenSkip;
                        rec.Cols = cols;
                        rec.Rows = rows;
                        rec.Bpp = bpp;

                        // 2) базовое время из имени файла: yyyy_MM_dd__HH_mm_ss
                        try
                        {
                            string name = Path.GetFileNameWithoutExtension(p);
                            int dot = name.IndexOf('.');
                            if (dot > 0) name = name.Substring(0, dot);

                            DateTime dt;
                            if (DateTime.TryParseExact(
                                    name,
                                    "yyyy_MM_dd__HH_mm_ss",
                                    CultureInfo.InvariantCulture,
                                    DateTimeStyles.AssumeUniversal,
                                    out dt))
                            {
                                rec.BaseUnixMs = new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc))
                                                    .ToUnixTimeMilliseconds();
                            }
                            else
                            {
                                // имя «776_...»: базы нет — 0
                                rec.BaseUnixMs = 0;
                                Debug.WriteLine("[SxReader] Date parse failed, use baseUnixMs=0 for: " + name);
                            }
                        }
                        catch (Exception exName)
                        {
                            rec.BaseUnixMs = 0;
                            Debug.WriteLine("[SxReader] Date parse exception: " + exName.Message);
                        }

                        // 3) попытка определить энд-иан мета-строки по первому кадру
                        try
                        {
                            long headerSize = 4 + rec.HeaderSkip + 1;
                            fs.Seek(headerSize, SeekOrigin.Begin);

                            byte[] meta = br.ReadBytes(16);
                            if (meta.Length == 16)
                            {
                                bool signatureOk =
                                    meta[0] == 0xF5 && meta[1] == 0x32 &&
                                    meta[2] == 0xF5 && meta[3] == 0x32 &&
                                    meta[4] == 0xF5 && meta[5] == 0x32 &&
                                    meta[6] == 0xF5 && meta[7] == 0x32;

                                if (signatureOk)
                                {
                                    uint tsBE = ReadUInt32BE(meta, 12);
                                    uint tsLE = ReadUInt32LE(meta, 12);
                                    const uint tenDaysMs = 10u * 24u * 60u * 60u * 1000u;

                                    if (tsBE > 0 && tsBE < tenDaysMs) rec.MetaEndian = Endian.Big;
                                    else if (tsLE > 0 && tsLE < tenDaysMs) rec.MetaEndian = Endian.Little;
                                    else rec.MetaEndian = Endian.Big; // по умолчанию
                                }
                            }
                        }
                        catch (Exception exMeta)
                        {
                            Debug.WriteLine("[SxReader] meta-endian probe failed: " + exMeta.Message);
                            rec.MetaEndian = Endian.Big;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[SxReader] GetFileInfo error: " + ex.Message);
                    // если заголовок не прочитали — всё равно вернём rec c нулями,
                    // вызывающая сторона получит заглушку при чтении кадра.
                }

                return rec;
            });
        }

        // --------------------------- служебные вещи ---------------------------

        private static Bitmap MakeErrorBitmap(string message, int w = 320, int h = 200)
        {
            try
            {
                var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(bmp))
                using (var font = new Font("Consolas", 11, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.Red))
                {
                    g.Clear(Color.Black);
                    g.DrawString(message ?? "error", font, brush, new RectangleF(5, 5, w - 10, h - 10));
                }
                return bmp;
            }
            catch
            {
                // худший случай
                return new Bitmap(2, 2);
            }
        }

        private static ushort ReadUInt16BE(byte[] a, int ofs)
        {
            if (a == null || a.Length < ofs + 2) return 0;
            return (ushort)((a[ofs] << 8) | a[ofs + 1]);
        }

        private static ushort ReadUInt16LE(byte[] a, int ofs)
        {
            if (a == null || a.Length < ofs + 2) return 0;
            return (ushort)(a[ofs] | (a[ofs + 1] << 8));
        }

        private static uint ReadUInt32BE(byte[] a, int ofs)
        {
            if (a == null || a.Length < ofs + 4) return 0;
            return (uint)((a[ofs] << 24) | (a[ofs + 1] << 16) | (a[ofs + 2] << 8) | a[ofs + 3]);
        }

        private static uint ReadUInt32LE(byte[] a, int ofs)
        {
            if (a == null || a.Length < ofs + 4) return 0;
            return (uint)(a[ofs] | (a[ofs + 1] << 8) | (a[ofs + 2] << 16) | (a[ofs + 3] << 24));
        }
    }
}
