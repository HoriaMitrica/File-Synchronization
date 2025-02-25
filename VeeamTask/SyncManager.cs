using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VeeamTask
{
    public class SyncManager
    {
        public async Task StartSync(string sourceFolder, string replicaFolder, int interval, string logFolderPath, Action<string> AppendLog, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    string logFilePath = CreateLogFile(logFolderPath);
                    AppendLog($"📂 Sync started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    SynchronizeFolders(sourceFolder, replicaFolder, logFilePath, AppendLog);
                }
                catch (Exception ex)
                {
                    AppendLog($"❌ Error: {ex.Message}");
                }
                await Task.Delay(interval * 1000, cancellationToken);
            }
        }

        private static string CreateLogFile(string logFolderPath)
        {
            if (!Directory.Exists(logFolderPath))
            {
                Directory.CreateDirectory(logFolderPath);
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            return Path.Combine(logFolderPath, $"log_{timestamp}.txt");
        }


        private static void SynchronizeFolders(string source, string replica, string logFilePath, Action<string> AppendLog)
        {
            if (!Directory.Exists(source) || !Directory.Exists(replica))
            {
                AppendLog("⚠ Source or replica folder does not exist.");
                return;
            }

            using StreamWriter logFile = new StreamWriter(logFilePath, true);

            string startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            AppendLogAndWriteToFile(logFile, $"📂 Synchronization started at {startTime}", AppendLog);

            DeleteFilesNotInSource(logFile, source, replica, AppendLog);
            CreateFilesInReplica(logFile, source, replica, AppendLog);

            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(source, file);
                string replicaFile = Path.Combine(replica, relativePath);
                if (!File.Exists(replicaFile))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(replicaFile) ?? string.Empty);
                    File.Copy(file, replicaFile, true);
                    AppendLogAndWriteToFile(logFile, $"✅ Copied: {replicaFile} ({DateTime.Now:HH:mm:ss})", AppendLog);
                }
            }

            string endTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            AppendLogAndWriteToFile(logFile, $"🎯 Synchronization completed at {endTime}", AppendLog);
        }


        private static void AppendLogAndWriteToFile(StreamWriter logFile, string message, Action<string> AppendLog)
        {
            string formattedMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}";
            logFile.WriteLine(formattedMessage);
            AppendLog(formattedMessage);
        }


        private static void DeleteFilesNotInSource(StreamWriter logFile, string source, string replica, Action<string> AppendLog)
        {
            foreach (FileInfo replicaFile in new DirectoryInfo(replica).GetFiles())
            {
                if (!File.Exists(Path.Combine(source, replicaFile.Name)))
                {
                    replicaFile.Delete();
                    AppendLogAndWriteToFile(logFile, $"🗑 Deleted: {replicaFile.FullName} ({DateTime.Now:HH:mm:ss})", AppendLog);
                }
            }
        }

        private static void CreateFilesInReplica(StreamWriter logFile, string source, string replica, Action<string> AppendLog)
        {
            foreach (DirectoryInfo subDir in new DirectoryInfo(source).GetDirectories())
            {
                string subfolderPath = Path.Combine(replica, subDir.Name);
                if (!Directory.Exists(subfolderPath))
                {
                    Directory.CreateDirectory(subfolderPath);
                    AppendLogAndWriteToFile(logFile, $"📁 Created folder: {subfolderPath} ({DateTime.Now:HH:mm:ss})", AppendLog);
                }
                CreateFilesInReplica(logFile, subDir.FullName, subfolderPath, AppendLog);
            }
        }
    }
}
