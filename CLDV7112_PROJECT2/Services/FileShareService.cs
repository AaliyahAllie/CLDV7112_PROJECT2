using Azure.Storage.Files.Shares;
using CLDV7112_PROJECT2.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CLDV7112_PROJECT2.Services
{
    /// <summary>
    /// Service managing direct interaction with Azure File Shares ("contracts" share).
    /// Stores and reads audit log text files and vendor agreement documents in cloud file storage.
    /// </summary>
    public class FileShareService
    {
        private readonly ShareClient _shareClient;
        private readonly string _shareName = "contracts";

        public static readonly string[] LogFileNames =
        {
            "system-logs.txt",
            "order-logs.txt",
            "product-logs.txt",
            "customer-logs.txt",
            "error-logs.txt"
        };

        public FileShareService(string connectionString)
        {
            _shareClient = new ShareClient(connectionString, _shareName);
            EnsureShareAndFilesCreated();
        }

        // Ensures Azure File Share directory and log files exist
        private void EnsureShareAndFilesCreated()
        {
            try
            {
                _shareClient.CreateIfNotExists();
                var directory = _shareClient.GetRootDirectoryClient();

                foreach (var fileName in LogFileNames)
                {
                    var file = directory.GetFileClient(fileName);
                    if (!file.Exists())
                    {
                        AppendToFileInternal(fileName, "INFO", $"Log file '{fileName}' initialized in Azure File Share.");
                    }
                }
            }
            catch { }
        }

        // Log appending helper methods
        public Task AppendSystemLogAsync(string level, string message)
            => Task.Run(() => AppendToFileInternal("system-logs.txt", level, message));

        public Task AppendOrderLogAsync(string level, string message)
            => Task.Run(() => AppendToFileInternal("order-logs.txt", level, message));

        public Task AppendProductLogAsync(string level, string message)
            => Task.Run(() => AppendToFileInternal("product-logs.txt", level, message));

        public Task AppendCustomerLogAsync(string level, string message)
            => Task.Run(() => AppendToFileInternal("customer-logs.txt", level, message));

        public Task AppendErrorLogAsync(string level, string message)
            => Task.Run(() => AppendToFileInternal("error-logs.txt", level, message));

        public void AppendLog(string level, string message)
            => AppendToFileInternal("system-logs.txt", level, message);

        public Task AppendLogAsync(string level, string message)
            => Task.Run(() => AppendToFileInternal("system-logs.txt", level, message));

        // Synchronous internal writer for Azure File Share log ranges
        private void AppendToFileInternal(string fileName, string level, string message)
        {
            try
            {
                var directory = _shareClient.GetRootDirectoryClient();
                var file = directory.GetFileClient(fileName);

                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                string logLine = $"[{timestamp}] [{level.ToUpper()}] {message}\n";
                byte[] bytes = Encoding.UTF8.GetBytes(logLine);

                if (!file.Exists())
                {
                    file.Create(bytes.Length);
                    using var ms = new MemoryStream(bytes);
                    file.UploadRange(new Azure.HttpRange(0, bytes.Length), ms);
                }
                else
                {
                    long currentSize = file.GetProperties().Value.ContentLength;
                    file.Create(currentSize + bytes.Length);

                    using var ms = new MemoryStream(bytes);
                    file.UploadRange(new Azure.HttpRange(currentSize, bytes.Length), ms);
                }
            }
            catch { }
        }

        // Reads log file entries from Azure File Share
        public async Task<List<LogEntry>> ReadLogFileAsync(string fileName)
        {
            var entries = new List<LogEntry>();
            try
            {
                var directory = _shareClient.GetRootDirectoryClient();
                var file = directory.GetFileClient(fileName);

                if (!await file.ExistsAsync()) return entries;

                var download = await file.DownloadAsync();
                using var reader = new StreamReader(download.Value.Content, Encoding.UTF8);

                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (line.StartsWith("[") && line.Contains("]"))
                    {
                        var closeBracket = line.IndexOf(']');
                        var timestamp = line.Substring(1, closeBracket - 1);
                        var remainder = line.Substring(closeBracket + 1).Trim();

                        string level = "INFO";
                        string msg = remainder;

                        if (remainder.StartsWith("["))
                        {
                            var closeLevel = remainder.IndexOf(']');
                            level = remainder.Substring(1, closeLevel - 1);
                            msg = remainder.Substring(closeLevel + 1).Trim();
                        }

                        entries.Add(new LogEntry
                        {
                            Timestamp = timestamp,
                            Level = level,
                            Message = msg
                        });
                    }
                    else
                    {
                        entries.Add(new LogEntry
                        {
                            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                            Level = "INFO",
                            Message = line
                        });
                    }
                }
            }
            catch { }

            return entries;
        }

        // Clears a log file in Azure File Share
        public async Task ClearLogFileAsync(string fileName)
        {
            var directory = _shareClient.GetRootDirectoryClient();
            var file = directory.GetFileClient(fileName);
            if (await file.ExistsAsync())
            {
                await file.DeleteAsync();
                await Task.Run(() => AppendToFileInternal(fileName, "INFO", $"Log file '{fileName}' cleared and reset."));
            }
        }

        // Clears all log files in Azure File Share
        public async Task ClearLogsAsync()
        {
            foreach (var fn in LogFileNames)
            {
                await ClearLogFileAsync(fn);
            }
        }

        public Task<List<LogEntry>> ReadLogsAsync() => ReadLogFileAsync("system-logs.txt");
    }
}
