﻿/*
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
using System.ComponentModel.Composition;
using System.IO.Abstractions;
using System.Linq;
using SonarLint.VisualStudio.CFamily.Analysis;
using SonarLint.VisualStudio.CFamily.SubProcess;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Infrastructure.VS.DocumentEvents;
using SonarLint.VisualStudio.IssueVisualization.Editor.LanguageDetection;

namespace SonarLint.VisualStudio.CFamily.PreCompiledHeaders
{
    public interface IPreCompiledHeadersEventListener : IDisposable
    {
    }

    [Export(typeof(IPreCompiledHeadersEventListener))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class PreCompiledHeadersEventListener : IPreCompiledHeadersEventListener
    {
        internal const string PchJobId = "pch-generation";
        internal readonly int pchJobTimeoutInMilliseconds;

        private readonly ICFamilyAnalyzer cFamilyAnalyzer;
        private readonly IActiveDocumentTracker activeDocumentTracker;
        private readonly IScheduler scheduler;
        private readonly ISonarLanguageRecognizer sonarLanguageRecognizer;
        private readonly IPchCacheCleaner pchCacheCleaner;
        private bool disposed;

        [ImportingConstructor]
        public PreCompiledHeadersEventListener(ICFamilyAnalyzer cFamilyAnalyzer,
            IActiveDocumentTracker activeDocumentTracker,
            IScheduler scheduler,
            ISonarLanguageRecognizer sonarLanguageRecognizer)
            : this(cFamilyAnalyzer, activeDocumentTracker, scheduler, sonarLanguageRecognizer, new EnvironmentSettings(), new PchCacheCleaner(new FileSystem(), SubProcessFilePaths.PchFilePath))
        {
        }

        internal PreCompiledHeadersEventListener(ICFamilyAnalyzer cFamilyAnalyzer,
            IActiveDocumentTracker activeDocumentTracker,
            IScheduler scheduler,
            ISonarLanguageRecognizer sonarLanguageRecognizer,
            IEnvironmentSettings environmentSettings,
            IPchCacheCleaner pchCacheCleaner)
        {
            this.cFamilyAnalyzer = cFamilyAnalyzer;
            this.activeDocumentTracker = activeDocumentTracker;
            this.scheduler = scheduler;
            this.sonarLanguageRecognizer = sonarLanguageRecognizer;
            this.pchCacheCleaner = pchCacheCleaner;

            pchJobTimeoutInMilliseconds = environmentSettings.PCHGenerationTimeoutInMs(60 * 1000);

            activeDocumentTracker.ActiveDocumentChanged += OnActiveDocumentFocused;
        }

        private void OnActiveDocumentFocused(object sender, ActiveDocumentChangedEventArgs e)
        {
            if (e.ActiveTextDocument == null)
            {
                return;
            }

            var detectedLanguages = sonarLanguageRecognizer.Detect(e.ActiveTextDocument.FilePath, e.ActiveTextDocument.TextBuffer.ContentType);

            if (!detectedLanguages.Any() || !cFamilyAnalyzer.IsAnalysisSupported(detectedLanguages))
            {
                return;
            }

            var cFamilyAnalyzerOptions = new CFamilyAnalyzerOptions
            {
                CreatePreCompiledHeaders = true
            };

            scheduler.Schedule(PchJobId, token =>
            {
                cFamilyAnalyzer.ExecuteAnalysis(e.ActiveTextDocument.FilePath,
                    detectedLanguages,
                    null,
                    cFamilyAnalyzerOptions,
                    null,
                    token);
            }, pchJobTimeoutInMilliseconds);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                activeDocumentTracker.ActiveDocumentChanged -= OnActiveDocumentFocused;
                activeDocumentTracker?.Dispose();

                try
                {
                    pchCacheCleaner.Cleanup();
                }
                catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
                {
                    // Nothing to do if we failed to clear the cache
                }

                disposed = true;
            }
        }
    }
}
