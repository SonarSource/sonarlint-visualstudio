/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Infrastructure.VS.Editor;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Listener.Analysis;

namespace SonarLint.VisualStudio.Integration.Vsix.SLCore
{
    [Export(typeof(IRaiseIssueParamsToAnalysisIssueConverter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class RaiseIssueParamsToAnalysisIssueConverter : IRaiseIssueParamsToAnalysisIssueConverter
    {
        private readonly ITextDocumentFactoryService textDocumentFactoryService;
        private readonly IContentTypeRegistryService contentTypeRegistryService;
        private readonly ILineHashCalculator lineHashCalculator;

        [ImportingConstructor]
        public RaiseIssueParamsToAnalysisIssueConverter(ITextDocumentFactoryService textDocumentFactoryService, IContentTypeRegistryService contentTypeRegistryService)
            : this(textDocumentFactoryService, contentTypeRegistryService, new LineHashCalculator())
        { }

        internal RaiseIssueParamsToAnalysisIssueConverter(ITextDocumentFactoryService textDocumentFactoryService, IContentTypeRegistryService contentTypeRegistryService, ILineHashCalculator lineHashCalculator)
        {
            this.textDocumentFactoryService = textDocumentFactoryService;
            this.contentTypeRegistryService = contentTypeRegistryService;
            this.lineHashCalculator = lineHashCalculator;
        }

        public IEnumerable<IAnalysisIssue> GetAnalysisIssues(RaiseIssuesParams raiseIssuesParams)
        {
            var result = new List<IAnalysisIssue>();
            foreach (var issue in raiseIssuesParams.issuesByFileUri)
            {
                foreach (var item in issue.Value)
                {
                    result.Add(new AnalysisIssue
                        (item.ruleKey,
                        item.severity.ToAnalysisIssueSeverity(),
                        item.type.ToAnalysisIssueType(),
                        GetHighestSoftwareQualitySeverity(item.impacts),
                        GetAnalysisIssueLocation(issue.Key.LocalPath, item.primaryMessage, item.textRange),
                        item.flows?.Select(GetAnalysisIssueFlow).ToList(),
                        item.quickFixes?.Select(qf => GetQuickFix(issue.Key, qf)).Where(qf => qf is not null).ToList(),
                        item.ruleDescriptionContextKey
                        ));
                }
            }

            return result;
        }

        private SoftwareQualitySeverity? GetHighestSoftwareQualitySeverity(IEnumerable<ImpactDto> impacts)
        {
            if (impacts is not null && impacts.Any())
            {
                return impacts.Max(i => i.impactSeverity).ToSoftwareQualitySeverity();
            }
            return null;
        }

        private IAnalysisIssueLocation GetAnalysisIssueLocation(string filePath, string message, TextRangeDto textRangeDto)
        {
            var textDocument = textDocumentFactoryService.CreateAndLoadTextDocument(filePath, contentTypeRegistryService.UnknownContentType); // load the document from disc
            var currentSnapshot = textDocument.TextBuffer.CurrentSnapshot;

            var lineHash = lineHashCalculator.Calculate(currentSnapshot, textRangeDto.startLine + 1);

            var textRange = new TextRange(textRangeDto.startLine, textRangeDto.endLine, textRangeDto.startLineOffset, textRangeDto.endLineOffset, lineHash);

            return new AnalysisIssueLocation(message, filePath, textRange);
        }

        private IAnalysisIssueFlow GetAnalysisIssueFlow(IssueFlowDto issueFlowDto) =>
            new AnalysisIssueFlow(issueFlowDto.locations.Select(l => GetAnalysisIssueLocation(l.fileUri.LocalPath, l.message, l.textRange)).ToList());

        private IQuickFix GetQuickFix(Uri fileURi, QuickFixDto quickFixDto)
        {
            var fileEdit = quickFixDto.inputFileEdits.Find(e => e.target == fileURi);

            if (fileEdit is not null)
            {
                return new QuickFix(quickFixDto.message, fileEdit.textEdits.Select(GetEdit).ToList());
            }
            return null;
        }

        private IEdit GetEdit(TextEditDto textEdit) => new Edit(textEdit.newText, new TextRange(textEdit.range.startLine, textEdit.range.endLine, textEdit.range.startLineOffset, textEdit.range.endLineOffset, null));
    }
}
