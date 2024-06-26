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
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.InfoBar;

namespace SonarLint.VisualStudio.IssueVisualization.OpenInIde;

public interface IOpenInIdeFailureInfoBar
{
    Task ShowAsync(Guid toolWindowId, string text, bool showMoreInfoButton);

    Task ClearAsync();
}

[Export(typeof(IOpenInIdeFailureInfoBar))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class OpenInIdeFailureInfoBar : IOpenInIdeFailureInfoBar, IDisposable
{
    private readonly IInfoBarManager infoBarManager;
    private readonly IOutputWindowService outputWindowService;
    private readonly IBrowserService browService;
    private readonly IThreadHandling threadHandling;
    private readonly object lockObject = new();
    private IInfoBar currentInfoBar;

    [ImportingConstructor]
    public OpenInIdeFailureInfoBar(IInfoBarManager infoBarManager,
        IOutputWindowService outputWindowService,
        IBrowserService browService,
        IThreadHandling threadHandling)
    {
        this.infoBarManager = infoBarManager;
        this.outputWindowService = outputWindowService;
        this.browService = browService;
        this.threadHandling = threadHandling;
    }

    public async Task ShowAsync(Guid toolWindowId, string text, bool showMoreInfoButton)
    {
        await threadHandling.RunOnUIThreadAsync(() =>
        {
            lock (lockObject)
            {
                RemoveExistingInfoBar();
                AddInfoBar(toolWindowId, text, showMoreInfoButton);
            }
        });
    }

    public async Task ClearAsync()
    {
        await threadHandling.RunOnUIThreadAsync(() =>
        {
            lock (lockObject)
            {
                RemoveExistingInfoBar();
            }
        });
    }

    private void AddInfoBar(Guid toolWindowId, string text, bool hasMoreInfo)
    {
        List<string> buttonTexts = hasMoreInfo 
            ? [OpenInIdeResources.InfoBar_Button_MoreInfo, OpenInIdeResources.InfoBar_Button_ShowLogs] 
            : [OpenInIdeResources.InfoBar_Button_ShowLogs];
        currentInfoBar = infoBarManager.AttachInfoBarWithButtons(toolWindowId,
            text ?? OpenInIdeResources.DefaultInfoBarMessage, buttonTexts, default);
        Debug.Assert(currentInfoBar != null, "currentInfoBar != null");

        currentInfoBar.ButtonClick += HandleInfoBarAction;
        currentInfoBar.Closed += CurrentInfoBar_Closed;
    }

    private void HandleInfoBarAction(object sender, InfoBarButtonClickedEventArgs e)
    {
        if (e.ClickedButtonText == OpenInIdeResources.InfoBar_Button_MoreInfo)
        {
            browService.Navigate(DocumentationLinks.OpenInIdeIssueLocation);
        }

        if (e.ClickedButtonText == OpenInIdeResources.InfoBar_Button_ShowLogs)
        {
            outputWindowService.Show();
        }
    }

    private void RemoveExistingInfoBar()
    {
        if (currentInfoBar != null)
        {
            currentInfoBar.ButtonClick -= HandleInfoBarAction;
            currentInfoBar.Closed -= CurrentInfoBar_Closed;
            infoBarManager.DetachInfoBar(currentInfoBar);
            currentInfoBar = null;
        }
    }

    private void CurrentInfoBar_Closed(object sender, EventArgs e)
    {
        lock (lockObject)
        {
            RemoveExistingInfoBar();
        }
    }

    public void Dispose()
    {
        lock (lockObject)
        {
            RemoveExistingInfoBar();
        }
    }
}
