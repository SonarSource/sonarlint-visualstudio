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
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.Secrets.DotNet;
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
                MefTestHelpers.CreateExport<IFileSystem>(Mock.Of<IFileSystem>()),
                MefTestHelpers.CreateExport<IAnalysisStatusNotifier>(Mock.Of<IAnalysisStatusNotifier>()),
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
            var fileSystem = SetupFileSystem(ValidFilePath, ValidFileContent);

            var consumer = new Mock<IIssueConsumer>();

            var secretDetectors = new[]
            {
                SetupSecretDetector(ValidFileContent),
                SetupSecretDetector(ValidFileContent),
                SetupSecretDetector(ValidFileContent)
            };

            ExecuteAnalysis(ValidFilePath, consumer.Object, fileSystem, secretDetectors: secretDetectors);

            foreach (var detector in secretDetectors)
            {
                detector.Verify(x=> x.Find(ValidFileContent), Times.Once());
            }
        }

        [TestMethod]
        public void ExecuteAnalysis_ResponseWithNoIssues_ConsumerNotCalled()
        {
            var fileSystem = SetupFileSystem(ValidFilePath, ValidFileContent);

            var consumer = new Mock<IIssueConsumer>();

            var secretDetector = SetupSecretDetector(ValidFileContent);

            ExecuteAnalysis(ValidFilePath, consumer.Object, fileSystem, secretDetectors: secretDetector);

            secretDetector.Verify(x=> x.Find(ValidFileContent), Times.Once);
            consumer.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public void ExecuteAnalysis_ResponseWithIssues_ConsumerCalled()
        {
            var fileSystem = SetupFileSystem(ValidFilePath, ValidFileContent);

            var consumer = new Mock<IIssueConsumer>();

            var secretDetector = SetupSecretDetector(ValidFileContent, Mock.Of<ISecret>());

            ExecuteAnalysis(ValidFilePath, consumer.Object, fileSystem, secretDetectors: secretDetector);

            secretDetector.Verify(x => x.Find(ValidFileContent), Times.Once);
            consumer.Verify(x => x.Accept(ValidFilePath, It.IsAny<IEnumerable<IAnalysisIssue>>()));
        }

        [TestMethod]
        public void ExecuteAnalysis_CriticalException_ExceptionThrown()
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.ReadAllText(ValidFilePath)).Throws<StackOverflowException>();

            var secretDetector = SetupSecretDetector(ValidFileContent, Mock.Of<ISecret>());

            Action act = () => ExecuteAnalysis(ValidFilePath, null, fileSystem.Object, secretDetectors: secretDetector);
            act.Should().ThrowExactly<StackOverflowException>();
        }

        [TestMethod]
        public void ExecuteAnalysis_AnalysisFailed_NotifiesThatAnalysisFailed()
        {
            var statusNotifier = new Mock<IAnalysisStatusNotifier>();
            var exception = new NotImplementedException("this is a test");

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.ReadAllText(ValidFilePath)).Throws(exception);

            var consumer = new Mock<IIssueConsumer>();

            ExecuteAnalysis(ValidFilePath, consumer.Object, fileSystem.Object, statusNotifier.Object);

            statusNotifier.Verify(x => x.AnalysisStarted(ValidFilePath), Times.Once);
            statusNotifier.Verify(x => x.AnalysisFailed(ValidFilePath, exception), Times.Once);
            statusNotifier.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ExecuteAnalysis_AnalysisFinished_NotifiesThatAnalysisFinished()
        {
            var fileSystem = SetupFileSystem(ValidFilePath, ValidFileContent);
            var statusNotifier = new Mock<IAnalysisStatusNotifier>();

            var consumer = new Mock<IIssueConsumer>();

            var secrets = new[] { Mock.Of<ISecret>(), Mock.Of<ISecret>() };
            var secretDetector = SetupSecretDetector(ValidFileContent, secrets);

            ExecuteAnalysis(ValidFilePath, consumer.Object, fileSystem, statusNotifier.Object, secretDetector);

            statusNotifier.Verify(x => x.AnalysisStarted(ValidFilePath), Times.Once);
            statusNotifier.Verify(x => x.AnalysisFinished(ValidFilePath, secrets.Length, It.IsAny<TimeSpan>()), Times.Once);
            statusNotifier.VerifyNoOtherCalls();
        }

        private void ExecuteAnalysis(string filePath, IIssueConsumer consumer, IFileSystem fileSystem, IAnalysisStatusNotifier statusNotifier = null, params Mock<ISecretDetector>[] secretDetectors)
        {
            var testSubject = CreateTestSubject(statusNotifier, fileSystem, secretDetectors);
            testSubject.ExecuteAnalysis(filePath, "", Array.Empty<AnalysisLanguage>(), consumer, null, CancellationToken.None);
        }

        private SecretsAnalyzer CreateTestSubject(IAnalysisStatusNotifier statusNotifier = null,
            IFileSystem fileSystem = null,
            IEnumerable<Mock<ISecretDetector>> detectors = null)
        {
            statusNotifier ??= Mock.Of<IAnalysisStatusNotifier>();
            fileSystem ??= Mock.Of<IFileSystem>();
            detectors ??= Array.Empty<Mock<ISecretDetector>>();

            return new SecretsAnalyzer(detectors.Select(x=> x.Object),
                statusNotifier,
                fileSystem);
        }

        private Mock<ISecretDetector> SetupSecretDetector(string input, params ISecret[] secrets)
        {
            var detector = new Mock<ISecretDetector>();
            detector.Setup(x => x.Find(input)).Returns(secrets);

            return detector;
        }

        private IFileSystem SetupFileSystem(string filePath, string content)
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.ReadAllText(filePath)).Returns(content);

            return fileSystem.Object;
        }
    }
}
