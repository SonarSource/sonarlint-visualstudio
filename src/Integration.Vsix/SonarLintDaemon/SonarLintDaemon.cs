/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using Grpc.Core;
using Sonarlint;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [Export(typeof(ISonarLintDaemon))]
    class SonarLintDaemon : ISonarLintDaemon
    {
        private static readonly string DAEMON_HOST = "localhost";
        private static readonly int DEFAULT_DAEMON_PORT = 8050;

        public const string daemonVersion = "2.13.0.606";
        private const string uriFormat = "https://sonarsource.bintray.com/Distribution/sonarlint-daemon/sonarlint-daemon-{0}-windows.zip";
        private readonly string version;
        private readonly string tmpPath;
        private readonly string storagePath;
        private int port;

        private Process process;

        private readonly string workingDirectory;

        public event DownloadProgressChangedEventHandler DownloadProgressChanged;
        public event AsyncCompletedEventHandler DownloadCompleted;

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
            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    FileName = ExePath,
                    Arguments = GetCmdArgs(port),
                    CreateNoWindow = true
                }
            };
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
            Uri uri = new Uri(string.Format(uriFormat, version));
            using (var client = new WebClient())
            {
                client.DownloadProgressChanged += (sender, args) => DownloadProgressChanged?.Invoke(sender, args);
                client.DownloadFileCompleted += Unzip;
                client.DownloadFileCompleted += (sender, args) => DownloadCompleted?.Invoke(sender, args);
                client.DownloadFileAsync(uri, ZipFilePath);
            }
        }

        private void Unzip(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                return;
            }

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

        public void RequestAnalysis(string path, string charset, IIssueConsumer consumer)
        {
            Analyze(path, charset, consumer);
        }

        private async void Analyze(string path, string charset, IIssueConsumer consumer)
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

            var channel = new Channel($"{DAEMON_HOST}:{port}", ChannelCredentials.Insecure);
            var client = new StandaloneSonarLint.StandaloneSonarLintClient(channel);

            using (var call = client.Analyze(request))
            {
                try
                {
                    await ProcessIssues(call, path, consumer);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Call to client.Analyze failed: {0}", e);
                }
            }

            await channel.ShutdownAsync();
        }

        private async System.Threading.Tasks.Task ProcessIssues(AsyncServerStreamingCall<Issue> call, string path, IIssueConsumer consumer)
        {
            var issues = new List<Issue>();

            while (await call.ResponseStream.MoveNext())
            {
                var issue = call.ResponseStream.Current;
                issues.Add(issue);
            }

            consumer.Accept(path, issues);
        }
    }
}
