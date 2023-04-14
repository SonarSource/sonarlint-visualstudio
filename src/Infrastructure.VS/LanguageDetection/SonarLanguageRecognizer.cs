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

        AnalysisLanguage? GetAnalysisLanguageFromExtension(string fileName);
    }

    [Export(typeof(ISonarLanguageRecognizer))]
    internal class SonarLanguageRecognizer : ISonarLanguageRecognizer
    {
        private static readonly ISet<string> JavascriptSupportedExtensions = new HashSet<string> { "js", "jsx", "vue" };

        private const string CFamilyTypeName = "C/C++";
        private const string TypeScriptTypeName = "TypeScript";
        private const string CSharpTypeName = "CSharp";
        private const string BasicTypeName = "Basic";
        private const string CSSTypeName = "css";
        private const string SCSSTypeName = "SCSS";
        private const string LESSTypeName = "LESS";

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
            var fileExtension = Path.GetExtension(filePath).TrimStart('.');
            var contentTypes = GetExtensionContentTypes(fileExtension, bufferContentType);

            // Languages are for now mainly exclusive but it should possible for the same file to be analyzed by multiple
            // plugins (language plugin).
            var detectedLanguages = new List<AnalysisLanguage>();

            if (IsJavascriptDocument(fileExtension, contentTypes))
            {
                detectedLanguages.Add(AnalysisLanguage.Javascript);
            }
            else if (IsTypeScriptDocument(contentTypes))
            {
                detectedLanguages.Add(AnalysisLanguage.TypeScript);
            }

            if (IsCFamilyDocument(contentTypes))
            {
                detectedLanguages.Add(AnalysisLanguage.CFamily);
            }

            if (IsRoslynFamilyDocument(contentTypes))
            {
                detectedLanguages.Add(AnalysisLanguage.RoslynFamily);
            }

            if (IsCssDocument(contentTypes))
            {
                detectedLanguages.Add(AnalysisLanguage.CascadingStyleSheets);
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

        private static bool IsRoslynFamilyDocument(IEnumerable<IContentType> contentTypes) =>
            contentTypes.Any(type => type.IsOfType("Roslyn Languages"));

        private static bool IsTypeScriptDocument(IEnumerable<IContentType> contentTypes)
        {
            return contentTypes.Any(type => type.IsOfType("TypeScript"));
        }

        private static bool IsCssDocument(IEnumerable<IContentType> contentTypes) =>
            contentTypes.Any(type => type.IsOfType(CSSTypeName) || type.IsOfType(SCSSTypeName) || type.IsOfType(LESSTypeName));

        public AnalysisLanguage? GetAnalysisLanguageFromExtension(string fileName)
        {
            if(IsFileNameInvalid(fileName))
            {
                return null;
            }

            var extension = GetNormalizedExtention(fileName);
            
            // ContentType for "js" is typescript we do manual check to be consistent with Detect method
            if (JavascriptSupportedExtensions.Contains(extension)) { return AnalysisLanguage.Javascript; }

            var contentTypeName = fileExtensionRegistryService.GetContentTypeForExtension(extension).TypeName;
            switch (contentTypeName)
            {
                case TypeScriptTypeName:
                    return AnalysisLanguage.TypeScript;
                case CFamilyTypeName:
                    return AnalysisLanguage.CFamily;
                case CSharpTypeName:
                case BasicTypeName:
                    return AnalysisLanguage.RoslynFamily;
                case CSSTypeName:
                case SCSSTypeName:
                case LESSTypeName:
                    return AnalysisLanguage.CascadingStyleSheets;
                default:
                    return null;                   
            }
        }

        private string GetNormalizedExtention(string fileName)
        {
            var extension = Path.GetExtension(fileName);

            if (extension.Length > 0 && extension[0] == '.')
            {
                //remove the leading . on extension
                extension = extension.Substring(1);
            }
            return extension.ToLowerInvariant();
        }
        
        private bool IsFileNameInvalid(string fileName) 
            => Path.GetInvalidFileNameChars().Any(x => fileName.Contains(x));
    }  
}
