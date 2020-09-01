/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging
{
    /// <summary>
    /// Marks a span for location relating to a selected issue
    /// </summary>
    /// <remarks>Consumed by our view taggers to provide editor visualizations (highlighting and adornments)</remarks>
    internal interface ISelectedIssueLocationTag : ITag
    {
        IAnalysisIssueLocationVisualization Location { get; }
    }

    internal class SelectedIssueLocationTag : ISelectedIssueLocationTag
    {
        public SelectedIssueLocationTag(IAnalysisIssueLocationVisualization location)
        {
            Location = location ?? throw new ArgumentNullException(nameof(location));
        }

        public IAnalysisIssueLocationVisualization Location { get; }
    }
}
