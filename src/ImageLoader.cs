using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ImageToIco
{
    public static class ImageLoader
    {
        public static readonly string[] Extensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".ico", ".webp", ".tif", ".tiff" };

        public static bool IsSupported(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return Array.IndexOf(Extensions, ext) >= 0;
        }

        public static Bitmap Load(string path)
        {
            Exception first = null;
            try
            {
                return LoadWithGdi(path);
            }
            catch (Exception ex)
            {
                first = ex;
            }

            try
            {
                return LoadWithWpf(path);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "无法解码这张图片（" + Path.GetExtension(path) + "）。\n"
                    + "如果是 WebP，系统需要安装「WebP 图像扩展」。\n\n"
                    + "GDI+：" + first.Message + "\nWIC：" + ex.Message);
            }
        }

        static Bitmap LoadWithGdi(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (Image img = Image.FromStream(fs, true, true))
            {
                Bitmap bmp = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height));
                }
                return bmp;
            }
        }

        static Bitmap LoadWithWpf(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                System.Windows.Media.Imaging.BitmapDecoder decoder =
                    System.Windows.Media.Imaging.BitmapDecoder.Create(
                        fs,
                        System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat,
                        System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);

                if (decoder.Frames.Count == 0) throw new InvalidOperationException("图片没有可用帧");

                System.Windows.Media.Imaging.BitmapSource frame = decoder.Frames[0];
                System.Windows.Media.Imaging.PngBitmapEncoder encoder =
                    new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(frame));

                using (MemoryStream ms = new MemoryStream())
                {
                    encoder.Save(ms);
                    ms.Position = 0;
                    using (Image img = Image.FromStream(ms, true, true))
                    {
                        Bitmap bmp = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                            g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height));
                        }
                        return bmp;
                    }
                }
            }
        }
    }
}