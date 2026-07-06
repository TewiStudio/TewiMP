using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.ObjectModel;
using TewiMP.Core;
using TewiMP.Services.Storage;

namespace TewiMP.Services;

public class LogService
{
    public delegate void LogEventHandler(LogData logData);
    public event LogEventHandler LogListAdded;

    public static void Log(string name, string content, LogLevel logLevel = LogLevel.Info, bool writeToLogStream = true)
    {
        App.Instance?.LogService?.LogInstance(name, content, logLevel, writeToLogStream);
    }

    public static void LogIf(bool b, string name, string content, LogLevel logLevel = LogLevel.Info, bool writeToLogStream = true)
    {
        if (b) Log(name, content, logLevel, writeToLogStream);
    }

    public static void Info(string name, string content, bool writeToLogStream = true) => Log(name, content, LogLevel.Info, writeToLogStream);
    public static void Warning(string name, string content, bool writeToLogStream = true) => Log(name, content, LogLevel.Warning, writeToLogStream);
    public static void Error(string name, string content, bool writeToLogStream = true) => Log(name, content, LogLevel.Error, writeToLogStream);
    public static void LogDebug(string content, bool writeToLogStream = false) => Log("Debug", content, LogLevel.Info, writeToLogStream);
    public static TimeSpan Elapsed(string name, string content, DateTime lastTime, bool writeToLogStream = true)
    {
        var elapsedTime = DateTime.Now - lastTime;
        Log(name, string.Format(content, $"{elapsedTime.TotalMilliseconds:0}ms"), writeToLogStream: writeToLogStream);
        return elapsedTime;
    }

    public ObservableCollection<LogData> LogDatas { get; set; } = [];

    public void LogInstance(string name, string content, LogLevel logLevel = LogLevel.Info, bool writeToLogStream = true)
    {
        App.MainWindowInstance?.Invoke(() =>
        {
            var ld = new LogData { LogTime = DateTime.Now, LogName = name, LogContent = content, LogLevel = logLevel };
            LogDatas.Add(ld);
            LogListAdded?.Invoke(ld);
        });
        var str = $"[{DateTime.Now}][{logLevel}][{name}]: {content}";
        Debug.WriteLine(str);
        if (writeToLogStream) WriteToLogStream(str);
    }

    public static string NowLogFilePath { get; private set; }
    public static DateTime StartTime;
    private static FileStream NowLog;
    private static StreamWriter NowLogWriter;
    private static Lock locker = new();
    public static void InitNowLog()
    {
        StartTime = DateTime.Now;
        NowLogFilePath = Path.Combine(DataFolderBase.RunLogFolder, DateTime.Now.ToFileTime().ToString());
        NowLog = new FileStream(NowLogFilePath, FileMode.CreateNew, FileAccess.Write);
        NowLogWriter = new StreamWriter(NowLog);
        WriteToLogStream($"{App.Instance.AppName} launched on {StartTime}");
        WriteToLogStream($"Version: {App.Instance.NowVersion}, built time: {App.Instance.NowVersion.ReleaseTime}");
        WriteToLogStream($"System: {Environment.OSVersion}\n");
        if (App.Instance.LogService is not null)
        {
            foreach (var l in App.Instance.LogService.LogDatas)
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
            try
            {
                NowLogWriter.Write($"{text}\n");
                NowLogWriter.Flush();
                NowLog.Flush();
            }
            catch
            {
                LogService.Error("LogManager", "Failed to write to log stream.", false); // 当文件无法写入时，取消写入以避免无限递归
            }
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
