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

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using SonarLint.VisualStudio.CFamily.Analysis;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.ErrorList;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    ///<summary>
    /// Tracks SonarLint errors for a specific buffer.
    ///</summary>
    /// <remarks>
    /// <para>The lifespan of this object is tied to the lifespan of the taggers on the view. On creation of the first tagger,
    /// it starts tracking errors. On the disposal of the last tagger, it shuts down.</para>
    /// <para>
    /// See the README.md in this folder for more information
    /// </para>
    /// </remarks>
    internal sealed class TextBufferIssueTracker : IIssueTracker, ITagger<IErrorTag>, IDisposable
    {
        internal /* for testing */ TaggerProvider Provider { get; }
        private readonly ITextBuffer textBuffer;
        private readonly IEnumerable<AnalysisLanguage> detectedLanguages;
        
        private readonly ITextDocument document;
        private readonly ILogger logger;
        private readonly IVsAwareAnalysisService vsAwareAnalysisService;
        private readonly ISonarErrorListDataSource sonarErrorDataSource;

        public string LastAnalysisFilePath { get; private set; }
        internal /* for testing */ IssuesSnapshotFactory Factory { get; }

        public TextBufferIssueTracker(
            TaggerProvider provider,
            ITextDocument document,
            IEnumerable<AnalysisLanguage> detectedLanguages,
            ISonarErrorListDataSource sonarErrorDataSource,
            IVsAwareAnalysisService vsAwareAnalysisService,
            IFileTracker fileTracker,
            ILogger logger)
        {

            this.Provider = provider;
            this.textBuffer = document.TextBuffer;

            this.detectedLanguages = detectedLanguages;
            this.sonarErrorDataSource = sonarErrorDataSource;
            this.vsAwareAnalysisService = vsAwareAnalysisService;
            this.logger = logger;

            this.document = document;
            LastAnalysisFilePath = document.FilePath;
            fileTracker.AddFiles(LastAnalysisFilePath);
            Factory = new IssuesSnapshotFactory(LastAnalysisFilePath);

            document.FileActionOccurred += SafeOnFileActionOccurred;

            sonarErrorDataSource.AddFactory(this.Factory);
            Provider.AddIssueTracker(this);

            RequestAnalysis(new AnalyzerOptions{ IsOnOpen = true });
        }

        private void SafeOnFileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            // Handles callback from VS. Suppress non-critical errors to prevent them
            // propagating to VS, which would display a dialogue and disable the extension.
            try
            {
                if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
                {
                    RequestAnalysis(new AnalyzerOptions { IsOnOpen = false });
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Strings.Analysis_ErrorTriggeringAnalysis, ex);
            }
        }

        private void SnapToNewSnapshot(IIssuesSnapshot newSnapshot)
        {
            // Tell our factory to snap to a new snapshot.
            Factory.UpdateSnapshot(newSnapshot);

            sonarErrorDataSource.RefreshErrorList(Factory);
        }

        public void RequestAnalysis(IAnalyzerOptions options)
        {
            try
            {
                vsAwareAnalysisService.CancelForFile(LastAnalysisFilePath);
                LastAnalysisFilePath = document.FilePath; // Refresh the stored file path in case the document has been renamed
                vsAwareAnalysisService.RequestAnalysis(LastAnalysisFilePath, document, detectedLanguages, SnapToNewSnapshot, options);
            }
            catch (NotSupportedException ex)
            {
                // Display a simple user-friendly message for options we know are not supported.
                // See https://github.com/SonarSource/sonarlint-visualstudio/pull/2212
                logger.WriteLine(Strings.Analysis_NotSupported, ex.Message);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Strings.Analysis_ErrorTriggeringAnalysis, ex);
            }
        }

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans) => [];

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public void Dispose()
        {
            vsAwareAnalysisService.CancelForFile(LastAnalysisFilePath);
            document.FileActionOccurred -= SafeOnFileActionOccurred;
            textBuffer.Properties.RemoveProperty(TaggerProvider.SingletonManagerPropertyCollectionKey);
            sonarErrorDataSource.RemoveFactory(this.Factory);
            Provider.RemoveIssueTracker(this);
        }
    }
}
