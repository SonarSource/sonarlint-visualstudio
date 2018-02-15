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

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using SonarAnalyzer.Helpers;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class SonarAnalyzerStandaloneWorkflow : SonarAnalyzerWorkflowBase
    {
        public SonarAnalyzerStandaloneWorkflow(Workspace workspace)
            : base(workspace)
        {
            SonarAnalysisContext.ReportDiagnostic = VsixAnalyzerReportDiagnostic;
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