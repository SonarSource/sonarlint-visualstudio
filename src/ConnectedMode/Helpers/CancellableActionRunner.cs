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
    public interface ICancellableActionRunner : IDisposable
    {
        Task RunAsync(Func<CancellationToken, Task> newAction);
    }
    
    // todo: this class will be replaced with correct implementation in a later PR
    [Export(typeof(ICancellableActionRunner))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class SimpleCancellableActionRunner : ICancellableActionRunner
    {
        private readonly ILogger logger;
        private CancellationTokenSource updateAllCancellationTokenSource = new CancellationTokenSource();

        [ImportingConstructor]
        public SimpleCancellableActionRunner(ILogger logger)
        {
            this.logger = logger;
        }

        public Task RunAsync(Func<CancellationToken, Task> newAction)
        {
            CancelCurrentOperation();
            var localCopy = updateAllCancellationTokenSource = new CancellationTokenSource();
            return newAction(localCopy.Token);
        }

        public void Dispose()
        {
            updateAllCancellationTokenSource?.Dispose();
        }
        
        private void CancelCurrentOperation()
        {
            // We don't want multiple "fetch all" operations running at once (e.g. if the
            // user opens a solution then clicks "update").
            // If there is an operation in progress we'll cancel it.
            var copy = updateAllCancellationTokenSource;

            // If there is already a fetch operation in process then cancel it.
            if (copy != null && !copy.IsCancellationRequested)
            {
                logger.LogVerbose(Resources.Suppressions_CancellingCurrentOperation);
                copy.Cancel();
            }
        }
    }
}
