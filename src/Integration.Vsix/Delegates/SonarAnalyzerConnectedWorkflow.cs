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
    // This workflow affects only the VSIX Analyzers
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
            if (descriptors == null ||
                !descriptors.Any())
            {
                Debug.Fail("Was expecting to have at least one descriptor");
                return Language.Unknown;
            }

            DEBUG_LanguageCustomTags(descriptors);

            var firstDescriptorTags = descriptors.First().CustomTags;
            if (firstDescriptorTags.Contains(LanguageNames.CSharp))
            {
                return Language.CSharp;
            }
            else if (firstDescriptorTags.Contains(LanguageNames.VisualBasic))
            {
                return Language.VBNET;
            }
            else
            {
                return Language.Unknown;
            }
        }

        [Conditional("DEBUG")]
        private static void DEBUG_LanguageCustomTags(IEnumerable<DiagnosticDescriptor> descriptors)
        {
            var supportedLanguages = new List<string> { LanguageNames.CSharp, LanguageNames.VisualBasic };
            string sharedLanguage = null;

            foreach (var languageTags in descriptors.Select(d => d.CustomTags.Intersect(supportedLanguages).ToList()))
            {
                switch (languageTags.Count)
                {
                    case 0:
                        Debug.Fail("Was expecting the diagnostic descriptor tags to contain either C# or Visual Basic");
                        break;

                    case 1:
                        if (sharedLanguage == null)
                        {
                            sharedLanguage = languageTags[0];
                        }
                        else if (sharedLanguage != languageTags[0])
                        {
                            Debug.Fail("Was expecting all diagnostic descriptors to be of the same language");
                        }
                        else
                        {
                            // nothing
                        }
                        break;

                    default:
                        Debug.Fail("Was expecting the diagnostic descriptor tags to contain only one of C# or Visual Basic");
                        break;
                }
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