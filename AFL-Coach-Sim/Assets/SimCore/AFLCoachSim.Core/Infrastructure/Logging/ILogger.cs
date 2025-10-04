using System;

namespace AFLCoachSim.Core.Infrastructure.Logging
{
    /// <summary>
    /// Logging abstraction for the Core assembly to avoid Unity dependencies
    /// </summary>
    public interface ILogger
    {
        void Log(string message);
        void LogWarning(string message);
        void LogError(string message);
        void LogError(Exception exception, string message = null);
    }
    
    /// <summary>
    /// Console-based logger implementation that can be used without Unity
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Console.WriteLine($"[LOG] {DateTime.Now:HH:mm:ss} - {message}");
        }

        public void LogWarning(string message)
        {
            Console.WriteLine($"[WARN] {DateTime.Now:HH:mm:ss} - {message}");
        }

        public void LogError(string message)
        {
            Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} - {message}");
        }

        public void LogError(Exception exception, string message = null)
        {
            var msg = string.IsNullOrEmpty(message) ? exception.Message : $"{message}: {exception.Message}";
            Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} - {msg}");
        }
    }
    
    /// <summary>
    /// No-op logger that discards all log messages
    /// </summary>
    public class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new NullLogger();
        
        public void Log(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message) { }
        public void LogError(Exception exception, string message = null) { }
    }
}