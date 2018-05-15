using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace YirenReaderConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                //new DoDownload().DowonloadResource();
                //new DoDownload().DownloadPics();
                new DoDownload().DowonloadMovie();
                //new DoDownload().ErgodicNovels();
                //new DoDownload().SimplifyContent();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }

    public class DoDownload
    {
        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", EntryPoint = "FindWindowEx")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("User32.dll", EntryPoint = "SendMessage")]  
        private static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, string lParam);
        [DllImport("User32.dll", EntryPoint = "ShowWindow")]
        private static extern bool ShowWindow(IntPtr hWnd, int type);//隐藏本dos窗体, 0: 后台执行；1:正常启动；2:最小化到任务栏；3:最大化

        readonly HttpWebClient webclient = new HttpWebClient();

        public DoDownload()
        {
            ////设置后台运行
            //Console.Title = "TestConsole";
            //IntPtr ParenthWnd = new IntPtr(0);
            //ParenthWnd = FindWindow(null, "TestConsole");
            //ShowWindow(ParenthWnd, 0);

            webclient.DataReceive += new HttpWebClient.DataReceiveEventHandler(x_DataReceive);
            webclient.ExceptionOccurrs += new HttpWebClient.ExceptionEventHandler(x_ExceptionOccurrs);
            webclient.ThreadProcessEnd += new HttpWebClient.ThreadProcessEventHandler(x_ThreadProcessEnd);
        }

        #region 遍历解析novels并存入

        public void ErgodicNovels()
        {
            //遍历取出数据库记录，每次100条
            var IDstr = 0;
            IDstr = int.Parse(ReadConfig("novelconfig.txt"));
            try
            {
                while (true)
                {
                    var sql = "SELECT * FROM story a JOIN (select ID from story limit " + IDstr + ", 100) b ON a.ID = b.id";//大数据列表的分页查询
                    using (var dssource = MysqlHelper.GetDataSet(sql, CommandType.Text, null))
                    {
                        if (dssource == null || dssource.Tables.Count == 0 || dssource.Tables[0].Rows.Count == 0)
                        {
                            continue;
                        }
                        for (int j = 0; j < dssource.Tables[0].Rows.Count; j++)
                        {
                            var ID = dssource.Tables[0].Rows[j]["ID"].ToString();
                            var Type = dssource.Tables[0].Rows[j]["Type2"].ToString();
                            var Title = dssource.Tables[0].Rows[j]["Title"].ToString();
                            var Content = dssource.Tables[0].Rows[j]["Content"].ToString();
                            Console.WriteLine("正在提取第" + ID + "个小说...");
                            Title = ReplaceNovelTitle(Title);
                            var novel = GetNovel(Content);
                            novel = ReplaceHtml(novel);
                            //更新本条记录
                            var sqlupd = "UPDATE story SET Title='" + Title + "', col5='" + novel + "' WHERE ID=" + ID + ";";
                            MysqlHelper.ExecuteNonQuery(sqlupd, CommandType.Text, null);
                            Console.WriteLine("存储成功！");
                        }
                    }
                    IDstr += 100;
                }
            }
            catch(Exception ex)
            {
                WriteConfig("novelconfig.txt", IDstr.ToString());
                Console.WriteLine("程序出错，原因是："+ex.Message);
                Console.ReadKey();
            }

        }

        public void SimplifyContent()
        {
            //遍历取出数据库记录，每次100条
            var IDstr = 0;
            IDstr = int.Parse(ReadConfig("novelconfig.txt"));
            try
            {
                while (true)
                {
                    var sql = "SELECT * FROM story a JOIN (select ID from story limit " + IDstr + ", 100) b ON a.ID = b.id";//大数据列表的分页查询
                    using (var dssource = MysqlHelper.GetDataSet(sql, CommandType.Text, null))
                    {
                        if (dssource == null || dssource.Tables.Count == 0 || dssource.Tables[0].Rows.Count == 0)
                        {
                            continue;
                        }
                        for (int j = 0; j < dssource.Tables[0].Rows.Count; j++)
                        {
                            var ID = dssource.Tables[0].Rows[j]["ID"].ToString();
                            var Content = dssource.Tables[0].Rows[j]["col5"].ToString();
                            Console.WriteLine("正在优化第" + ID + "个...");
                            Content = ReplaceHtml(Content);
                            //更新本条记录
                            var sqlupd = "UPDATE story SET col5='" + Content + "' WHERE ID=" + ID + ";";
                            MysqlHelper.ExecuteNonQuery(sqlupd, CommandType.Text, null);
                            Console.WriteLine("存储成功！");
                        }
                    }
                    IDstr += 100;
                }
            }
            catch (Exception ex)
            {
                WriteConfig("novelconfig.txt", IDstr.ToString());
                Console.WriteLine("程序出错，原因是：" + ex.Message);
                Console.ReadKey();
            }
        }

        private string GetNovel(string content)
        {
            const string head = "938px;><tbody><tr><td>";
            const string tail = "<td><tr><tbody><table><div>";
            var IndexofA = 0;//游标
            IndexofA = content.IndexOf(head, IndexofA, StringComparison.Ordinal);//寻找开始位置
            if (IndexofA == -1)
                return "";
            var IndexofB = content.IndexOf(tail, IndexofA, StringComparison.Ordinal);//寻找结束位置
            return content.Substring(IndexofA + head.Length, IndexofB - IndexofA - head.Length);
        }

        #endregion

        #region 读写配置文件

        public string ReadConfig(string path)
        {
            StreamReader sr = new StreamReader(path, Encoding.Default);
            string line;
            string rtn = "";
            while ((line = sr.ReadLine()) != null)
            {
                rtn += line;
            }
            return rtn;
        }

        public void WriteConfig(string path,string content)
        {
            FileStream fs = new FileStream(path, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            //开始写入
            sw.Write(content);
            //清空缓冲区
            sw.Flush();
            //关闭流
            sw.Close();
            fs.Close();
        }

        #endregion

        #region 下载图片

        /// <summary>
        /// 从图片页面获取图片地址并下载分类打包
        /// </summary>
        public void DownloadPics()
        {
            var IDstr = 47582;//初始ID，真实自拍45542，美腿丝袜164314
            IDstr = int.Parse(ReadConfig("config.txt")) + 1;
            while (true)
            {
                //从数据库获取所需分类的记录
                var sql = "select * from picture where ID='" + IDstr + "'";
                using (var dssource = MysqlHelper.GetDataSet(sql, CommandType.Text, null))
                {
                    if (dssource == null || dssource.Tables.Count == 0 || dssource.Tables[0].Rows.Count == 0)
                    {
                        break;//若递增的记录不存在，则迭代完成
                    }
                    try
                    {
                        int loopcout = 3;//重试3次
                        //取得存储的源码
                        B:
                        var filename = dssource.Tables[0].Rows[0]["Title"].ToString();
                        filename = ReplaceFilename(filename);
                        filename = filename.Substring(7, filename.Length - 7);
                        filename = IDstr + "-" + filename;
                        Filename = filename;
                        var source = dssource.Tables[0].Rows[0]["Content"].ToString();
                        //取得图片组
                        List<string> picurls = GetPicUrls(source);
                        if (picurls.Count == 0)
                        {
                            IDstr++;
                            continue;
                        }
                        //Console.WriteLine("正在获取——" + Filename + "，共计" + picurls.Count + "张图片...");
                        Console.WriteLine("正在获取——" + IDstr + "，共计" + picurls.Count + "张图片...");
                        //按名称创建下载文件夹，并下载图片组至入
                        if (!Directory.Exists(filename))
                        {
                            Directory.CreateDirectory(filename);
                        }

                        iCount = 0;//重置计数器
                        iMaxCount = picurls.Count;//重置计数器上限
                        eventX = new ManualResetEvent(false);//重置事件信号量
                        cts = new CancellationTokenSource();//强制退出线程
                        ThreadPool.SetMaxThreads(300, 300);//设置设置线程池线程最大最小
                        foreach (var url in picurls)
                        {
                            //ThreadPool.QueueUserWorkItem(new WaitCallback(Downloadpic), url);
                            ThreadPool.QueueUserWorkItem(o => Downloadpic(cts.Token, url));//改用线程池管理
                        }
                        //倒计时强制退出本轮循环，防止死磕无法下载的图片
                        tCountdown = new System.Timers.Timer();
                        tCountdown.Elapsed += TCountdown_Elapsed;
                        tCountdown.Interval = picurls.Count * 1500;//设置倒计时
                        tCountdown.Start();
                        //等待事件的完成，即线程调用ManualResetEvent.Set()方法
                        //阻塞当前线程，直到当前 WaitHandle 收到信号为止。 
                        eventX.WaitOne(Timeout.Infinite, true);
                        tCountdown.Stop();

                        //检查文件夹内容是否为空，为空则删除文件夹并重试3次
                        if (Directory.GetFiles(filename).Length == 0)
                        {
                            new DirectoryInfo(filename).Delete(true);//true:删除里面所有的文件，包括文件夹和子文件夹
                            loopcout--;
                            if (loopcout > 0)
                            {
                                //Console.WriteLine("正在重试...");
                                Console.WriteLine("获取失败...");

                                //记录当前失败ID，并退出程序，等待守护进程重新唤醒
                                WriteConfig("config.txt", IDstr.ToString());
                                goto C;
                            }
                        }

                        Console.WriteLine("获取成功！");

                        //打包图片文件夹成zip文件
                        //ZipHelper.CreateZip(filename, filename + ".zip"); //创建压缩文件
                        //ZipHelper.ZipDirectory(filename, filename + ".zip","",false);
                        //删除打包前的文件夹
                    }
                    catch
                    {
                    }

                }
                IDstr++;
            }
            C:
            Console.WriteLine("程序退出...");
        }

        private void TCountdown_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            eventX.Set();//强制设置主线程不阻塞
            cts.Cancel();//同时原线程池中活动线程也结束
        }

        /// <summary>
        /// 获取源码中的图片地址集合
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private List<string> GetPicUrls(string content)
        {
            List<string> list = new List<string>();
            try
            {
                const string kk = "<table cellspacing=\"0\" cellpadding=\"0\">";
                const string head = "\" src=\"";
                const string tail = "\" border=\"";
                var IndexofA = 0;//游标
                IndexofA = content.IndexOf(kk, IndexofA, StringComparison.Ordinal);//开始位置重定向
                while (true)
                {
                    IndexofA = content.IndexOf(head, IndexofA, StringComparison.Ordinal);//寻找开始位置
                    if (IndexofA == -1) break;
                    var IndexofB = content.IndexOf(tail, IndexofA, StringComparison.Ordinal);//寻找结束位置
                    var url = content.Substring(IndexofA + head.Length, IndexofB - IndexofA - head.Length);
                    list.Add(url);
                    IndexofA = IndexofA + 2 + head.Length;//游标右移
                }
            }
            catch
            {
            }
            return list;
        }

        //新建ManualResetEvent对象并且初始化为无信号状态
        private static ManualResetEvent eventX;
        private System.Timers.Timer tCountdown;//倒计时
        private CancellationTokenSource cts;
        public static int iCount = 0;
        public static int iMaxCount = 0;
        private static string Filename = "";
        private static void Downloadpic(CancellationToken token, object _url)
        {
            int cout = 3;//失败重试3次
            A:
            try
            {
                var url = _url.ToString();
                var request = WebRequest.Create(url);
                request.Timeout = 3000;//3秒超时
                var response = request.GetResponse();
                var reader = response.GetResponseStream();
                var path = Filename + "\\" + url.Substring(url.LastIndexOf('/') + 1);
                if (File.Exists(path))
                {
                    //Interlocked.Increment()操作是一个原子操作，作用是:iCount++ 具体请看下面说明 
                    //原子操作，就是不能被更高等级中断抢夺优先的操作。你既然提这个问题，我就说深一点。
                    //由于操作系统大部分时间处于开中断状态，
                    //所以，一个程序在执行的时候可能被优先级更高的线程中断。
                    //而有些操作是不能被中断的，不然会出现无法还原的后果，这时候，这些操作就需要原子操作。
                    //就是不能被中断的操作。
                    Interlocked.Increment(ref iCount);
                    if (iCount == iMaxCount)
                    {
                        //将事件状态设置为终止状态，允许一个或多个等待线程继续。此处即通知主线程结束等待。
                        eventX.Set();
                    }
                    return;
                }
                var writer = new FileStream(path, FileMode.Create, FileAccess.Write);//x:\\pic.jpg
                var buff = new byte[512];
                int c = 0; //实际读取的字节数
                while ((c = reader.Read(buff, 0, buff.Length)) > 0)
                {
                    writer.Write(buff, 0, c);
                }
                writer.Close();
                writer.Dispose();
                reader.Close();
                reader.Dispose();
                response.Close();

                //Interlocked.Increment()操作是一个原子操作，作用是:iCount++ 具体请看下面说明 
                //原子操作，就是不能被更高等级中断抢夺优先的操作。你既然提这个问题，我就说深一点。
                //由于操作系统大部分时间处于开中断状态，
                //所以，一个程序在执行的时候可能被优先级更高的线程中断。
                //而有些操作是不能被中断的，不然会出现无法还原的后果，这时候，这些操作就需要原子操作。
                //就是不能被中断的操作。
                Interlocked.Increment(ref iCount);
                if (iCount == iMaxCount)
                {
                    //将事件状态设置为终止状态，允许一个或多个等待线程继续。此处即通知主线程结束等待。
                    eventX.Set();
                }
            }
            catch
            {
                cout--;
                if (cout > 0 && !token.IsCancellationRequested)
                {
                    goto A;
                }
            }
        }

        #endregion

        #region 下载电影
        
        public void DowonloadMovie()
        {
            var IDstr = 35779;//初始ID
            //IDstr = int.Parse(ReadConfig("configmovie.txt")) + 1;
            while (true)
            {
                //从数据库获取所需分类的记录
                var sql = "select * from video where ID='" + IDstr + "'";
                using (var dssource = MysqlHelper.GetDataSet(sql, CommandType.Text, null))
                {
                    if (dssource == null || dssource.Tables.Count == 0 || dssource.Tables[0].Rows.Count == 0)
                    {
                        break;//若递增的记录不存在，则迭代完成
                    }
                    try
                    {
                        int loopcout = 3;//重试3次
                        //取得存储的源码
                        D:
                        var filename = dssource.Tables[0].Rows[0]["Title"].ToString();
                        filename = ReplaceFilename(filename);
                        filename = IDstr + "-" + filename;
                        Filename = filename;
                        var source = dssource.Tables[0].Rows[0]["Content"].ToString();
                        //取得视频地址
                        string movieurl = GetMovieUrl(source);
                        if (string.IsNullOrEmpty(movieurl))
                        {
                            IDstr++;
                            Console.WriteLine("没有找到视频文件！");
                            continue;
                        }
                        Console.WriteLine();
                        Console.WriteLine("正在获取——" + IDstr + "..." + movieurl);

                        var file = movieurl.Split('/')[movieurl.Split('/').Length - 1];//原文件名
                        var arr = file.Split('.');
                        if (arr.Length < 1)
                        {
                            continue;
                        }

                        //倒计时强制退出本轮循环，防止死磕无法下载的图片
                        tCountdown = new System.Timers.Timer();
                        tCountdown.Elapsed += TCountdown_Elapsed;
                        tCountdown.Interval = 5*60*1000;//5分钟没有更新进度则退出，启动下一个电影下载
                        tCountdown.Start();

                        eventX = new ManualResetEvent(false);//主线程获取子线程事件信号量
                        cts = new CancellationTokenSource();//主线程告知子线程退出线程信号量
                        //下载文件并保存
                        //pBar = new ProgressBar(Console.CursorLeft, Console.CursorTop, 50, ProgressBarType.Character);//星号进度条
                        //pBar = new ProgressBar(Console.CursorLeft, Console.CursorTop, 50, ProgressBarType.Multicolor);//彩色进度条
                        i = 0;
                        _k = 0;
                        _K = 10;
                        webclient.DownloadFile(movieurl, filename + "." + arr[1], _K);//多线程异步断点续传下载文件
                        eventX.WaitOne(Timeout.Infinite, true);
                        tCountdown.Stop();
                        

                        //打包图片文件夹成zip文件
                        Console.WriteLine("正在压缩文件...");
                        ProgressBar progressBar = new ProgressBar(Console.CursorLeft, Console.CursorTop, 50, ProgressBarType.Multicolor);//星号进度条
                        GZipHelper.Compress(filename + "." + arr[1], progressBar.Dispaly);
                        Console.WriteLine();
                        Console.WriteLine("压缩完成！");
                    }
                    catch
                    {
                    }

                }
                IDstr++;
            }
            E:
            Console.WriteLine("程序退出...");
        }

        private static string GetMovieUrl(string content)
        {
            const string head = "<source src=\"";
            const string tail = "\" type=";
            var IndexofA = 0;//游标
            IndexofA = content.IndexOf(head, IndexofA, StringComparison.Ordinal);//寻找开始位置
            if (IndexofA == -1)
                return "";
            var IndexofB = content.IndexOf(tail, IndexofA, StringComparison.Ordinal);//寻找结束位置
            return content.Substring(IndexofA + head.Length, IndexofB - IndexofA - head.Length);
        }

        #region 下载类方法

        int _k = 0;
        int _K = 10;
        string bs = ""; //用于记录上次的位数 
        bool b = false;
        private int i = 0;
        private static readonly object _SyncLockObject = new object();
        string FilePath;
        string _File;
        private static double percent = 0;
        private ProgressBar pBar;

        /// <summary>
        /// 接收到数据
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="e"></param>
        private void x_DataReceive(HttpWebClient Sender, DownLoadEventArgs e)
        {
            tCountdown.Stop();
            try
            {
                if (!b)
                {
                    lock (_SyncLockObject)
                    {
                        if (!b)
                        {
                            //Console.Write(DateTime.Now.ToString() + " 已接收数据: ");
                            b = true;
                        }
                    }
                }
                string f = e.DownloadState.FileName;
                if (e.DownloadState.AttachmentName != null)
                    f = Path.GetDirectoryName(f) + @"\" + e.DownloadState.AttachmentName;

                _File = f;

                using (var sw = new FileStream(f, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    sw.Position = e.DownloadState.Position;
                    sw.Write(e.DownloadState.Data, 0, e.DownloadState.Data.Length);
                    sw.Close();
                }
                string s = DateTime.Now.ToLongTimeString();
                lock (_SyncLockObject)
                {
                    i += e.DownloadState.Data.Length;
                    //int aa = int.Parse(((double) i*100/Sender.FileLength).ToString());
                    string aa = i + " / " + Sender.FileLength + " 字节数据 ";
                    string bb = "(" + ((double)i * 100 / Sender.FileLength).ToString("0.000") + "%)";

                    //pBar.Dispaly(aa, bb);

                    //Console.Write("(" + ((double)i * 100 / Sender.FileLength).ToString("0.000") + "%)");//12:12:12
                    //Console.Write(bs + "\b\b\b\b\b\b\b\b\b\b" + i + " / " + Sender.FileLength + " 字节数据 " + bb + DigiLenth(bb) + s);
                    //bs = new string('\b', Digits(i) + 3 + Digits(Sender.FileLength) + s.Length);

                    Console.Write(aa + bb + s + DigiLenth(aa + bb + s)+"\b\b\b\b");

                    if (i== Sender.FileLength)
                    {
                        eventX.Set();
                    }
                }
            }
            catch
            {

            }
            tCountdown.Start();//重新计时
        }

        /// <summary>
        /// 得到字符串所占位数个退格
        /// </summary>
        /// <param name="_s"></param>
        /// <returns></returns>
        private string DigiLenth(string _s)
        {
            return _s.Aggregate("", (current, c) => current + "\b");
        }

        /// <summary>
        /// 发生异常
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="e"></param>
        private void x_ExceptionOccurrs(HttpWebClient Sender, ExceptionEventArgs e)
        {
            try
            {
                //Console.WriteLine(e.Exception.Message);
                //发生异常重新下载相当于断点续传,你可以自己自行选择处理方式 
                var webclient = new HttpWebClient();
                //DownloadFileChunk(string Address, string FileName, int FromPosition, int Length)
                webclient.DownloadFileChunk(FilePath, _File, e.DownloadState.Position, e.DownloadState.Length);
                e.ExceptionAction = ExceptionActions.Ignore;
            }
            catch
            {
            }
        }

        /// <summary>
        /// 线程结束
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="e"></param>
        private void x_ThreadProcessEnd(HttpWebClient Sender, ThreadProcessEventArgs e)
        {
            try
            {
                //if (e.thread.ThreadState == System.Threading.ThreadState.Stopped) 
                if (_k++ == _K - 1)
                {
                    var name = FilePath.Split('/')[FilePath.Split('/').Length - 1];//获取文件名
                    //下载文件并保存
                    Console.WriteLine(name + "下载完成！");
                    //Console.WriteLine("\nend");
                    eventX.Set();
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// 数字所占位数
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        static int Digits(int n)
        {
            n = Math.Abs(n);
            n = n / 10;
            int i = 1;
            while (n > 0)
            {
                n = n / 10;
                i++;
            }
            return i;
        }

        #endregion

        #endregion

        #region 下载小说、图片、电影源码

        /// <summary>
        /// 下载小说、图片、电影源码页面
        /// </summary>
        public void DowonloadResource()
        {
            var urls = Makedic();
            //Dictionary<string, string> cols = new Dictionary<string, string>();
            //foreach (KeyValuePair<string, Dictionary<string, string>> pair in urls)
            //{
            //    foreach (var dic in pair.Value)
            //    {
            //        cols.Add(dic.Key, GetHtmlSource(dic.Value));
            //    }
            //}

            //网站根
            var url = "http://www.yiren02.com/";
            foreach (var url1 in urls)
            {
                if (url1.Key == "电影")
                {
                    //逐个赋值起始页
                    foreach (var url2 in url1.Value)
                    {
                        if (url2.Key != "疯狂群交") //已下载过的去掉
                        {
                            //找到文章目录页
                            var urlreal = url2.Value;
                            //循环读取目录页
                            while (true)
                            {
                                //根据目录页地址读取目录页面
                                var cnt = GetWeb(urlreal);
                                if (string.IsNullOrEmpty(cnt))
                                {
                                    Console.WriteLine("获取到了空列表网页！");
                                    break;
                                }

                                //从目录页面获取所有文章链接数组（标题，URL）
                                //Dictionary<string, string> dicUrlName = GetList(cnt);
                                Dictionary<string, string> dicUrlName = GetListVideo(cnt);//电影列表
                                Console.WriteLine("获取到了" + dicUrlName.Count + "个项！——" + url2.Value);
                                //若无目录则退出循环
                                if (dicUrlName.Count == 0)
                                {
                                    break;
                                }
                                //循环数组，获取每一篇文章
                                foreach (var urlname in dicUrlName)
                                {
                                    try
                                    {
                                        var Type1 = url1.Key;
                                        var Type2 = url2.Key;
                                        var Title = urlname.Key;
                                        var URL = url + urlname.Value;
                                        var Content = ReplaceCode(GetWeb(URL));
                                        if (string.IsNullOrEmpty(Content))
                                        {
                                            Console.WriteLine("获取到了空网页！");
                                            break;
                                        }
                                        var Count = Content.Length.ToString();
                                        var sql = "INSERT INTO video (Type1, Type2, Title, URL, Content, Count) VALUES ('" + Type1 + "','" +
                                                     Type2 + "','" + Title + "','" + URL + "','" + Content + "','" + Count + "')";
                                        MysqlHelper.ExecuteNonQuery(sql, CommandType.Text, null);
                                        Console.WriteLine(URL + "，获取成功！");
                                    }
                                    catch (Exception ex)
                                    {
                                        try
                                        {
                                            var Type1 = url1.Key;
                                            var Type2 = url2.Key;
                                            var Title = urlname.Key;
                                            var URL = url + urlname.Value;
                                            var sql = "INSERT INTO video (Type1, Type2, Title, URL) VALUES ('" + Type1 + "','" +
                                                         Type2 + "','" + Title + "','" + URL + "')";
                                            MysqlHelper.ExecuteNonQuery(sql, CommandType.Text, null);
                                        }
                                        catch
                                        {

                                        }
                                    }
                                }
                                //更新目录页地址
                                urlreal = url + GetNextListUrl(cnt);
                            }
                        }
                    }
                }
            }
        }

        private static string GetNextListUrl(string content)
        {
            //HTML代码  
            var str = content;
            string rtn = "";
            try
            {
                //反过来查
                const string head = "\">下一页</a>";//实际上在后面
                const string tail = "<a href=\"";//实际上在前面
                var IndexofA = 0;//游标
                IndexofA = str.IndexOf(head, IndexofA, StringComparison.Ordinal);//寻找开始位置（在后面）
                if (IndexofA == -1)
                    return "";
                var IndexofB = str.IndexOf(tail, IndexofA - 70, StringComparison.Ordinal);//寻找结束位置（在前面）
                rtn = str.Substring(IndexofB + 1 + head.Length, IndexofA - IndexofB - 1 - head.Length);
            }
            catch (Exception msg)
            {
            }
            return rtn;
        }

        /// <summary>
        /// 获取小说、图片列表（标题，URL）
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, string> GetList(string content)
        {
            //HTML代码  
            var str = content;
            var rtn = new Dictionary<string, string>();
            try
            {
                const string head = " <li><a href=\"";
                const string tail = "\" target=\"_blank\" title=\"";
                const string tail2 = "\"><span>";
                var IndexofA = 0;//游标
                while (true)
                {
                    IndexofA = str.IndexOf(head, IndexofA, StringComparison.Ordinal);//寻找开始位置
                    if (IndexofA == -1) break;
                    var IndexofB = str.IndexOf(tail, IndexofA, StringComparison.Ordinal);//寻找结束位置
                    var url = str.Substring(IndexofA + 1 + head.Length, IndexofB - IndexofA - 1 - head.Length);
                    var IndexofC = str.IndexOf(tail2, IndexofB, StringComparison.Ordinal);//寻找结束位置
                    var title = str.Substring(IndexofB + 1 + head.Length, IndexofC - IndexofB - 1 - head.Length);
                    rtn.Add(title, url);
                    //rtn += ReplaceHtml(str);
                    IndexofA = IndexofA + 2 + head.Length;//游标右移
                }
            }
            catch (Exception msg)
            {
            }
            return rtn;
        }

        /// <summary>
        /// 获取电影列表
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private static Dictionary<string, string> GetListVideo(string content)
        {
            //HTML代码  
            var str = content;
            var rtn = new Dictionary<string, string>();
            try
            {
                const string hh = "<ul class=\"movieList\">";
                const string head = "<li><a href=\"";//电影URL开始
                const string tail = "\" target=\"_blank\"><";//电影URL结束
                const string tail2 = " /><h3>";//电影名称开始
                const string tail3 = "</h3><span>";//电影名称结束
                var IndexofA = 0;//游标
                IndexofA = str.IndexOf(hh, IndexofA, StringComparison.Ordinal);//重定向指针
                while (true)
                {
                    IndexofA = str.IndexOf(head, IndexofA, StringComparison.Ordinal);//寻找开始位置
                    if (IndexofA == -1) break;
                    var IndexofB = str.IndexOf(tail, IndexofA, StringComparison.Ordinal);//寻找结束位置
                    var url = str.Substring(IndexofA + 1 + head.Length, IndexofB - IndexofA - 1 - head.Length);

                    var IndexofC = str.IndexOf(tail2, IndexofA, StringComparison.Ordinal);//寻找开始位置
                    if (IndexofC == -1) break;
                    var IndexofD = str.IndexOf(tail3, IndexofA, StringComparison.Ordinal);//寻找结束位置
                    var title = str.Substring(IndexofC + 1 + head.Length - 7, IndexofD - IndexofC - 1 - head.Length + 7);


                    //var IndexofC = str.IndexOf(tail2, IndexofB, StringComparison.Ordinal);//寻找结束位置
                    //var title = str.Substring(IndexofB + 1 + head.Length, IndexofC - IndexofB - 1 - head.Length);
                    rtn.Add(title, url);
                    //rtn += ReplaceHtml(str);
                    IndexofA = IndexofD;//游标右移
                }
            }
            catch (Exception msg)
            {
            }
            return rtn;
        }

        #endregion

        #region 获取、处理网页数据

        public static string GetWeb(string url)
        {
            WebRequest request = WebRequest.Create(url);
            WebResponse response = request.GetResponse();
            Stream s = response.GetResponseStream();
            StreamReader sr = new StreamReader(s, Encoding.GetEncoding("utf-8"));
            string rtn = sr.ReadToEnd();
            sr.Dispose();
            sr.Close();
            s.Dispose();
            s.Close();
            return rtn;
        }

        public static string PostWeb(string url)
        {
            string postData = "";

            WebRequest request = WebRequest.Create(url);
            request.Method = "Post";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = postData.Length;
            StreamWriter sw = new StreamWriter(request.GetRequestStream());
            sw.Write(postData);
            sw.Flush();


            WebResponse response = request.GetResponse();
            Stream s = response.GetResponseStream();
            StreamReader sr = new StreamReader(s, Encoding.GetEncoding("gb2312"));
            string rtn = sr.ReadToEnd();

            sw.Dispose();
            sw.Close();
            sr.Dispose();
            sr.Close();
            s.Dispose();
            s.Close();

            return rtn;
        }

        public static string GetHtmlSource(string url)
        {
            string html = "";
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Accept = "*/*"; //接受任意文件
                request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.1.4322)"; // 模拟使用IE在浏览 http://www.52mvc.com
                request.AllowAutoRedirect = true;//是否允许302
                //request.CookieContainer = new CookieContainer();//cookie容器，
                request.Referer = url; //当前页面的引用
                var response = (HttpWebResponse)request.GetResponse();
                var stream = response.GetResponseStream();
                var reader = new StreamReader(stream, Encoding.UTF8);
                html = reader.ReadToEnd();
                stream.Close();
            }
            catch (Exception)
            {
            }
            return html;
        }

        public static string GetHtmlSource4(string urlString, Encoding encoding)
        {
            if (encoding == null)
            {
                encoding = Encoding.Default;
            }
            //定义局部变量
            HttpWebRequest httpWebRequest = null;
            HttpWebResponse httpWebRespones = null;
            Stream stream = null;
            string htmlString = string.Empty;

            //请求页面
            try
            {
                httpWebRequest = WebRequest.Create(urlString) as HttpWebRequest;
            }
            //处理异常
            catch (Exception ex)
            {
                return "";
                //throw new Exception("建立页面请求时发生错误！", ex);
            }
            httpWebRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 2.0.50727; Maxthon 2.0)";
            //获取服务器的返回信息
            try
            {
                httpWebRespones = (HttpWebResponse)httpWebRequest.GetResponse();
                stream = httpWebRespones.GetResponseStream();
            }
            //处理异常
            catch (Exception ex)
            {
                return "";
                //throw new Exception("接受服务器返回页面时发生错误！", ex);
            }
            StreamReader streamReader = new StreamReader(stream, encoding);
            //读取返回页面
            try
            {
                htmlString = streamReader.ReadToEnd();
            }
            //处理异常
            catch (Exception ex)
            {
                return "";
                //throw new Exception("读取页面数据时发生错误！", ex);
            }
            //释放资源返回结果
            streamReader.Close();
            stream.Close();
            return htmlString;
        }

        /// <summary>
        /// 去除HTML标签
        /// </summary>
        /// <param name="_s"></param>
        /// <returns></returns>
        public static string ReplaceHtml(string _s)
        {
            _s = _s.ToLower();
            _s = _s.Replace("<br/>", "");
            _s = _s.Replace("<br>", "");
            _s = _s.Replace("</font>", "");
            _s = _s.Replace("</div>", "");
            _s = _s.Replace("</td>", "");
            _s = _s.Replace("</tr>", "");
            _s = _s.Replace("</table>", "");
            _s = _s.Replace("</th>", "");
            _s = _s.Replace("</tbody>", "");
            _s = _s.Replace("<tbody>", "");
            _s = _s.Replace("<", "");
            _s = _s.Replace(">", "");
            _s = _s.Replace("font size=", "");
            _s = _s.Replace("div class=", "");
            _s = _s.Replace("style=", "");
            _s = _s.Replace("width:", "");
            _s = _s.Replace("font", "");
            _s = _s.Replace("color=", "");
            _s = _s.Replace("tr", "");
            _s = _s.Replace("tips", "");
            _s = _s.Replace("auto", "");
            _s = _s.Replace("center", "");
            _s = _s.Replace(" br ", "");
            _s = _s.Replace(" p ", "");
            _s = _s.Replace("=", "");
            _s = _s.Replace("align", "");
            _s = _s.Replace("onclick", "");
            _s = _s.Replace("javascript", "");
            _s = _s.Replace("()", "");
            _s = _s.Replace("input", "");
            _s = _s.Replace("name", "");
            _s = _s.Replace("type", "");
            _s = _s.Replace("style", "");
            _s = _s.Replace("value", "");
            _s = _s.Replace("??", "");
            _s = _s.Replace("p", "");
            _s = _s.Replace("line", "");
            _s = _s.Replace("-", "");
            _s = _s.Replace("height", "");
            _s = _s.Replace("px", "");
            _s = _s.Replace(";", "");
            _s = _s.Replace("text", "");
            _s = _s.Replace("indent", "");
            _s = _s.Replace("em;", "");
            _s = _s.Replace("left", "");
            _s = _s.Replace("right", "");
            _s = _s.Replace("pbr", "");
            _s = _s.Replace("class", "");
            return _s;
        }

        private static string ReplaceCode(string _s)
        {
            _s = _s.Replace("'", "");
            //_s = _s.Replace("\"", "");//小说去掉
            //_s = _s.Replace("/", "");//小说去掉
            //_s = _s.Replace("\\", "");//小说去掉
            return _s;
        }

        private static string ReplaceFilename(string _s)
        {
            _s = _s.ToLower();
            _s = _s.Replace(" ", "");
            _s = _s.Replace("?", "");
            _s = _s.Replace("？", "");
            _s = _s.Replace("\\", "");
            _s = _s.Replace("/", "");
            _s = _s.Replace("、", "");
            _s = _s.Replace("*", "");
            _s = _s.Replace("\"", "");
            _s = _s.Replace("“", "");
            _s = _s.Replace("”", "");
            _s = _s.Replace("<", "[");
            _s = _s.Replace(">", "]");
            _s = _s.Replace("|", "");
            _s = _s.Replace(",", "");
            _s = _s.Replace("，", "");
            _s = _s.Replace(".", "");
            _s = _s.Replace("。", "");
            return _s;
        }

        private static string ReplaceNovelTitle(string _s)
        {
            _s = _s.Replace("k\" title=\"", "");
            return _s;
        }

        #endregion

        private static Dictionary<string, Dictionary<string, string>> Makedic()
        {
            var DicTree = new Dictionary<string, Dictionary<string, string>>();
            var dic = new Dictionary<string, string>();
            dic.Add("亚洲色图", "http://www.yiren02.com/se/yazhousetu");
            dic.Add("真实自拍", "http://www.yiren02.com/se/zhenshizipai");
            dic.Add("成人卡通", "http://www.yiren02.com/se/chengrenkatong");
            dic.Add("精品套图", "http://www.yiren02.com/se/jingpintaotu");
            dic.Add("欧美色图", "http://www.yiren02.com/se/oumeisetu");
            dic.Add("风韵熟女", "http://www.yiren02.com/se/shunvluanlun");
            dic.Add("美腿丝袜", "http://www.yiren02.com/se/meituisiwa");
            dic.Add("另类同人1", "http://www.yiren02.com/se/tongxingzhuanqu");
            DicTree.Add("图片", dic);
            dic = new Dictionary<string, string>();
            dic.Add("现代激情", "http://www.yiren02.com/se/dushijiqing");
            dic.Add("家庭乱伦", "http://www.yiren02.com/se/jiatingluanlun");
            dic.Add("淫色人妻", "http://www.yiren02.com/se/yinqijiaohuan");
            dic.Add("情色武侠", "http://www.yiren02.com/se/gudianwuxia");
            dic.Add("校园春色", "http://www.yiren02.com/se/xiaoyuanchunse");
            dic.Add("长篇连载", "http://www.yiren02.com/se/changpianlianzai");
            dic.Add("黄色笑话", "http://www.yiren02.com/se/huangsexiaohua");
            dic.Add("暴力强奸", "http://www.yiren02.com/se/qiangjianxilie");
            DicTree.Add("小说", dic);
            dic = new Dictionary<string, string>();
            dic.Add("亚洲情色", "http://www.yiren02.com/se/yazhouqingseAV");
            dic.Add("制服丝袜", "http://www.yiren02.com/se/zhifusiwaAV");
            dic.Add("自拍视频", "http://www.yiren02.com/se/zipaishipin");
            dic.Add("欧美情色", "http://www.yiren02.com/se/oumeiqingseAV");
            dic.Add("卡通动漫", "http://www.yiren02.com/se/katongdongman");
            dic.Add("另类同人2", "http://www.yiren02.com/se/tongxingAV");
            dic.Add("三级电影", "http://www.yiren02.com/se/sanjidianying");
            dic.Add("疯狂群交", "http://www.yiren02.com/se/fengkuangqunjiao");
            DicTree.Add("电影", dic);
            return DicTree;
        }
    }
    
    public static class Staticsignal
    {
        public static ManualResetEvent eventXm;
    }
}
