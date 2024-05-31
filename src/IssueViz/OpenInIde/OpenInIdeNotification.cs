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
using System.Windows;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.IssueVisualization.OpenInIde;

internal interface IOpenInIdeNotification
{
    void UnableToLocateIssue(string filePath, Guid toolWindowId);
    void UnableToOpenFile(string filePath, Guid toolWindowId);
    void InvalidRequest(string reason, Guid toolWindowId);
    void Clear();
}

[Export(typeof(IOpenInIdeNotification))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class OpenInIdeNotification : IOpenInIdeNotification
{
    private readonly IToolWindowService toolWindowService;
    private readonly IOpenInIdeFailureInfoBar infoBar;

    [ImportingConstructor]
    public OpenInIdeNotification(IOpenInIdeFailureInfoBar infoBar, IToolWindowService toolWindowService)
    {
        this.toolWindowService = toolWindowService;
        this.infoBar = infoBar;
    }

    public void UnableToLocateIssue(string filePath, Guid toolWindowId) => 
        Show(string.Format(OpenInIdeResources.Notification_UnableToLocateIssue, filePath), toolWindowId, true);

    public void UnableToOpenFile(string filePath, Guid toolWindowId) => 
        Show(string.Format(OpenInIdeResources.Notification_UnableToOpenFile, filePath), toolWindowId, true);
    
    public void InvalidRequest(string reason, Guid toolWindowId) => 
        Show(string.Format(OpenInIdeResources.Notification_InvalidConfiguration, reason), toolWindowId, false);

    public void Clear()
    {
        infoBar.ClearAsync().Forget();
    }

    private void Show(string message, Guid toolWindowId, bool hasMoreInfo)
    {
        toolWindowService.Show(toolWindowId);
        infoBar.ShowAsync(toolWindowId, message, hasMoreInfo).Forget();
    }
}
