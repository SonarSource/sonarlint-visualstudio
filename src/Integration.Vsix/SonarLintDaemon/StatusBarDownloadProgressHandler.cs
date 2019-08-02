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

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using System;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Handles displaying daemon download progress in the VS status bar
    /// </summary>
    public sealed class StatusBarDownloadProgressHandler : IDisposable
    {
        private readonly IDaemonInstaller installer;
        private readonly ILogger logger;

        private readonly IVsStatusbar statusBar;
        private uint pwdCookie;

        public StatusBarDownloadProgressHandler(IVsStatusbar statusBar, IDaemonInstaller installer, ILogger logger)
        {
            if (statusBar == null)
            {
                return; // no point in doing anything if we don't have a status bar
            }

            this.statusBar = statusBar;
            this.installer = installer;
            this.logger = logger;

            this.installer.InstallationProgressChanged += Daemon_DownloadProgressChanged;
            this.installer.InstallationCompleted += Daemon_DownloadCompleted;
        }

        private void Daemon_DownloadCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            try
            {
                Cleanup();
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Strings.ERROR_InstallingDaemon, ex);
            }
        }

        private void Daemon_DownloadProgressChanged(object sender, InstallationProgressChangedEventArgs e)
        {
            try
            {
                UpdateStatusBar(e.BytesReceived, e.TotalBytesToReceive);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Strings.ERROR_InstallingDaemon, ex);
            }
        }

        private void UpdateStatusBar(long bytesReceived, long totalBytes)
        {
            statusBar?.Progress(ref pwdCookie, 1, "Downloading SonarLint daemon: ", (uint)bytesReceived, (uint)totalBytes);
        }

        private void Cleanup()
        {
            installer.InstallationCompleted -= Daemon_DownloadCompleted;
            installer.InstallationProgressChanged -= Daemon_DownloadProgressChanged;
            statusBar.Progress(ref pwdCookie, 0, "Downloading SonarLint daemon: ", 0, 0);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Cleanup();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion
    }
}
