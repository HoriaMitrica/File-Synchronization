using System;
using System.Threading;
using System.Threading.Tasks;

namespace VeeamTask
{
    class Program
    {
        static async Task Main()
        {
            Console.WriteLine("Enter source folder:");
            string source = Console.ReadLine() ?? string.Empty;

            Console.WriteLine("Enter replica folder:");
            string replica = Console.ReadLine() ?? string.Empty;

            Console.WriteLine("Enter sync interval (seconds):");
            if (!int.TryParse(Console.ReadLine(), out int interval) || interval <= 0)
            {
                Console.WriteLine("Invalid interval. Exiting...");
                return;
            }

            Console.WriteLine("Enter log folder path:");
            string logFolder = Console.ReadLine() ?? string.Empty;

            SyncManager syncManager = new();
            CancellationTokenSource cts = new();

            // AppendLog function: Logs to console for CLI mode
            void AppendLog(string message) => Console.WriteLine(message);

            Console.WriteLine("Starting synchronization... Press any key to stop.");
            Task syncTask = syncManager.StartSync(source, replica, interval, logFolder, AppendLog, cts.Token);

            Console.ReadKey();
            cts.Cancel(); // Stop sync when key is pressed
            await syncTask;
            Console.WriteLine("Synchronization stopped.");
        }
    }
}
