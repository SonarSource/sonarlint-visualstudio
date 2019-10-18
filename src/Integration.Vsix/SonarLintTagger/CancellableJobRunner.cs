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
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Integration.Vsix.Resources;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Runs a series of operations on a separate background thread.
    /// Can be cancelled.
    /// </summary>
    internal class CancellableJobRunner
    {
        internal enum RunnerState
        {
            NotStarted, Running, Finished, Cancelled, Faulted
        }

        private readonly IEnumerable<Action> operations;
        private readonly ILogger logger;
        private readonly string jobDescription;
        private CancellationTokenSource cancellationSource = new CancellationTokenSource();
        private DateTime startTime;

        internal /* for testing */ RunnerState State { get; private set; }

        public static CancellableJobRunner Start(string jobDescription, IEnumerable<Action> operations, ILogger logger)
        {
            var runner = new CancellableJobRunner(jobDescription, operations, logger);

            runner.Execute()
                .Forget(); // kick off the re-analysis process and return

            return runner;
        }

        private CancellableJobRunner(string jobDescription, IEnumerable<Action> operations, ILogger logger)
        {
            this.jobDescription = jobDescription;
            this.operations = operations;
            this.logger = logger;

            State = RunnerState.NotStarted;
        }

        private async System.Threading.Tasks.Task Execute()
        {
            State = RunnerState.Running;

            // See https://github.com/microsoft/vs-threading/blob/master/doc/cookbook_vs.md for
            // info on VS threading.
            await System.Threading.Tasks.Task.Yield(); // Get off the caller's callstack

            try
            {
                startTime = DateTime.UtcNow;
                logger.WriteLine(Strings.JobRunner_StartingJob, jobDescription, startTime.ToLongTimeString());

                foreach (var op in operations)
                {
                    if (cancellationSource.Token.IsCancellationRequested)
                    {
                        break;
                    }
                    op();
                }

                if (!cancellationSource.Token.IsCancellationRequested)
                {
                    State = RunnerState.Finished;
                    var elapsedTime = DateTime.UtcNow - startTime;
                    logger.WriteLine(Strings.JobRunner_FinishedJob,
                        jobDescription, startTime.ToLongTimeString(), (long)elapsedTime.TotalMilliseconds);
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                State = RunnerState.Faulted;
                logger.WriteLine(Strings.JobRunner_ExecutionError, ex.Message);
            }
            finally
            {
                cancellationSource?.Dispose();
                cancellationSource = null;
            }
        }

        public void Cancel()
        {
            // Cancel called by the owner, but the work might already have finished
            try
            {
                if (!cancellationSource?.Token.IsCancellationRequested ?? false)
                {
                    State = RunnerState.Cancelled;
                    logger.WriteLine(Strings.JobRunner_CancellingJob, jobDescription, startTime.ToLongTimeString());
                    cancellationSource?.Cancel();
                }
            }
            catch (ObjectDisposedException)
            {
                // Guard against a possible race condition - the job might have finished and
                // dispose the source but not set the source to null at the point this method
                // calls cancel
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Strings.JobRunner_ExecutionError, ex.Message);
            }
        }
    }
}
