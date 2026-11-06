using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace ImageToIco
{
    public static class IcoWriter
    {
        public static Bitmap MakeSquare(Bitmap src, int size, int radiusPercent)
        {
            Bitmap dst = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(dst))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.Clear(Color.Transparent);

                float scale = Math.Min((float)size / src.Width, (float)size / src.Height);
                float w = src.Width * scale;
                float h = src.Height * scale;
                RectangleF rect = new RectangleF((size - w) / 2f, (size - h) / 2f, w, h);

                int pct = Math.Max(0, Math.Min(50, radiusPercent));
                if (pct > 0)
                {
                    float r = size * pct / 100f;
                    using (GraphicsPath clip = RoundedRect(new RectangleF(0, 0, size, size), r))
                    {
                        g.SetClip(clip);
                    }
                }
                g.DrawImage(src, rect);
            }
            return dst;
        }

        static GraphicsPath RoundedRect(RectangleF r, float radius)
        {
            GraphicsPath p = new GraphicsPath();
            float d = Math.Min(radius * 2f, Math.Min(r.Width, r.Height));
            if (d <= 0.2f)
            {
                p.AddRectangle(r);
                return p;
            }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void Save(string path, Bitmap source, IList<int> sizes, int radiusPercent)
        {
            if (sizes == null || sizes.Count == 0) throw new ArgumentException("没有选择尺寸");

            List<byte[]> frames = new List<byte[]>();
            List<int> dims = new List<int>();
            List<int> ordered = new List<int>(sizes);
            ordered.Sort();

            foreach (int s in ordered)
            {
                using (Bitmap bmp = MakeSquare(source, s, radiusPercent))
                {
                    frames.Add(s >= 128 ? EncodePng(bmp) : EncodeDib(bmp));
                    dims.Add(s);
                }
            }

            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (BinaryWriter w = new BinaryWriter(fs))
            {
                w.Write((ushort)0);
                w.Write((ushort)1);
                w.Write((ushort)frames.Count);
                int offset = 6 + 16 * frames.Count;
                for (int i = 0; i < frames.Count; i++)
                {
                    byte b = dims[i] >= 256 ? (byte)0 : (byte)dims[i];
                    w.Write(b);
                    w.Write(b);
                    w.Write((byte)0);
                    w.Write((byte)0);
                    w.Write((ushort)1);
                    w.Write((ushort)32);
                    w.Write(frames[i].Length);
                    w.Write(offset);
                    offset += frames[i].Length;
                }
                for (int i = 0; i < frames.Count; i++) w.Write(frames[i]);
            }
        }

        static byte[] EncodePng(Bitmap bmp)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }

        static byte[] EncodeDib(Bitmap bmp)
        {
            int w = bmp.Width;
            int h = bmp.Height;
            int xorSize = w * h * 4;
            int maskStride = ((w + 31) / 32) * 4;
            int maskSize = maskStride * h;

            byte[] outBytes = new byte[40 + xorSize + maskSize];
            using (MemoryStream ms = new MemoryStream(outBytes))
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(40);
                bw.Write(w);
                bw.Write(h * 2);
                bw.Write((ushort)1);
                bw.Write((ushort)32);
                bw.Write(0);
                bw.Write(xorSize + maskSize);
                bw.Write(0);
                bw.Write(0);
                bw.Write(0);
                bw.Write(0);
            }

            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                int rowBytes = w * 4;
                byte[] row = new byte[rowBytes];
                for (int y = 0; y < h; y++)
                {
                    IntPtr src = new IntPtr(bd.Scan0.ToInt64() + (long)(h - 1 - y) * bd.Stride);
                    Marshal.Copy(src, row, 0, rowBytes);
                    Buffer.BlockCopy(row, 0, outBytes, 40 + y * rowBytes, rowBytes);
                }
            }
            finally
            {
                bmp.UnlockBits(bd);
            }
            return outBytes;
        }
    }
}