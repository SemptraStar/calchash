using System;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

namespace calchash
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = "";

            if (args.Length != 0)
            {
                path = args[0];
            }
            else
            {
                path = Directory.GetCurrentDirectory();
            }

            string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);

            int cores = Environment.ProcessorCount;
            List<byte[]> SHAList = new List<byte[]>();

            var currentProcess = Process.GetCurrentProcess();
            double startProcessorTime = currentProcess.UserProcessorTime.TotalMilliseconds;          

            if (cores == 1)
                SHAList = CalculateFilesSHASingleCore(files);
            else
                SHAList = CalculateFilesSHAMultyCore(files, cores);

            double endProcessorTime = currentProcess.UserProcessorTime.TotalMilliseconds;

            double totalFilesSizeInMB = GetFilesSize(files) / 1048576.0;
            double totalProcessorTimeInSec = (endProcessorTime - startProcessorTime) / 1000.0;

            double totalProcessorTime = totalFilesSizeInMB / totalProcessorTimeInSec;

            string outputFile = Directory.GetCurrentDirectory() +
                Path.DirectorySeparatorChar +
                "file.txt";

            WriteSHAToFile(outputFile, files, SHAList);

            File.AppendAllText(outputFile,
                String.Format("{0}Performance: {1} MB/s (by CPU time)",
                    Environment.NewLine,
                    totalProcessorTime));

            OpenFile(outputFile);
        }

        static double GetFilesSize(string[] files)
        {
            double totalSize = 0;

            foreach (var file in files)
            {
                totalSize += new FileInfo(file).Length;
            }

            return totalSize;
        }

        static List<byte[]> CalculateFilesSHASingleCore(string[] files)
        {
            List<byte[]> SHAList = new List<byte[]>();
            SHA256 sha256 = SHA256.Create();

            foreach (var file in files)
            {
                SHAList.Add(CalculateSHA(file, sha256));
            }

            return SHAList;
        }

        static List<byte[]> CalculateFilesSHAMultyCore(string[] files, int cores)
        {
            byte[][] SHAList = new byte[files.Length][];
            int part = (int)Math.Ceiling((float)files.Length / cores);

            Parallel.For(0, cores, i =>
            {
                SHA256 sha256 = SHA256.Create();
                int lastIndex = part * i + part;

                for (int fileIndex = part * i;
                     fileIndex < lastIndex && fileIndex < files.Length;
                     fileIndex++)
                {
                    SHAList[fileIndex] = CalculateSHA(files[fileIndex], sha256);
                }
            });

            return SHAList.ToList();
        }

        static byte[] CalculateSHA(string filePath, SHA256 sha256)
        {
            using (var fstream = File.OpenRead(filePath))
            {
                return sha256.ComputeHash(fstream);
            }
        }

        static void WriteSHAToFile(string path, string[] files, List<byte[]> SHAList)
        {
            File.Create(path).Close();

            for (int i = 0; i < files.Length; i++)
            {
                File.AppendAllText(path,
                    String.Format("{0} - SHA-256: {1}{2}",
                        files[i],
                        Encoding.Default.GetString(SHAList[i]),
                        Environment.NewLine)
                );
            }
        }

        static void OpenFile(string path)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            };

            using (var process = new Process())
            {
                process.StartInfo = processStartInfo;
                process.Start();
            }
        }
    }
}
