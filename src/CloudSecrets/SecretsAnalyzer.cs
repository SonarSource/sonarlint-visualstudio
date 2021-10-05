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
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using SonarLint.Secrets.DotNet;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.CloudSecrets
{
    [Export(typeof(IAnalyzer))]
    internal class SecretsAnalyzer : IAnalyzer
    {
        private readonly IEnumerable<ISecretDetector> secretDetectors;
        private readonly IAnalysisStatusNotifier analysisStatusNotifier;
        private readonly IFileSystem fileSystem;

        [ImportingConstructor]
        public SecretsAnalyzer([ImportMany] IEnumerable<ISecretDetector> secretDetectors, IAnalysisStatusNotifier analysisStatusNotifier)
            : this(secretDetectors, analysisStatusNotifier, new FileSystem())
        {
        }

        internal SecretsAnalyzer(IEnumerable<ISecretDetector> secretDetectors,
            IAnalysisStatusNotifier analysisStatusNotifier,
            IFileSystem fileSystem)
        {
            this.secretDetectors = secretDetectors;
            this.analysisStatusNotifier = analysisStatusNotifier;
            this.fileSystem = fileSystem;
        }

        public bool IsAnalysisSupported(IEnumerable<AnalysisLanguage> languages)
        {
            return true;
        }

        public void ExecuteAnalysis(string filePath,
            string charset,
            IEnumerable<AnalysisLanguage> detectedLanguages,
            IIssueConsumer consumer,
            IAnalyzerOptions analyzerOptions,
            CancellationToken cancellationToken)
        {
            analysisStatusNotifier.AnalysisStarted(filePath);

            try
            {
                var stopwatch = Stopwatch.StartNew();

                var fileContent = fileSystem.File.ReadAllText(filePath);

                // todo: pass rules configuration if should run the detector? or should each detector expose metadata?
                // todo: pass cancellation token that should be checked before running each detector? or should check the cancellation here?
                var detectedSecrets = secretDetectors.SelectMany(x => x.Find(fileContent));
                var issues = detectedSecrets.Select(x => CreateIssue(x, filePath)).ToArray();

                analysisStatusNotifier.AnalysisFinished(filePath, issues.Length, stopwatch.Elapsed);

                if (issues.Any())
                {
                    consumer.Accept(filePath, issues);
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                analysisStatusNotifier.AnalysisFailed(filePath, ex);
            }
        }

        private IAnalysisIssue CreateIssue(ISecret secret, string filePath)
        {
            var ruleKey = secret.RuleKey;
            var severity = AnalysisIssueSeverity.Major;
            var message = "this is a secret";

            var startLine = 0;
            var endLine = 0;
            var startLineOffset = 0;
            var endLineOffset = 0;
            string lineHash = null;

            return new AnalysisIssue(ruleKey,
                severity,
                AnalysisIssueType.Vulnerability,
                message,
                filePath,
                startLine,
                endLine,
                startLineOffset,
                endLineOffset,
                lineHash,
                null);
        }
    }
}
