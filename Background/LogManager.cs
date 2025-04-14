using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using TewiMP.DataEditor;
using TewiMP.Helpers;

namespace TewiMP.Background
{
    public enum LogLevel { Information, Warning, Error }
    public class LogData
    {
        public DateTime LogTime { get; set; }
        public LogLevel LogLevel { get; set; }
        public string LogName { get; set; }
        public string LogContent { get; set; }
    }

    public class LogManager
    {
        public ObservableCollection<LogData> LogDatas { get; set; } = [];

        public void Log(string name, string content, LogLevel logLevel = LogLevel.Information)
        {
            MainWindow.Invoke(() =>
            {
                LogDatas.Add(new LogData { LogTime = DateTime.Now, LogName = name, LogContent = content, LogLevel = logLevel });
            });
            var str = $"[{DateTime.Now}][{logLevel}][{name}]: {content}";
            Debug.WriteLine(str);
            WriteToLogStream(str);
        }

        public void LogIf(bool b, string name, string content, LogLevel logLevel = LogLevel.Information)
        {
            if (b) Log(name, content, logLevel);
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
            WriteToLogStream($"{App.AppName} launched on {StartTime}");
            WriteToLogStream($"Version: {App.Version}, built time: {App.Version.ReleaseTime}");
            WriteToLogStream($"System: {Environment.OSVersion}\n");
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
