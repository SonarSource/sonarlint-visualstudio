/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2019 SonarSource SA
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

using SonarLint.VisualStudio.Integration.Vsix.Resources;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [Export(typeof(IDaemonInstaller))]
    internal class DaemonInstaller : IDaemonInstaller
    {
        internal /* for testing */ const string DefaultDaemonVersion = "4.3.2.2521";
        internal /* for testing */ const string SonarLintDownloadUrlEnvVar = "SONARLINT_DAEMON_DOWNLOAD_URL";
        internal /* for testing */ static readonly string DefaultDownloadUrl = string.Format("https://binaries.sonarsource.com/Distribution/sonarlint-daemon/sonarlint-daemon-{0}-windows.zip",
            DefaultDaemonVersion);

        private readonly ILogger logger;
        private readonly string tmpPath;
        private readonly string storagePath;

        public bool InstallInProgress { get; private set; }
        public string DaemonVersion { get; }

        internal /* for testing */ string DownloadUrl { get; }


        public event InstallationProgressChangedEventHandler InstallationProgressChanged;
        public event AsyncCompletedEventHandler InstallationCompleted;
            
        [ImportingConstructor]
        public DaemonInstaller(ILogger logger)
            : this(logger,
              Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SonarLint for Visual Studio"),
              Path.GetTempPath())
        {
        }

        internal /* for testing */ DaemonInstaller(ILogger logger, string storagePath, string tmpPath)
        {
            this.logger = logger;
            this.tmpPath = tmpPath;
            this.storagePath = storagePath;

            this.DownloadUrl = GetDownloadUrl();
            this.DaemonVersion = ExtractVersionStringFromUrl(this.DownloadUrl);
            Debug.Assert(this.DaemonVersion != null, "Daemon version should not be null - check the hard-coded download URL is valid");

            logger.WriteLine(Strings.Daemon_Version, this.DaemonVersion);
            logger.WriteLine(Strings.Daemon_Download_Url, this.DownloadUrl);
        }

        public string InstallationPath => Path.Combine(storagePath, $"sonarlint-daemon-{DaemonVersion}-windows");

        public bool IsInstalled()
        {
            bool isInstalled = Directory.Exists(InstallationPath)
                && Directory.GetFiles(InstallationPath, "java.exe", SearchOption.AllDirectories).Length > 0;
            return isInstalled;
        }

        public void Install()
        {
            Download();
        }

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
            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
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
            // TODO: this isn't thread-safe
            if (InstallInProgress)
            {
                return;
            }

            InstallInProgress = true;
            this.logger.WriteLine(Strings.Daemon_Downloading);

            Uri uri = new Uri(DownloadUrl);

            using (var client = new WebClient())
            {
                client.DownloadProgressChanged += (sender, args) => InstallationProgressChanged?.Invoke(sender, new InstallationProgressChangedEventArgs(args.BytesReceived, args.TotalBytesToReceive));
                client.DownloadFileCompleted += Unzip;
                client.DownloadFileCompleted += (sender, args) =>
                {
                    this.logger.WriteLine(Strings.Daemon_Downloaded);
                    InstallationCompleted?.Invoke(sender, args);
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

            InstallInProgress = false;
        }

        internal /* for testing */string ZipFilePath => Path.Combine(tmpPath, $"sonarlint-daemon-{DaemonVersion}-windows.zip");

    }
}
