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

using System.IO;
using Microsoft.VisualStudio.Text;
using SonarLint.Secrets.DotNet;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Infrastructure.VS.Editor;

namespace SonarLint.VisualStudio.CloudSecrets
{
    internal interface ISecretsToAnalysisIssueConverter
    {
        IAnalysisIssue Convert(ISecret secret, ISecretDetector secretDetector, string filePath, ITextSnapshot textSnapshot);
    }

    internal class SecretsToAnalysisIssueConverter : ISecretsToAnalysisIssueConverter
    {
        private readonly ILineHashCalculator lineHashCalculator;

        public SecretsToAnalysisIssueConverter() : this(new LineHashCalculator()) { }

        internal SecretsToAnalysisIssueConverter(ILineHashCalculator lineHashCalculator)
        {
            this.lineHashCalculator = lineHashCalculator;
        }

        public IAnalysisIssue Convert(ISecret secret, ISecretDetector secretDetector, string filePath, ITextSnapshot textSnapshot)
        {
            var snapshotSpan = new SnapshotSpan(textSnapshot, secret.StartIndex, secret.Length);

            var vsStartLine = snapshotSpan.Start.GetContainingLine();
            var startLine = vsStartLine.LineNumber + 1;
            var startLineOffset = secret.StartIndex - vsStartLine.Start.Position;

            var vsEndLine = snapshotSpan.End.GetContainingLine();
            var endLine = vsEndLine.LineNumber + 1;
            var endLineOffset = secret.StartIndex + secret.Length - vsEndLine.Start.Position;

            return new AnalysisIssue(ruleKey: secretDetector.RuleKey,
                severity: AnalysisIssueSeverity.Major,
                type: AnalysisIssueType.Vulnerability,
                highestSoftwareQualitySeverity: null,
                primaryLocation: new AnalysisIssueLocation(
                    message: secretDetector.Message,
                    filePath: Path.IsPathRooted(filePath) ? Path.GetFullPath(filePath) : filePath,
                    textRange: new TextRange(
                        startLine: startLine,
                        endLine: endLine,
                        startLineOffset: startLineOffset,
                        endLineOffset: endLineOffset,
                        lineHash: lineHashCalculator.Calculate(textSnapshot, startLine))),
                flows: null);
        }
    }
}
