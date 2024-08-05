using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace InternalDevelopmentTestTask
{
    class Program
    {
        public class Options
        {
            [Option('s', "source", Required = true, HelpText = "Source folder path.")]
            public string Source { get; set; }

            [Option('r', "replica", Required = true, HelpText = "Replica folder path.")]
            public string Replica { get; set; }

            [Option('i', "interval", Required = true, HelpText = "Synchronization interval in seconds.")]
            public int Interval { get; set; }

            [Option('l', "log", Required = true, HelpText = "Log file path.")]
            public string Log { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(opts => RunOptionsAndReturnExitCode(opts));
        }

        static void RunOptionsAndReturnExitCode(Options opts)
        {
            var sourcePath = opts.Source;
            var replicaPath = opts.Replica;
            var interval = opts.Interval;
            var logFilePath = opts.Log;

            using (var logStream = new StreamWriter(logFilePath, true))
            {
                while (true)
                {
                    FoldersSync(sourcePath, replicaPath, logStream);
                    Thread.Sleep(interval * 1000);
                }
            }
        }

        static void FoldersSync(string source, string replica, StreamWriter logStream)
        {
            foreach (var srcDir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                var relDir = Path.GetRelativePath(source, srcDir);
                var destDir = Path.Combine(replica, relDir);

                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                    Log($"Created directory: {destDir}", logStream);
                }
            }

            foreach (var srcFile in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var relPath = Path.GetRelativePath(source, srcFile);
                var destFile = Path.Combine(replica, relPath);

                if (!File.Exists(destFile) || !FilesAreEqual(srcFile, destFile))
                {
                    File.Copy(srcFile, destFile, true);
                    Log($"Copied file from {srcFile} to {destFile}", logStream);
                }
            }

            foreach (var repFile in Directory.GetFiles(replica, "*", SearchOption.AllDirectories))
            {
                var relPath = Path.GetRelativePath(replica, repFile);
                var srcFile = Path.Combine(source, relPath);

                if (!File.Exists(srcFile))
                {
                    File.Delete(repFile);
                    Log($"Removed file: {repFile}", logStream);
                }
            }

            foreach (var repDir in Directory.GetDirectories(replica, "*", SearchOption.AllDirectories))
            {
                var relDir = Path.GetRelativePath(replica, repDir);
                var srcDir = Path.Combine(source, relDir);

                if (!Directory.Exists(srcDir))
                {
                    Directory.Delete(repDir, true);
                    Log($"Removed directory: {repDir}", logStream);
                }
            }
        }

        static bool FilesAreEqual(string file1, string file2)
        {
            using (var hashAlgorithm = SHA256.Create())
            {
                var file1Hash = hashAlgorithm.ComputeHash(File.ReadAllBytes(file1));
                var file2Hash = hashAlgorithm.ComputeHash(File.ReadAllBytes(file2));

                return StructuralComparisons.StructuralEqualityComparer.Equals(file1Hash, file2Hash);
            }
        }

        static void Log(string message, StreamWriter logStream)
        {
            var logMessage = $"{DateTime.Now} - {message}";
            Console.WriteLine(logMessage);
            logStream.WriteLine(logMessage);
            logStream.Flush();
        }
    }
}
