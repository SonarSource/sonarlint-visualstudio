/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.Core;

public abstract class DocumentEventArgs(string fullPath,  IEnumerable<AnalysisLanguage> detectedLanguages) : EventArgs
{
    /// <summary>
    /// Full file path to the document being closed
    /// </summary>
    public string FullPath { get; } = fullPath;
    public IEnumerable<AnalysisLanguage> DetectedLanguages { get; } = detectedLanguages;
}

public class DocumentClosedEventArgs(string fullPath, IEnumerable<AnalysisLanguage> detectedLanguages) : DocumentEventArgs(fullPath, detectedLanguages);

public class DocumentOpenedEventArgs(string fullPath, IEnumerable<AnalysisLanguage> detectedLanguages) : DocumentEventArgs(fullPath, detectedLanguages);

public class DocumentSavedEventArgs(string fullPath, string newContent, IEnumerable<AnalysisLanguage> detectedLanguages) : DocumentEventArgs(fullPath, detectedLanguages)
{
    public string NewContent { get; } = newContent;
}

public class DocumentRenamedEventArgs(string fullPath, string oldFilePath, IEnumerable<AnalysisLanguage> detectedLanguages) : DocumentEventArgs(fullPath, detectedLanguages)
{
    public string OldFilePath { get; } = oldFilePath;
}

/// <summary>
/// Raises notifications about document events
/// </summary>
public interface IDocumentEvents
{
    event EventHandler<DocumentClosedEventArgs> DocumentClosed;
    event EventHandler<DocumentOpenedEventArgs> DocumentOpened;
    event EventHandler<DocumentSavedEventArgs> DocumentSaved;

    /// <summary>
    /// Raised when an opened document is renamed
    /// </summary>
    event EventHandler<DocumentRenamedEventArgs> OpenDocumentRenamed;
}
