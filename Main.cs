using ImageMagick;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xbm
{
    public partial class Main : Form
    {
        /// <summary>
        /// ffmpeg.exe -i D:\Pictures\111.mp4  -q:v 2 -s 64x48 -r 10 D:\Downloads\111\image%d.xbm
        /// convert D:\Pictures\222.mp4 -thumbnail "64x48^" -gravity center -extent 64x48 D:\Downloads\111\frame_%d.xbm
        /// </summary>
        public Main()
        {
            InitializeComponent();
            SelectedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp");
            txtHeight.Text = "48";
            txtWidth.Text = "64";
            pictureBox1.Width = Convert.ToInt32(txtWidth.Text);
            pictureBox1.Height = Convert.ToInt32(txtHeight.Text);
        }
        public int FPS { get { return Convert.ToInt32(fps.Text); } }

        public string SelectedPath { get; set; }
        void Send(byte[] buffer)
        {
            UdpClient udpClient = new UdpClient();
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Broadcast, 10249);
            udpClient.Send(buffer, buffer.Length, ipEndPoint);
        }
        private bool run = false;
        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(SelectedPath)) return;
            run = !run;
            button1.Text = run ? "停止" : "开始";
            if (!run) return;
            Task.Run(() =>
            {

                var file = new DirectoryInfo(SelectedPath).GetFiles("*.xbm").OrderBy(t => t.Name).ToArray();
                var i = 0;
                long lastTime = 0;
                while (file.Count() != i)
                {
                    if (run == false) break;
                    if ((DateTime.Now.ToFileTimeUtc() / 10000) > lastTime + (1000 / FPS))
                    {
                        lastTime = DateTime.Now.ToFileTimeUtc() / 10000;
                        Task.Run(() =>
                        {
                            Send(ConvertFile(File.ReadAllText(file[i].FullName)));
                        });

                        i++;
                        this.Invoke(new Action(() =>
                        {
                            this.Text = $"{i}/{file.Count()}";

                        }));

                    }
                }
                //DateTime dt = DateTime.MinValue;
                //foreach (FileInfo info in new DirectoryInfo(SelectedPath).GetFiles("*.xbm").OrderBy(t => t.Name))
                //{
                //    if (run == false) break;
                //    TimeSpan ts = new TimeSpan(DateTime.Now.Ticks);

                //    Task.Run(() =>
                //    {
                //        Send(ConvertFile(File.ReadAllText(info.FullName)));
                //    }).ContinueWith((t) =>
                //    {
                //        #region 
                //        //Task.Run(() =>
                //        //{
                //        using (MagickImage image = new MagickImage())
                //        {
                //            using (MemoryStream memStream = new MemoryStream())
                //            {
                //                image.Read(info.FullName, MagickFormat.Xbm);
                //                image.Format = MagickFormat.Jpg;
                //                image.Write(memStream);
                //                pictureBox2.Invoke(new Action(() =>
                //                {
                //                    pictureBox2.Image = Image.FromStream(memStream);
                //                }));
                //            }
                //        }
                //        //});
                //        #endregion
                //    }).Wait();
                //    var time = (ts.Subtract(new TimeSpan(DateTime.Now.Ticks)).Duration()).Milliseconds;





                //    Thread.Sleep((1000 / FPS) - time);
                //    //Task.Delay((1000 / FPS) - time).Wait();
                //}
                Invoke(new Action(() =>
                {
                    run = false;
                    button1.Text = "开始";
                }));
            });

        }

        private byte[] ConvertFile(string input)
        {
            //string input = File.ReadAllText(fileFullNme);
            string bytes = System.Text.RegularExpressions.Regex.Match(input, @"\{(.*)\}", System.Text.RegularExpressions.RegexOptions.Singleline).Groups[1].Value;
            string[] StringArray = bytes.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            byte[] pixels = new byte[StringArray.Length - 1];
            for (int k = 0; k < StringArray.Length - 1; k++)
                if (byte.TryParse(StringArray[k].TrimStart().Substring(2, 2), NumberStyles.HexNumber, CultureInfo.CurrentCulture, out byte result))
                    pixels[k] = result;
                else
                    throw new Exception();

            return pixels;
            //Send(pixels);
        }

        private void button2_Click(object sender, EventArgs e)
        {

            using (FolderBrowserDialog dialog = new FolderBrowserDialog
            {
                Description = "请选择Txt所在文件夹"
            })
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (string.IsNullOrEmpty(dialog.SelectedPath)) { MessageBox.Show(this, "文件夹路径不能为空", "提示"); return; }
                    SelectedPath = dialog.SelectedPath;
                    label2.Text = SelectedPath;
                }
            }


        }

        private void button3_Click(object sender, EventArgs e)
        {
            //pictureBox1.Image = CreateBitmapFromRawDataBuffer(Convert.ToInt32(txtWidth.Text), Convert.ToInt32(txtHeight.Text), System.Drawing.Imaging.PixelFormat.Format8bppIndexed, File.ReadAllBytes(@"D:\Pictures\Relay.jpg"));
            using (MagickImage image = new MagickImage(/*@"D:\Pictures\Relay.jpg"*/imagePath))
            {
                MagickGeometry size = new MagickGeometry()
                {
                    IgnoreAspectRatio = true,
                    IsPercentage = false
                };
                image.ColorSpace = ColorSpace.LinearGray;
                image.Resize(new MagickGeometry($"{Convert.ToInt32(txtWidth.Text)}x{ Convert.ToInt32(txtHeight.Text)}!"));

                image.Format = MagickFormat.Xbm;
                byte[] data = image.ToByteArray();
                richTextBox1.Text = System.Text.Encoding.Default.GetString(data);
                using (var img = new MagickImage(data, MagickFormat.Xbm))
                using (MemoryStream memStream = new MemoryStream())
                {
                    img.Format = MagickFormat.Jpg;
                    img.Write(memStream);
                    pictureBox1.Image = Image.FromStream(memStream);
                }
                Send(ConvertFile(System.Text.Encoding.Default.GetString(data)));
            }
        }

        public string imagePath { get; set; }
        private void button4_Click(object sender, EventArgs e)
        {
            using (FileDialog fileDialog = new OpenFileDialog()
            {
                Title = "请选择图片",
                Filter = "图像文件 (*.BMP;*.JPG;*.GIF;*.PNG)|*.BMP;*.JPG;*.GIF;*.PNG",

            })
            {
                if (fileDialog.ShowDialog() != DialogResult.OK) return;
                imagePath = fileDialog.FileName;
                pictureBox1.Image = Image.FromFile(imagePath);
            }
        }

        private void txtWidth_TextChanged(object sender, EventArgs e)
        {
            pictureBox1.Width = Convert.ToInt32(txtWidth.Text);
        }

        private void txtHeight_TextChanged(object sender, EventArgs e)
        {
            pictureBox1.Height = Convert.ToInt32(txtHeight.Text);
        }

        readonly QueueThread qt = new QueueThread();
        private void button5_Click(object sender, EventArgs e)
        {
            var selectfile = @"D:\Pictures\111.mp4";
            var highQuality = checkBox1.Checked;
            using (FileDialog fileDialog = new OpenFileDialog()
            {
                Title = "请选择视频",
                Filter = "视频文件 (*.MP4;*.AVI;*.MOV;*.FLV,*.M4V)|*.MP4;*.AVI;*.MOV;*.FLV,*.M4V",
            })
            {
                if (fileDialog.ShowDialog() != DialogResult.OK) return;
                selectfile = fileDialog.FileName;
            }
            button5.Enabled = !button5.Enabled;
            string tmpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp");
            foreach (var item in Directory.GetFiles(tmpPath))
                File.Delete(item);
            Task.Run(() =>
            {
                var r = new FFMPEG();
                if (highQuality)
                {
                    r.OnProgress = (string message) =>
                    {
                        Invoke(new Action(() => { label5.Text = $"视频转图片 {message}"; }));
                    };
                    r.RealAction(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe"), $@"-i {selectfile} -r {textBox3.Text} -s {textBox1.Text}x{textBox2.Text} -q:v 0 -f image2 {tmpPath}\%08d.jpg");
                }
                else
                {
                    r.OnProgress = (string message) =>
                    {
                        Invoke(new Action(() => { label5.Text = $"视频转XBM {message}"; }));
                    };
                    r.RealAction(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe"), $@"-i {selectfile} -r {textBox3.Text} -s {textBox1.Text}x{textBox2.Text} -q:v 0 -f image2 {tmpPath}\%08d.xbm");
                }


            }).ContinueWith(t =>
            {
                Task.Run(() =>
                {
                    if (!highQuality) return;

                    foreach (string info in Directory.GetFiles(tmpPath, "*.jpg"))
                        qt.EnqueuelTask(info);
                    qt.OnProgress = (string message) =>
                    {
                        Invoke(new Action(() => { label5.Text = $"图片转XBM {message}"; }));
                    };
                    qt.Start(50);
                }).ContinueWith(o =>
                {


                    Invoke(new Action(() => { button5.Enabled = !button5.Enabled; }));
                });

            });

        }

        private void button6_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                byte[] all;
                var fileList = Directory.GetFiles(SelectedPath, "*.xbm").ToArray();
                var size = fileList.Count() * 8 + 7;
                byte[] tmp = new byte[size];
                tmp[0] = (byte)Convert.ToInt16(textBox1.Text);
                tmp[1] = (byte)Convert.ToInt16(textBox2.Text);
                tmp[2] = (byte)Convert.ToInt16(textBox3.Text);

                var filecount = fileList.Count();
                Buffer.BlockCopy(BitConverter.GetBytes(filecount), 0, tmp, 3, 4);
                using (MemoryStream memStream = new MemoryStream())
                {
                    for (int i = 0; i < filecount; i++)
                    {
                        var b = ConvertFile(File.ReadAllText(fileList[i]));
                        var l = b.Length;
                        size += b.Length;
                        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, tmp, (i * 8 + 7), 4);
                        Buffer.BlockCopy(BitConverter.GetBytes(l), 0, tmp, (i * 8 + 11), 4);
                        memStream.Write(b, 0, l);
                        Invoke(new Action(() => { label5.Text = $" XBM文件合并 {i + 1}/{filecount}"; }));
                    }

                    byte[] cc = memStream.ToArray();
                    all = new byte[tmp.Length + cc.Length];
                    Buffer.BlockCopy(tmp, 0, all, 0, tmp.Length);
                    Buffer.BlockCopy(cc, 0, all, tmp.Length, cc.Length);
                }
                File.WriteAllBytes(Path.Combine(SelectedPath, "tmp.bin"), all);
            });
        }
        private void button7_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(SelectedPath)) return;
            run = !run;
            button7.Text = run ? "停止" : "开始";
            Task.Run(() =>
            {
                byte[] file = File.ReadAllBytes(Path.Combine(SelectedPath, "tmp.bin"));
                var w = (short)file[0];
                var h = (short)file[1];
                var _fps = (short)file[2];
                var c = BitConverter.ToInt32(file, 3);

                long lastTime = 0;
                int i = 0;
                while (c != i)
                {
                    if (run == false) break;

                    if ((DateTime.Now.ToFileTimeUtc() / 10000) > lastTime + (1000 / _fps))
                    {
                        lastTime = DateTime.Now.ToFileTimeUtc() / 10000;
                        Task.Run(() =>
                        {
                            var start = BitConverter.ToInt32(file, i * 8 + 7);
                            var len = BitConverter.ToInt32(file, i * 8 + 11);
                            byte[] tmp = new byte[len];
                            Buffer.BlockCopy(file, start, tmp, 0, len);
                            Send(tmp);
                        });
                        i++;
                        Invoke(new Action(() =>
                        {
                            this.Text = $"{c} / {i}";
                        }));
                    }

                }

                Invoke(new Action(() =>
                {
                    run = false;
                    button7.Text = "开始";
                }));

            });

        }


    }

    class QueueThread
    {
        Queue<string> _tasks = new Queue<string>();
        public Action<string> OnProgress;
        static object _locker = new object();
        List<Thread> _threadList = new List<Thread>();
        public void Start(int threadcount)
        {
            for (int i = 0; i < threadcount; i++)
            {
                Thread t = new Thread(new ThreadStart(DoWork));
                _threadList.Add(t);
                t.IsBackground = true;
                t.Start();
            }
        }
        public void Stop()
        {
            foreach (var item in _threadList)
                item.Join();
        }

        public void EnqueuelTask(string filename)
        {
            lock (_locker)
                _tasks.Enqueue(filename);
        }



        private void DoWork()
        {
            using (MagickImage image = new MagickImage())
            {
                string fileName;
                while (true)
                {
                    lock (_locker)
                    {
                        OnProgress?.Invoke($"{_tasks.Count}");
                        if (_tasks.Count == 0) { break; }
                        fileName = _tasks.Dequeue();
                    }
                    FileInfo file = new FileInfo(fileName);
                    image.Read(file, MagickFormat.Jpg);
                    image.Resize(64, 48);
                    image.Format = MagickFormat.Xbm;
                    image.Write(file.FullName.Replace("jpg", "xbm"));
                    file.Delete();
                }
            }
        }

    }


    class FFMPEG
    {
        public Action<string> OnProgress;
        public void RealAction(string StartFileName, string StartFileArg)
        {
            Process CmdProcess = new Process();
            CmdProcess.StartInfo.FileName = StartFileName;      // 命令
            CmdProcess.StartInfo.Arguments = StartFileArg;      // 参数  

            CmdProcess.StartInfo.CreateNoWindow = true;         // 不创建新窗口
            CmdProcess.StartInfo.UseShellExecute = false;
            CmdProcess.StartInfo.RedirectStandardInput = true;  // 重定向输入
            CmdProcess.StartInfo.RedirectStandardOutput = true; // 重定向标准输出
            CmdProcess.StartInfo.RedirectStandardError = true;  // 重定向错误输出
                                                                //CmdProcess.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
            CmdProcess.ErrorDataReceived += new DataReceivedEventHandler(p_ErrorDataReceived);

            //CmdProcess.EnableRaisingEvents = true;                      // 启用Exited事件
            //CmdProcess.Exited += new EventHandler(CmdProcess_Exited);   // 注册进程结束事件  

            CmdProcess.Start();
            //CmdProcess.BeginOutputReadLine();
            CmdProcess.BeginErrorReadLine();

            // 如果打开注释，则以同步方式执行命令，此例子中用Exited事件异步执行。
            CmdProcess.WaitForExit();
        }

        private void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {

                //var percentage = Math.Round(t.TotalSeconds / _totalTime.TotalSeconds * 100, 2);
            }
        }

        private void p_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                var r = new System.Text.RegularExpressions.Regex(@"\w\w:\w\w:\w\w");
                var m = r.Match(e.Data);
                if (!e.Data.Contains("frame")) return;
                if (!m.Success) return;
                var t = TimeSpan.Parse(m.Value, CultureInfo.InvariantCulture);
                OnProgress?.Invoke(t.ToString());
            }
        }

        private void CmdProcess_Exited(object sender, EventArgs e)
        {
            // 执行结束后触发
        }

    }
}
