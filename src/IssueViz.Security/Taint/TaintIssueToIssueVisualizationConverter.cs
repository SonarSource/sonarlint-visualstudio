/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.IO;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Taint;

internal interface ITaintIssueToIssueVisualizationConverter
{
    IAnalysisIssueVisualization Convert(TaintVulnerabilityDto slcoreTaintIssue, string configScopeRoot);
}

[Export(typeof(ITaintIssueToIssueVisualizationConverter))]
[method: ImportingConstructor]
internal class TaintIssueToIssueVisualizationConverter(IAnalysisIssueVisualizationConverter issueVisualizationConverter)
    : ITaintIssueToIssueVisualizationConverter
{
    public IAnalysisIssueVisualization Convert(TaintVulnerabilityDto slcoreTaintIssue, string configScopeRoot)
    {
        var analysisIssue = ConvertToAnalysisIssue(slcoreTaintIssue, configScopeRoot);
        var issueViz = CreateAnalysisIssueVisualization(analysisIssue);
        issueViz.IsSuppressed = slcoreTaintIssue.resolved;

        return issueViz;
    }

    private static IAnalysisIssueBase ConvertToAnalysisIssue(TaintVulnerabilityDto slcoreTaintIssue, string configScopeRoot) =>
        new TaintIssue(
            slcoreTaintIssue.sonarServerKey,
            slcoreTaintIssue.ruleKey,
            CreateLocation(slcoreTaintIssue.message, slcoreTaintIssue.ideFilePath, configScopeRoot, slcoreTaintIssue.textRange),
            slcoreTaintIssue.severityMode.Left?.severity.ToAnalysisIssueSeverity(),
            slcoreTaintIssue.severityMode.Right?.impacts.Select(x => x?.impactSeverity.ToSoftwareQualitySeverity()).Max(),
            slcoreTaintIssue.introductionDate,
            slcoreTaintIssue
                .flows
                .Select(taintFlow =>
                    new AnalysisIssueFlow(
                        taintFlow
                            .locations
                            .Select(taintLocation =>
                                CreateLocation(
                                    taintLocation.message,
                                    taintLocation.filePath,
                                    configScopeRoot,
                                    taintLocation.textRange))
                            .ToArray()))
                .ToArray(),
            slcoreTaintIssue.ruleDescriptionContextKey);

    private static AnalysisIssueLocation CreateLocation(
        string message,
        string filePath,
        string configScopeRoot,
        TextRangeWithHashDto textRange) =>
        new(message,
            Path.Combine(configScopeRoot, filePath),
            new TextRange(textRange.startLine,
                textRange.endLine,
                textRange.startLineOffset,
                textRange.endLineOffset,
                textRange.hash));

    private IAnalysisIssueVisualization CreateAnalysisIssueVisualization(IAnalysisIssueBase analysisIssue) =>
        issueVisualizationConverter.Convert(analysisIssue);
}
