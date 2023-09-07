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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using SonarLint.Secrets.DotNet;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.CloudSecrets
{
    [Export(typeof(IAnalyzer))]
    internal class SecretsAnalyzer : IAnalyzer
    {
        private readonly ITextDocumentFactoryService textDocumentFactoryService;
        private readonly IEnumerable<ISecretDetector> secretDetectors;
        private readonly IAnalysisStatusNotifierFactory analysisStatusNotifierFactory;
        private readonly Lazy<IRuleSettingsProvider> ruleSettingsProvider;
        private readonly ICloudSecretsTelemetryManager cloudSecretsTelemetryManager;
        private readonly ISecretsToAnalysisIssueConverter secretsToAnalysisIssueConverter;
        private readonly Lazy<IContentType> filesContentType;

        [ImportingConstructor]
        public SecretsAnalyzer(
            ITextDocumentFactoryService textDocumentFactoryService,
            IContentTypeRegistryService contentTypeRegistryService,
            [ImportMany] IEnumerable<ISecretDetector> secretDetectors,
            IAnalysisStatusNotifierFactory analysisStatusNotifierFactory,
            IRuleSettingsProviderFactory ruleSettingsProviderFactory,
            ICloudSecretsTelemetryManager telemetryManager)
            : this(textDocumentFactoryService,
                contentTypeRegistryService,
                secretDetectors,
                analysisStatusNotifierFactory,
                ruleSettingsProviderFactory,
                telemetryManager,
                new SecretsToAnalysisIssueConverter())
        {
        }

        internal SecretsAnalyzer(ITextDocumentFactoryService textDocumentFactoryService,
            IContentTypeRegistryService contentTypeRegistryService,
            IEnumerable<ISecretDetector> secretDetectors,
            IAnalysisStatusNotifierFactory analysisStatusNotifierFactory,
            IRuleSettingsProviderFactory ruleSettingsProviderFactory,
            ICloudSecretsTelemetryManager cloudSecretsTelemetryManager,
            ISecretsToAnalysisIssueConverter secretsToAnalysisIssueConverter)
        {
            this.textDocumentFactoryService = textDocumentFactoryService;
            this.secretDetectors = secretDetectors;
            this.analysisStatusNotifierFactory = analysisStatusNotifierFactory;
            this.cloudSecretsTelemetryManager = cloudSecretsTelemetryManager;
            this.secretsToAnalysisIssueConverter = secretsToAnalysisIssueConverter;

            filesContentType = new Lazy<IContentType>(() => contentTypeRegistryService.UnknownContentType);
            ruleSettingsProvider = new Lazy<IRuleSettingsProvider>(() => ruleSettingsProviderFactory.Get(Language.Secrets));
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
            var analysisStatusNotifier = analysisStatusNotifierFactory.Create(nameof(SecretsAnalyzer), filePath);

            analysisStatusNotifier.AnalysisStarted();

            try
            {
                var stopwatch = Stopwatch.StartNew();

                var textDocument = textDocumentFactoryService.CreateAndLoadTextDocument(filePath, filesContentType.Value); // load the document from disc
                var currentSnapshot = textDocument.TextBuffer.CurrentSnapshot;
                var fileContent = currentSnapshot.GetText();
                var rulesSettings = ruleSettingsProvider.Value.Get();

                var issues = new List<IAnalysisIssue>();

                foreach (var secretDetector in secretDetectors)
                {
                    if (!IsRuleActive(rulesSettings, secretDetector.RuleKey))
                    {
                        continue;
                    }

                    // todo: check cancellation token
                    var detectedSecrets = secretDetector.Find(fileContent);

                    if (detectedSecrets.Any())
                    {
                        cloudSecretsTelemetryManager.SecretDetected(secretDetector.RuleKey);
                    }

                    issues.AddRange(detectedSecrets.Select(x => secretsToAnalysisIssueConverter.Convert(x, secretDetector, filePath, currentSnapshot)));
                }

                analysisStatusNotifier.AnalysisFinished(issues.Count, stopwatch.Elapsed);

                if (issues.Any())
                {
                    consumer.Accept(filePath, issues);
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                analysisStatusNotifier.AnalysisFailed(ex);
            }
        }

        private static bool IsRuleActive(RulesSettings rulesSettings, string ruleKey)
        {
            if (rulesSettings.Rules.TryGetValue(ruleKey, out var ruleConfig))
            {
                return ruleConfig.Level == RuleLevel.On;
            }

            // Secrets are active by default
            return true;
        }
    }
}
