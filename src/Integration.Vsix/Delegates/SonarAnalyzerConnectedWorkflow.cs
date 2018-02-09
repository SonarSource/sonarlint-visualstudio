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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SonarAnalyzer.Helpers;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.Rules;
using SonarLint.VisualStudio.Integration.Vsix.Suppression;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class SonarAnalyzerConnectedWorkflow : SonarAnalyzerWorkflowBase
    {
        private readonly IQualityProfileProvider qualityProfileProvider;
        private readonly BoundSonarQubeProject boundProject;
        private readonly ISuppressionHandler suppressionHandler;

        public SonarAnalyzerConnectedWorkflow(Workspace workspace, IQualityProfileProvider qualityProfileProvider,
            BoundSonarQubeProject boundProject, ISuppressionHandler suppressionHandler)
            : base(workspace)
        {
            if (qualityProfileProvider == null)
            {
                throw new ArgumentNullException(nameof(qualityProfileProvider));
            }

            if (boundProject == null)
            {
                throw new ArgumentNullException(nameof(boundProject));
            }

            if (suppressionHandler == null)
            {
                throw new ArgumentNullException(nameof(suppressionHandler));
            }

            this.qualityProfileProvider = qualityProfileProvider;
            this.boundProject = boundProject;
            this.suppressionHandler = suppressionHandler;

            SonarAnalysisContext.ShouldExecuteRuleFunc = ShouldExecuteVsixAnalyzer;
            SonarAnalysisContext.ReportDiagnosticAction = VsixAnalyzerReportDiagnostic;
        }

        internal /* for testing purposes */ bool ShouldExecuteVsixAnalyzer(IAnalysisRunContext context)
        {
            if (context.SyntaxTree == null)
            {
                return false;
            }

            Debug.Assert(context.SupportedDiagnostics?.Any() ?? false,
                "Not expecting a null or empty collection of diagnostic descriptors");

            // Disable the VSIX analyzer as we want the NuGet analyzer to take precedence
            if (GetProjectNuGetAnalyzerStatus(context.SyntaxTree) != ProjectAnalyzerStatus.NoAnalyzer)
            {
                return false;
            }

            return HasAnyRuleEnabled(context.SyntaxTree, context.SupportedDiagnostics);
        }

        private bool HasAnyRuleEnabled(SyntaxTree syntaxTree, IEnumerable<DiagnosticDescriptor> supportedDescriptors) =>
            this.qualityProfileProvider.GetQualityProfile(this.boundProject, GetLanguage(syntaxTree))
                ?.Rules
                .Select(x => x.Key)
                .Intersect(supportedDescriptors.Select(d => d.Id)) // We assume that a rule is enabled if present in the QP
                .Any()
            // Fallback using SonarWay
            ?? supportedDescriptors.Any(d => d.CustomTags.Contains(DiagnosticTagsHelper.SonarWayTag));

        internal /* for testing purposes */ virtual Language GetLanguage(SyntaxTree syntaxTree)
        {
            var rootNode = syntaxTree?.GetRoot();

            if (rootNode is CompilationUnitSyntax)
            {
                return Language.CSharp;
            }

            if (rootNode is Microsoft.CodeAnalysis.VisualBasic.Syntax.CompilationUnitSyntax)
            {
                return Language.VBNET;
            }

            return Language.Unknown;
        }

        internal /* for testing purposes */ void VsixAnalyzerReportDiagnostic(IReportingContext context)
        {
            Debug.Assert(context.SyntaxTree != null, "Not expecting to be called with a null SyntaxTree");
            Debug.Assert(context.Diagnostic != null, "Not expecting to be called with a null Diagnostic");
            Debug.Assert(GetProjectNuGetAnalyzerStatus(context.SyntaxTree) == ProjectAnalyzerStatus.NoAnalyzer,
                "Not expecting to be called when project contains any SonarAnalyzer NuGet");

            // A DiagnosticAnalyzer can have multiple supported diagnostics and we decided to run the rule as long as at least
            // one of the diagnostics is enabled. Therefore we need to filter the reported issues.
            if (HasAnyRuleEnabled(context.SyntaxTree, new[] { context.Diagnostic.Descriptor }) &&
                this.suppressionHandler.ShouldIssueBeReported(context.SyntaxTree, context.Diagnostic))
            {
                context.ReportDiagnostic(context.Diagnostic); // TODO: Update the diagnostic based on configuration
            }
        }
    }
}