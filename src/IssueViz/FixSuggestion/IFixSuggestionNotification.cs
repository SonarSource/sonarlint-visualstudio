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
using System.Drawing;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.InfoBar;
using SonarLint.VisualStudio.IssueVisualization.OpenInIde;

namespace SonarLint.VisualStudio.IssueVisualization.FixSuggestion;

public interface IFixSuggestionNotification
{
    Task UnableToOpenFileAsync(string filePath);
    Task InvalidRequestAsync(string reason);
    Task UnableToLocateIssue(string filePath);
    Task ShowAsync(string text);
    Task ClearAsync();
}

[Export(typeof(IFixSuggestionNotification))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class FixSuggestionNotification : IFixSuggestionNotification, IDisposable
{
    private readonly IInfoBarManager infoBarManager;
    private readonly IOutputWindowService outputWindowService;
    private readonly IBrowserService browService;
    private readonly IThreadHandling threadHandling;
    private readonly object lockObject = new();
    private IInfoBar currentInfoBar;

    [ImportingConstructor]
    public FixSuggestionNotification(IInfoBarManager infoBarManager,
        IOutputWindowService outputWindowService,
        IBrowserService browService,
        IThreadHandling threadHandling)
    {
        this.infoBarManager = infoBarManager;
        this.outputWindowService = outputWindowService;
        this.browService = browService;
        this.threadHandling = threadHandling;
    }

    public async Task UnableToOpenFileAsync(string filePath)
    {
        var unableToOpenFileMsg = string.Format(FixSuggestionResources.InfoBarUnableToOpenFile, filePath);
        await ShowAsync(unableToOpenFileMsg);
    }

    public async Task InvalidRequestAsync(string reason)
    {
        var unableToOpenFileMsg = string.Format(FixSuggestionResources.InfoBarInvalidRequest, reason);
        await ShowAsync(unableToOpenFileMsg);
    }

    public async Task UnableToLocateIssue(string filePath)
    {
        var unableToOpenFileMsg = string.Format(FixSuggestionResources.InfoBarUnableToLocateFixSuggestion, filePath);
        await ShowAsync(unableToOpenFileMsg);
    }

    public async Task ShowAsync(string text)
    {
        await threadHandling.RunOnUIThreadAsync(() =>
        {
            lock (lockObject)
            {
                RemoveExistingInfoBar();
                AddInfoBar(text);
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

    private void AddInfoBar(string text)
    {
        string[] buttonTexts = [FixSuggestionResources.InfoBarButtonMoreInfo, FixSuggestionResources.InfoBarButtonShowLogs];
        var textToShow = text ?? FixSuggestionResources.InfoBarDefaultMessage;
        currentInfoBar = infoBarManager.AttachInfoBarToMainWindow(textToShow, SonarLintImageMoniker.OfficialSonarLintMoniker, buttonTexts);
        Debug.Assert(currentInfoBar != null, "currentInfoBar != null");

        currentInfoBar.ButtonClick += HandleInfoBarAction;
        currentInfoBar.Closed += CurrentInfoBar_Closed;
    }

    private void HandleInfoBarAction(object sender, InfoBarButtonClickedEventArgs e)
    {
        if (e.ClickedButtonText == FixSuggestionResources.InfoBarButtonMoreInfo)
        {
            browService.Navigate(DocumentationLinks.OpenInIdeIssueLocation);
        }

        if (e.ClickedButtonText == FixSuggestionResources.InfoBarButtonShowLogs)
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
