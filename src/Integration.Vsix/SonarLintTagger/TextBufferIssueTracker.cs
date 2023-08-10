/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
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
        private readonly DTE2 dte;
        internal /* for testing */ TaggerProvider Provider { get; }
        private readonly ITextBuffer textBuffer;
        private readonly IEnumerable<AnalysisLanguage> detectedLanguages;

        private readonly ITextDocument document;
        private readonly string charset;
        private readonly ILogger logger;
        private readonly ISonarErrorListDataSource sonarErrorDataSource;
        private readonly IVsSolution5 vsSolution;
        private readonly IIssueConsumerFactory issueConsumerFactory;
        private readonly IThreadHandling threadHandling;

        public string FilePath { get; private set; }
        internal /* for testing */ IssuesSnapshotFactory Factory { get; }

        public TextBufferIssueTracker(DTE2 dte,
            TaggerProvider provider,
            ITextDocument document,
            IEnumerable<AnalysisLanguage> detectedLanguages,
            ISonarErrorListDataSource sonarErrorDataSource,
            IVsSolution5 vsSolution,
            IIssueConsumerFactory issueConsumerFactory,
            ILogger logger,
            IThreadHandling threadHandling)
        {
            this.dte = dte;

            this.Provider = provider;
            this.textBuffer = document.TextBuffer;

            this.detectedLanguages = detectedLanguages;
            this.sonarErrorDataSource = sonarErrorDataSource;
            this.vsSolution = vsSolution;
            this.issueConsumerFactory = issueConsumerFactory;
            this.logger = logger;
            this.threadHandling = threadHandling;

            this.document = document;
            this.FilePath = document.FilePath;
            this.charset = document.Encoding.WebName;

            this.Factory = new IssuesSnapshotFactory(FilePath);

            document.FileActionOccurred += SafeOnFileActionOccurred;

            sonarErrorDataSource.AddFactory(this.Factory);
            Provider.AddIssueTracker(this);

            RequestAnalysis(null /* no options */);
        }

        private void SafeOnFileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            // Handles callback from VS. Suppress non-critical errors to prevent them
            // propagating to VS, which would display a dialogue and disable the extension.
            try
            {
                if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
                {
                    RequestAnalysis(null /* no options */);
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
            RequestAnalysisAsync(options).Forget();
        }

        internal /* for testing */ async Task RequestAnalysisAsync(IAnalyzerOptions options)
        {
            try
            {
                string projectName = null;
                Guid projectGuid = Guid.Empty;

                await threadHandling.RunOnUIThreadAsync(() =>
                {
                    projectName = GetProjectName();
                    projectGuid = GetProjectGuid();
                });

                await threadHandling.SwitchToBackgroundThread();

                FilePath = document.FilePath; // Refresh the stored file path in case the document has been renamed

                var issueConsumer = issueConsumerFactory.Create(document, projectName, projectGuid, SnapToNewSnapshot);

                // Call the consumer with no analysis issues to immediately clear issues for this file
                // from the error list
                issueConsumer.Accept(FilePath, Enumerable.Empty<IAnalysisIssue>());

                Provider.RequestAnalysis(FilePath, charset, detectedLanguages, issueConsumer, options);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Strings.Analysis_ErrorTriggeringAnalysis, ex);
            }
        }

        private string GetProjectName() => GetProject()?.Name ?? "{none}";

        private Project GetProject()
        {
            // Bug #676: https://github.com/SonarSource/sonarlint-visualstudio/issues/676
            // It's possible to have a ProjectItem that doesn't have a ContainingProject
            // e.g. files under the "External Dependencies" project folder in the Solution Explorer
            var projectItem = dte.Solution.FindProjectItem(this.FilePath);
            return projectItem?.ContainingProject;
        }

        private Guid GetProjectGuid()
        {
            var project = GetProject();

            if (project == null || string.IsNullOrEmpty(project.FileName))
            {
                return Guid.Empty;
            }

            try
            {
                return vsSolution.GetGuidOfProjectFile(project.FileName);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.LogVerbose(Strings.TextBufferIssueTracker_ProjectGuidError, FilePath, ex);

                return Guid.Empty;
            }
        }

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            return Enumerable.Empty<ITagSpan<IErrorTag>>();
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public void Dispose()
        {
            document.FileActionOccurred -= SafeOnFileActionOccurred;
            textBuffer.Properties.RemoveProperty(TaggerProvider.SingletonManagerPropertyCollectionKey);
            sonarErrorDataSource.RemoveFactory(this.Factory);
            Provider.RemoveIssueTracker(this);
        }
    }
}
