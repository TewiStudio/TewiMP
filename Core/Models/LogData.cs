namespace TewiMP.Core.Models;

using System;

public class LogData
{
    public DateTime LogTime { get; set; }
    public LogLevel LogLevel { get; set; }
    public string LogName { get; set; }
    public string LogContent { get; set; }
}
