using SpawnDev.BlazorJS;

namespace Anaglyphohol.Services
{
    public class LoggerService
    {
        BlazorJSRuntime JS;
        public LoggerService(BlazorJSRuntime js)
        {
            JS = js;
        }
        public LogLevel LogLevel { get; set; } = LogLevel.None;

        public void Log(LogLevel logLevel, string message)
        {

        }
        public void Trace(string message) => Log(LogLevel.Trace, message);
        public void Debug(string message) => Log(LogLevel.Debug, message);
        public void Info(string message) => Log(LogLevel.Information, message);
        public void Warn(string message) => Log(LogLevel.Warning, message);
        public void Error(string message) => Log(LogLevel.Error, message);
        public void Critical(string message) => Log(LogLevel.Critical, message);
    }
}
