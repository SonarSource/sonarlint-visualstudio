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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.LanguageDetection
{
    public interface ISonarLanguageRecognizer
    {
        IEnumerable<AnalysisLanguage> Detect(string filePath, IContentType bufferContentType);
    }

    [Export(typeof(ISonarLanguageRecognizer))]
    public class SonarLanguageRecognizer : ISonarLanguageRecognizer
    {
        private static readonly ISet<string> JavascriptSupportedExtensions = new HashSet<string> { "js", "jsx", "vue" };

        private readonly IContentTypeRegistryService contentTypeRegistryService;
        private readonly IFileExtensionRegistryService fileExtensionRegistryService;

        [ImportingConstructor]
        public SonarLanguageRecognizer(IContentTypeRegistryService contentTypeRegistryService, IFileExtensionRegistryService fileExtensionRegistryService)
        {
            this.contentTypeRegistryService = contentTypeRegistryService ?? throw new ArgumentNullException(nameof(contentTypeRegistryService));
            this.fileExtensionRegistryService = fileExtensionRegistryService ?? throw new ArgumentNullException(nameof(fileExtensionRegistryService));
        }

        public IEnumerable<AnalysisLanguage> Detect(string filePath, IContentType bufferContentType)
        {
            var fileExtension = Path.GetExtension(filePath).Replace(".", "");
            var contentTypes = GetExtensionContentTypes(fileExtension, bufferContentType);

            // Languages are for now mainly exclusive but it should possible for the same file to be analyzed by multiple
            // plugins (language plugin).
            var detectedLanguages = new List<AnalysisLanguage>();
            if (IsJavascriptDocument(fileExtension, contentTypes))
            {
                detectedLanguages.Add(AnalysisLanguage.Javascript);
            }

            if (IsCFamilyDocument(contentTypes))
            {
                detectedLanguages.Add(AnalysisLanguage.CFamily);
            }
            return detectedLanguages;
        }

        private IEnumerable<IContentType> GetExtensionContentTypes(string fileExtension, IContentType bufferContentType)
        {
            var contentTypes = contentTypeRegistryService
                .ContentTypes
                .Where(type => fileExtensionRegistryService.GetExtensionsForContentType(type).Any(e =>
                    e.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (contentTypes.Count == 0 &&
                bufferContentType != null)
            {
                // Fallback on TextBuffer content type
                contentTypes.Add(bufferContentType);
            }

            return contentTypes;
        }

        private static bool IsJavascriptDocument(string fileExtension, IEnumerable<IContentType> contentTypes) =>
            JavascriptSupportedExtensions.Contains(fileExtension) ||
            contentTypes.Any(type => type.IsOfType("JavaScript") ||
                                     type.IsOfType("Vue"));

        private static bool IsCFamilyDocument(IEnumerable<IContentType> contentTypes) =>
            contentTypes.Any(type => type.IsOfType("C/C++"));
    }
}
