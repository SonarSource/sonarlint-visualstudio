using System;
using System.IO.Abstractions;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Roslyn.Suppressions.Settings.Cache;
using SonarLint.VisualStudio.Roslyn.Suppressions.SettingsFile;

namespace SonarLint.VisualStudio.Roslyn.Suppressions
{
    internal interface IContainer : IDisposable
    {
        ILogger Logger { get; }
        ISettingsCache SettingsCache { get; }
    }

    internal sealed class Container : IContainer
    {
        private static readonly object Locker = new object();
        private static IContainer _instance;
        
        private readonly ISuppressedIssuesFileWatcher fileWatcher;

        public ILogger Logger { get; }
        public ISettingsCache SettingsCache { get; }

        public Container()
        {
        }

        public Container(IFileSystem fileSystem)
        {
            fileSystem.Directory.CreateDirectory(RoslynSettingsFileInfo.Directory);

            Logger = new Logger();
            SettingsCache = new SettingsCache(Logger);
            fileWatcher = new SuppressedIssuesFileWatcher(SettingsCache, Logger);
        }

        public void Dispose()
        {
            fileWatcher?.Dispose();
        }

        public static IContainer Instance
        {
            get
            {
                lock (Locker)
                {
                    try
                    {
                        _instance ??= new Container();
                    }
                    catch
                    {
                        _instance = null;
                    }
                }

                return _instance;
            }
        }
    }

    internal class Logger : ILogger
    {
        public void WriteLine(string message)
        {
        }

        public void WriteLine(string messageFormat, params object[] args)
        {
        }
    }
}
