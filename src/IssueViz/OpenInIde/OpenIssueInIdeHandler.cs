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
using SonarLint.VisualStudio.SLCore.Listener.Visualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.OpenInIde;

[Export(typeof(IOpenIssueInIdeHandler))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class OpenIssueInIdeHandler : IOpenIssueInIdeHandler
{
    private readonly IOpenInIdeHandler handler;
    private readonly IIssueOpenInIdeConverter converter;

    [ImportingConstructor]
    public OpenIssueInIdeHandler(IOpenInIdeHandler handler, IIssueOpenInIdeConverter converter)
    {
        this.handler = handler;
        this.converter = converter;
    }

    public void Show(IssueDetailDto issueDetailDto, string configurationScope)
    {
        handler.ShowIssue(issueDetailDto,
            configurationScope,
            converter,
            issueDetailDto.isTaint
                ? IssueListIds.TaintId
                : IssueListIds.ErrorListId);
    }
}
