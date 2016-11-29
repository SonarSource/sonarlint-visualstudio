//-----------------------------------------------------------------------
// <copyright file="SonarAnalyzerManager.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using SonarAnalyzer.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    public sealed class SonarAnalyzerManager : IDisposable
    {
        internal /*for testing purposes*/ enum ProjectAnalyzerStatus
        {
            NoAnalyzer,
            SameVersion,
            DifferentVersion
        }

        private readonly Workspace workspace;
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly ISolutionAnalysisRequester solutionAnalysisRequester;

        private static readonly AssemblyName AnalyzerAssemblyName =
            new AssemblyName(typeof(SonarAnalysisContext).Assembly.FullName);
        internal /*for testing purposes*/ static readonly Version AnalyzerVersion = AnalyzerAssemblyName.Version;
        internal /*for testing purposes*/ static readonly string AnalyzerName = AnalyzerAssemblyName.Name;

        internal /*for testing purposes*/ SonarAnalyzerManager(IServiceProvider serviceProvider, Workspace workspace,
            ISolutionAnalysisRequester solutionAnalysisRequester)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            this.workspace = workspace;
            this.activeSolutionBoundTracker = serviceProvider.GetMefService<IActiveSolutionBoundTracker>();

            if (this.activeSolutionBoundTracker == null)
            {
                Debug.Fail($"Could not get {nameof(IActiveSolutionBoundTracker)}");
            }

            this.solutionAnalysisRequester = solutionAnalysisRequester;
            this.activeSolutionBoundTracker.SolutionBindingChanged += this.ActiveSolutionBoundTracker_SolutionBindingChanged;

            SonarAnalysisContext.ShouldAnalysisBeDisabled =
                tree => ShouldAnalysisBeDisabledOnTree(tree);
        }

        internal /*for testing purposes*/ SonarAnalyzerManager(IServiceProvider serviceProvider, Workspace workspace)
            : this(serviceProvider, workspace, new SolutionAnalysisRequester(serviceProvider, new WorkspaceConfigurator(workspace)))
        {
        }

        public SonarAnalyzerManager(IServiceProvider serviceProvider) :
            this(serviceProvider, GetWorkspace(serviceProvider))
        {
        }

        private static Workspace GetWorkspace(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            IComponentModel componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            return componentModel.GetService<VisualStudioWorkspace>();
        }

        private bool ShouldAnalysisBeDisabledOnTree(SyntaxTree tree)
        {
            if (tree == null)
            {
                return false;
            }

            IEnumerable<AnalyzerReference> references = workspace?.CurrentSolution?.GetDocument(tree)?.Project?.AnalyzerReferences;
            ProjectAnalyzerStatus projectAnalyzerStatus = GetProjectAnalyzerConflictStatus(references);

            return HasConflictingAnalyzerReference(projectAnalyzerStatus) ||
                this.GetIsBoundWithoutAnalyzer(projectAnalyzerStatus);
        }

        internal /*for testing purposes*/ bool GetIsBoundWithoutAnalyzer(ProjectAnalyzerStatus projectAnalyzerStatus)
        {
            return projectAnalyzerStatus == ProjectAnalyzerStatus.NoAnalyzer &&
                this.activeSolutionBoundTracker != null &&
                this.activeSolutionBoundTracker.IsActiveSolutionBound;
        }

        internal /*for testing purposes*/ static bool HasConflictingAnalyzerReference(
            ProjectAnalyzerStatus projectAnalyzerStatus)
        {
            return projectAnalyzerStatus == ProjectAnalyzerStatus.DifferentVersion;
        }

        internal /*for testing purposes*/ static ProjectAnalyzerStatus GetProjectAnalyzerConflictStatus(
            IEnumerable<AnalyzerReference> references)
        {
            if (references == null)
            {
                return ProjectAnalyzerStatus.NoAnalyzer;
            }

            List<AnalyzerReference> sameNamedAnalyzers = references
                .Where(reference => string.Equals(reference.Display, AnalyzerName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!sameNamedAnalyzers.Any())
            {
                return ProjectAnalyzerStatus.NoAnalyzer;
            }

            bool hasConflictingAnalyzer = sameNamedAnalyzers
                .Select(reference => (reference.Id as AssemblyIdentity)?.Version)
                .All(version => version != AnalyzerVersion);

            return hasConflictingAnalyzer
                ? ProjectAnalyzerStatus.DifferentVersion
                : ProjectAnalyzerStatus.SameVersion;
        }

        private void ActiveSolutionBoundTracker_SolutionBindingChanged(object sender, bool e)
        {
            this.solutionAnalysisRequester.ReanalyzeSolution();
        }

        #region IDisposable

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (this.activeSolutionBoundTracker != null)
            {
                this.activeSolutionBoundTracker.SolutionBindingChanged -= this.ActiveSolutionBoundTracker_SolutionBindingChanged;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            this.Dispose(true);
        }
        #endregion
    }
}