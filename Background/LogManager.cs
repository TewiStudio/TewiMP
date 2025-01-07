using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace TewiMP.Background
{
    public enum LogLevel { Information, Waring, Error }
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
            Debug.WriteLine($"[{DateTime.Now}][{logLevel}][{name}]: {content}");
        }

        public void LogIf(bool b, string name, string content, LogLevel logLevel = LogLevel.Information)
        {
            if (b) Log(name, content, logLevel);
        }
    }
}
