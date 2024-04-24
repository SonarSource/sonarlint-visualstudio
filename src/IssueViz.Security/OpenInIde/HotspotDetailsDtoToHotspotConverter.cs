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

using System;
using System.ComponentModel.Composition;
using System.IO;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.Models;
using SonarLint.VisualStudio.SLCore.Listener.Visualization.Models;
using SonarQube.Client;

namespace SonarLint.VisualStudio.IssueVisualization.Security.OpenInIde;

internal interface IHotspotDetailsDtoToHotspotConverter
{
    IHotspot Convert(HotspotDetailsDto hotspotDetailsDto, string rootPath);
}

[Export(typeof(IHotspotDetailsDtoToHotspotConverter))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class HotspotDetailsDtoToHotspotConverter : IHotspotDetailsDtoToHotspotConverter
{
    private readonly IChecksumCalculator checksumCalculator;

    [ImportingConstructor]
    public HotspotDetailsDtoToHotspotConverter() : this(new ChecksumCalculator())
    {
    }

    public HotspotDetailsDtoToHotspotConverter(IChecksumCalculator checksumCalculator)
    {
        this.checksumCalculator = checksumCalculator;
    }
    
    public IHotspot Convert(HotspotDetailsDto hotspotDetailsDto, string rootPath)
    {
        return new Hotspot(hotspotDetailsDto.key,
            hotspotDetailsDto.ideFilePath,
            new AnalysisIssueLocation(hotspotDetailsDto.message,
                Path.Combine(rootPath, hotspotDetailsDto.ideFilePath),
                GetTextRange(hotspotDetailsDto)),
            GetHotspotRule(hotspotDetailsDto),
            null);
    }

    private TextRange GetTextRange(HotspotDetailsDto hotspotDetailsDto)
    {
        return new TextRange(hotspotDetailsDto.textRange.startLine,
            hotspotDetailsDto.textRange.endLine,
            hotspotDetailsDto.textRange.startLineOffset,
            hotspotDetailsDto.textRange.endLineOffset,
            checksumCalculator.Calculate(hotspotDetailsDto.codeSnippet));
    }

    private HotspotRule GetHotspotRule(HotspotDetailsDto hotspotDetailsDto)
    {
        return new HotspotRule(hotspotDetailsDto.rule.key,
            hotspotDetailsDto.rule.name,
            hotspotDetailsDto.rule.securityCategory,
            GetPriority(hotspotDetailsDto.rule.vulnerabilityProbability),
            hotspotDetailsDto.rule.riskDescription,
            hotspotDetailsDto.rule.vulnerabilityDescription,
            hotspotDetailsDto.rule.fixRecommendations);
    }

    private HotspotPriority GetPriority(string vulnerabilityProbability)
    {
        if (vulnerabilityProbability == null)
        {
            throw new ArgumentNullException(nameof(vulnerabilityProbability));
        }

        return vulnerabilityProbability.ToLowerInvariant() switch
        {
            "high" => HotspotPriority.High,
            "medium" => HotspotPriority.Medium,
            "low" => HotspotPriority.Low,
            _ => throw new ArgumentOutOfRangeException(nameof(vulnerabilityProbability), vulnerabilityProbability,
                "Invalid hotspot probability")
        };
    }
}
