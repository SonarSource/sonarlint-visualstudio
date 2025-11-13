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

using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.Core;

public class DocumentEventArgs(Document document) : EventArgs
{
    public Document Document { get; } = document;
}

public class DocumentRenamedEventArgs(Document document, string oldFilePath) : DocumentEventArgs(document)
{
    public string OldFilePath { get; } = oldFilePath;
}

public class Document(string fullPath, IEnumerable<AnalysisLanguage> detectedLanguages)
{
    public string FullPath { get; } = fullPath;
    public IEnumerable<AnalysisLanguage> DetectedLanguages { get; } = detectedLanguages;
}

/// <summary>
/// Keeps track of open documents
/// </summary>
public interface IDocumentTracker
{
    event EventHandler<DocumentEventArgs> DocumentClosed;
    event EventHandler<DocumentEventArgs> DocumentOpened;
    event EventHandler<DocumentEventArgs> DocumentSaved;
    /// <summary>
    /// Raised when an opened document is renamed
    /// </summary>
    event EventHandler<DocumentRenamedEventArgs> OpenDocumentRenamed;

    Document[] GetOpenDocuments();
}
