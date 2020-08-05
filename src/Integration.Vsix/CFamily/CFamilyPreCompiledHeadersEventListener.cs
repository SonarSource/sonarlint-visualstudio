using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using SonarLint.VisualStudio.Core.Analysis;
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
        private readonly ICLangAnalyzer cLangAnalyzer;
        private readonly IDocumentFocusedEventRaiser documentFocusedEventRaiser;
        private readonly IScheduler scheduler;
        private bool disposed;

        [ImportingConstructor]
        public CFamilyPreCompiledHeadersEventListener(ICLangAnalyzer cLangAnalyzer, 
            IDocumentFocusedEventRaiser documentFocusedEventRaiser, 
            IScheduler scheduler)
        {
            this.cLangAnalyzer = cLangAnalyzer;
            this.documentFocusedEventRaiser = documentFocusedEventRaiser;
            this.scheduler = scheduler;
        }

        public void Listen()
        {
            documentFocusedEventRaiser.OnDocumentFocused += OnDocumentFocused;
        }

        private void OnDocumentFocused(object sender, DocumentFocusedEventArgs e)
        {
            var cFamilyAnalyzerOptions = new CFamilyAnalyzerOptions
            {
                CreatePreCompiledHeaders = true,
                PreCompiledHeadersFilePath = Path.Combine(Path.GetTempPath(), "SonarLintPCH.txt")
            };

            scheduler.Schedule("pch-generation", token =>
            {
                cLangAnalyzer.ExecuteAnalysis(e.DocumentFilePath,
                    null,
                    new List<AnalysisLanguage> { AnalysisLanguage.CFamily },
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
