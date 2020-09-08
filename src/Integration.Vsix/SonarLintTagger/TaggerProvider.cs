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

using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Factory for the <see cref="ITagger{T}"/>. There will be one instance of this class/VS session.
    /// </summary>
    /// <remarks>
    /// See the README.md in this folder for more information
    /// </remarks>
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IErrorTag))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class TaggerProvider : ITaggerProvider
    {
        private readonly ITextDocumentFactoryService textDocumentFactoryService;
        private readonly IAnalyzerController analyzerController;
        private readonly ISonarLanguageRecognizer languageRecognizer;
        private readonly IAnalysisRequestHandlersStore analysisRequestHandlersStore;
        private readonly IAnalysisRequestHandlerFactory analysisRequestHandlerFactory;

        [ImportingConstructor]
        internal TaggerProvider(ITextDocumentFactoryService textDocumentFactoryService,
            IAnalyzerController analyzerController,
            ISonarLanguageRecognizer languageRecognizer,
            IAnalysisRequestHandlersStore analysisRequestHandlersStore,
            IAnalysisRequestHandlerFactory analysisRequestHandlerFactory)
        {
            this.textDocumentFactoryService = textDocumentFactoryService;
            this.analyzerController = analyzerController;
            this.languageRecognizer = languageRecognizer;
            this.analysisRequestHandlersStore = analysisRequestHandlersStore;
            this.analysisRequestHandlerFactory = analysisRequestHandlerFactory;
        }

        /// <summary>
        /// Create a tagger that will track SonarLint issues on the view/buffer combination.
        /// </summary>
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            // Only attempt to track the view's edit buffer.
            if (typeof(T) != typeof(IErrorTag))
            {
                return null;
            }

            if (!textDocumentFactoryService.TryGetTextDocument(buffer, out var textDocument))
            {
                return null;
            }

            var detectedLanguages = languageRecognizer.Detect(textDocument.FilePath, buffer.ContentType);

            if (detectedLanguages.Any() && analyzerController.IsAnalysisSupported(detectedLanguages))
            {
                var issueTracker = buffer.Properties.GetOrCreateSingletonProperty(typeof(IAnalysisRequestHandler),
                    () =>
                    {
                        var tracker = analysisRequestHandlerFactory.Create(textDocument, detectedLanguages);
                        analysisRequestHandlersStore.Add(tracker);

                        return tracker;
                    });

                return issueTracker as ITagger<T>;
            }

            return null;
        }
    }
}
