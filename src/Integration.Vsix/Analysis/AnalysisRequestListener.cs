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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.Resources;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis
{
    internal interface IAnalysisRequestHandlersStore
    {
        void Add(IAnalysisRequestHandler analysisRequestHandler);
    }

    internal interface IAnalysisRequestListener
    {
    }

    [Export(typeof(IAnalysisRequestListener))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class AnalysisRequestListener : IAnalysisRequestListener, IAnalysisRequestHandlersStore, IDisposable
    {
        private readonly ISet<IAnalysisRequestHandler> requestHandlers = new HashSet<IAnalysisRequestHandler>();

        internal IEnumerable<IAnalysisRequestHandler> RequestHandlers => requestHandlers;

        private readonly IAnalysisRequester analysisRequester;
        private readonly ILogger logger;
        private readonly IVsStatusbar vsStatusBar;

        private readonly object reanalysisLockObject = new object();
        private CancellableJobRunner reanalysisJob;
        private StatusBarReanalysisProgressHandler reanalysisProgressHandler;

        [ImportingConstructor]
        public AnalysisRequestListener([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IAnalysisRequester analysisRequester,
            ILogger logger)
        {
            this.logger = logger;
            this.analysisRequester = analysisRequester;
            vsStatusBar = serviceProvider.GetService(typeof(IVsStatusbar)) as IVsStatusbar;

            analysisRequester.AnalysisRequested += OnAnalysisRequested;
        }

        private void OnAnalysisRequested(object sender, AnalysisRequestEventArgs args)
        {
            // Handle notification from the single file monitor that the settings file has changed.

            // Re-analysis could take multiple seconds so it's possible that we'll get another
            // file change notification before the re-analysis has completed.
            // If that happens we'll cancel the current re-analysis and start another one.
            lock (reanalysisLockObject)
            {
                reanalysisJob?.Cancel();
                reanalysisProgressHandler?.Dispose();

                var filteredRequestHandlers = FilterRequestHandlersByPath(this.requestHandlers, args.FilePaths);

                var operations = filteredRequestHandlers
                    .Select<IAnalysisRequestHandler, Action>(it => () => it.RequestAnalysis(args.Options))
                    .ToArray(); // create a fixed list - the user could close a file before the reanalysis completes which would cause the enumeration to change

                reanalysisProgressHandler = new StatusBarReanalysisProgressHandler(vsStatusBar, logger);

                var message = string.Format(CultureInfo.CurrentCulture, Strings.JobRunner_JobDescription_ReaanalyzeDocs, operations.Length);
                reanalysisJob = CancellableJobRunner.Start(message, operations,
                    reanalysisProgressHandler, logger);
            }
        }

        internal /* for testing */ static IEnumerable<IAnalysisRequestHandler> FilterRequestHandlersByPath(
            IEnumerable<IAnalysisRequestHandler> requestHandlers, IEnumerable<string> filePaths)
        {
            if (filePaths == null || !filePaths.Any())
            {
                return requestHandlers;
            }
            return requestHandlers.Where(it => filePaths.Contains(it.FilePath, StringComparer.OrdinalIgnoreCase));
        }

        void IAnalysisRequestHandlersStore.Add(IAnalysisRequestHandler analysisRequestHandler)
        {
            lock (requestHandlers)
            {
                requestHandlers.Add(analysisRequestHandler);
                analysisRequestHandler.Disposed += (e, args) => Remove(analysisRequestHandler);
            }
        }

        private void Remove(IAnalysisRequestHandler analysisRequestHandler)
        {
            lock (requestHandlers)
            {
                requestHandlers.Remove(analysisRequestHandler);
            }
        }

        public void Dispose()
        {
            reanalysisProgressHandler?.Dispose();
            analysisRequester.AnalysisRequested -= OnAnalysisRequested;
        }
    }
}
