/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using SonarAnalyzer.Helpers;
using SonarLint.VisualStudio.Integration.Vsix.Suppression;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    // This workflow affects only the VSIX Analyzers
    internal class SonarAnalyzerConnectedWorkflow : SonarAnalyzerWorkflowBase
    {
        private readonly IRoslynSuppressionHandler suppressionHandler;

        public SonarAnalyzerConnectedWorkflow(Workspace workspace, IRoslynSuppressionHandler suppressionHandler)
            : base(workspace)
        {
            if (suppressionHandler == null)
            {
                throw new ArgumentNullException(nameof(suppressionHandler));
            }

            this.suppressionHandler = suppressionHandler;

            SonarAnalysisContext.ReportDiagnostic = VsixAnalyzerReportDiagnostic;
        }

        internal /* for testing purposes */ void VsixAnalyzerReportDiagnostic(IReportingContext context)
        {
            Debug.Assert(context.SyntaxTree != null, "Not expecting to be called with a null SyntaxTree");
            Debug.Assert(context.Diagnostic != null, "Not expecting to be called with a null Diagnostic");
            Debug.Assert(GetProjectNuGetAnalyzerStatus(context.SyntaxTree) == ProjectAnalyzerStatus.NoAnalyzer,
                "Not expecting to be called when project contains any SonarAnalyzer NuGet");

            if (this.suppressionHandler.ShouldIssueBeReported(context.SyntaxTree, context.Diagnostic))
            {
                context.ReportDiagnostic(context.Diagnostic);
            }
        }
    }
}
