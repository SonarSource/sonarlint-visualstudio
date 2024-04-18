﻿/*
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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Listener.Visualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.OpenInIde;

internal interface IOpenInIdeConverter
{
    bool TryConvertIssue(IssueDetailDto issueDetails, string rootPath, out IAnalysisIssueVisualization visualization);
}

[Export(typeof(IOpenInIdeConverter))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class OpenInIdeConverter : IOpenInIdeConverter
{
    private readonly IIssueDetailDtoToAnalysisIssueConverter dtoToIssueConverter;
    private readonly IAnalysisIssueVisualizationConverter issueToVisualizationConverter;
    private readonly ILogger logger;

    [ImportingConstructor]
    public OpenInIdeConverter(IIssueDetailDtoToAnalysisIssueConverter dtoToIssueConverter, IAnalysisIssueVisualizationConverter issueToVisualizationConverter, ILogger logger)
    {
        this.dtoToIssueConverter = dtoToIssueConverter;
        this.issueToVisualizationConverter = issueToVisualizationConverter;
        this.logger = logger;
    }

    public bool TryConvertIssue(IssueDetailDto issueDetails, string rootPath, out IAnalysisIssueVisualization visualization)
    {
        visualization = null;

        try
        {
            var analysisIssueBase = dtoToIssueConverter.Convert(issueDetails, rootPath);
            visualization = issueToVisualizationConverter.Convert(analysisIssueBase);
            return true;
        }
        catch (Exception e) when (!Microsoft.VisualStudio.ErrorHandler.IsCriticalException(e))
        {
            logger.WriteLine(OpenInIdeResources.ApiHandler_UnableToConvertIssueData, e.Message);
            return false;
        }
    }
}
