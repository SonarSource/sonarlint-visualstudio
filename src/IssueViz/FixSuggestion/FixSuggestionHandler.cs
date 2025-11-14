/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.FixSuggestion.DiffView;
using SonarLint.VisualStudio.IssueVisualization.OpenInIde;

namespace SonarLint.VisualStudio.IssueVisualization.FixSuggestion;

[Export(typeof(IFixSuggestionHandler))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class FixSuggestionHandler(
    IDocumentNavigator documentNavigator,
    ITextViewEditor textViewEditor,
    IOpenInIdeConfigScopeValidator openInIdeConfigScopeValidator,
    IIDEWindowService ideWindowService,
    IFixSuggestionNotification fixSuggestionNotification,
    IDiffViewService diffViewService,
    ITelemetryManager telemetryManager,
    IThreadHandling threadHandling,
    ILogger logger)
    : IFixSuggestionHandler
{
    public void ApplyFixSuggestion(string configScopeId, string fixSuggestionId, string idePath, IReadOnlyList<FixSuggestionChange> changes)
    {
        try
        {
            logger.WriteLine(FixSuggestionResources.ProcessingRequest, configScopeId, fixSuggestionId);
            ideWindowService.BringToFront();
            fixSuggestionNotification.Clear();

            if (!ValidateConfiguration(configScopeId, out var configurationScopeRoot, out var failureReason))
            {
                logger.WriteLine(FixSuggestionResources.GetConfigScopeRootPathFailed, configScopeId, failureReason);
                fixSuggestionNotification.InvalidRequest(failureReason);
                return;
            }

            threadHandling.RunOnUIThread(() => ApplyAndShowAppliedFixSuggestions(fixSuggestionId, idePath, changes, configurationScopeRoot));
            logger.WriteLine(FixSuggestionResources.DoneProcessingRequest, configScopeId, fixSuggestionId);
        }
        catch (Exception exception) when (!ErrorHandler.IsCriticalException(exception))
        {
            logger.WriteLine(FixSuggestionResources.ProcessingRequestFailed, configScopeId, fixSuggestionId, exception.Message);
        }
    }

    private bool ValidateConfiguration(string configurationScopeId, out string configurationScopeRoot, out string failureReason) =>
        openInIdeConfigScopeValidator.TryGetConfigurationScopeRoot(configurationScopeId, out configurationScopeRoot, out failureReason);

    private void ApplyAndShowAppliedFixSuggestions(string fixSuggestionId, string idePath, IReadOnlyList<FixSuggestionChange> changes, string configurationScopeRoot)
    {
        var absoluteFilePath = Path.Combine(configurationScopeRoot, idePath);
        var textView = GetFileContent(absoluteFilePath);
        if (!ValidateFileExists(textView, absoluteFilePath))
        {
            return;
        }
        ApplySuggestedChangesAndFocus(textView, GetFinalizedChanges(textView, changes, fixSuggestionId), absoluteFilePath);
    }

    private FinalizedFixSuggestionChange[] GetFinalizedChanges(ITextView textView, IReadOnlyList<FixSuggestionChange> changes, string fixSuggestionId)
    {
        var finalizedFixSuggestionChanges = diffViewService.ShowDiffView(textView.TextBuffer, changes);

        telemetryManager.FixSuggestionResolved(fixSuggestionId, finalizedFixSuggestionChanges.Select(x => x.IsAccepted));

        return finalizedFixSuggestionChanges;
    }

    private void ApplySuggestedChangesAndFocus(
        ITextView textView,
        FinalizedFixSuggestionChange[] finalizedFixSuggestionChanges,
        string filePath)
    {
        if (!finalizedFixSuggestionChanges.Any(x => x.IsAccepted))
        {
            return;
        }

        var acceptedChanges = finalizedFixSuggestionChanges.Where(x => x.IsAccepted).Select(x => x.Change).ToArray();

        var changesApplied = textViewEditor.ApplyChanges(textView.TextBuffer, acceptedChanges, abortOnOriginalTextChanged: true);
        if (!changesApplied)
        {
            fixSuggestionNotification.UnableToLocateIssue(filePath);
            return;
        }

        textViewEditor.FocusLine(textView, acceptedChanges[0].BeforeStartLine);
    }

    private bool ValidateFileExists(ITextView fileContent, string absoluteFilePath)
    {
        if (fileContent != null)
        {
            return true;
        }

        fixSuggestionNotification.UnableToOpenFile(absoluteFilePath);
        return false;
    }

    private ITextView GetFileContent(string filePath)
    {
        try
        {
            return documentNavigator.Open(filePath);
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(Resources.ERR_OpenDocumentException, filePath, ex.Message);
            return null;
        }
    }
}
