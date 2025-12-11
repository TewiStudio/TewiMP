using System;

namespace TewiMP.Core;

public class LogData
{
    public DateTime LogTime { get; set; }
    public LogLevel LogLevel { get; set; }
    public string LogName { get; set; }
    public string LogContent { get; set; }
}
