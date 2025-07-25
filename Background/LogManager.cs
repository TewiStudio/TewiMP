using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using TewiMP.DataEditor;

namespace TewiMP.Background
{
    [Flags]
    public enum LogLevel
    {
        None = 0,
        Info = 1,
        Warning = 2,
        Error = 4,
        All = Info | Warning | Error
    }
    public class LogData
    {
        public DateTime LogTime { get; set; }
        public LogLevel LogLevel { get; set; }
        public string LogName { get; set; }
        public string LogContent { get; set; }
    }

    public class LogManager
    {
        public delegate void LogEventHandler(LogData logData);
        public event LogEventHandler LogListAdded;

        public static void Log(string name, string content, LogLevel logLevel = LogLevel.Info)
        {
            App.Instance?.logManager.LogInstance(name, content, logLevel);
        }

        public static void LogIf(bool b, string name, string content, LogLevel logLevel = LogLevel.Info)
        {
            if (b) Log(name, content, logLevel);
        }

        public static void Info(string name, string content) => Log(name, content, LogLevel.Info);
        public static void Warning(string name, string content) => Log(name, content, LogLevel.Warning);
        public static void Error(string name, string content) => Log(name, content, LogLevel.Error);

        public ObservableCollection<LogData> LogDatas { get; set; } = [];

        public void LogInstance(string name, string content, LogLevel logLevel = LogLevel.Info)
        {
            App.MainWindowInstance?.Invoke(() =>
            {
                var ld = new LogData { LogTime = DateTime.Now, LogName = name, LogContent = content, LogLevel = logLevel };
                LogDatas.Add(ld);
                LogListAdded?.Invoke(ld);
            });
            var str = $"[{DateTime.Now}][{logLevel}][{name}]: {content}";
            Debug.WriteLine(str);
            WriteToLogStream(str);
        }

        public static string NowLogFilePath { get; private set; }
        public static DateTime StartTime;
        private static FileStream NowLog;
        private static StreamWriter NowLogWriter;
        private static object locker = new();
        public static void InitNowLog()
        {
            StartTime = DateTime.Now;
            NowLogFilePath = Path.Combine(DataFolderBase.RunLogFolder, DateTime.Now.ToFileTime().ToString());
            NowLog = new FileStream(NowLogFilePath, FileMode.CreateNew, FileAccess.Write);
            NowLogWriter = new StreamWriter(NowLog);
            WriteToLogStream($"{App.Instance.AppName} launched on {StartTime}");
            WriteToLogStream($"Version: {App.Instance.NowVersion}, built time: {App.Instance.NowVersion.ReleaseTime}");
            WriteToLogStream($"System: {Environment.OSVersion}\n");
            if (App.Instance.logManager is not null)
            {
                foreach (var l in App.Instance.logManager.LogDatas)
                {
                    WriteToLogStream($"[{l.LogTime}][{l.LogLevel}][{l.LogName}]: {l.LogContent}");
                }
            }
        }

        public static void WriteToLogStream(string text)
        {
            if (NowLog is null || NowLogWriter is null) return;
            lock (locker)
            {
                NowLogWriter.Write($"{text}\n");
                NowLogWriter.Flush();
                NowLog.Flush();
            }
        }

        public static async Task FlushStream()
        {
            await NowLogWriter.FlushAsync();
            await NowLog.FlushAsync();
        }

        public static void DisposeNowLogStream()
        {
            WriteToLogStream($"\nTewiMP stopped at {DateTime.Now}, running time: {DateTime.Now - StartTime}");
            NowLogWriter.Close();
            NowLogWriter?.Dispose();
            NowLog?.Close();
            NowLog?.Dispose();
            NowLogWriter = null;
            NowLog = null;
        }
    }
}
