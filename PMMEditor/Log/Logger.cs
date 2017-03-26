using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;
using Reactive.Bindings.Notifiers;

namespace PMMEditor.Log
{
    public struct LogLevel
    {
        public static readonly LogLevel Trace = new LogLevel("Trace", 0);
        public static readonly LogLevel Debug = new LogLevel("Debug", 1);
        public static readonly LogLevel Info = new LogLevel("Info", 2);
        public static readonly LogLevel Warn = new LogLevel("Warn", 3);
        public static readonly LogLevel Error = new LogLevel("Error", 4);
        public static readonly LogLevel Fatal = new LogLevel("Fatal", 5);

        public LogLevel(string name, int level)
        {
            Name = name;
            Level = level;
        }

        public string Name { get; }

        public int Level { get; }
    }

    public class LogMessage
    {
        public DateTime CreatedAt { get; }

        public string Message { get; }

        public LogLevel Level { get; }

        public Exception Exception { get; }

        public bool HasException => Exception != null;

        public LogMessage(string message, LogLevel level, Exception e = null)
        {
            CreatedAt = DateTime.Now;
            Message = message;
            Level = level;
            Exception = e;
        }

        public override string ToString()
        {
            if (HasException)
            {
                return $"{Level.Name}:[{CreatedAt:G}] {Message} [{Exception.Message}] \n\nStackTrace:[{Exception.StackTrace}]";
            }
            return $"{Level.Name}:[{CreatedAt:G}] {Message}";
        }

        public static LogMessage CreateTrace(string message, Exception e = null)
        {
            return new LogMessage(message, LogLevel.Trace, e);
        }

        public static LogMessage CreateDebug(string message, Exception e = null)
        {
            return new LogMessage(message, LogLevel.Debug, e);
        }

        public static LogMessage CreateInfo(string message, Exception e = null)
        {
            return new LogMessage(message, LogLevel.Info, e);
        }

        public static LogMessage CreateWarn(string message, Exception e = null)
        {
            return new LogMessage(message, LogLevel.Warn, e);
        }

        public static LogMessage CreateError(string message, Exception e = null)
        {
            return new LogMessage(message, LogLevel.Error, e);
        }

        public static LogMessage CreateFatal(string message, Exception e = null)
        {
            return new LogMessage(message, LogLevel.Fatal, e);
        }
    }

    public class LogMessageNotifier : ScheduledNotifier<LogMessage>, ILogger
    {
        public LogMessageNotifier(IScheduler scheduler) : base(scheduler) { }
        public LogMessageNotifier() { }

        public void Trace(string message, Exception e = null) => Report(LogMessage.CreateTrace(message, e));
        public void Debug(string message, Exception e = null) => Report(LogMessage.CreateDebug(message, e));
        public void Info(string message, Exception e = null) => Report(LogMessage.CreateInfo(message, e));
        public void Warn(string message, Exception e = null) => Report(LogMessage.CreateWarn(message, e));
        public void Error(string message, Exception e = null) => Report(LogMessage.CreateError(message, e));
        public void Fatal(string message, Exception e = null) => Report(LogMessage.CreateFatal(message, e));
    }

    public interface ILogger
    {
        void Trace(string message, Exception e);
        void Debug(string message, Exception e);
        void Info(string message, Exception e);
        void Warn(string message, Exception e);
        void Error(string message, Exception e);
        void Fatal(string message, Exception e);
    }

    public static class LoggerOnTaskExtensions
    {
        public static Task ContinueOnlyOnFaultedErrorLog(
            this Task self, ILogger logger, string errorMessage, Action<Task> func)
        {
            return self.ContinueWith(t =>
            {
                logger.Error(errorMessage, t.Exception.InnerException);
                func?.Invoke(t);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public static Task ContinueOnlyOnFaultedErrorLog(
            this Task self, ILogger logger, string errorMessage = "Unknown", Action func = null)
        {
            return ContinueOnlyOnFaultedErrorLog(self, logger, errorMessage, t => func?.Invoke());
        }


        public static Task<U> ContinueOnlyOnFaultedErrorLog<T, U>(
            this Task<T> self, ILogger logger, string errorMessage, Func<Task<T>, U> func)
        {
            return self.ContinueWith(t =>
            {
                logger.Error(errorMessage, t.Exception.InnerException);
                return func.Invoke(t);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public static Task<U> ContinueOnlyOnFaultedErrorLog<T, U>(
            this Task<T> self, ILogger logger, string errorMessage, Func<U> func)
        {
            return ContinueOnlyOnFaultedErrorLog(self, logger, errorMessage, t => func());
        }
    }
}
