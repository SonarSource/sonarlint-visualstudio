/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.ETW;
using SonarLint.VisualStudio.IssueVisualization.Editor;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    internal interface ICFamilyIssueToAnalysisIssueConverter
    {
        IAnalysisIssue Convert(Message cFamilyIssue, string sqLanguage, ICFamilyRulesConfig rulesConfiguration);
    }

    [Export(typeof(ICFamilyIssueToAnalysisIssueConverter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class CFamilyIssueToAnalysisIssueConverter : ICFamilyIssueToAnalysisIssueConverter
    {
        private readonly ITextDocumentFactoryService textDocumentFactoryService;
        private readonly ILineHashCalculator lineHashCalculator;
        private readonly IFileSystem fileSystem;
        private readonly IContentType filesContentType;

        [ImportingConstructor]
        public CFamilyIssueToAnalysisIssueConverter(ITextDocumentFactoryService textDocumentFactoryService, IContentTypeRegistryService contentTypeRegistryService)
            : this(textDocumentFactoryService, contentTypeRegistryService, new LineHashCalculator(), new FileSystem())
        {
        }

        internal CFamilyIssueToAnalysisIssueConverter(ITextDocumentFactoryService textDocumentFactoryService,
            IContentTypeRegistryService contentTypeRegistryService,
            ILineHashCalculator lineHashCalculator,
            IFileSystem fileSystem)
        {
            this.textDocumentFactoryService = textDocumentFactoryService;
            this.lineHashCalculator = lineHashCalculator;
            this.fileSystem = fileSystem;
            filesContentType = contentTypeRegistryService.UnknownContentType;
        }

        public IAnalysisIssue Convert(Message cFamilyIssue, string sqLanguage, ICFamilyRulesConfig rulesConfiguration)
        {
            CodeMarkers.Instance.CFamilyConvertIssueStart(cFamilyIssue.Filename);

            // Lines and character positions are 1-based
            Debug.Assert(cFamilyIssue.Line > 0);

            // BUT special case of EndLine=0, Column=0, EndColumn=0 meaning "select the whole line"
            Debug.Assert(cFamilyIssue.EndLine >= 0);
            Debug.Assert(cFamilyIssue.Column > 0 || cFamilyIssue.Column == 0);
            Debug.Assert(cFamilyIssue.EndColumn > 0 || cFamilyIssue.EndLine == 0);

            // Look up default severity and type
            var defaultSeverity = rulesConfiguration.RulesMetadata[cFamilyIssue.RuleKey].DefaultSeverity;
            var defaultType = rulesConfiguration.RulesMetadata[cFamilyIssue.RuleKey].Type;

            var fileContents = LoadFileContentsOfReportedFiles(cFamilyIssue); 

            var locations = cFamilyIssue.Parts
                .Select(x=> ToAnalysisIssueLocation(x, fileContents))
                .ToArray();

            // If PartsMakeFlow is set to true the issues are expected to be in the reversed order
            if (cFamilyIssue.PartsMakeFlow)
            {
                Array.Reverse(locations);
            }

            var flows = locations.Any() ? new[] { new AnalysisIssueFlow(locations) } : null;

            var result = ToAnalysisIssue(cFamilyIssue, sqLanguage, defaultSeverity, defaultType, flows, fileContents);

            CodeMarkers.Instance.CFamilyConvertIssueStop();

            return result;
        }

        private IDictionary<string, ITextDocument> LoadFileContentsOfReportedFiles(Message cFamilyIssue)
        {
            var filePaths = cFamilyIssue.Parts
                .Select(x => x.Filename)
                .Union(new[] {cFamilyIssue.Filename})
                .Distinct();

            return filePaths.ToDictionary(x => x,
                path => GetTextDocument(path));
        }

        private ITextDocument GetTextDocument(string filePath)
        {
            if (fileSystem.File.Exists(filePath))
            {
                // The document is being loaded from disc, so it should match the version that was analyzed by the subprocess
                var doc = textDocumentFactoryService.CreateAndLoadTextDocument(filePath, filesContentType);
                CodeMarkers.Instance.CFamilyConvertIssueFileLoaded(filePath);
                return doc;
            };

            return null;
        }

        private IAnalysisIssue ToAnalysisIssue(Message cFamilyIssue, 
            string sqLanguage,
            IssueSeverity defaultSeverity,
            IssueType defaultType,
            IReadOnlyList<IAnalysisIssueFlow> flows,
            IDictionary<string, ITextDocument> fileContents)
        {
            return new AnalysisIssue
            (
                ruleKey: sqLanguage + ":" + cFamilyIssue.RuleKey,
                severity: Convert(defaultSeverity),
                type: Convert(defaultType),

                filePath: Path.IsPathRooted(cFamilyIssue.Filename) ? Path.GetFullPath(cFamilyIssue.Filename) : cFamilyIssue.Filename,
                message: cFamilyIssue.Text,
                lineHash: CalculateLineHash(cFamilyIssue, fileContents),
                startLine: cFamilyIssue.Line,
                endLine: cFamilyIssue.EndLine,

                // We don't care about the columns in the special case EndLine=0
                startLineOffset: cFamilyIssue.EndLine == 0 ? 0 : cFamilyIssue.Column - 1,
                endLineOffset: cFamilyIssue.EndLine == 0 ? 0 : cFamilyIssue.EndColumn - 1,

                flows: flows
            );
        }

        private string CalculateLineHash(MessagePart cFamilyIssueLocation, IDictionary<string, ITextDocument> fileContents)
        {
            var isFileLevelLocation = cFamilyIssueLocation.Line == 1 &&
                                      cFamilyIssueLocation.Column <= 1 &&
                                      cFamilyIssueLocation.EndColumn == 0 &&
                                      cFamilyIssueLocation.EndLine == 0;

            if (isFileLevelLocation)
            {
                return null;
            }

            var textSnapshot = fileContents[cFamilyIssueLocation.Filename]?.TextBuffer?.CurrentSnapshot;

            return textSnapshot == null ? null : lineHashCalculator.Calculate(textSnapshot, cFamilyIssueLocation.Line);
        }

        private AnalysisIssueLocation ToAnalysisIssueLocation(MessagePart cFamilyIssueLocation, IDictionary<string, ITextDocument> fileContents)
        {
            return new AnalysisIssueLocation
            (
                filePath: Path.GetFullPath(cFamilyIssueLocation.Filename),
                message: cFamilyIssueLocation.Text,
                lineHash: CalculateLineHash(cFamilyIssueLocation, fileContents),
                startLine: cFamilyIssueLocation.Line,
                endLine: cFamilyIssueLocation.EndLine,

                // We don't care about the columns in the special case EndLine=0
                startLineOffset: cFamilyIssueLocation.EndLine == 0 ? 0 : cFamilyIssueLocation.Column - 1,
                endLineOffset: cFamilyIssueLocation.EndLine == 0 ? 0 : cFamilyIssueLocation.EndColumn - 1
            );
        }

        /// <summary>
        /// Converts from the CFamily issue severity enum to the standard AnalysisIssueSeverity
        /// </summary>
        internal /* for testing */ static AnalysisIssueSeverity Convert(IssueSeverity issueSeverity)
        {
            switch (issueSeverity)
            {
                case IssueSeverity.Blocker:
                    return AnalysisIssueSeverity.Blocker;
                case IssueSeverity.Critical:
                    return AnalysisIssueSeverity.Critical;
                case IssueSeverity.Info:
                    return AnalysisIssueSeverity.Info;
                case IssueSeverity.Major:
                    return AnalysisIssueSeverity.Major;
                case IssueSeverity.Minor:
                    return AnalysisIssueSeverity.Minor;

                default:
                    throw new ArgumentOutOfRangeException(nameof(issueSeverity));
            }
        }

        /// <summary>
        /// Converts from the CFamily issue type enum to the standard AnalysisIssueType
        /// </summary>
        internal /* for testing */static AnalysisIssueType Convert(IssueType issueType)
        {
            switch (issueType)
            {
                case IssueType.Bug:
                    return AnalysisIssueType.Bug;
                case IssueType.CodeSmell:
                    return AnalysisIssueType.CodeSmell;
                case IssueType.Vulnerability:
                    return AnalysisIssueType.Vulnerability;

                default:
                    throw new ArgumentOutOfRangeException(nameof(issueType));
            }
        }
    }
}
