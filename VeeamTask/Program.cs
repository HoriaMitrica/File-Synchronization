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
            CreateSourceAndReplica(source, replica);
            StreamWriter logFile = new StreamWriter(logFilePath, true);
            
            //Checks every directory in the replica and deletes any that are not found in the source
            DeleteFilesNotInSource(logFile,source,replica);
            
            //Creates the missing directories that are not found in the replica
            CreateFilesInReplica(logFile,source, replica);
            
            string[] files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string relativePath = GetRelativePath(source, file);
                string replicaFile = replica + relativePath;
                if (!File.Exists(replicaFile))
                {
                    LogMessage("File created because it was found in Source and not in Replica: "+replicaFile,logFile);
                    File.Copy(file, replicaFile, true);
                }
            }
            LogMessage( "Synchronization completed at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),logFile);
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

            static void LogMessage( string message,StreamWriter logFile=null)
            {
                if (logFile != null)
                {
                    logFile.WriteLine(message + "\n");
                }
                Console.WriteLine(message + "\n");
            }

            static void CreateSourceAndReplica(string source, string replica)
            {
                if (!Directory.Exists(source))
                {
                    LogMessage("Created folder: "+source);
                    Directory.CreateDirectory(source);
                }
                if (!Directory.Exists(replica))
                {
                    LogMessage("Created folder: "+replica);
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
                        LogMessage("Folder created because it was in Source but not in Replica: "+subfolderPath,
                            logFile); 
                    }
                    else
                    { 
                        //recursively checking the sub directories for any other directories
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
                        LogMessage("File deleted because it was not found in Source: " + replicaFile.FullName,
                            logFile);
                    }
                }
                foreach (DirectoryInfo subfolder in replica_info.GetDirectories())
                {
                    string subfolderPath = Path.Combine(source, subfolder.Name);
                    if (!Directory.Exists(subfolderPath))
                    {
                        subfolder.Delete(true);
                        LogMessage(
                            "Folder deleted because it was not found in Source: "+subfolder.FullName,
                            logFile); 
                    }
                    else
                    { 
                        //recursively checking the sub directories for any other directories
                        DeleteFilesNotInSource(logFile,subfolderPath, subfolder.FullName);
                    }
                }
            }
        }
    }
