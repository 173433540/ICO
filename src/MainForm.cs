using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace ImageToIco
{
    public class MainForm : Form
    {
        static readonly Color Accent = Color.FromArgb(66, 185, 131);
        static readonly Color AccentDark = Color.FromArgb(58, 168, 118);
        static readonly Color Ink = Color.FromArgb(44, 62, 80);
        static readonly Color Muted = Color.FromArgb(153, 153, 153);
        static readonly Color Line = Color.FromArgb(226, 226, 226);
        static readonly Color Soft = Color.FromArgb(250, 250, 250);
        static readonly int[] SizeOptions = { 16, 24, 32, 48, 64, 128, 256 };

        PictureBox iconBox;
        ListView list;
        ImageList thumbs = new ImageList();
        DropZone drop;
        FlowLayoutPanel chipPanel;
        readonly List<SizeChip> chips = new List<SizeChip>();
        Button selectAll;
        NumericUpDown radiusBox;
        TrackBar radiusBar;
        RadioButton rdoSource;
        RadioButton rdoCustom;
        TextBox outBox;
        Button browseBtn;
        Button convertBtn;
        Label status;
        bool busy;

        public MainForm()
        {
            Text = "图片转 ICO  v" + ShortVersion();
            ClientSize = new Size(920, 636);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.White;
            Font = UiFont(9f, FontStyle.Regular);
            AllowDrop = true;
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }

            BuildHeader();
            BuildDropZone();
            BuildList();
            BuildOptions();
            BuildFooter();

            DragEnter += FormDragEnter;
            DragLeave += delegate { drop.Hot = false; drop.Invalidate(); };
            DragDrop += FormDragDrop;
        }

        static Font UiFont(float size, FontStyle style)
        {
            try { return new Font("Microsoft YaHei UI", size, style); }
            catch { return new Font("Microsoft YaHei", size, style); }
        }

        static string ShortVersion()
        {
            Version v = typeof(MainForm).Assembly.GetName().Version;
            return v.Major + "." + v.Minor + "." + v.Build;
        }

        void BuildHeader()
        {
            iconBox = new PictureBox();
            iconBox.Bounds = new Rectangle(32, 24, 48, 48);
            iconBox.SizeMode = PictureBoxSizeMode.Zoom;
            try
            {
                using (Icon ico = Icon.ExtractAssociatedIcon(Application.ExecutablePath))
                {
                    if (ico != null) iconBox.Image = ico.ToBitmap();
                }
            }
            catch { }
            Controls.Add(iconBox);

            Label title = new Label();
            title.Text = "图片转 ICO";
            title.Font = UiFont(16f, FontStyle.Bold);
            title.ForeColor = Ink;
            title.AutoSize = true;
            title.Location = new Point(94, 22);
            Controls.Add(title);

            Label sub = new Label();
            sub.Text = "将图片转换为图标格式，支持自定义尺寸和圆角";
            sub.Font = UiFont(9f, FontStyle.Regular);
            sub.ForeColor = Muted;
            sub.AutoSize = true;
            sub.Location = new Point(96, 56);
            Controls.Add(sub);

            Panel sep = new Panel();
            sep.Bounds = new Rectangle(32, 92, 856, 1);
            sep.BackColor = Color.FromArgb(240, 240, 240);
            Controls.Add(sep);
        }

        void BuildDropZone()
        {
            drop = new DropZone();
            drop.Bounds = new Rectangle(32, 108, 856, 146);
            drop.Browse += OpenFiles;
            drop.FilesDropped += AddFiles;
            Controls.Add(drop);
        }

        void BuildList()
        {
            thumbs.ImageSize = new Size(32, 32);
            thumbs.ColorDepth = ColorDepth.Depth32Bit;

            Label caption = new Label();
            caption.Text = "待转换图片";
            caption.Font = UiFont(9f, FontStyle.Regular);
            caption.ForeColor = Ink;
            caption.AutoSize = true;
            caption.Location = new Point(32, 266);
            Controls.Add(caption);

            Button clear = new Button();
            clear.Text = "清空";
            clear.FlatStyle = FlatStyle.Flat;
            clear.FlatAppearance.BorderSize = 0;
            clear.BackColor = Color.White;
            clear.ForeColor = Muted;
            clear.Font = UiFont(9f, FontStyle.Regular);
            clear.Bounds = new Rectangle(816, 260, 72, 24);
            clear.Cursor = Cursors.Hand;
            clear.Click += delegate { ClearList(); };
            Controls.Add(clear);

            list = new ListView();
            list.Bounds = new Rectangle(32, 288, 856, 128);
            list.View = View.Details;
            list.FullRowSelect = true;
            list.MultiSelect = true;
            list.BorderStyle = BorderStyle.FixedSingle;
            list.SmallImageList = thumbs;
            list.ShowItemToolTips = true;
            list.Font = UiFont(9f, FontStyle.Regular);
            list.Columns.Add("文件", 470);
            list.Columns.Add("大小", 110);
            list.Columns.Add("状态", 250);
            list.DragEnter += FormDragEnter;
            list.DragDrop += FormDragDrop;
            list.KeyDown += delegate (object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Delete) RemoveSelected();
            };
            Controls.Add(list);
        }

        void BuildOptions()
        {
            Label lblSizes = MakeLabel("目标尺寸", 32, 434);
            Controls.Add(lblSizes);

            chipPanel = new FlowLayoutPanel();
            chipPanel.Bounds = new Rectangle(112, 428, 620, 30);
            chipPanel.FlowDirection = FlowDirection.LeftToRight;
            chipPanel.WrapContents = false;
            chipPanel.BackColor = Color.White;
            Controls.Add(chipPanel);

            foreach (int s in SizeOptions)
            {
                SizeChip chip = new SizeChip(s + "", Accent, Ink, Line);
                chip.Checked = true;
                chipPanel.Controls.Add(chip);
                chips.Add(chip);
            }

            selectAll = new Button();
            selectAll.Text = "全选";
            selectAll.FlatStyle = FlatStyle.Flat;
            selectAll.FlatAppearance.BorderSize = 0;
            selectAll.BackColor = Color.White;
            selectAll.ForeColor = Accent;
            selectAll.Font = UiFont(9f, FontStyle.Regular);
            selectAll.Size = new Size(56, 28);
            selectAll.Cursor = Cursors.Hand;
            selectAll.Click += delegate { ToggleAll(); };
            chipPanel.Controls.Add(selectAll);

            Label lblRadius = MakeLabel("圆角半径", 32, 476);
            Controls.Add(lblRadius);

            radiusBox = new NumericUpDown();
            radiusBox.Bounds = new Rectangle(112, 472, 64, 26);
            radiusBox.Minimum = 0;
            radiusBox.Maximum = 50;
            radiusBox.Value = 0;
            radiusBox.TextAlign = HorizontalAlignment.Center;
            radiusBox.Font = UiFont(9f, FontStyle.Regular);
            radiusBox.ValueChanged += delegate
            {
                if (radiusBar.Value != (int)radiusBox.Value) radiusBar.Value = (int)radiusBox.Value;
            };
            Controls.Add(radiusBox);

            radiusBar = new TrackBar();
            radiusBar.Bounds = new Rectangle(186, 470, 240, 32);
            radiusBar.Minimum = 0;
            radiusBar.Maximum = 50;
            radiusBar.TickFrequency = 5;
            radiusBar.Value = 0;
            radiusBar.SmallChange = 1;
            radiusBar.LargeChange = 5;
            radiusBar.Scroll += delegate { radiusBox.Value = radiusBar.Value; };
            Controls.Add(radiusBar);

            Label pct = new Label();
            pct.Text = "% （0 = 直角，越大越圆）";
            pct.ForeColor = Muted;
            pct.AutoSize = true;
            pct.Location = new Point(432, 478);
            Controls.Add(pct);

            Label lblOut = MakeLabel("输出位置", 32, 518);
            Controls.Add(lblOut);

            rdoSource = new RadioButton();
            rdoSource.Text = "图片所在文件夹";
            rdoSource.Checked = true;
            rdoSource.AutoSize = true;
            rdoSource.ForeColor = Ink;
            rdoSource.Location = new Point(112, 514);
            rdoSource.CheckedChanged += delegate { SyncOutputMode(); };
            Controls.Add(rdoSource);

            rdoCustom = new RadioButton();
            rdoCustom.Text = "指定文件夹";
            rdoCustom.AutoSize = true;
            rdoCustom.ForeColor = Ink;
            rdoCustom.Location = new Point(248, 514);
            rdoCustom.CheckedChanged += delegate { SyncOutputMode(); };
            Controls.Add(rdoCustom);

            outBox = new TextBox();
            outBox.Bounds = new Rectangle(352, 512, 380, 26);
            outBox.ReadOnly = true;
            outBox.BackColor = Color.White;
            outBox.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(outBox);

            browseBtn = new Button();
            browseBtn.Text = "浏览...";
            browseBtn.Bounds = new Rectangle(742, 510, 146, 30);
            browseBtn.FlatStyle = FlatStyle.Flat;
            browseBtn.FlatAppearance.BorderColor = Line;
            browseBtn.BackColor = Color.White;
            browseBtn.ForeColor = Ink;
            browseBtn.Cursor = Cursors.Hand;
            browseBtn.Click += delegate { BrowseOutput(); };
            Controls.Add(browseBtn);

            SyncOutputMode();
        }

        void BuildFooter()
        {
            convertBtn = new Button();
            convertBtn.Text = "生成 ICO 图标";
            convertBtn.Bounds = new Rectangle(32, 564, 220, 46);
            convertBtn.FlatStyle = FlatStyle.Flat;
            convertBtn.FlatAppearance.BorderSize = 0;
            convertBtn.BackColor = Accent;
            convertBtn.ForeColor = Color.White;
            convertBtn.Font = UiFont(11f, FontStyle.Bold);
            convertBtn.Cursor = Cursors.Hand;
            convertBtn.FlatAppearance.MouseOverBackColor = AccentDark;
            convertBtn.Click += delegate { ConvertAll(); };
            Controls.Add(convertBtn);

            status = new Label();
            status.Text = "";
            status.ForeColor = Muted;
            status.AutoSize = true;
            status.Location = new Point(268, 580);
            Controls.Add(status);
        }

        Label MakeLabel(string text, int x, int y)
        {
            Label l = new Label();
            l.Text = text;
            l.AutoSize = true;
            l.ForeColor = Ink;
            l.Location = new Point(x, y);
            return l;
        }

        void SyncOutputMode()
        {
            bool custom = rdoCustom.Checked;
            outBox.Enabled = custom;
            browseBtn.Enabled = custom;
            outBox.BackColor = custom ? Color.White : Color.FromArgb(248, 248, 248);
        }

        void BrowseOutput()
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "选择 ICO 输出文件夹";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    outBox.Text = dlg.SelectedPath;
                    rdoCustom.Checked = true;
                }
            }
        }

        void ToggleAll()
        {
            if (busy) return;
            bool allOn = true;
            foreach (SizeChip c in chips)
            {
                if (!c.Checked) { allOn = false; break; }
            }
            foreach (SizeChip c in chips) c.SetChecked(!allOn);
        }

        void OpenFiles()
        {
            if (busy) return;
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "选择图片";
                dlg.Multiselect = true;
                dlg.Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.ico;*.webp;*.tif;*.tiff|所有文件|*.*";
                if (dlg.ShowDialog(this) == DialogResult.OK) AddFiles(dlg.FileNames);
            }
        }

        void FormDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
                drop.Hot = true;
                drop.Invalidate();
            }
        }

        void FormDragDrop(object sender, DragEventArgs e)
        {
            drop.Hot = false;
            drop.Invalidate();
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null) AddFiles(files);
        }

        public const long MaxFileBytes = 100L * 1024 * 1024;

        public void AddFiles(string[] paths)
        {
            if (paths == null || busy) return;
            int added = 0;
            int skipped = 0;
            int tooBig = 0;
            int dup = 0;

            HashSet<string> existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ListViewItem row in list.Items)
            {
                string known = row.Tag as string;
                if (known != null) existing.Add(known);
            }

            foreach (string raw in paths)
            {
                string path = raw;
                if (Directory.Exists(path))
                {
                    string[] files;
                    try { files = Directory.GetFiles(path); }
                    catch { skipped++; continue; }
                    foreach (string f in files)
                    {
                        if (!ImageLoader.IsSupported(f)) continue;
                        if (TooBig(f)) { tooBig++; continue; }
                        if (!existing.Add(f)) { dup++; continue; }
                        AddOne(f);
                        added++;
                    }
                    continue;
                }
                if (!File.Exists(path)) continue;
                if (!ImageLoader.IsSupported(path)) { skipped++; continue; }
                if (TooBig(path)) { tooBig++; continue; }
                if (!existing.Add(path)) { dup++; continue; }
                AddOne(path);
                added++;
            }

            string msg = "已添加 " + added + " 张";
            if (dup > 0) msg += "，" + dup + " 张已在列表中";
            if (skipped > 0) msg += "，跳过 " + skipped + " 个不支持的格式或无法访问的文件夹";
            if (tooBig > 0) msg += "，跳过 " + tooBig + " 个超过 100 MB 的文件";
            status.Text = msg;
        }

        static bool TooBig(string path)
        {
            try { return new FileInfo(path).Length > MaxFileBytes; }
            catch { return false; }
        }

        void AddOne(string path)
        {
            string name = Path.GetFileName(path);
            long size = 0;
            try { size = new FileInfo(path).Length; } catch { }

            int imageIndex = -1;
            try
            {
                using (Bitmap bmp = ImageLoader.Load(path))
                {
                    using (Bitmap thumb = new Bitmap(32, 32))
                    {
                        using (Graphics g = Graphics.FromImage(thumb))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.Clear(Color.Transparent);
                            float scale = Math.Min(32f / bmp.Width, 32f / bmp.Height);
                            int w = Math.Max(1, (int)(bmp.Width * scale));
                            int h = Math.Max(1, (int)(bmp.Height * scale));
                            g.DrawImage(bmp, new Rectangle((32 - w) / 2, (32 - h) / 2, w, h));
                        }
                        thumbs.Images.Add(thumb);
                        imageIndex = thumbs.Images.Count - 1;
                    }
                }
            }
            catch
            {
                imageIndex = -1;
            }

            ListViewItem row = new ListViewItem(name, imageIndex);
            row.SubItems.Add(FormatSize(size));
            row.SubItems.Add("待转换");
            row.Tag = path;
            if (imageIndex < 0) row.ForeColor = Color.FromArgb(120, 120, 120);
            list.Items.Add(row);
        }

        void RemoveSelected()
        {
            if (busy) return;
            foreach (ListViewItem row in list.SelectedItems)
            {
                list.Items.Remove(row);
                if (row.ImageIndex >= 0 && row.ImageIndex < thumbs.Images.Count)
                {
                    thumbs.Images.RemoveAt(row.ImageIndex);
                    for (int i = 0; i < list.Items.Count; i++)
                    {
                        if (list.Items[i].ImageIndex > row.ImageIndex) list.Items[i].ImageIndex--;
                    }
                }
            }
            status.Text = "已移除选中项";
        }

        void ClearList()
        {
            if (busy) return;
            list.Items.Clear();
            thumbs.Images.Clear();
            status.Text = "列表已清空";
        }

        static string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("0.0") + " KB";
            return (bytes / 1024.0 / 1024.0).ToString("0.00") + " MB";
        }

        List<int> SelectedSizes()
        {
            List<int> result = new List<int>();
            for (int i = 0; i < chips.Count; i++)
            {
                if (chips[i].Checked) result.Add(SizeOptions[i]);
            }
            return result;
        }

        void ConvertAll()
        {
            if (busy) return;

            if (list.Items.Count == 0)
            {
                MessageBox.Show(this, "请先添加要转换的图片。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            List<int> sizes = SelectedSizes();
            if (sizes.Count == 0)
            {
                MessageBox.Show(this, "请至少选择一个目标尺寸。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string customDir = outBox.Text.Trim();
            if (rdoCustom.Checked)
            {
                if (customDir.Length == 0 || !Directory.Exists(customDir))
                {
                    MessageBox.Show(this, "请先选择一个存在的输出文件夹。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            int radius = (int)radiusBox.Value;
            int ok = 0;
            int fail = 0;
            string firstError = null;

            ListViewItem[] rows = new ListViewItem[list.Items.Count];
            list.Items.CopyTo(rows, 0);
            HashSet<string> used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            busy = true;
            Application.UseWaitCursor = true;
            convertBtn.Enabled = false;

            try
            {
                foreach (ListViewItem row in rows)
                {
                    if (row.ListView == null) continue;
                    string src = row.Tag as string;
                    if (src == null) continue;
                    row.SubItems[2].Text = "转换中...";
                    Application.DoEvents();

                    try
                    {
                        string dir = rdoCustom.Checked ? customDir : Path.GetDirectoryName(src);
                        if (string.IsNullOrEmpty(dir)) dir = Environment.CurrentDirectory;
                        string outPath = UniqueOutputPath(dir, Path.GetFileNameWithoutExtension(src), src, used);

                        using (Bitmap bmp = ImageLoader.Load(src))
                        {
                            IcoWriter.Save(outPath, bmp, sizes, radius);
                        }

                        row.SubItems[2].Text = "已生成";
                        row.SubItems[2].ForeColor = AccentDark;
                        row.ToolTipText = outPath;
                        ok++;
                    }
                    catch (Exception ex)
                    {
                        row.SubItems[2].Text = "失败";
                        row.SubItems[2].ForeColor = Color.FromArgb(200, 70, 70);
                        row.ToolTipText = ex.Message;
                        if (firstError == null) firstError = Path.GetFileName(src) + "：" + ex.Message;
                        fail++;
                    }
                }
            }
            finally
            {
                busy = false;
                convertBtn.Enabled = true;
                Application.UseWaitCursor = false;
            }

            if (fail > 0)
            {
                status.Text = "完成：" + ok + " 个成功，" + fail + " 个失败（鼠标停在「状态」列看原因）";
                if (ok == 0)
                {
                    MessageBox.Show(this, "全部转换失败。\n\n" + firstError, "转换失败",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                status.Text = "完成：" + ok + " 个成功";
            }
        }

        static string UniqueOutputPath(string dir, string baseName, string src, HashSet<string> used)
        {
            string candidate = Path.Combine(dir, baseName + ".ico");
            for (int i = 2; ; i++)
            {
                if (!SamePath(candidate, src) && used.Add(candidate)) return candidate;
                candidate = Path.Combine(dir, baseName + " (" + i + ").ico");
            }
        }

        static bool SamePath(string a, string b)
        {
            try
            {
                return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }

    public class SizeChip : Button
    {
        readonly Color accent;
        readonly Color ink;
        readonly Color line;
        bool isChecked;

        public SizeChip(string text, Color accentColor, Color inkColor, Color lineColor)
        {
            accent = accentColor;
            ink = inkColor;
            line = lineColor;
            Text = text;
            Size = new Size(52, 28);
            Margin = new Padding(0, 0, 8, 0);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 1;
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            Cursor = Cursors.Hand;
            isChecked = false;
            Apply();
            Click += delegate { SetChecked(!isChecked); };
        }

        public bool Checked
        {
            get { return isChecked; }
            set { isChecked = value; Apply(); }
        }

        public void SetChecked(bool value)
        {
            isChecked = value;
            Apply();
        }

        void Apply()
        {
            BackColor = isChecked ? accent : Color.White;
            ForeColor = isChecked ? Color.White : ink;
            FlatAppearance.BorderColor = isChecked ? accent : line;
            FlatAppearance.MouseOverBackColor = isChecked ? Color.FromArgb(58, 168, 118) : Color.FromArgb(246, 246, 246);
        }
    }

    public class DropZone : Panel
    {
        static readonly Color Accent = Color.FromArgb(66, 185, 131);
        bool hot;

        public event Action Browse;
        public event Action<string[]> FilesDropped;

        public DropZone()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            AllowDrop = true;
            Cursor = Cursors.Hand;
            Click += delegate { if (Browse != null) Browse(); };
            DragEnter += delegate (object s, DragEventArgs e)
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effect = DragDropEffects.Copy;
                    hot = true;
                    Invalidate();
                }
            };
            DragLeave += delegate { hot = false; Invalidate(); };
            DragDrop += delegate (object s, DragEventArgs e)
            {
                hot = false;
                Invalidate();
                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null && FilesDropped != null) FilesDropped(files);
            };
        }

        public bool Hot
        {
            get { return hot; }
            set { hot = value; }
        }

        static GraphicsPath Round(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle r = new Rectangle(1, 1, Width - 3, Height - 3);
            using (GraphicsPath path = Round(r, 10))
            {
                using (SolidBrush bg = new SolidBrush(hot ? Color.FromArgb(243, 251, 247) : Color.FromArgb(250, 250, 250)))
                {
                    g.FillPath(bg, path);
                }
                using (Pen pen = new Pen(hot ? Accent : Color.FromArgb(212, 212, 212), 1.4f))
                {
                    pen.DashStyle = DashStyle.Dash;
                    g.DrawPath(pen, path);
                }
            }

            int cx = Width / 2;
            int iconY = 32;
            using (Pen pen = new Pen(hot ? Accent : Color.FromArgb(180, 180, 180), 2f))
            {
                g.DrawRectangle(pen, cx - 14, iconY, 28, 22);
                g.DrawLine(pen, cx - 14, iconY + 22, cx - 4, iconY + 12);
                g.DrawLine(pen, cx - 4, iconY + 12, cx + 3, iconY + 19);
                g.DrawLine(pen, cx + 3, iconY + 19, cx + 8, iconY + 14);
                g.DrawLine(pen, cx + 8, iconY + 14, cx + 14, iconY + 22);
                using (SolidBrush dot = new SolidBrush(hot ? Accent : Color.FromArgb(200, 200, 200)))
                {
                    g.FillEllipse(dot, cx + 4, iconY + 5, 5, 5);
                }
            }

            using (Font f1 = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Regular))
            using (Font f2 = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Regular))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                using (SolidBrush b1 = new SolidBrush(hot ? Accent : Color.FromArgb(90, 90, 90)))
                {
                    g.DrawString("点击或将图片文件拖放到此处", f1, b1, new RectangleF(0, 70, Width, 24), sf);
                }
                using (SolidBrush b2 = new SolidBrush(Color.FromArgb(153, 153, 153)))
                {
                    g.DrawString("支持 PNG / JPG / JPEG / BMP / GIF / ICO / WEBP，单张不超过 100 MB", f2, b2, new RectangleF(0, 98, Width, 20), sf);
                }
            }
        }
    }
}