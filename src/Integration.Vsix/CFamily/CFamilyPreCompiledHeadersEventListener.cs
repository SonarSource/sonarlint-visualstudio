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
using System.Threading;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.Vsix.Helpers.DocumentEvents;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    interface ICFamilyPreCompiledHeadersEventListener : IDisposable
    {
        void Listen();
    }

    [Export(typeof(ICFamilyPreCompiledHeadersEventListener))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class CFamilyPreCompiledHeadersEventListener : ICFamilyPreCompiledHeadersEventListener
    {
        internal const string PchJobId = "pch-generation";
        internal string pchFilePath = Path.Combine(Path.GetTempPath(), "SonarLintPCH.txt");

        private readonly ICLangAnalyzer cLangAnalyzer;
        private readonly IDocumentFocusedEventRaiser documentFocusedEventRaiser;
        private readonly IScheduler scheduler;
        private readonly ISonarLanguageRecognizer sonarLanguageRecognizer;
        private bool disposed;

        [ImportingConstructor]
        public CFamilyPreCompiledHeadersEventListener(ICLangAnalyzer cLangAnalyzer, 
            IDocumentFocusedEventRaiser documentFocusedEventRaiser, 
            IScheduler scheduler,
            ISonarLanguageRecognizer sonarLanguageRecognizer)
        {
            this.cLangAnalyzer = cLangAnalyzer;
            this.documentFocusedEventRaiser = documentFocusedEventRaiser;
            this.scheduler = scheduler;
            this.sonarLanguageRecognizer = sonarLanguageRecognizer;
        }

        public void Listen()
        {
            documentFocusedEventRaiser.OnDocumentFocused += OnDocumentFocused;
        }

        private void OnDocumentFocused(object sender, DocumentFocusedEventArgs e)
        {
            var detectedLanguages = sonarLanguageRecognizer.Detect(e.DocumentFilePath, e.TextBufferContentType);

            if (!detectedLanguages.Any() || !cLangAnalyzer.IsAnalysisSupported(detectedLanguages))
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
                cLangAnalyzer.ExecuteAnalysis(e.DocumentFilePath,
                    null,
                    detectedLanguages,
                    null,
                    cFamilyAnalyzerOptions,
                    token);
            }, Timeout.Infinite);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                documentFocusedEventRaiser.OnDocumentFocused -= OnDocumentFocused;
                documentFocusedEventRaiser?.Dispose();
                disposed = true;
            }
        }
    }
}
