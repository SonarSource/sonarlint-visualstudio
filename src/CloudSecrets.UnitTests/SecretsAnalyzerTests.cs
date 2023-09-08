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
using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Moq;
using SonarLint.Secrets.DotNet;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.CloudSecrets.UnitTests
{
    [TestClass]
    public class SecretsAnalyzerTests
    {
        private const string ValidFilePath = "c:\\test";
        private const string ValidFileContent = "this is a content";

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SecretsAnalyzer, IAnalyzer>(
                MefTestHelpers.CreateExport<ITextDocumentFactoryService>(),
                MefTestHelpers.CreateExport<IContentTypeRegistryService>(),
                MefTestHelpers.CreateExport<IAnalysisStatusNotifierFactory>(),
                MefTestHelpers.CreateExport<ICloudSecretsTelemetryManager>(),
                MefTestHelpers.CreateExport<IRuleSettingsProviderFactory>());
        }

        [TestMethod]
        public void Ctor_DoesNotCallAnyNonFreeThreadedServices()
        {
            // Arrange
            var textDocumentFactoryService = new Mock<ITextDocumentFactoryService>();
            var contentTypeRegistryService = new Mock<IContentTypeRegistryService>();
            var analysisStatusNotifierFactory = new Mock<IAnalysisStatusNotifierFactory>();
            var cloudSecretsTelemetryManager = new Mock<ICloudSecretsTelemetryManager>();
            var ruleSettingsProviderFactory = new Mock<IRuleSettingsProviderFactory>();
            var secretDetectors = new[]
            {
                SetupSecretDetector("rule1", "rule1"),
            };

            // Act
            _ = new SecretsAnalyzer(textDocumentFactoryService.Object, contentTypeRegistryService.Object, secretDetectors.Select(x => x.Object), analysisStatusNotifierFactory.Object,
                ruleSettingsProviderFactory.Object, cloudSecretsTelemetryManager.Object);

            // The MEF constructor should be free-threaded, which it will be if
            // it doesn't make any external calls.
            textDocumentFactoryService.Invocations.Should().BeEmpty();
            contentTypeRegistryService.Invocations.Should().BeEmpty();
            analysisStatusNotifierFactory.Invocations.Should().BeEmpty();
            cloudSecretsTelemetryManager.Invocations.Should().BeEmpty();
            ruleSettingsProviderFactory.Invocations.Should().BeEmpty();
            secretDetectors[0].Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void IsAnalysisSupported_NoLanguages_True()
        {
            var testSubject = CreateTestSubject();

            var languages = Array.Empty<AnalysisLanguage>();
            var result = testSubject.IsAnalysisSupported(languages);

            result.Should().BeTrue();
        }

        [TestMethod]
        public void IsAnalysisSupported_AnyLanguage_True()
        {
            var testSubject = CreateTestSubject();

            var languages = new[] { (AnalysisLanguage)123456879 };
            var result = testSubject.IsAnalysisSupported(languages);

            result.Should().BeTrue();
        }

        [TestMethod]
        public void ExecuteAnalysis_AllDetectorsAreCalled()
        {
            var textDocumentFactoryService = SetupTextDocumentFactoryService(ValidFilePath, ValidFileContent);

            var consumer = new Mock<IIssueConsumer>();

            var secretDetectors = new[]
            {
                SetupSecretDetector(ValidFileContent),
                SetupSecretDetector(ValidFileContent),
                SetupSecretDetector(ValidFileContent)
            };

            var testSubject = CreateTestSubject(textDocumentFactoryService: textDocumentFactoryService, detectors: secretDetectors);
            ExecuteAnalysis(testSubject, ValidFilePath, consumer.Object);

            foreach (var detector in secretDetectors)
            {
                detector.Verify(x => x.Find(ValidFileContent), Times.Once());
            }
        }

        [TestMethod]
        public void ExecuteAnalysis_ResponseWithNoIssues_ConsumerNotCalled()
        {
            var textDocumentFactoryService = SetupTextDocumentFactoryService(ValidFilePath, ValidFileContent);

            var consumer = new Mock<IIssueConsumer>();

            var secretDetector = SetupSecretDetector(ValidFileContent);

            var testSubject = CreateTestSubject(textDocumentFactoryService: textDocumentFactoryService, detectors: secretDetector);
            ExecuteAnalysis(testSubject, ValidFilePath, consumer.Object );

            secretDetector.Verify(x => x.Find(ValidFileContent), Times.Once);
            consumer.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public void ExecuteAnalysis_ResponseWithIssues_ConsumerCalledWithConvertedIssues()
        {
            var textDocumentFactoryService = SetupTextDocumentFactoryService(ValidFilePath, ValidFileContent);

            var consumer = new Mock<IIssueConsumer>();

            var secrets = new[] { Mock.Of<ISecret>(), Mock.Of<ISecret>() };
            var secretDetector = SetupSecretDetector(ValidFileContent, secrets: secrets);

            var convertedIssues = new[] { Mock.Of<IAnalysisIssue>(), Mock.Of<IAnalysisIssue>() };
            var secretsToAnalysisIssueConverter = SetupIssuesConverter(new[]
            {
                (secrets[0], convertedIssues[0]),
                (secrets[1], convertedIssues[1])
            }, secretDetector.Object);

            var testSubject = CreateTestSubject(
                textDocumentFactoryService: textDocumentFactoryService, 
                secretsToAnalysisIssueConverter: secretsToAnalysisIssueConverter,
                detectors: secretDetector);

            ExecuteAnalysis(testSubject, ValidFilePath, consumer.Object);

            secretDetector.Verify(x => x.Find(ValidFileContent), Times.Once);
            consumer.Verify(x => x.Accept(ValidFilePath, convertedIssues), Times.Once);
            consumer.Invocations.Count.Should().Be(1);
        }

        [TestMethod]
        public void ExecuteAnalysis_CriticalException_ExceptionThrown()
        {
            var textDocumentFactoryService = new Mock<ITextDocumentFactoryService>();
            textDocumentFactoryService
                .Setup(x => x.CreateAndLoadTextDocument(ValidFilePath, It.IsAny<IContentType>()))
                .Throws<StackOverflowException>();

            var testSubject = CreateTestSubject(textDocumentFactoryService: textDocumentFactoryService.Object);

            Action act = () => ExecuteAnalysis(testSubject, ValidFilePath, null);
            act.Should().ThrowExactly<StackOverflowException>();
        }

        [TestMethod]
        public void ExecuteAnalysis_AnalysisFailed_NotifiesThatAnalysisFailed()
        {
            var statusNotifier = new Mock<IAnalysisStatusNotifier>();
            var exception = new NotImplementedException("this is a test");

            var textDocumentFactoryService = new Mock<ITextDocumentFactoryService>();
            textDocumentFactoryService
                .Setup(x => x.CreateAndLoadTextDocument(ValidFilePath, It.IsAny<IContentType>()))
                .Throws(exception);

            var consumer = new Mock<IIssueConsumer>();

            var testSubject = CreateTestSubject(textDocumentFactoryService: textDocumentFactoryService.Object, statusNotifier: statusNotifier.Object);
            ExecuteAnalysis(testSubject, ValidFilePath, consumer.Object);

            statusNotifier.Verify(x => x.AnalysisStarted(), Times.Once);
            statusNotifier.Verify(x => x.AnalysisFailed(exception), Times.Once);
            statusNotifier.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ExecuteAnalysis_AnalysisFinished_NotifiesThatAnalysisFinished()
        {
            var textDocumentFactoryService = SetupTextDocumentFactoryService(ValidFilePath, ValidFileContent);

            var statusNotifier = new Mock<IAnalysisStatusNotifier>();

            var consumer = new Mock<IIssueConsumer>();

            var secrets = new[] { Mock.Of<ISecret>(), Mock.Of<ISecret>() };
            var secretDetector = SetupSecretDetector(ValidFileContent, secrets: secrets);

            var testSubject = CreateTestSubject(textDocumentFactoryService: textDocumentFactoryService, statusNotifier: statusNotifier.Object, detectors: secretDetector);
            ExecuteAnalysis(testSubject, ValidFilePath, consumer.Object);

            statusNotifier.Verify(x => x.AnalysisStarted(), Times.Once);
            statusNotifier.Verify(x => x.AnalysisFinished(secrets.Length, It.IsAny<TimeSpan>()), Times.Once);
            statusNotifier.VerifyNoOtherCalls();
        }

       [TestMethod]
        public void ExecuteAnalysis_DisabledDetectorsAreNotChecked()
        {
            var textDocumentFactoryService = SetupTextDocumentFactoryService(ValidFilePath, ValidFileContent);

            var rulesSettings = new RulesSettings();
            rulesSettings.Rules.Add("rule1", new RuleConfig{Level = RuleLevel.On});
            rulesSettings.Rules.Add("rule2", new RuleConfig{Level = RuleLevel.Off });
            rulesSettings.Rules.Add("rule3", new RuleConfig{Level = RuleLevel.On });
            rulesSettings.Rules.Add("rule4", new RuleConfig{Level = RuleLevel.Off });

            var consumer = new Mock<IIssueConsumer>();

            var secretDetectors = new[]
            {
                SetupSecretDetector(ValidFileContent, "rule1"),
                SetupSecretDetector(ValidFileContent, "rule2"),
                SetupSecretDetector(ValidFileContent, "rule3"),
                SetupSecretDetector(ValidFileContent, "rule4"),
                SetupSecretDetector(ValidFileContent, "rule5")
            };

            var testSubject = CreateTestSubject(
                textDocumentFactoryService: textDocumentFactoryService,
                detectors: secretDetectors, 
                rulesSettings: rulesSettings);

            ExecuteAnalysis(testSubject, ValidFilePath, consumer.Object);

            secretDetectors[0].Verify(x => x.Find(ValidFileContent), Times.Once());
            secretDetectors[1].Verify(x => x.Find(It.IsAny<string>()), Times.Never());
            secretDetectors[2].Verify(x => x.Find(ValidFileContent), Times.Once());
            secretDetectors[3].Verify(x => x.Find(It.IsAny<string>()), Times.Never());
            // enabled by default
            secretDetectors[4].Verify(x => x.Find(ValidFileContent), Times.Once());
        }

        [TestMethod]
        public void ExecuteAnalysis_DetectorsThatRaisedIssuesAreReportedToTelemetry()
        {
            var textDocumentFactoryService = SetupTextDocumentFactoryService(ValidFilePath, ValidFileContent);

            var rulesSettings = new RulesSettings();
            rulesSettings.Rules.Add("rule1", new RuleConfig { Level = RuleLevel.On });
            rulesSettings.Rules.Add("rule2", new RuleConfig { Level = RuleLevel.On });
            rulesSettings.Rules.Add("rule3", new RuleConfig { Level = RuleLevel.On });

            var consumer = new Mock<IIssueConsumer>();

            var secretDetectors = new[]
            {
                SetupSecretDetector(ValidFileContent, "rule1", Mock.Of<ISecret>()),
                SetupSecretDetector(ValidFileContent, "rule2"),
                SetupSecretDetector(ValidFileContent, "rule3", Mock.Of<ISecret>(), Mock.Of<ISecret>())
            };

            var telemetryManager = new Mock<ICloudSecretsTelemetryManager>();

            var testSubject = CreateTestSubject(
                textDocumentFactoryService: textDocumentFactoryService,
                detectors: secretDetectors,
                rulesSettings: rulesSettings,
                telemetryManager: telemetryManager.Object);

            ExecuteAnalysis(testSubject, ValidFilePath, consumer.Object);

            telemetryManager.Verify(x=> x.SecretDetected("rule1"), Times.Once());
            telemetryManager.Verify(x => x.SecretDetected("rule2"), Times.Never);
            telemetryManager.Verify(x=> x.SecretDetected("rule3"), Times.Once());
            telemetryManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        [Description("Regression test for https://github.com/SonarSource/sonarlint-visualstudio/issues/3935")]
        public void ExecuteAnalysis_FetchRulesSettingsOnlyOnce()
        {
            var textDocumentFactoryService = SetupTextDocumentFactoryService(ValidFilePath, ValidFileContent);
            var ruleSettingsProvider = new Mock<IRuleSettingsProvider>();

            var consumer = new Mock<IIssueConsumer>();

            var secretDetectors = new[]
            {
                SetupSecretDetector(ValidFileContent, "rule1"),
                SetupSecretDetector(ValidFileContent, "rule2"),
                SetupSecretDetector(ValidFileContent, "rule3")
            };

            var testSubject = CreateTestSubject(
                textDocumentFactoryService: textDocumentFactoryService,
                detectors: secretDetectors,
                ruleSettingsProvider: ruleSettingsProvider);

            ExecuteAnalysis(testSubject, ValidFilePath, consumer.Object);

            ruleSettingsProvider.Verify(x=> x.Get(), Times.Once);
            ruleSettingsProvider.VerifyNoOtherCalls();
        }

        private void ExecuteAnalysis(SecretsAnalyzer testSubject, string filePath, IIssueConsumer consumer)
        {
            testSubject.ExecuteAnalysis(filePath, "", Array.Empty<AnalysisLanguage>(), consumer, null, CancellationToken.None);
        }

        private SecretsAnalyzer CreateTestSubject(IAnalysisStatusNotifier statusNotifier = null,
            ITextDocumentFactoryService textDocumentFactoryService = null,
            ISecretsToAnalysisIssueConverter secretsToAnalysisIssueConverter = null,
            RulesSettings rulesSettings = null,
            Mock<IRuleSettingsProvider> ruleSettingsProvider = null,
            ICloudSecretsTelemetryManager telemetryManager = null,
            params Mock<ISecretDetector>[] detectors)
        {
            statusNotifier ??= Mock.Of<IAnalysisStatusNotifier>();

            detectors ??= Array.Empty<Mock<ISecretDetector>>();
            secretsToAnalysisIssueConverter ??= Mock.Of<ISecretsToAnalysisIssueConverter>();
            telemetryManager ??= Mock.Of<ICloudSecretsTelemetryManager>();

            var ruleSettingsProviderFactory = SetupRuleProviderFactory(rulesSettings, ruleSettingsProvider);

            var contentTypeRegistryService = new Mock<IContentTypeRegistryService>();
            contentTypeRegistryService.Setup(x => x.UnknownContentType).Returns(Mock.Of<IContentType>());
            
            return new SecretsAnalyzer(textDocumentFactoryService,
                contentTypeRegistryService.Object,
                detectors.Select(x => x.Object),
                SetupStatusNotifierFactory(statusNotifier),
                ruleSettingsProviderFactory,
                telemetryManager,
                secretsToAnalysisIssueConverter);
        }

        private IRuleSettingsProviderFactory SetupRuleProviderFactory(RulesSettings rulesSettings = null,
            Mock<IRuleSettingsProvider> ruleSettingsProvider = null)
        {
            rulesSettings ??= new RulesSettings();

            ruleSettingsProvider ??= new Mock<IRuleSettingsProvider>();
            ruleSettingsProvider.Setup(x => x.Get()).Returns(rulesSettings);

            var ruleSettingsProviderFactory = new Mock<IRuleSettingsProviderFactory>();
            ruleSettingsProviderFactory.Setup(x => x.Get(Language.Secrets)).Returns(ruleSettingsProvider.Object);

            return ruleSettingsProviderFactory.Object;
        }

        private Mock<ISecretDetector> SetupSecretDetector(string input, string ruleKey = "some rule", params ISecret[] secrets)
        {
            var detector = new Mock<ISecretDetector>();
            detector.Setup(x => x.Find(input)).Returns(secrets);
            detector.Setup(x => x.RuleKey).Returns(ruleKey);

            return detector;
        }

        private ITextDocumentFactoryService SetupTextDocumentFactoryService(string filePath, string content)
        {
            var snapshot = new Mock<ITextSnapshot>();
            snapshot.Setup(x => x.GetText()).Returns(content);

            var textDocument = new Mock<ITextDocument>();
            textDocument.Setup(x => x.TextBuffer.CurrentSnapshot).Returns(snapshot.Object);

            var textDocumentFactoryService = new Mock<ITextDocumentFactoryService>();
            textDocumentFactoryService
                .Setup(x => x.CreateAndLoadTextDocument(filePath, It.IsAny<IContentType>()))
                .Returns(textDocument.Object);

            return textDocumentFactoryService.Object;
        }

        private ISecretsToAnalysisIssueConverter SetupIssuesConverter(IEnumerable<(ISecret, IAnalysisIssue)> convertedIssues, ISecretDetector secretDetector)
        {
            var converter = new Mock<ISecretsToAnalysisIssueConverter>();

            foreach (var convertedIssue in convertedIssues)
            {
                converter
                    .Setup(x => x.Convert(convertedIssue.Item1, secretDetector, ValidFilePath, It.IsAny<ITextSnapshot>()))
                    .Returns(convertedIssue.Item2);
            }

            return converter.Object;
        }

        private IAnalysisStatusNotifierFactory SetupStatusNotifierFactory(IAnalysisStatusNotifier statusNotifier)
        {
            var statusNotifierFactory = new Mock<IAnalysisStatusNotifierFactory>();
            statusNotifierFactory.Setup(x => x.Create(nameof(SecretsAnalyzer), ValidFilePath)).Returns(statusNotifier);

            return statusNotifierFactory.Object;
        }
    }
}

