
using System;
using System.Collections.Generic;
using System.Text;

namespace SonarJsConfig
{
    public interface ILogger
    {
        void LogMessage(string message);
        void LogError(string message);
    }

    public class ConsoleLogger : ILogger
    {
        void ILogger.LogError(string message)
        {
            Console.Error.WriteLine(message);
        }

        void ILogger.LogMessage(string message)
        {
            Console.WriteLine(message);
        }
    }
}
