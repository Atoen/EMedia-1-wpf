namespace Emedia_1_wpf.Models;

public record Log(LogType Type, string Message);

public enum LogType
{
    Info,
    Warning,
    Error
}