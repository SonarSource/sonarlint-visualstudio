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

            SonarAnalysisContext.ReportDiagnostic = VsixAnalyzerReportDiagnostic;
        }

        internal /* for testing purposes */ protected override bool? ShouldRegisterContextAction(
            IEnumerable<DiagnosticDescriptor> descriptors) =>
            this.qualityProfileProvider.GetQualityProfile(this.boundProject, GetLanguage(descriptors))
                ?.Rules
                .Select(x => x.Key)
                .Intersect(descriptors.Select(d => d.Id)) // We assume that a rule is enabled if present in the QP
                .Any();

        internal /* for testing purposes */ static Language GetLanguage(IEnumerable<DiagnosticDescriptor> descriptors)
        {
            // We assume that all descriptors share the same language so we can take only the first one
            var tags = descriptors?.FirstOrDefault()?.CustomTags;

            if (tags == null)
            {
                Debug.Fail("Was expecting to have at least one descriptor");
                return Language.Unknown;
            }
            else if (tags.Contains(LanguageNames.CSharp))
            {
                return Language.CSharp;
            }
            else if (tags.Contains(LanguageNames.VisualBasic))
            {
                return Language.VBNET;
            }
            else
            {
                Debug.Fail("Was expecting the diagnostic descriptor tags to contain C# or Visual Basic");
                return Language.Unknown;
            }
        }

        internal /* for testing purposes */ void VsixAnalyzerReportDiagnostic(IReportingContext context)
        {
            Debug.Assert(context.SyntaxTree != null, "Not expecting to be called with a null SyntaxTree");
            Debug.Assert(context.Diagnostic != null, "Not expecting to be called with a null Diagnostic");
            Debug.Assert(GetProjectNuGetAnalyzerStatus(context.SyntaxTree) == ProjectAnalyzerStatus.NoAnalyzer,
                "Not expecting to be called when project contains any SonarAnalyzer NuGet");

            // A DiagnosticAnalyzer can have multiple supported diagnostics and we decided to run the rule as long as at least
            // one of the diagnostics is enabled. Therefore we need to filter the reported issues.
            if (ShouldRegisterContextActionWithFallback(new[] { context.Diagnostic.Descriptor }) &&
                this.suppressionHandler.ShouldIssueBeReported(context.SyntaxTree, context.Diagnostic))
            {
                context.ReportDiagnostic(context.Diagnostic); // TODO: Update the diagnostic based on configuration
            }
        }
    }
}