using System;

namespace AFLCoachSim.Core.Infrastructure.Logging
{
    /// <summary>
    /// Static logger helper for the Core assembly to provide easy replacement for Debug.Log
    /// </summary>
    public static class CoreLogger
    {
        private static ILogger _logger = NullLogger.Instance;
        
        /// <summary>
        /// Set the logger implementation to use throughout the Core assembly
        /// </summary>
        public static void SetLogger(ILogger logger)
        {
            _logger = logger ?? NullLogger.Instance;
        }
        
        /// <summary>
        /// Log an informational message
        /// </summary>
        public static void Log(string message)
        {
            _logger.Log(message);
        }
        
        /// <summary>
        /// Log a warning message
        /// </summary>
        public static void LogWarning(string message)
        {
            _logger.LogWarning(message);
        }
        
        /// <summary>
        /// Log an error message
        /// </summary>
        public static void LogError(string message)
        {
            _logger.LogError(message);
        }
        
        /// <summary>
        /// Log an exception with optional message
        /// </summary>
        public static void LogError(Exception exception, string message = null)
        {
            _logger.LogError(exception, message);
        }
    }
}