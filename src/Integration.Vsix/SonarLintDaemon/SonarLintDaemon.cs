using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [Export(typeof(ISonarLintDaemon))]
    class SonarLintDaemon : ISonarLintDaemon
    {
        public const string daemonVersion = "2.12.0.492";
        private const string uriFormat = "https://repox.sonarsource.com/sonarsource-public-builds/org/sonarsource/sonarlint/core/sonarlint-daemon/{0}/sonarlint-daemon-{0}-windows.zip";
        private readonly string version;
        private readonly string tmpPath;
        private readonly string storagePath;
        private int port;

        private Process process;

        public SonarLintDaemon() : this(daemonVersion, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Path.GetTempPath())
        {
        }

        public SonarLintDaemon(string version, string storagePath, string tmpPath)
        {
            this.version = version;
            this.tmpPath = tmpPath;
            this.storagePath = storagePath;
        }

        public void Dispose()
        {
            if (IsRunning)
            {
                Stop();
            }
        }

        public void Install()
        {
            Download();
            Unzip();
        }

        public bool IsRunning => process != null && !process.HasExited;

        public void Start()
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("Process already running");
            }
            if (!IsInstalled())
            {
                throw new InvalidOperationException("Daemon is not installed");
            }

            if (process != null)
            {
                process.Close();
            }

            port = TcpUtil.FindFreePort(8050);
            process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = ExePath;
            process.StartInfo.Arguments = GetCmdArgs(port);
            process.StartInfo.CreateNoWindow = true;
            process.Start();
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                return;
                // throw exception?
            }
            process.Kill();
            process.WaitForExit();
        }

        public int Port()
        {
            return port;
        }

        public bool IsInstalled()
        {
            return Directory.Exists(InstallationPath) && File.Exists(ExePath);
        }

        private void Download()
        {
            string uri = string.Format(uriFormat, version);
            using (var client = new WebClient())
            {
                client.DownloadFile(uri, ZipFilePath);
            }
        }

        private void Unzip()
        {
            if (Directory.Exists(InstallationPath))
            {
                Directory.Delete(InstallationPath, true);
            }
            ZipFile.ExtractToDirectory(ZipFilePath, storagePath);
        }

        private string InstallationPath => Path.Combine(storagePath, $"sonarlint-daemon-{version}-windows");

        private string ZipFilePath => Path.Combine(tmpPath, $"sonarlint-daemon-{version}-windows.zip");

        private string ExePath => Path.Combine(InstallationPath, "jre", "bin", "java.exe");

        private string GetCmdArgs(int port)
        {
            string jarPath = Path.Combine(InstallationPath, "lib", $"sonarlint-daemon-{version}.jar");
            string logPath = Path.Combine(InstallationPath, "conf", "logback.xml");
            string className = "org.sonarlint.daemon.Daemon";

            return string.Format("-Djava.awt.headless=true" +
                " -cp \"{0}\"" +
                " \"-Dlogback.configurationFile={1}\"" +
                " \"-Dsonarlint.home={2}\"" +
                " {3}" +
                " \"--port\" \"{4}\"",
                jarPath, logPath, InstallationPath, className, port);
        }
    }
}
