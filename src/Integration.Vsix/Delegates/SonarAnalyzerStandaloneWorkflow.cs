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
using Microsoft.CodeAnalysis;
using SonarAnalyzer.Helpers;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    // This workflow affects only the VSIX Analyzers
    internal class SonarAnalyzerStandaloneWorkflow : SonarAnalyzerWorkflowBase
    {
        private readonly IProjectsRuleSetProvider ruleSetsProvider;

        public SonarAnalyzerStandaloneWorkflow(Workspace workspace, IProjectsRuleSetProvider ruleSetsProvider)
            : base(workspace)
        {
            if (ruleSetsProvider == null)
            {
                throw new ArgumentNullException(nameof(ruleSetsProvider));
            }

            this.ruleSetsProvider = ruleSetsProvider;

            SonarAnalysisContext.ShouldRegisterContextAction = ShouldRegisterContextAction;
            SonarAnalysisContext.ReportDiagnostic = VsixAnalyzerReportDiagnostic;
        }

        // We don't want to interfere with potential SonarRules in ruleset so we always let the rule execute
        internal /* for testing purposes */ bool ShouldRegisterContextAction(IEnumerable<DiagnosticDescriptor> descriptors)
            => true;

        protected internal override bool ShouldExecuteRegisteredAction(IEnumerable<DiagnosticDescriptor> descriptors,
            SyntaxTree syntaxTree)
        {
            if (!base.ShouldExecuteRegisteredAction(descriptors, syntaxTree))
            {
                return false;
            }

            Debug.Assert(descriptors != null, "Should have been handled by the base method.");

            // If the descriptor is marked as not configurable then we shouldn't change its behavior (enabled/disabled)
            // Note: Utility analyzers have the NotConfigurable tag so they will fit in this case (but they have a built-in
            // mechanism to be turned-off when run under SLVS).
            if (descriptors.Any(d => d.CustomTags.Contains(WellKnownDiagnosticTags.NotConfigurable)))
            {
                return true;
            }

            // If the project has a ruleset with any Sonar rule then the ruleset has already decided to run this rule
            return ruleSetsProvider.HasRuleSetWithSonarAnalyzerRules(base.workspace?.CurrentSolution
                ?.GetDocument(syntaxTree)?.Project?.FilePath) ||
                 descriptors.Any(d => d.CustomTags.Contains(DiagnosticTagsHelper.SonarWayTag));
        }

        internal /* for testing purposes */ void VsixAnalyzerReportDiagnostic(IReportingContext context)
        {
            Debug.Assert(context.SyntaxTree != null, "Not expecting to be called with a null SyntaxTree");
            Debug.Assert(context.Diagnostic != null, "Not expecting to be called with a null Diagnostic");
            Debug.Assert(GetProjectNuGetAnalyzerStatus(context.SyntaxTree) == ProjectAnalyzerStatus.NoAnalyzer,
                "Not expecting to be called when project contains any SonarAnalyzer NuGet");

            // A DiagnosticAnalyzer can have multiple supported diagnostics and we decided to run the rule as long as at least
            // one of the diagnostics is enabled. Therefore we need to filter the reported issues.
            if (context.Diagnostic.Descriptor.CustomTags.Contains(DiagnosticTagsHelper.SonarWayTag))
            {
                context.ReportDiagnostic(context.Diagnostic);
            }
        }
    }
}