using Grpc.Core;
using Sonarlint;
using System;
using System.Collections.Generic;
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
        private static readonly string DAEMON_HOST = "localhost";
        private static readonly int DEFAULT_DAEMON_PORT = 8050;

        public const string daemonVersion = "2.12.0.492";
        private const string uriFormat = "https://repox.sonarsource.com/sonarsource-public-builds/org/sonarsource/sonarlint/core/sonarlint-daemon/{0}/sonarlint-daemon-{0}-windows.zip";
        private readonly string version;
        private readonly string tmpPath;
        private readonly string storagePath;
        private int port;

        private Process process;

        private readonly string workingDirectory;

        public SonarLintDaemon() : this(daemonVersion, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Path.GetTempPath())
        {
        }

        public SonarLintDaemon(string version, string storagePath, string tmpPath)
        {
            this.version = version;
            this.tmpPath = tmpPath;
            this.storagePath = storagePath;
            this.workingDirectory = CreateTempDirectory();
        }

        public void Dispose()
        {
            if (Directory.Exists(workingDirectory))
            {
                Directory.Delete(workingDirectory, true);
            }

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
            if (!IsInstalled)
            {
                throw new InvalidOperationException("Daemon is not installed");
            }

            if (process != null)
            {
                process.Close();
            }

            port = TcpUtil.FindFreePort(DEFAULT_DAEMON_PORT);
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

        public int Port => port;

        public bool IsInstalled => Directory.Exists(InstallationPath) && File.Exists(ExePath);

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

        private string CreateTempDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), "SonarLintDaemon", Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public void RequestAnalysis(string path, string charset)
        {
            Analyze(path, charset);
        }

        private async void Analyze(string path, string charset)
        {
            var request = new AnalysisReq
            {
                BaseDir = path,
                WorkDir = workingDirectory,
            };
            request.File.Add(new InputFile
            {
                Path = path,
                Charset = charset,
            });

            var channel = new Channel(string.Join(":", DAEMON_HOST, port), ChannelCredentials.Insecure);
            var client = new StandaloneSonarLint.StandaloneSonarLintClient(channel);

            using (var call = client.Analyze(request))
            {
                try
                {
                    await ProcessIssues(call, path);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Call to client.Analyze failed: {0}", e);
                }
            }

            await channel.ShutdownAsync();
        }

        private async System.Threading.Tasks.Task ProcessIssues(AsyncServerStreamingCall<Issue> call, string path)
        {
            var issues = new List<Issue>();

            while (await call.ResponseStream.MoveNext())
            {
                var issue = call.ResponseStream.Current;
                issues.Add(issue);
            }

            TaggerProvider.Instance.UpdateIssues(path, issues);
        }
    }
}
