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
using Microsoft.VisualStudio.Text.Tagging;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.ErrorTagging
{
    /// <summary>
    /// Implementation of <see cref="IErrorTag"/> that lazily creates tooltips.
    /// </summary>
    /// <remarks>
    /// Mitigation for a performance issue. See https://github.com/SonarSource/sonarlint-visualstudio/issues/2798
    /// </remarks>
    internal class SonarErrorTag : IErrorTag
    {
        private readonly Lazy<object> tooltipFactory;

        public SonarErrorTag(string errorType, IAnalysisIssueBase analysisIssue,  IErrorTagTooltipProvider errorTagTooltipProvider)
        {
            ErrorType = errorType;
            tooltipFactory = new Lazy<object>(() => errorTagTooltipProvider.Create(analysisIssue));
        }

        public string ErrorType { get; }

        public object ToolTipContent => tooltipFactory.Value;
    }
}
