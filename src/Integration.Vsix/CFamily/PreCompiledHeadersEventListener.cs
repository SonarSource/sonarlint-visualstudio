/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.IO;
using System.Linq;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.Vsix.Helpers.DocumentEvents;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    interface IPreCompiledHeadersEventListener : IDisposable
    {
        void Listen();
    }

    [Export(typeof(IPreCompiledHeadersEventListener))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class PreCompiledHeadersEventListener : IPreCompiledHeadersEventListener
    {
        internal const string PchJobId = "pch-generation";
        internal const int PchJobTimeoutInMilliseconds = 60 * 1000;
        internal const string PchFilePathSuffix = "SonarLintPCH.preamble";
        internal string pchFilePath = Path.Combine(Path.GetTempPath(), PchFilePathSuffix);

        private readonly ICFamilyAnalyzer cFamilyAnalyzer;
        private readonly IActiveDocumentTracker activeDocumentTracker;
        private readonly IScheduler scheduler;
        private readonly ISonarLanguageRecognizer sonarLanguageRecognizer;
        private bool disposed;

        [ImportingConstructor]
        public PreCompiledHeadersEventListener(ICFamilyAnalyzer cFamilyAnalyzer,
            IActiveDocumentTracker activeDocumentTracker, 
            IScheduler scheduler,
            ISonarLanguageRecognizer sonarLanguageRecognizer)
        {
            this.cFamilyAnalyzer = cFamilyAnalyzer;
            this.activeDocumentTracker = activeDocumentTracker;
            this.scheduler = scheduler;
            this.sonarLanguageRecognizer = sonarLanguageRecognizer;
        }

        public void Listen()
        {
            activeDocumentTracker.OnDocumentFocused += OnActiveDocumentFocused;
        }

        private void OnActiveDocumentFocused(object sender, DocumentFocusedEventArgs e)
        {
            var detectedLanguages = sonarLanguageRecognizer.Detect(e.TextDocument.FilePath, e.TextDocument.TextBuffer.ContentType);

            if (!detectedLanguages.Any() || !cFamilyAnalyzer.IsAnalysisSupported(detectedLanguages))
            {
                return;
            }

            var cFamilyAnalyzerOptions = new CFamilyAnalyzerOptions
            {
                CreatePreCompiledHeaders = true,
                PreCompiledHeadersFilePath = pchFilePath
            };

            scheduler.Schedule(PchJobId, token =>
            {
                cFamilyAnalyzer.ExecuteAnalysis(e.TextDocument.FilePath,
                    null,
                    detectedLanguages,
                    null,
                    cFamilyAnalyzerOptions,
                    token);
            }, PchJobTimeoutInMilliseconds);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                activeDocumentTracker.OnDocumentFocused -= OnActiveDocumentFocused;
                activeDocumentTracker?.Dispose();
                disposed = true;
            }
        }
    }
}
