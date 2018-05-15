/***********************************************************************
* 文 件 名：GZipHelper.cs
* CopyRight(C) 2016-2020 中国XX工程技术有限公司
* 文件编号：201603230002
* 创 建 人：张华斌
* 创建日期：2016-03-23
* 修 改 人：
* 修改日期：
* 描    述：GZip文件操作类
* 
*
*
* 示例：
            try
            {
                Console.WriteLine();
                Console.WriteLine("正在压缩文件...");
                ProgressBar progressBar = new ProgressBar(Console.CursorLeft, Console.CursorTop, 50, ProgressBarType.Character);//星号进度条
                GZipHelper.Compress(@"D:\Temp\book.pdf", progressBar.Dispaly);
                Console.WriteLine();

                Console.WriteLine();
                Console.WriteLine("正在解压文件...");
                progressBar = new ProgressBar(Console.CursorLeft, Console.CursorTop, 50, ProgressBarType.Multicolor);//彩色进度条
                GZipHelper.Decompress(@"D:\Temp\book.7z", @"D:\Temp\book.pdf", progressBar.Dispaly);
                Console.WriteLine();
               
            }
            catch (System.ArgumentOutOfRangeException ex)
            {
                Console.Beep();
                Console.WriteLine("进度条宽度超出可显示区域！");
            }
            finally
            {
                Console.WriteLine();
                Console.WriteLine("操作完成，按任意键退出！");
                Console.ReadKey(true);
            }
* 
* 
***********************************************************************/

using System;
using System.IO;
using System.IO.Compression;

namespace YirenReaderConsole
{
    /// <summary>
    /// GZip文件操作类；
    /// </summary>
    public class GZipHelper
    {
        /// <summary>
        /// 压缩文件；
        /// </summary>
        /// <param name="inputFileName">输入文件</param>
        /// <param name="dispalyProgress">进度条显示函数</param>
        public static void Compress(string inputFileName, Func<int, int> dispalyProgress = null)
        {
            using (FileStream inputFileStream = File.Open(inputFileName, FileMode.Open))
            {
                using (FileStream outputFileStream = new FileStream(Path.Combine(Path.GetDirectoryName(inputFileName), string.Format("{0}.7z", Path.GetFileNameWithoutExtension(inputFileName))), FileMode.Create, FileAccess.Write))
                {
                    using (GZipStream gzipStream = new GZipStream(outputFileStream, CompressionMode.Compress))
                    {
                        byte[] buffer = new byte[1024];
                        int count = 0;
                        while ((count = inputFileStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            gzipStream.Write(buffer, 0, count);
                            if (dispalyProgress != null) { dispalyProgress(Convert.ToInt32((inputFileStream.Position / (inputFileStream.Length * 1.0)) * 100)); }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 解压文件
        /// </summary>
        /// <param name="inputFileName">输入文件</param>
        /// <param name="outFileName">输出文件</param>
        /// <param name="dispalyProgress">进度条显示函数</param>
        public static void Decompress(string inputFileName, string outFileName, Func<int, int> dispalyProgress = null)
        {
            using (FileStream inputFileStream = File.Open(inputFileName, FileMode.Open))
            {
                using (FileStream outputFileStream = new FileStream(outFileName, FileMode.Create, FileAccess.Write))
                {
                    using (GZipStream decompressionStream = new GZipStream(inputFileStream, CompressionMode.Decompress))
                    {
                        byte[] buffer = new byte[1024];
                        int count = 0;
                        while ((count = decompressionStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            outputFileStream.Write(buffer, 0, count);
                            if (dispalyProgress != null) { dispalyProgress(Convert.ToInt32((inputFileStream.Position / (inputFileStream.Length * 1.0)) * 100)); }
                        }
                    }
                }
            }
        }
    }
}