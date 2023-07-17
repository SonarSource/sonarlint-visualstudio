/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.ConnectedMode.Helpers
{
    /// <summary>
    /// Action runner that supports cancellation of previous actions.
    /// <see cref="IDisposable"/> implementation cancels the last action and prevents new actions from being launched. 
    /// </summary>
    public interface ICancellableActionRunner : IDisposable
    {
        /// <summary>
        /// Cancels any previous action and starts a new one
        /// </summary>
        /// <param name="newAction">New action to run. CancellationToken is supplied for cancellation</param>
        /// <returns>Resulting Task of <paramref name="newAction"/></returns>
        Task RunAsync(Func<CancellationToken, Task> newAction);
    }
    
    [Export(typeof(ICancellableActionRunner))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public sealed class SynchronizedCancellableActionRunner : ICancellableActionRunner
    {
        private static readonly CancellationTokenSource DisposedCancellationTokenSource = null;

        private readonly ILogger logger;
        private readonly object lockObject = new object();
        
        private CancellationTokenSource currentCancellationTokenSource = new CancellationTokenSource();

        [ImportingConstructor]
        public SynchronizedCancellableActionRunner(ILogger logger)
        {
            this.logger = logger;
        }

        public Task RunAsync(Func<CancellationToken, Task> newAction)
        {
            CancellationTokenSource newCancellationTokenSource;
            
            lock (lockObject)
            {
                if (currentCancellationTokenSource == DisposedCancellationTokenSource)
                {
                    throw new ObjectDisposedException("Runner disposed");
                }
                
                logger.LogVerbose(Resources.ActionRunner_CancellingCurrentOperation);
                currentCancellationTokenSource.Cancel();
                newCancellationTokenSource = currentCancellationTokenSource = new CancellationTokenSource();
            }

            return newAction(newCancellationTokenSource.Token);
        }
        
        public void Dispose()
        {
            if (currentCancellationTokenSource == DisposedCancellationTokenSource)
            {
                return;
            }

            lock (lockObject)
            {
                if (currentCancellationTokenSource == DisposedCancellationTokenSource)
                {
                    return;
                }

                logger.LogVerbose(Resources.ActionRunner_CancellingCurrentOperation);
                currentCancellationTokenSource.Cancel();
                currentCancellationTokenSource = DisposedCancellationTokenSource;
            }
        }
    }
}
