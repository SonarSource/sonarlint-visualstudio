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

using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.InfoBar;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.OpenInIde;
using SonarLint.VisualStudio.SLCore.Listener.FixSuggestion;
using SonarLint.VisualStudio.SLCore.Listener.FixSuggestion.Models;

namespace SonarLint.VisualStudio.IssueVisualization.FixSuggestion;

[Export(typeof(IFixSuggestionHandler))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class FixSuggestionHandler : IFixSuggestionHandler
{
    private readonly IOpenInIdeConfigScopeValidator openInIdeConfigScopeValidator;
    private readonly IIDEWindowService ideWindowService;
    private readonly IFixSuggestionNotification fixSuggestionNotification;
    private readonly IThreadHandling threadHandling;
    private readonly ILogger logger;
    private readonly IDocumentNavigator documentNavigator;
    private readonly IIssueSpanCalculator issueSpanCalculator;

    [ImportingConstructor]
    internal FixSuggestionHandler(
        ILogger logger,
        IDocumentNavigator documentNavigator,
        IIssueSpanCalculator issueSpanCalculator,
        IOpenInIdeConfigScopeValidator openInIdeConfigScopeValidator,
        IIDEWindowService ideWindowService,
        IFixSuggestionNotification fixSuggestionNotification) : 
        this(
            ThreadHandling.Instance,
            logger,
            documentNavigator,
            issueSpanCalculator,
            openInIdeConfigScopeValidator,
            ideWindowService, 
            fixSuggestionNotification)
    {
    }

    internal FixSuggestionHandler(
        IThreadHandling threadHandling,
        ILogger logger,
        IDocumentNavigator documentNavigator,
        IIssueSpanCalculator issueSpanCalculator,
        IOpenInIdeConfigScopeValidator openInIdeConfigScopeValidator,
        IIDEWindowService ideWindowService, 
        IFixSuggestionNotification fixSuggestionNotification)
    {
        this.threadHandling = threadHandling;
        this.logger = logger;
        this.documentNavigator = documentNavigator;
        this.issueSpanCalculator = issueSpanCalculator;
        this.openInIdeConfigScopeValidator = openInIdeConfigScopeValidator;
        this.ideWindowService = ideWindowService;
        this.fixSuggestionNotification = fixSuggestionNotification;
    }

    public void ApplyFixSuggestion(ShowFixSuggestionParams parameters)
    {
        if (!ValidateConfiguration(parameters.configurationScopeId, out var configurationScopeRoot, out var failureReason))
        {
            logger.WriteLine(FixSuggestionResources.GetConfigScopeRootPathFailed, parameters.configurationScopeId, failureReason);
            return;
        }
        
        try
        {
            logger.WriteLine(FixSuggestionResources.ProcessingRequest, parameters.configurationScopeId, parameters.fixSuggestion.suggestionId);
            threadHandling.RunOnUIThread(() => ApplyAndShowAppliedFixSuggestions(parameters, configurationScopeRoot));
            logger.WriteLine(FixSuggestionResources.DoneProcessingRequest, parameters.configurationScopeId, parameters.fixSuggestion.suggestionId);
        }
        catch (Exception exception) when (!ErrorHandler.IsCriticalException(exception))
        {
            logger.WriteLine(FixSuggestionResources.ProcessingRequestFailed, parameters.configurationScopeId, parameters.fixSuggestion.suggestionId, exception.Message);
        }
    }

    private bool ValidateConfiguration(string configurationScopeId, out string configurationScopeRoot, out string failureReason)
    {
        return openInIdeConfigScopeValidator.TryGetConfigurationScopeRoot(configurationScopeId, out configurationScopeRoot, out failureReason);
    }

    private void ApplyAndShowAppliedFixSuggestions(ShowFixSuggestionParams parameters, string configurationScopeRoot)
    {
        fixSuggestionNotification.ClearAsync().Forget();
        ideWindowService.BringToFront();
        var absoluteFilePath = Path.Combine(configurationScopeRoot, parameters.fixSuggestion.fileEdit.idePath);
        var textView = GetFileContent(parameters, absoluteFilePath);
        if (!ValidateFileExists(textView, absoluteFilePath))
        {
            return;
        }
        ApplySuggestedChanges(textView, parameters.fixSuggestion.fileEdit.changes);
    }

    private void ApplySuggestedChanges(ITextView textView, List<ChangesDto> changes)
    {
        for (var i = changes.Count - 1; i >= 0; i--)
        {
            var changeDto = changes[i];
            var textEdit = textView.TextBuffer.CreateEdit();
            try
            {
                var spanToUpdate = issueSpanCalculator.CalculateSpan(textView.TextSnapshot, changeDto.beforeLineRange.startLine, changeDto.beforeLineRange.endLine);
                textView.Caret.MoveTo(spanToUpdate.Start);
                textView.ViewScroller.EnsureSpanVisible(spanToUpdate, EnsureSpanVisibleOptions.AlwaysCenter);
                textEdit.Replace(spanToUpdate, changeDto.after);
                textEdit.Apply();
            }
            catch (Exception)
            {
                textEdit.Cancel();
                throw;
            }
        }
    }

    private bool ValidateFileExists(ITextView fileContent, string absoluteFilePath)
    {
        if (fileContent != null)
        {
            return true;
        }

        fixSuggestionNotification.UnableToOpenFileAsync(absoluteFilePath).Forget();
        return false;
    }

    private ITextView GetFileContent(ShowFixSuggestionParams parameters, string absoluteFilePath)
    {
        try
        {
          
            return documentNavigator.Open(absoluteFilePath);
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(Resources.ERR_OpenDocumentException, parameters.fixSuggestion.fileEdit.idePath, ex.Message);
            return null;
        }
    }
}
