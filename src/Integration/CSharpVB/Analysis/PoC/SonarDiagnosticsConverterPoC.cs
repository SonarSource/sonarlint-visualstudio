/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

namespace SonarLint.VisualStudio.Integration.CSharpVB.Analysis.PoC;

public interface ISonarDiagnosticsConverterPoC
{
    IAnalysisIssue ConvertToAnalysisIssue(RoslynIssue diagnostic);
}

[Export(typeof(ISonarDiagnosticsConverterPoC))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
public class SonarDiagnosticsConverterPoC(ICodeActionStorage storage) : ISonarDiagnosticsConverterPoC
{
    public IAnalysisIssue ConvertToAnalysisIssue(RoslynIssue diagnostic)
    {
        var analysisIssueFlows = diagnostic.Flows.Select(flow =>
            new AnalysisIssueFlow(
                flow.Locations.Select(location =>
                    new AnalysisIssueLocation(
                        location.Message,
                        location.FilePath,
                        new TextRange(
                            location.TextRange.StartLine,
                            location.TextRange.EndLine,
                            location.TextRange.StartLineOffset,
                            location.TextRange.EndLineOffset,
                            null)
                    )
                ).ToList()
            )
        ).ToList();

        var primaryLocation = new AnalysisIssueLocation(
            diagnostic.PrimaryLocation.Message,
            diagnostic.PrimaryLocation.FilePath,
            new TextRange(
                diagnostic.PrimaryLocation.TextRange.StartLine,
                diagnostic.PrimaryLocation.TextRange.EndLine,
                diagnostic.PrimaryLocation.TextRange.StartLineOffset,
                diagnostic.PrimaryLocation.TextRange.EndLineOffset,
                null)
        );



        return new AnalysisIssue(
            null,
            diagnostic.RuleKey,
            null,
            false,
            AnalysisIssueSeverity.Critical,
            AnalysisIssueType.CodeSmell,
            new Impact(SoftwareQuality.Maintainability,
                SoftwareQualitySeverity.High),
            primaryLocation,
            analysisIssueFlows,
                diagnostic.QuickFixes.Select( x => storage.GetCodeActionOrNull(primaryLocation.FilePath, x)).Where(x => x is not null).ToList());
    }
}
