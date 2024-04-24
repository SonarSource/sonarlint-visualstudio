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
using System.Diagnostics.CodeAnalysis;
using SonarLint.VisualStudio.IssueVisualization.OpenInIde;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Visualization;

namespace SonarLint.VisualStudio.SLCore.Listeners.Implementation;

[Export(typeof(ISLCoreListener))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class ShowInIdeListener : IShowInIdeListener
{
    private readonly IOpenIssueInIdeHandler openIssueInIdeHandler;

    [ImportingConstructor]
    public ShowInIdeListener(IOpenIssueInIdeHandler openIssueInIdeHandler)
    {
        this.openIssueInIdeHandler = openIssueInIdeHandler;
    }

    public void ShowIssue(ShowIssueParams parameters)
    {
        openIssueInIdeHandler.ShowIssue(parameters.issueDetails, parameters.configurationScopeId);
    }

    [ExcludeFromCodeCoverage]
    public void ShowHotspot(ShowHotspotParams parameters)
    {
        // todo https://github.com/SonarSource/sonarlint-visualstudio/issues/5377
        throw new NotImplementedException();
    }
}
