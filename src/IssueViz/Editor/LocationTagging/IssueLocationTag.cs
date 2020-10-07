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

namespace SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging
{
    /// <summary>
    /// Marks a span relating to a primary or secondary location in a file
    /// </summary>
    /// <remarks>Used to track span changes when a file is edited. These tags are not directly
    /// consumed by our view taggers i.e. that are not directly visualized in the editor.
    /// Instead, they are consumed by our buffer taggers which in turn add more specific tags
    /// for a specific subset of the locations, namely
    /// * IErrorTags for primary issue locations, and
    /// * ISelectedIssueLocationTags for locations that relate to the currently selected issue</remarks>
    internal interface IIssueLocationTag : ITag
    {
        IAnalysisIssueLocationVisualization Location { get; }
    }

    internal class IssueLocationTag : IIssueLocationTag
    {
        public IssueLocationTag(IAnalysisIssueLocationVisualization location)
        {
            Location = location ?? throw new ArgumentNullException(nameof(location));
        }

        public IAnalysisIssueLocationVisualization Location { get; }
    }
}
