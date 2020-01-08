/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis
{
    /// <summary>
    /// Handles displaying reanalysis progress in the VS status bar
    /// </summary>
    internal sealed class StatusBarReanalysisProgressHandler : IProgress<CancellableJobRunner.JobRunnerProgress>, IDisposable
    {
        private readonly ILogger logger;

        private readonly IVsStatusbar statusBar;
        private uint pwdCookie;

        public StatusBarReanalysisProgressHandler(IVsStatusbar statusBar, ILogger logger)
        {
            if (statusBar == null)
            {
                return; // no point in doing anything if we don't have a status bar
            }

            this.statusBar = statusBar;
            this.logger = logger;
        }

        public void Report(CancellableJobRunner.JobRunnerProgress value)
        {
            try
            {
                if (value.CurrentState == CancellableJobRunner.RunnerState.Running)
                {
                    UpdateStatusBar(value.CompletedOperations, value.TotalOperations);
                }
                else
                {
                    Debug.Assert(value.CurrentState != CancellableJobRunner.RunnerState.NotStarted,
                        "Not expecting a progress notification until the runner has started");
                    Dispose();
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(AnalysisStrings.ReanalysisStatusBar_Error, ex.Message);
            }
        }
        
        private void UpdateStatusBar(int completed, int total)
        {
            if (!disposedValue)
            {
                var message = string.Format(AnalysisStrings.ReanalysisStatusBar_InProgress, completed, total);
                statusBar?.Progress(ref pwdCookie, 1, message, (uint)completed, (uint)total);
            }
        }

        private void Cleanup()
        {
            if (pwdCookie != 0)
            {
                statusBar.Progress(ref pwdCookie, 0, "", 0, 0);
                pwdCookie = 0;
            }
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
