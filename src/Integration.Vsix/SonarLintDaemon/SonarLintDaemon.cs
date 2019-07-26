/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Grpc.Core;
using Microsoft.VisualStudio;
using Sonarlint;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;
using SonarLint.VisualStudio.Integration.Vsix.Resources;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [Export(typeof(ISonarLintDaemon))]
    internal class SonarLintDaemon : ISonarLintDaemon
    {
        private const string DaemonHost = "localhost";
        private const int DefaultDaemonPort = 8050;

        internal /* for testing */ const string DefaultDaemonVersion = "4.3.0.2475";
        internal /* for testing */ const string SonarLintDownloadUrlEnvVar = "SONARLINT_DAEMON_DOWNLOAD_URL";
        internal /* for testing */ static readonly string DefaultDownloadUrl = string.Format("https://binaries.sonarsource.com/Distribution/sonarlint-daemon/sonarlint-daemon-{0}-windows.zip",
            DefaultDaemonVersion);

        private readonly ISonarLintSettings settings;
        private readonly ILogger logger;
        private readonly string tmpPath;
        private readonly string storagePath;
        internal /* for testing */ Process process;

        internal /* for testing */  string WorkingDirectory { get; }
        internal /* for testing */ string DaemonVersion { get; }
        internal /* for testing */ string DownloadUrl { get; }

        public event DownloadProgressChangedEventHandler DownloadProgressChanged;
        public event AsyncCompletedEventHandler DownloadCompleted;
        public event EventHandler<EventArgs> Ready;

        private Channel channel;
        private StandaloneSonarLint.StandaloneSonarLintClient daemonClient;

        [ImportingConstructor]
        public SonarLintDaemon(ISonarLintSettings settings, ILogger logger)
            : this(settings, logger,
                  Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SonarLint for Visual Studio"),
                  Path.GetTempPath())
        {
        }

        internal /* for testing */  SonarLintDaemon(ISonarLintSettings settings, ILogger logger, string storagePath, string tmpPath)
        {
            this.settings = settings;
            this.logger = logger;

            this.tmpPath = tmpPath;
            this.storagePath = storagePath;
            this.WorkingDirectory = CreateTempDirectory();

            this.DownloadUrl = GetDownloadUrl();
            this.DaemonVersion = ExtractVersionStringFromUrl(this.DownloadUrl);
            Debug.Assert(this.DaemonVersion != null, "Daemon version should not be null - check the hard-coded download URL is valid");

            logger.WriteLine(Strings.Daemon_Version, this.DaemonVersion);
            logger.WriteLine(Strings.Daemon_Download_Url, this.DownloadUrl);
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

            logger.WriteLine(Strings.Daemon_Starting);

            Port = TcpUtil.FindFreePort(DefaultDaemonPort);
            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    ErrorDialog = false, // don't display UI to user
                    FileName = ExePath,
                    Arguments = GetCmdArgs(Port),
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.OutputDataReceived += (sender, args) => HandleOutputDataReceived(args?.Data);
            process.ErrorDataReceived += (sender, args) => HandleErrorDataReceived(args?.Data);

            if (IsVerbose())
            {
                WritelnToPane($"Running {ExePath}");
            }

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                logger.WriteLine(Strings.Daemon_Started);
            }
            catch (Exception e) when (!ErrorHandler.IsCriticalException(e))
            {
                Debug.WriteLine("Unable to start SonarLint daemon: {0}", e);
                WritelnToPane($"Unable to start SonarLint daemon {e.Message}");
                this.SafeInternalStop();
            }
        }
    
        internal /* for testing */ void HandleOutputDataReceived(string data)
        {
            if (data == null)
            {
                return;
            }

            if (IsVerbose())
            {
                WritelnToPane(data);
            }

            if (data.Contains("Server started"))
            {
                bool succeeded = false;
                SafeOperation(() =>
                {
                    CreateChannelAndStreamLogs();
                    Ready?.Invoke(this, EventArgs.Empty);
                    succeeded = true;
                });

                if (!succeeded)
                {
                    this.SafeInternalStop();
                }
            }
        }

        internal /* for testing */ void HandleErrorDataReceived(string data)
        {
            if (data != null)
            {
                WritelnToPane(data);
            }
        }

        // Need to be able to stub this method out for testing
        protected virtual /* for testing */ void CreateChannelAndStreamLogs()
        {
            channel = new Channel($"{DaemonHost}:{Port}", ChannelCredentials.Insecure);
            daemonClient = new StandaloneSonarLint.StandaloneSonarLintClient(channel);
            ListenForLogs();
            StartHeartBeat();
        }

        private async System.Threading.Tasks.Task ListenForLogs()
        {
            try
            {
                using (var streamLogs = daemonClient.StreamLogs(new Sonarlint.Void(), new CallOptions(null, null, channel.ShutdownToken).WithWaitForReady(true)))
                {
                    while (await streamLogs.ResponseStream.MoveNext())
                    {
                        var log = streamLogs.ResponseStream.Current;
                        if (ShouldLog(settings, log))
                        {
                            WritelnToPane($"{log.Level} {log.Log}");
                        }
                    }
                }
            }
            catch (RpcException e)
            {
                if (e.Status.StatusCode == StatusCode.Cancelled)
                {
                    return;
                }
                Debug.WriteLine("RPC failed: {0}", e);
                WritelnToPane("Unexpected error: " + e);
            }
        }

        private async System.Threading.Tasks.Task StartHeartBeat()
        {
            try
            {
                using (var heartBeat = daemonClient.HeartBeat(new CallOptions(null, null, channel.ShutdownToken).WithWaitForReady(true)))
                {
                    await heartBeat.ResponseAsync;
                    WritelnToPane("Server died?");
                }
            }
            catch (RpcException e)
            {
                if (e.Status.StatusCode == StatusCode.Cancelled)
                {
                    return;
                }
                Debug.WriteLine("RPC failed: {0}", e);
                WritelnToPane("Unexpected error: " + e);
            }
        }

        private static bool ShouldLog(ISonarLintSettings settings, LogEvent log)
        {
            return settings.DaemonLogLevel == DaemonLogLevel.Minimal && log.Level == "ERROR"
                || settings.DaemonLogLevel == DaemonLogLevel.Info && new[] { "ERROR", "WARN", "INFO" }.Contains(log.Level)
                || settings.DaemonLogLevel == DaemonLogLevel.Verbose;
        }

        private void WritelnToPane(string msg)
        {
            logger.WriteLine(msg);
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                return;
                // throw exception?
            }

            this.SafeInternalStop();
        }

        // Need to be able to stub this method out for testing 
        protected virtual void SafeInternalStop()
        {
            // Note: we're not checking IsRunning here as that check can fail if the
            // process wasn't started correctly.
            logger.WriteLine(Strings.Daemon_Stopping);

            SafeOperation(() =>
            {
                daemonClient = null;
                channel?.ShutdownAsync().Wait();
                channel = null;

                // Will throw an InvalidOperationException if the process isn't valid
                if (!process?.HasExited ?? false)
                {
                    process?.Kill();
                    process?.WaitForExit();
                }
                process = null;
            });

            logger.WriteLine(Strings.Daemon_Stopped);
        }

        public int Port { get; private set; }

        public bool IsInstalled => File.Exists(ExePath);

        private static string ExtractVersionStringFromUrl(string url)
        {
            // We're loosely match the "version" part of the zip file name
            var regEx = "/sonarlint-daemon-(?'version'[\\w.-]+)-windows.zip$";
            var match = Regex.Match(url, regEx, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups["version"].Value;
            }

            return null;
        }

        private string GetDownloadUrl()
        {
            var actualUrl = System.Environment.GetEnvironmentVariable(SonarLintDownloadUrlEnvVar);

            if (string.IsNullOrWhiteSpace(actualUrl))
            {
                logger.WriteLine(Strings.Daemon_UsingDefaultDownloadLocation);
                actualUrl = DefaultDownloadUrl;
            }
            else
            {
                if (IsValidUrl(actualUrl))
                {
                    logger.WriteLine(Strings.Daemon_UsingDownloadUrlFromEnvVar, SonarLintDownloadUrlEnvVar);
                }
                else
                {
                    actualUrl = DefaultDownloadUrl;
                }
            }

            return actualUrl;
        }

        private bool IsValidUrl(string url)
        {
            Uri dummyResult;
            if (!Uri.TryCreate(url, UriKind.Absolute, out dummyResult))
            {
                logger.WriteLine(Strings.Daemon_InvalidUrlInDownloadEnvVar, SonarLintDownloadUrlEnvVar, url);
                return false;
            }

            if (ExtractVersionStringFromUrl(url) == null)
            {
                logger.WriteLine(Strings.Daemon_InvalidFileNameInDownloadEnvVar, SonarLintDownloadUrlEnvVar, url);
                return false;
            }

            return true;
        }

        private void Download()
        {
            this.logger.WriteLine(Strings.Daemon_Downloading);

            Uri uri = new Uri(DownloadUrl);

            using (var client = new WebClient())
            {
                client.DownloadProgressChanged += (sender, args) => DownloadProgressChanged?.Invoke(sender, args);
                client.DownloadFileCompleted += Unzip;
                client.DownloadFileCompleted += (sender, args) =>
                    {
                        this.logger.WriteLine(Strings.Daemon_Downloaded);
                        DownloadCompleted?.Invoke(sender, args);
                    };
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

        internal /* for testing */ string InstallationPath => Path.Combine(storagePath, $"sonarlint-daemon-{DaemonVersion}-windows");

        internal /* for testing */string ZipFilePath => Path.Combine(tmpPath, $"sonarlint-daemon-{DaemonVersion}-windows.zip");

        internal virtual /* for testing */string ExePath => Path.Combine(InstallationPath, "jre", "bin", "java.exe");

        private string GetCmdArgs(int port)
        {
            string jarPath = Path.Combine(InstallationPath, "lib", $"sonarlint-daemon-{DaemonVersion}.jar");
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
            return CreateTempDirectory(Path.Combine(Path.GetTempPath(), "SonarLintDaemon"));
        }

        private static string CreateTempDirectory(string path)
        {
            string tempDirectory = Path.Combine(path, Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public void RequestAnalysis(string path, string charset, string sqLanguage, IIssueConsumer consumer)
        {
            if (daemonClient == null)
            {
                Debug.WriteLine("Daemon not ready yet");
                return;
            }
            WritelnToPane($"Analyzing {path}");
            Analyze(path, charset, sqLanguage, consumer);
        }

        private async void Analyze(string path, string charset, string sqLanguage, IIssueConsumer consumer)
        {
            var request = new AnalysisReq
            {
                BaseDir = path,
                WorkDir = WorkingDirectory,
            };
            request.File.Add(new InputFile
            {
                Path = path,
                Charset = charset,
                Language = sqLanguage
            });

            // Concurrent requests should not use same directory:
            var buildWrapperOutDir = CreateTempDirectory(WorkingDirectory);

            using (var call = daemonClient.Analyze(request))
            {
                try
                {
                    await ProcessIssues(call, path, consumer);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Call to client.Analyze failed: {0}", e);
                }
                finally
                {
                    Directory.Delete(buildWrapperOutDir, true);
                }
            }
        }

        private bool IsVerbose()
        {
            return settings.DaemonLogLevel == DaemonLogLevel.Verbose;
        }

        private async System.Threading.Tasks.Task ProcessIssues(AsyncServerStreamingCall<Issue> call, string path, IIssueConsumer consumer)
        {
            var issues = new List<Issue>();
            int issueCount = 0;
            while (await call.ResponseStream.MoveNext())
            {
                var issue = call.ResponseStream.Current;
                issues.Add(issue);
                issueCount++;
            }
            WritelnToPane($"Found {issueCount} issue(s)");

            consumer.Accept(path, issues);
        }

        internal void SafeOperation(Action op)
        {
            try
            {
                op();
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine($"Daemon error: {ex.ToString()}");
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (Directory.Exists(WorkingDirectory))
                    {
                        Directory.Delete(WorkingDirectory, true);
                    }

                    SafeInternalStop();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);

            // We don't add a finalizer, but child classes might
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}

