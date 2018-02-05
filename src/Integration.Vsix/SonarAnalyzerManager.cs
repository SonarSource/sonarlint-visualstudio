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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;
using SonarAnalyzer.Helpers;
using SonarLint.VisualStudio.Integration.Rules;
using SonarLint.VisualStudio.Integration.Suppression;
using SonarLint.VisualStudio.Integration.Vsix.Suppression;
using SonarQube.Client.Services;

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

        private static readonly AssemblyName AnalyzerAssemblyName =
            new AssemblyName(typeof(SonarAnalysisContext).Assembly.FullName);
        internal /*for testing purposes*/ static readonly Version AnalyzerVersion = AnalyzerAssemblyName.Version;
        internal /*for testing purposes*/ static readonly string AnalyzerName = AnalyzerAssemblyName.Name;

        private readonly IServiceProvider serviceProvider;
        private readonly IQualityProfileProvider qualityProfileProvider;
        private readonly Workspace workspace;
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly ILogger logger;
        private readonly ISonarQubeService sonarQubeService;

        private readonly Dictionary<Language, QualityProfile> cachedQualityProfiles = new Dictionary<Language, QualityProfile>();

        private DelegateInjector delegateInjector;
        private ISonarQubeIssuesProvider sonarqubeIssueProvider;
        private SuppressionHandler suppressionHandler;

        public SonarAnalyzerManager(IServiceProvider serviceProvider, IQualityProfileProvider qualityProfileProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (qualityProfileProvider == null)
            {
                throw new ArgumentNullException(nameof(qualityProfileProvider));
            }

            this.serviceProvider = serviceProvider;
            this.qualityProfileProvider = qualityProfileProvider;

            this.activeSolutionBoundTracker = serviceProvider.GetMefService<IActiveSolutionBoundTracker>();
            this.logger = serviceProvider.GetMefService<ILogger>();
            this.workspace = serviceProvider.GetMefService<VisualStudioWorkspace>();
            this.sonarQubeService = serviceProvider.GetMefService<ISonarQubeService>();

            if (activeSolutionBoundTracker == null ||
                logger == null ||
                workspace == null ||
                sonarQubeService == null)
            {
                Debug.Fail($@"There was a problem resolving types from MEF catalog:
- Null IActiveSolutionBoundTracker? {activeSolutionBoundTracker == null}
- Null ILogger? {logger == null}
- Null VisualStudioWorkspace? {workspace == null}
- Null ISonarQubeService? {sonarQubeService == null}");
                return;
            }

            this.activeSolutionBoundTracker.SolutionBindingChanged += OnSolutionBindingChanged;

            SetupVsixSonarAnalyzerDelegates();
            RefreshSuppresionHandlingInNuGetAnalyzers();
            RefreshQualityProfiles();
        }

        public void Dispose()
        {
            this.activeSolutionBoundTracker.SolutionBindingChanged -= OnSolutionBindingChanged;
            CleanupSuppresionHandlingInNuGetAnalyzers();
            cachedQualityProfiles.Clear();
        }

        private void OnSolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs e)
        {
            RefreshSuppresionHandlingInNuGetAnalyzers();
            RefreshQualityProfiles();
        }

        #region NuGet SonarAnalyzer Delegates
        private void RefreshSuppresionHandlingInNuGetAnalyzers()
        {
            try
            {
                if (activeSolutionBoundTracker.CurrentConfiguration.Mode != NewConnectedMode.SonarLintMode.Standalone)
                {
                    SetupSuppresionHandlingInNuGetAnalyzers();
                }
                else
                {
                    CleanupSuppresionHandlingInNuGetAnalyzers();
                }
            }
            catch (Exception ex) when (!Microsoft.VisualStudio.ErrorHandler.IsCriticalException(ex))
            {
                this.logger.WriteLine($"Failed to refresh suppression handling: {ex.Message}");
            }
        }

        private void SetupSuppresionHandlingInNuGetAnalyzers()
        {
            this.delegateInjector = new DelegateInjector(ShouldNuGetAnalyzerIssueBeReported, logger);

            this.sonarqubeIssueProvider = new SonarQubeIssuesProvider(sonarQubeService,
                this.activeSolutionBoundTracker.CurrentConfiguration.Project.ProjectKey, new TimerFactory());

            var solution = this.serviceProvider.GetService<SVsSolution, IVsSolution>();
            var liveIssueFactory = new LiveIssueFactory(this.workspace, solution);

            this.suppressionHandler = new SuppressionHandler(liveIssueFactory, sonarqubeIssueProvider);
        }

        private void CleanupSuppresionHandlingInNuGetAnalyzers()
        {
            delegateInjector?.Dispose();
            delegateInjector = null;
            (sonarqubeIssueProvider as IDisposable)?.Dispose();
            sonarqubeIssueProvider = null;
        }

        private bool ShouldNuGetAnalyzerIssueBeReported(SyntaxTree syntaxTree, Diagnostic diagnostic) =>
            activeSolutionBoundTracker == null ||
            activeSolutionBoundTracker.CurrentConfiguration.Mode == NewConnectedMode.SonarLintMode.Standalone ||
            suppressionHandler.ShouldIssueBeReported(syntaxTree, diagnostic);
        #endregion // NuGet SonarAnalyzer Delegates

        #region VSIX SonarAnalyzer Delegates
        private void SetupVsixSonarAnalyzerDelegates()
        {
            SonarAnalysisContext.ShouldExecuteRuleFunc = ShouldExecuteDiagnosticAnalyzer;
            SonarAnalysisContext.ReportDiagnosticAction = ReportDiagnostic;
        }

        private bool ShouldExecuteDiagnosticAnalyzer(AnalysisRunContext context)
        {
            if (context.SyntaxTree == null)
            {
                return true;
            }

            if (context.SupportedDiagnostics == null ||
                !context.SupportedDiagnostics.Any())
            {
                Debug.Fail("The 'ShouldExecuteDiagnosticAnalyzer' delegate is not expected to be called without any supported" +
                    "diagnostics.");
                return false;
            }

            var references = this.workspace.CurrentSolution?.GetDocument(context.SyntaxTree)?.Project?.AnalyzerReferences;
            var projectAnalyzerStatus = GetProjectAnalyzerConflictStatus(references);

            return !HasConflictingAnalyzerReference(projectAnalyzerStatus) &&
                !GetIsBoundWithoutAnalyzer(projectAnalyzerStatus) &&
                HasAnyDiagnosticEnabled(context.SupportedDiagnostics, context.SyntaxTree);
        }

        internal /*for testing purposes*/ bool HasAnyDiagnosticEnabled(IEnumerable<DiagnosticDescriptor> diagnostics, SyntaxTree
            syntaxTree)
        {
            switch (this.activeSolutionBoundTracker.CurrentConfiguration.Mode)
            {
                case NewConnectedMode.SonarLintMode.Standalone:
                    // For now the standalone is not configurable so we only enable rules part of SonarWay profile
                    return HasAnyRuleInSonarWay(diagnostics);

                case NewConnectedMode.SonarLintMode.LegacyConnected:
                    // Ruleset was used to decide whether or not the rule should be enabled
                    // (i.e. getting here means the rule is enabled).
                    return true;

                case NewConnectedMode.SonarLintMode.Connected:
                    return cachedQualityProfiles.GetValueOrDefault(Language.CSharp) // TODO: AMAURY - use correct language
                        ?.Rules
                        .Select(x => x.Key)
                        .Intersect(diagnostics.Select(d => d.Id))
                        .Any()
                        ?? HasAnyRuleInSonarWay(diagnostics);

                default:
                    Debug.Fail($"Unexpected SonarLintMode: {this.activeSolutionBoundTracker.CurrentConfiguration.Mode}");
                    return false; // No rule will be enabled
            }
        }

        private bool HasAnyRuleInSonarWay(IEnumerable<DiagnosticDescriptor> diagnostics) =>
            diagnostics.Any(d => d.CustomTags.Contains(DiagnosticTagsHelper.SonarWayTag));

        private void ReportDiagnostic(ReportingContext context)
        {
            if (this.activeSolutionBoundTracker.CurrentConfiguration.Mode == NewConnectedMode.SonarLintMode.LegacyConnected)
            {
                Debug.Fail($"The VSIX {nameof(SonarAnalysisContext.ReportDiagnosticAction)} delegate should not have been " +
                    "called in this mode.");
                return;
            }

            if (context.SyntaxTree == null ||
                context.Diagnostic == null)
            {
                Debug.Fail("The 'ReportDiagnostic' delegate is not expected to be called with either a null syntax tree or a " +
                    "null diagnostic.");
                return;
            }

            // A DiagnosticAnalyzer can have multiple supported diagnostics and we decided to run the rule as long as at least
            // one of the diagnostics is enabled. Therefore we need to filter the reported issues.
            if (!HasAnyDiagnosticEnabled(new[] { context.Diagnostic.Descriptor }, null))
            {
                return;
            }

            // We are in the new connected mode and the issue is suppressed
            if (this.activeSolutionBoundTracker.CurrentConfiguration.Mode == NewConnectedMode.SonarLintMode.Connected &&
                !suppressionHandler.ShouldIssueBeReported(context.SyntaxTree, context.Diagnostic))
            {
                return;
            }

            context.ReportDiagnostic(GetConfiguredDiagnostic(context.Diagnostic));
        }

        private Diagnostic GetConfiguredDiagnostic(Diagnostic diagnostic)
        {
            if (this.activeSolutionBoundTracker.CurrentConfiguration.Mode == NewConnectedMode.SonarLintMode.Connected)
            {
                // TODO: (not on V1) Update the diagnostic based on the ruleset
            }

            return diagnostic;
        }

        internal /*for testing purposes*/ bool GetIsBoundWithoutAnalyzer(ProjectAnalyzerStatus projectAnalyzerStatus)
        {
            return projectAnalyzerStatus == ProjectAnalyzerStatus.NoAnalyzer &&
                this.activeSolutionBoundTracker?.CurrentConfiguration.Mode == NewConnectedMode.SonarLintMode.LegacyConnected;
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

            var sameNamedAnalyzers = references
                .Where(reference => string.Equals(reference.Display, AnalyzerName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (sameNamedAnalyzers.Count == 0)
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

        #endregion // VSIX SonarAnalyzer Delegates

        #region Quality Profiles
        private void RefreshQualityProfiles()
        {
            if (activeSolutionBoundTracker.CurrentConfiguration.Mode == NewConnectedMode.SonarLintMode.Connected)
            {
                FetchRuleSetsForCurrentlyBoundSonarQubeProject();
            }
            else
            {
                this.cachedQualityProfiles.Clear();
            }
        }

        private void FetchRuleSetsForCurrentlyBoundSonarQubeProject()
        {
            foreach (var language in Language.SupportedLanguages)
            {
                var qualityProfile = this.qualityProfileProvider.GetQualityProfile(
                    this.activeSolutionBoundTracker.CurrentConfiguration.Project, language);

                if (qualityProfile != null)
                {
                    cachedQualityProfiles.Add(qualityProfile.Language, qualityProfile);
                }
            }
        }
        #endregion
    }
}