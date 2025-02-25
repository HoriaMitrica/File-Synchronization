using Avalonia.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace VeeamTask.GUI
{
    public partial class MainWindow : Window
    {
        private readonly SyncManager syncManager = new();
        private CancellationTokenSource? _cancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();

            BrowseSourceButton.Click += async (_, _) => await SelectFolder(SourceTextBox);
            BrowseReplicaButton.Click += async (_, _) => await SelectFolder(ReplicaTextBox);
            BrowseLogFolderButton.Click += async (_, _) => await SelectFolder(LogFolderTextBox);

            StartSyncButton.Click += async (_, _) => await StartSync();
            StopSyncButton.Click += (_, _) => StopSync();
        }

        private async Task SelectFolder(TextBox targetTextBox)
        {
            if (StorageProvider is { CanOpen: true })
            {
                var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Select Folder",
                    AllowMultiple = false
                });

                if (result?.Count > 0)
                {
                    targetTextBox.Text = result[0].Path.LocalPath;
                }
            }
        }
        private async Task StartSync()
        {
            string source = SourceTextBox.Text ?? string.Empty;
            string replica = ReplicaTextBox.Text ?? string.Empty;
            string logFolder = LogFolderTextBox.Text ?? string.Empty;

            ErrorLabel.Text = "";
            ErrorLabel.IsVisible = false;
            LogsTextBlock.Text = ""; // Clear previous logs

            if (!int.TryParse(IntervalTextBox.Text ?? "0", out int interval) || interval <= 0)
            {
                ShowError("❌ Please enter a valid positive number for interval!");
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            StartSyncButton.IsEnabled = false;
            StopSyncButton.IsEnabled = true;

            AppendLog($"✅ Sync started at {DateTime.Now:HH:mm:ss}");

            await Task.Run(async () =>
            {
                await syncManager.StartSync(source, replica, interval, logFolder, AppendLog, _cancellationTokenSource.Token);
            });

            AppendLog($"🛑 Sync stopped at {DateTime.Now:HH:mm:ss}");
        }

        private void StopSync()
        {
            _cancellationTokenSource?.Cancel();
            AppendLog($"🛑 Sync manually stopped at {DateTime.Now:HH:mm:ss}");
            StartSyncButton.IsEnabled = true;
            StopSyncButton.IsEnabled = false;
        }

        private void AppendLog(string message)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                LogsTextBlock.Text += message + Environment.NewLine;
            });
        }

        private void ShowError(string message)
        {
            ErrorLabel.Text = message;
            ErrorLabel.IsVisible = true;
        }
    }
}
