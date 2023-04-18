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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.TypeScript.Analyzer;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;
using SonarLint.VisualStudio.TypeScript.Rules;
using SonarLint.VisualStudio.TypeScript.TsConfig;
using Resources = SonarLint.VisualStudio.TypeScript.Analyzer.Resources;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.Analyzer
{
    [TestClass]
    public class TypeScriptAnalyzerTests
    {
        private const string ValidFilePath = "some path";

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            var eslintBridgeClient = Mock.Of<ITypeScriptEslintBridgeClient>();

            var rulesProvider = Mock.Of<IRulesProvider>();
            var rulesProviderFactory = new Mock<IRulesProviderFactory>();
            rulesProviderFactory
                .Setup(x => x.Create("typescript", Language.Ts))
                .Returns(rulesProvider);

            var eslintBridgeAnalyzer = Mock.Of<IEslintBridgeAnalyzer>();
            var eslintBridgeAnalyzerFactory = new Mock<IEslintBridgeAnalyzerFactory>();
            eslintBridgeAnalyzerFactory
                .Setup(x => x.Create(rulesProvider, eslintBridgeClient))
                .Returns(eslintBridgeAnalyzer);

            MefTestHelpers.CheckTypeCanBeImported<TypeScriptAnalyzer, IAnalyzer>(
                MefTestHelpers.CreateExport<ITypeScriptEslintBridgeClient>(eslintBridgeClient),
                MefTestHelpers.CreateExport<IRulesProviderFactory>(rulesProviderFactory.Object),
                MefTestHelpers.CreateExport<ITsConfigProvider>(),
                MefTestHelpers.CreateExport<IAnalysisStatusNotifierFactory>(),
                MefTestHelpers.CreateExport<IEslintBridgeAnalyzerFactory>(eslintBridgeAnalyzerFactory.Object),
                MefTestHelpers.CreateExport<ITelemetryManager>(),
                MefTestHelpers.CreateExport<ILogger>(),
                MefTestHelpers.CreateExport<IThreadHandling>());
        }

        [TestMethod]
        public void IsAnalysisSupported_NotTypeScript_False()
        {
            var testSubject = CreateTestSubject();

            var languages = new[] { AnalysisLanguage.CFamily, AnalysisLanguage.Javascript, AnalysisLanguage.CascadingStyleSheets };
            var result = testSubject.IsAnalysisSupported(languages);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsAnalysisSupported_HasTypeScript_True()
        {
            var testSubject = CreateTestSubject();

            var languages = new[] { AnalysisLanguage.CFamily, AnalysisLanguage.TypeScript };
            var result = testSubject.IsAnalysisSupported(languages);

            result.Should().BeTrue();
        }

        [TestMethod]
        public async Task ExecuteAnalysis_AlwaysCollectsTelemetry()
        {
            var telemetryManager = new Mock<ITelemetryManager>();
            var client = SetupEslintBridgeAnalyzer(null);

            var testSubject = CreateTestSubject(client.Object, telemetryManager: telemetryManager.Object);
            await testSubject.ExecuteAnalysisAsync(ValidFilePath, Mock.Of<IIssueConsumer>(), CancellationToken.None);

            telemetryManager.Verify(x => x.LanguageAnalyzed("ts"), Times.Once);
            telemetryManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ExecuteAnalysis_NoTsConfig_ConsumerNotCalled()
        {
            var tsConfigProvider = SetupTsConfigProvider(result: null);
            var eslintBridgeAnalyzer = SetupEslintBridgeAnalyzer();
            var consumer = new Mock<IIssueConsumer>();

            var testSubject = CreateTestSubject(eslintBridgeAnalyzer.Object, tsConfigProvider);
            await testSubject.ExecuteAnalysisAsync(ValidFilePath, consumer.Object, CancellationToken.None);

            consumer.VerifyNoOtherCalls();
            eslintBridgeAnalyzer.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ExecuteAnalysis_NoTsConfig_NotifiesThatAnalysisFailed()
        {
            var tsConfigProvider = SetupTsConfigProvider(result: null);
            var statusNotifier = new Mock<IAnalysisStatusNotifier>();
            var eslintBridgeAnalyzer = SetupEslintBridgeAnalyzer();

            var testSubject = CreateTestSubject(eslintBridgeAnalyzer.Object, tsConfigProvider, statusNotifier: statusNotifier.Object);
            await testSubject.ExecuteAnalysisAsync(ValidFilePath, Mock.Of<IIssueConsumer>(), CancellationToken.None);

            statusNotifier.Verify(x => x.AnalysisStarted(), Times.Once);
            statusNotifier.Verify(x => x.AnalysisFailed(Resources.ERR_NoTsConfig), Times.Once);
            statusNotifier.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ExecuteAnalysis_ResponseWithNoIssues_ConsumerNotCalled()
        {
            var issues = Array.Empty<IAnalysisIssue>();
            var eslintBridgeAnalyzer = SetupEslintBridgeAnalyzer(issues);
            var consumer = new Mock<IIssueConsumer>();

            var testSubject = CreateTestSubject(eslintBridgeAnalyzer.Object);
            await testSubject.ExecuteAnalysisAsync(ValidFilePath, consumer.Object, CancellationToken.None);

            consumer.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ExecuteAnalysis_ResponseWithIssues_ConsumerCalled()
        {
            var issues = new[] { Mock.Of<IAnalysisIssue>(), Mock.Of<IAnalysisIssue>() };
            var eslintBridgeAnalyzer = SetupEslintBridgeAnalyzer(issues);
            var consumer = new Mock<IIssueConsumer>();

            var testSubject = CreateTestSubject(eslintBridgeAnalyzer.Object);
            await testSubject.ExecuteAnalysisAsync(ValidFilePath, consumer.Object, CancellationToken.None);

            consumer.Verify(x => x.Accept(ValidFilePath, issues));
        }

        [TestMethod]
        public void ExecuteAnalysis_CriticalException_ExceptionThrown()
        {
            var eslintBridgeAnalyzer = SetupEslintBridgeAnalyzer(exceptionToThrow: new StackOverflowException());

            var testSubject = CreateTestSubject(eslintBridgeAnalyzer.Object);

            Func<Task> act = async () => await testSubject.ExecuteAnalysisAsync(ValidFilePath, Mock.Of<IIssueConsumer>(), CancellationToken.None);
            act.Should().ThrowExactly<StackOverflowException>();
        }

        [TestMethod]
        public void Dispose_DisposesEslintBridgeAnalyzer()
        {
            var eslintBridgeAnalyzer = SetupEslintBridgeAnalyzer(null);

            var testSubject = CreateTestSubject(eslintBridgeAnalyzer.Object);

            Action act = () => testSubject.Dispose();
            act.Should().NotThrow();

            eslintBridgeAnalyzer.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod, Description("Regression test for #2452")]
        public async Task ExecuteAnalysis_EslintBridgeProcessLaunchException_NotifiesThatAnalysisFailedWithoutStackTrace()
        {
            var statusNotifier = new Mock<IAnalysisStatusNotifier>();
            var exception = new EslintBridgeProcessLaunchException("this is a test");
            var eslintBridgeAnalyzer = SetupEslintBridgeAnalyzer(exceptionToThrow: exception);

            var testSubject = CreateTestSubject(eslintBridgeAnalyzer.Object, statusNotifier: statusNotifier.Object);
            await testSubject.ExecuteAnalysisAsync(ValidFilePath, Mock.Of<IIssueConsumer>(), CancellationToken.None);

            statusNotifier.Verify(x => x.AnalysisStarted(), Times.Once);
            statusNotifier.Verify(x => x.AnalysisFailed("this is a test"), Times.Once);
            statusNotifier.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ExecuteAnalysis_AnalysisFailed_NotifiesThatAnalysisFailed()
        {
            var statusNotifier = new Mock<IAnalysisStatusNotifier>();
            var exception = new NotImplementedException("this is a test");
            var eslintBridgeAnalyzer = SetupEslintBridgeAnalyzer(exceptionToThrow: exception);

            var testSubject = CreateTestSubject(eslintBridgeAnalyzer.Object, statusNotifier: statusNotifier.Object);
            await testSubject.ExecuteAnalysisAsync(ValidFilePath, Mock.Of<IIssueConsumer>(), CancellationToken.None);

            statusNotifier.Verify(x => x.AnalysisStarted(), Times.Once);
            statusNotifier.Verify(x => x.AnalysisFailed(exception), Times.Once);
            statusNotifier.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ExecuteAnalysis_TaskCancelled_NotifiesThatAnalysisWasCancelled()
        {
            var statusNotifier = new Mock<IAnalysisStatusNotifier>();
            var eslintBridgeAnalyzer = SetupEslintBridgeAnalyzer(exceptionToThrow: new TaskCanceledException());

            var testSubject = CreateTestSubject(eslintBridgeAnalyzer.Object, statusNotifier: statusNotifier.Object);
            await testSubject.ExecuteAnalysisAsync(ValidFilePath, Mock.Of<IIssueConsumer>(), CancellationToken.None);

            statusNotifier.Verify(x => x.AnalysisStarted(), Times.Once);
            statusNotifier.Verify(x => x.AnalysisCancelled(), Times.Once);
            statusNotifier.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ExecuteAnalysis_AnalysisFinished_NotifiesThatAnalysisFinished()
        {
            var statusNotifier = new Mock<IAnalysisStatusNotifier>();
            var issues = new[] { Mock.Of<IAnalysisIssue>(), Mock.Of<IAnalysisIssue>() };
            var eslintBridgeAnalyzer = SetupEslintBridgeAnalyzer(issues);

            var testSubject = CreateTestSubject(eslintBridgeAnalyzer.Object, statusNotifier: statusNotifier.Object);
            await testSubject.ExecuteAnalysisAsync(ValidFilePath, Mock.Of<IIssueConsumer>(), CancellationToken.None);

            statusNotifier.Verify(x => x.AnalysisStarted(), Times.Once);
            statusNotifier.Verify(x => x.AnalysisFinished(2, It.IsAny<TimeSpan>()), Times.Once);
            statusNotifier.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ExecuteAnalysis_SwitchesToBackgroundThreadBeforeProcessing()
        {
            var callOrder = new List<string>();

            var threadHandling = new Mock<IThreadHandling>();
            threadHandling.Setup(x => x.SwitchToBackgroundThread())
                .Returns(() => new NoOpThreadHandler.NoOpAwaitable())
                .Callback(() => callOrder.Add("SwitchToBackgroundThread"));

            var statusNotifier = new Mock<IAnalysisStatusNotifier>();
            statusNotifier.Setup(x => x.AnalysisStarted()).Callback(() => callOrder.Add("AnalysisStarted"));

            var testSubject = CreateTestSubject(statusNotifier: statusNotifier.Object, threadHandling: threadHandling.Object);
            await testSubject.ExecuteAnalysisAsync(ValidFilePath, Mock.Of<IIssueConsumer>(), CancellationToken.None);

            callOrder.Should().Equal("SwitchToBackgroundThread", "AnalysisStarted");
        }

        private Mock<IEslintBridgeAnalyzer> SetupEslintBridgeAnalyzer(IReadOnlyCollection<IAnalysisIssue> issues = null, Exception exceptionToThrow = null)
        {
            var eslintBridgeClient = new Mock<IEslintBridgeAnalyzer>();
            var setup = eslintBridgeClient.Setup(x => x.Analyze(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));

            if (exceptionToThrow == null)
            {
                setup.ReturnsAsync(issues);
            }
            else
            {
                setup.ThrowsAsync(exceptionToThrow);
            }

            return eslintBridgeClient;
        }

        private ITsConfigProvider SetupTsConfigProvider(string result = "some config")
        {
            var tsConfigProvider = new Mock<ITsConfigProvider>();
            tsConfigProvider
                .Setup(x => x.GetConfigForFile(ValidFilePath, CancellationToken.None))
                .ReturnsAsync(result);

            return tsConfigProvider.Object;
        }

        private TypeScriptAnalyzer CreateTestSubject(
            IEslintBridgeAnalyzer eslintBridgeAnalyzer = null,
            ITsConfigProvider tsConfigProvider = null,
            IRulesProvider rulesProvider = null,
            IAnalysisStatusNotifier statusNotifier = null,
            ITelemetryManager telemetryManager = null,
            ILogger logger = null,
            IThreadHandling threadHandling = null)
        {
            statusNotifier ??= Mock.Of<IAnalysisStatusNotifier>();
            rulesProvider ??= Mock.Of<IRulesProvider>();
            logger ??= Mock.Of<ILogger>();
            tsConfigProvider ??= SetupTsConfigProvider();
            eslintBridgeAnalyzer ??= Mock.Of<IEslintBridgeAnalyzer>();
            telemetryManager ??= Mock.Of<ITelemetryManager>();
            threadHandling ??= new NoOpThreadHandler();

            var rulesProviderFactory = new Mock<IRulesProviderFactory>();
            rulesProviderFactory.Setup(x => x.Create("typescript", Language.Ts)).Returns(rulesProvider);

            var eslintBridgeClient = Mock.Of<ITypeScriptEslintBridgeClient>();
            var eslintBridgeAnalyzerFactory = new Mock<IEslintBridgeAnalyzerFactory>();
            eslintBridgeAnalyzerFactory
                .Setup(x => x.Create(rulesProvider, eslintBridgeClient))
                .Returns(eslintBridgeAnalyzer);

            return new TypeScriptAnalyzer(eslintBridgeClient,
                rulesProviderFactory.Object,
                tsConfigProvider,
                SetupStatusNotifierFactory(statusNotifier),
                eslintBridgeAnalyzerFactory.Object,
                telemetryManager,
                logger,
                threadHandling);
        }

        private IAnalysisStatusNotifierFactory SetupStatusNotifierFactory(IAnalysisStatusNotifier statusNotifier)
        {
            var statusNotifierFactory = new Mock<IAnalysisStatusNotifierFactory>();
            statusNotifierFactory.Setup(x => x.Create(nameof(TypeScriptAnalyzer), ValidFilePath)).Returns(statusNotifier);

            return statusNotifierFactory.Object;
        }
    }
}
