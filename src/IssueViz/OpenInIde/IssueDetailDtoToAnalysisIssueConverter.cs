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
using System.IO;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Listener.Visualization.Models;
using SonarQube.Client;

namespace SonarLint.VisualStudio.IssueVisualization.OpenInIde;

[Export(typeof(IIssueDetailDtoToAnalysisIssueConverter))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class IssueDetailDtoToAnalysisIssueConverter : IIssueDetailDtoToAnalysisIssueConverter
{
    private readonly ILogger logger;
    private readonly IChecksumCalculator checksumCalculator;

    [ImportingConstructor]
    public IssueDetailDtoToAnalysisIssueConverter(ILogger logger) : this(new ChecksumCalculator())
    {
        this.logger = logger.ForContext("Open In IDE").ForContext(nameof(IssueDetailDtoToAnalysisIssueConverter));
    }

    public IssueDetailDtoToAnalysisIssueConverter(IChecksumCalculator checksumCalculator)
    {
        this.checksumCalculator = checksumCalculator;
    }

    public IAnalysisIssueBase Convert(IssueDetailDto issueDetailDto, string rootPath)
    {
        logger.LogVerbose($"Issue {issueDetailDto.Key} is not hotspot {issueDetailDto.ideFilePath}");
        return new ServerIssue(Id: null,
            issueDetailDto.ruleKey,
            new AnalysisIssueLocation(issueDetailDto.message,
                Path.Combine(rootPath, issueDetailDto.ideFilePath),
                new TextRange(issueDetailDto.textRange.startLine,
                    issueDetailDto.textRange.endLine,
                    issueDetailDto.textRange.startLineOffset,
                    issueDetailDto.textRange.endLineOffset,
                    checksumCalculator.Calculate(issueDetailDto.codeSnippet))),
            null,
            issueDetailDto.flows
                ?.Select(flowDto =>
                    new AnalysisIssueFlow(flowDto.locations
                        .Select(locationDto =>
                            new AnalysisIssueLocation(locationDto.message,
                                Path.Combine(rootPath, locationDto.ideFilePath),
                                new TextRange(locationDto.textRange.startLine,
                                    locationDto.textRange.endLine,
                                    locationDto.textRange.startLineOffset,
                                    locationDto.textRange.endLineOffset,
                                    checksumCalculator.Calculate(locationDto.codeSnippet))))
                        .ToList()))
                .ToList());
    }

    private sealed record ServerIssue(
        Guid? Id,
        string RuleKey,
        IAnalysisIssueLocation PrimaryLocation,
        string RuleDescriptionContextKey,
        IReadOnlyList<IAnalysisIssueFlow> Flows)
        : IAnalysisIssueBase
    {
    }
}
