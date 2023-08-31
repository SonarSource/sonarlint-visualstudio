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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.CFamily.Analysis;
using SonarLint.VisualStudio.CFamily.Rules;
using SonarLint.VisualStudio.CFamily.SubProcess;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Configuration;
using SonarLint.VisualStudio.Core.ETW;
using SonarLint.VisualStudio.Infrastructure.VS.Editor;

/* Instancing: a new issue converter should be created for each analysis run.
 *
 * Overview
 * --------
 * Each analysis is for a single file, although the issues returned could be for related
 * files (e.g. a header file). The results will generally be a small set of files - the
 * analysis file + [zero or more related files].
 *
 * Converting an issue entails loading the text document for the file. We don't want to load
 * the same document multiple times, so we'll create a separate converter for each analysis,
 * so the converter can cache the loaded text documents.
 */

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    [Export(typeof(ICFamilyIssueConverterFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class CFamilyIssueConverterFactory : ICFamilyIssueConverterFactory
    {
        private readonly ITextDocumentFactoryService textDocumentFactoryService;
        private readonly IContentTypeRegistryService contentTypeRegistryService;
        private readonly IConnectedModeFeaturesConfiguration connectedModeFeaturesConfiguration;

        [ImportingConstructor]
        public CFamilyIssueConverterFactory(ITextDocumentFactoryService textDocumentFactoryService, IContentTypeRegistryService contentTypeRegistryService, IConnectedModeFeaturesConfiguration connectedModeFeaturesConfiguration)
        {
            this.textDocumentFactoryService = textDocumentFactoryService;
            this.contentTypeRegistryService = contentTypeRegistryService;
            this.connectedModeFeaturesConfiguration = connectedModeFeaturesConfiguration;
        }

        public ICFamilyIssueToAnalysisIssueConverter Create()
        {
            return new CFamilyIssueToAnalysisIssueConverter(textDocumentFactoryService, contentTypeRegistryService, connectedModeFeaturesConfiguration);
        }
    }

    // Short-lived class - one instance per analysis
    internal class CFamilyIssueToAnalysisIssueConverter : ICFamilyIssueToAnalysisIssueConverter
    {
        private readonly ITextDocumentFactoryService textDocumentFactoryService;
        private readonly ILineHashCalculator lineHashCalculator;
        private readonly IFileSystem fileSystem;
        private readonly IContentType filesContentType;
        private readonly Dictionary<string, ITextDocument> pathToTextDocMap;
        private readonly IConnectedModeFeaturesConfiguration connectedModeFeaturesConfiguration;

        public CFamilyIssueToAnalysisIssueConverter(ITextDocumentFactoryService textDocumentFactoryService, IContentTypeRegistryService contentTypeRegistryService, IConnectedModeFeaturesConfiguration connectedModeFeaturesConfiguration)
            : this(textDocumentFactoryService, contentTypeRegistryService, connectedModeFeaturesConfiguration, new LineHashCalculator(), new FileSystem())
        {
        }

        internal CFamilyIssueToAnalysisIssueConverter(ITextDocumentFactoryService textDocumentFactoryService,
            IContentTypeRegistryService contentTypeRegistryService,
            IConnectedModeFeaturesConfiguration connectedModeFeaturesConfiguration,
            ILineHashCalculator lineHashCalculator,
            IFileSystem fileSystem)
        {
            this.textDocumentFactoryService = textDocumentFactoryService;
            this.lineHashCalculator = lineHashCalculator;
            this.fileSystem = fileSystem;
            this.connectedModeFeaturesConfiguration = connectedModeFeaturesConfiguration;

            filesContentType = contentTypeRegistryService.UnknownContentType;

            pathToTextDocMap = new Dictionary<string, ITextDocument>(StringComparer.OrdinalIgnoreCase);
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

            var ruleMetaData = rulesConfiguration.RulesMetadata[cFamilyIssue.RuleKey];

            // Look up default severity and type
            var defaultSeverity = ruleMetaData.DefaultSeverity;
            var defaultType = ruleMetaData.Type;
            SoftwareQualitySeverity? highestSoftwareQualitySeverity = null;

            if (ruleMetaData.Type != IssueType.SecurityHotspot && connectedModeFeaturesConfiguration.IsNewCctAvailable())
            {
                highestSoftwareQualitySeverity = GetHighestSoftwareQualitySeverity(ruleMetaData);
            }

            var fileContents = GetFileContentsOfReportedFiles(cFamilyIssue);

            var locations = cFamilyIssue.Parts
                .Select(x => ToAnalysisIssueLocation(x, fileContents))
                .ToArray();

            // If PartsMakeFlow is set to true the issues are expected to be in the reversed order
            if (cFamilyIssue.PartsMakeFlow)
            {
                Array.Reverse(locations);
            }

            var flows = locations.Any() ? new[] { new AnalysisIssueFlow(locations) } : null;

            var result = ToAnalysisIssue(cFamilyIssue, sqLanguage, defaultSeverity, defaultType, flows, fileContents, highestSoftwareQualitySeverity);

            CodeMarkers.Instance.CFamilyConvertIssueStop();

            return result;
        }

        private IReadOnlyDictionary<string, ITextDocument> GetFileContentsOfReportedFiles(Message cFamilyIssue)
        {
            var filePaths = cFamilyIssue.Parts
                .Select(x => x.Filename)
                .Union(new[] { cFamilyIssue.Filename })
                .Distinct();

            foreach (var filePath in filePaths)
            {
                if (pathToTextDocMap.ContainsKey(filePath))
                {
                    CodeMarkers.Instance.CFamilyConvertIssueFileAlreadyLoaded(filePath);
                }
                else
                {
                    var doc = GetTextDocument(filePath);
                    pathToTextDocMap.Add(filePath, doc);

                    CodeMarkers.Instance.CFamilyConvertIssueFileLoaded(filePath);
                }
            }

            return pathToTextDocMap;
        }

        private ITextDocument GetTextDocument(string filePath)
        {
            if (fileSystem.File.Exists(filePath))
            {
                // The document is being loaded from disc, so it should match the version that was analyzed by the subprocess
                var doc = textDocumentFactoryService.CreateAndLoadTextDocument(filePath, filesContentType);
                return doc;
            }

            return null;
        }

        private IAnalysisIssue ToAnalysisIssue(Message cFamilyIssue,
            string sqLanguage,
            IssueSeverity defaultSeverity,
            IssueType defaultType,
            IReadOnlyList<IAnalysisIssueFlow> flows,
            IReadOnlyDictionary<string, ITextDocument> fileContents,
            SoftwareQualitySeverity? highestSoftwareQualitySeverity)
        {
            return new AnalysisIssue
            (
                ruleKey: sqLanguage + ":" + cFamilyIssue.RuleKey,
                severity: Convert(defaultSeverity),
                type: Convert(defaultType),
                highestSoftwareQualitySeverity,
                primaryLocation: ToAnalysisIssueLocation(cFamilyIssue, fileContents),
                flows: flows,
                fixes: ToQuickFixes(cFamilyIssue)
            );
        }

        private static List<QuickFix> ToQuickFixes(Message cFamilyIssue)
        {
            return cFamilyIssue.Fixes.Select(f =>
                    new QuickFix(
                        message: f.Message,
                        f.Edits?.Select(e =>
                                new Core.Analysis.Edit(
                                    textRange: new TextRange(
                                        startLine: e.StartLine,
                                        startLineOffset: e.StartColumn - 1,
                                        endLine: e.EndLine,
                                        endLineOffset: e.EndColumn - 1,
                                        lineHash: null),
                                    text: e.Text))
                            .ToList()))
                .ToList();
        }

        private string CalculateLineHash(MessagePart cFamilyIssueLocation, IReadOnlyDictionary<string, ITextDocument> fileContents)
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

        private AnalysisIssueLocation ToAnalysisIssueLocation(MessagePart cFamilyIssueLocation,
            IReadOnlyDictionary<string, ITextDocument> fileContents) =>
            new AnalysisIssueLocation
            (
                filePath: Path.IsPathRooted(cFamilyIssueLocation.Filename)
                    ? Path.GetFullPath(cFamilyIssueLocation.Filename)
                    : cFamilyIssueLocation.Filename,
                message: cFamilyIssueLocation.Text,
                textRange: new TextRange(
                    lineHash: CalculateLineHash(cFamilyIssueLocation, fileContents),
                    startLine: cFamilyIssueLocation.Line,
                    endLine: cFamilyIssueLocation.EndLine,

                    // We don't care about the columns in the special case EndLine=0
                    startLineOffset: cFamilyIssueLocation.EndLine == 0 ? 0 : cFamilyIssueLocation.Column - 1,
                    endLineOffset: cFamilyIssueLocation.EndLine == 0 ? 0 : cFamilyIssueLocation.EndColumn - 1
                ));

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

                case IssueType.SecurityHotspot:
                    return AnalysisIssueType.SecurityHotspot;

                default:
                    throw new ArgumentOutOfRangeException(nameof(issueType));
            }
        }

        internal /* for testing */ static SoftwareQualitySeverity? GetHighestSoftwareQualitySeverity(RuleMetadata ruleMetadata)
            => ruleMetadata.Code?.Impacts?.Count > 0 ? (SoftwareQualitySeverity?)ruleMetadata.Code.Impacts.Max(r => r.Value) : null;
    }
}
