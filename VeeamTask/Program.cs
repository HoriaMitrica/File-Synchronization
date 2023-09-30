using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace VeeamTask
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Please introduce the correct arguments:");
                Console.WriteLine("<source folder> <replica folder> <interval in seconds> <log file>");
                return;
            }

            string sourceFolder = args[0];
            string replicaFolder = args[1];
            int interval = int.Parse(args[2]);
            string logFilePath = args[3];
            while (true)
            {
                SynchronizeFolders(sourceFolder, replicaFolder, logFilePath);
                Thread.Sleep(interval * 1000);
            }
        }

        static void SynchronizeFolders(string source, string replica, string logFilePath)
        {
            StreamWriter logFile = new StreamWriter(logFilePath, true);
            
            CreateSourceAndReplica(logFile,source, replica);
            DeleteFilesNotInSource(logFile,source,replica);
            CreateFilesInReplica(logFile,source, replica);
            string[] files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string relativePath = GetRelativePath(source, file);
                string replicaFile = replica + relativePath;
                string filePath = Path.GetDirectoryName(replicaFile);
                if (!File.Exists(replicaFile))
                {
                    LogMessage(logFile,"File created because it was found in Source and not in Replica: "+replicaFile);
                    File.Copy(file, replicaFile, true);
                }
                
            }
            LogMessage(logFile, "Synchronization completed at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            logFile.Close();
            }

            static string GetRelativePath(string source, string file)
            {
                string path = Path.GetDirectoryName(file);
                string filename = "\\" + Path.GetFileName(file);
                if (source.Length == path.Length)
                    return filename;
                return path.Substring(source.Length, path.Length - source.Length) + filename;
            }

            static void LogMessage(StreamWriter logFile, string message)
            {
                logFile.WriteLine(message + "\n");
                Console.WriteLine(message + "\n");
            }

            static void CreateSourceAndReplica(StreamWriter logFile,string source, string replica)
            {
                if (!Directory.Exists(source))
                {
                    LogMessage(logFile,"Created folder: "+source);
                    Directory.CreateDirectory(source);
                }
                if (!Directory.Exists(replica))
                {
                    LogMessage(logFile,"Created folder: "+replica);
                    Directory.CreateDirectory(replica);
                }
            }

            static void CreateFilesInReplica(StreamWriter logFile, string source, string replica)
            {
                DirectoryInfo source_info = new DirectoryInfo(source);
                foreach (DirectoryInfo subDirectory in source_info.GetDirectories())
                {
                    string subfolderPath = Path.Combine(replica, subDirectory.Name);
                    if (!Directory.Exists(subfolderPath))
                    {
                        Directory.CreateDirectory(Path.Combine(replica,subDirectory.Name));
                        LogMessage(logFile,
                            "Folder created because it was in Source but not in Replica: "+subfolderPath); 
                    }
                    else
                    { 
                        CreateFilesInReplica(logFile,subDirectory.FullName, subfolderPath);
                    }
                }
            }
            static void DeleteFilesNotInSource(StreamWriter logFile, string source, string replica)
            {
                DirectoryInfo source_info = new DirectoryInfo(source);
                DirectoryInfo replica_info = new DirectoryInfo(replica);
                string[] sourceFileNames = source_info.GetFiles().Select(file => file.Name).ToArray();
                foreach (FileInfo replicaFile in replica_info.GetFiles())
                {
                    if (!sourceFileNames.Contains(replicaFile.Name))
                    {
                        replicaFile.Delete();
                        LogMessage(logFile,
                            "File deleted because it was not found in Source: " + replicaFile.FullName);
                    }
                }
                foreach (DirectoryInfo subfolder in replica_info.GetDirectories())
                {
                    string subfolderPath = Path.Combine(source, subfolder.Name);
                    if (!Directory.Exists(subfolderPath))
                    {
                        subfolder.Delete(true);
                        LogMessage(logFile,
                            "Folder deleted because it was not found in Source: "+subfolder.FullName); 
                    }
                    else
                    { 
                        DeleteFilesNotInSource(logFile,subfolderPath, subfolder.FullName);
                    }
                }
            }
        }
    }
