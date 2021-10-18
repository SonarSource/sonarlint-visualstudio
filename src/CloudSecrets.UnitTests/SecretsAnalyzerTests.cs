﻿/*
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
using SonarLint.VisualStudio.Integration.UnitTests;

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
            MefTestHelpers.CheckTypeCanBeImported<SecretsAnalyzer, IAnalyzer>(null, new[]
            {
                MefTestHelpers.CreateExport<ITextDocumentFactoryService>(Mock.Of<ITextDocumentFactoryService>()),
                MefTestHelpers.CreateExport<IContentTypeRegistryService>(Mock.Of<IContentTypeRegistryService>()),
                MefTestHelpers.CreateExport<IAnalysisStatusNotifier>(Mock.Of<IAnalysisStatusNotifier>()),
                MefTestHelpers.CreateExport<ICloudSecretsTelemetryManager>(Mock.Of<ICloudSecretsTelemetryManager>()),
                MefTestHelpers.CreateExport<IUserSettingsProvider>(Mock.Of<IUserSettingsProvider>())
            });
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

            statusNotifier.Verify(x => x.AnalysisStarted(ValidFilePath), Times.Once);
            statusNotifier.Verify(x => x.AnalysisFailed(ValidFilePath, exception), Times.Once);
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

            statusNotifier.Verify(x => x.AnalysisStarted(ValidFilePath), Times.Once);
            statusNotifier.Verify(x => x.AnalysisFinished(ValidFilePath, secrets.Length, It.IsAny<TimeSpan>()), Times.Once);
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

        private void ExecuteAnalysis(SecretsAnalyzer testSubject, string filePath, IIssueConsumer consumer)
        {
            testSubject.ExecuteAnalysis(filePath, "", Array.Empty<AnalysisLanguage>(), consumer, null, CancellationToken.None);
        }

        private SecretsAnalyzer CreateTestSubject(IAnalysisStatusNotifier statusNotifier = null,
            ITextDocumentFactoryService textDocumentFactoryService = null,
            ISecretsToAnalysisIssueConverter secretsToAnalysisIssueConverter = null,
            RulesSettings rulesSettings = null,
            ICloudSecretsTelemetryManager telemetryManager = null,
            params Mock<ISecretDetector>[] detectors)
        {
            statusNotifier ??= Mock.Of<IAnalysisStatusNotifier>();
            detectors ??= Array.Empty<Mock<ISecretDetector>>();
            secretsToAnalysisIssueConverter ??= Mock.Of<ISecretsToAnalysisIssueConverter>();
            telemetryManager ??= Mock.Of<ICloudSecretsTelemetryManager>();

            rulesSettings ??= new RulesSettings();
            var userSettingsProvider = new Mock<IUserSettingsProvider>();
            userSettingsProvider.Setup(x => x.UserSettings).Returns(new UserSettings(rulesSettings));

            var contentTypeRegistryService = new Mock<IContentTypeRegistryService>();
            contentTypeRegistryService.Setup(x => x.UnknownContentType).Returns(Mock.Of<IContentType>());
            
            return new SecretsAnalyzer(textDocumentFactoryService,
                contentTypeRegistryService.Object,
                detectors.Select(x => x.Object),
                statusNotifier,
                userSettingsProvider.Object,
                telemetryManager,
                secretsToAnalysisIssueConverter);
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
    }
}
