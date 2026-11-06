using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ImageToIco
{
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length > 0 && (args[0] == "--convert" || args[0] == "-c"))
            {
                return RunCli(args);
            }

            if (args.Length > 1 && args[0] == "--snapshot")
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                MainForm f = new MainForm();
                f.StartPosition = FormStartPosition.Manual;
                f.Location = new Point(-4000, -4000);
                f.Show();
                Application.DoEvents();
                using (Bitmap bmp = new Bitmap(f.Width, f.Height))
                {
                    f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
                    bmp.Save(args[1], System.Drawing.Imaging.ImageFormat.Png);
                }
                f.Close();
                return 0;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            return 0;
        }

        static int RunCli(string[] args)
        {
            try
            {
                string input = null;
                string output = null;
                List<int> sizes = new List<int>();
                int radius = 15;

                for (int i = 1; i < args.Length; i++)
                {
                    string a = args[i];
                    if (a == "--sizes" && i + 1 < args.Length)
                    {
                        i++;
                        foreach (string part in args[i].Split(','))
                        {
                            int v;
                            if (int.TryParse(part.Trim(), out v) && v > 0 && v <= 256) sizes.Add(v);
                        }
                    }
                    else if (a == "--radius" && i + 1 < args.Length)
                    {
                        i++;
                        int.TryParse(args[i], out radius);
                    }
                    else if (input == null) input = a;
                    else if (output == null) output = a;
                }

                if (sizes.Count == 0) sizes.AddRange(new int[] { 16, 24, 32, 48, 64, 128, 256 });
                if (input == null)
                {
                    Console.Error.WriteLine("用法: ImageToIco.exe --convert <输入图片> [输出.ico] [--sizes 16,32,256] [--radius 15]");
                    return 2;
                }
                if (!File.Exists(input))
                {
                    Console.Error.WriteLine("找不到文件: " + input);
                    return 3;
                }
                if (new FileInfo(input).Length > MainForm.MaxFileBytes)
                {
                    Console.Error.WriteLine("文件超过 100 MB 上限: " + input);
                    return 4;
                }
                if (output == null)
                {
                    output = Path.Combine(Path.GetDirectoryName(input), Path.GetFileNameWithoutExtension(input) + ".ico");
                }

                using (Bitmap bmp = ImageLoader.Load(input))
                {
                    IcoWriter.Save(output, bmp, sizes, radius);
                }

                Console.WriteLine("已生成: " + output + "  (" + sizes.Count + " 个尺寸, 圆角 " + radius + "%)");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("失败: " + ex.Message);
                return 1;
            }
        }
    }
}