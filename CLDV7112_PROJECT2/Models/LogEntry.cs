using System;

namespace CLDV7112_PROJECT2.Models
{
    /// <summary>
    /// Represents a single system audit log entry recorded from background Azure Functions and cloud operations.
    /// </summary>
    public class LogEntry
    {
        // Timestamp when the event occurred
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        // Log severity level (INFO, WARNING, ERROR, CONTRACT)
        public string Level { get; set; } = "INFO";

        // Detailed description of the event or Azure Function trigger response
        public string Message { get; set; } = string.Empty;
    }
}
