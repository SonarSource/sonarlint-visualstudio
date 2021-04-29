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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.TypeScript.Analyzer;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;
using SonarLint.VisualStudio.TypeScript.Rules;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.Analyzer
{
    [TestClass]
    public class JavaScriptAnalyzerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<JavaScriptAnalyzer, IAnalyzer>(null, new[]
            {
                MefTestHelpers.CreateExport<IEslintBridgeClientFactory>(Mock.Of<IEslintBridgeClientFactory>()),
                MefTestHelpers.CreateExport<IRulesProviderFactory>(Mock.Of<IRulesProviderFactory>()),
                MefTestHelpers.CreateExport<ITelemetryManager>(Mock.Of<ITelemetryManager>()),
                MefTestHelpers.CreateExport<IAnalysisStatusNotifier>(Mock.Of<IAnalysisStatusNotifier>()),
                MefTestHelpers.CreateExport<IActiveSolutionTracker>(Mock.Of<IActiveSolutionTracker>()),
                MefTestHelpers.CreateExport<IAnalysisConfigMonitor>(Mock.Of<IAnalysisConfigMonitor>()),
                MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>())
            });
        }

        [TestMethod]
        public void Ctor_RegisterToSolutionChangedEvent()
        {
            var activeSolutionTracker = SetupActiveSolutionTracker();

            CreateTestSubject(activeSolutionTracker: activeSolutionTracker.Object);

            activeSolutionTracker.VerifyAdd(x => x.ActiveSolutionChanged += It.IsAny<EventHandler<ActiveSolutionChangedEventArgs>>(), Times.Once);
            activeSolutionTracker.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Ctor_RegisterToConfigChangedEvent()
        {
            var analysisConfigMonitor = SetupAnalysisConfigMonitor();

            CreateTestSubject(analysisConfigMonitor: analysisConfigMonitor.Object);

            analysisConfigMonitor.VerifyAdd(x => x.ConfigChanged += It.IsAny<EventHandler>(), Times.Once);
            analysisConfigMonitor.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void IsAnalysisSupported_NotJavascript_False()
        {
            var testSubject = CreateTestSubject();

            var languages = new[] { AnalysisLanguage.CFamily, AnalysisLanguage.RoslynFamily };
            var result = testSubject.IsAnalysisSupported(languages);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsAnalysisSupported_HasJavascript_True()
        {
            var testSubject = CreateTestSubject();

            var languages = new[] { AnalysisLanguage.CFamily, AnalysisLanguage.Javascript };
            var result = testSubject.IsAnalysisSupported(languages);

            result.Should().BeTrue();
        }

        [TestMethod]
        public async Task ExecuteAnalysis_AlwaysCollectsTelemetry()
        {
            var telemetryManager = new Mock<ITelemetryManager>();
            var client = SetupEslintBridgeClient(null);

            var testSubject = CreateTestSubject(client.Object, telemetryManager: telemetryManager.Object);
            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), CancellationToken.None);

            telemetryManager.Verify(x => x.LanguageAnalyzed("js"), Times.Once);
            telemetryManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ExecuteAnalysis_InitLinterCallFails_CallsInitLinterAgain()
        {
            var client = SetupEslintBridgeClient(null);
            client.Setup(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()))
                .Throws<NotImplementedException>();

            var testSubject = CreateTestSubject(client.Object);

            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), CancellationToken.None);

            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Once);

            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), CancellationToken.None);

            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task ExecuteAnalysis_InitLinterCallSucceeds_DoesNotCallInitLinterAgain()
        {
            var activeRules = new[] { new Rule { Key = "rule1" }, new Rule { Key = "rule2" } };
            var activeRulesProvider = SetupActiveRulesProvider(activeRules);
            var client = SetupEslintBridgeClient(new AnalysisResponse{Issues = Enumerable.Empty<Issue>()});

            var testSubject = CreateTestSubject(client.Object, rulesProvider: activeRulesProvider.Object);

            var cancellationToken = new CancellationToken();
            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), cancellationToken);

            client.VerifyAll();
            client.Verify(x => x.InitLinter(activeRules, cancellationToken), Times.Once);
            client.VerifyNoOtherCalls();

            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), cancellationToken);

            client.VerifyAll();
            client.Verify(x => x.InitLinter(activeRules, cancellationToken), Times.Once);
            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Once);
            client.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ExecuteAnalysis_EslintBridgeClientCalledWithCorrectParams()
        {
            var activeRules = new[] { new Rule { Key = "rule1" }, new Rule { Key = "rule2" } };
            var activeRulesProvider = SetupActiveRulesProvider(activeRules);
            var eslintBridgeClient = SetupEslintBridgeClient(response: null);

            var testSubject = CreateTestSubject(eslintBridgeClient.Object, rulesProvider: activeRulesProvider.Object);

            var cancellationToken = new CancellationToken();
            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), cancellationToken);

            eslintBridgeClient.Verify(x => x.InitLinter(activeRules, cancellationToken), Times.Once);
            eslintBridgeClient.Verify(x => x.AnalyzeJs("some path", cancellationToken), Times.Once);
            eslintBridgeClient.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ExecuteAnalysis_NoResponse_ConsumerNotCalled()
        {
            var eslintBridgeClient = SetupEslintBridgeClient(response: null);
            var consumer = new Mock<IIssueConsumer>();

            var testSubject = CreateTestSubject(eslintBridgeClient.Object);
            await testSubject.ExecuteAnalysis("some path", consumer.Object, CancellationToken.None);

            consumer.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ExecuteAnalysis_ResponseWithNoIssues_ConsumerNotCalled()
        {
            var response = new AnalysisResponse();
            var eslintBridgeClient = SetupEslintBridgeClient(response: response);
            var consumer = new Mock<IIssueConsumer>();

            var testSubject = CreateTestSubject(eslintBridgeClient.Object);
            await testSubject.ExecuteAnalysis("some path", consumer.Object, CancellationToken.None);

            consumer.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ExecuteAnalysis_ResponseWithParsingError_MissingTypescript_ParsingErrorLogged()
        {
            var logger = new TestLogger();

            var parsingError = new ParsingError
            {
                Code = ParsingErrorCode.MISSING_TYPESCRIPT,
                Line = 5,
                Message = "some message"
            };

            await SetupAnalysisWithParsingError(parsingError, logger);

            logger.AssertPartialOutputStringExists(TypeScript.Analyzer.Resources.ERR_ParsingError_MissingTypescript);
        }

        [TestMethod]
        public async Task ExecuteAnalysis_ResponseWithParsingError_UnsupportedTypescript_ParsingErrorLogged()
        {
            var logger = new TestLogger();

            var parsingError = new ParsingError
            {
                Code = ParsingErrorCode.UNSUPPORTED_TYPESCRIPT,
                Line = 5,
                Message = "some message"
            };

            await SetupAnalysisWithParsingError(parsingError, logger);

            logger.AssertPartialOutputStringExists(TypeScript.Analyzer.Resources.ERR_ParsingError_UnsupportedTypescript);
        }

        [TestMethod]
        [DataRow(ParsingErrorCode.FAILING_TYPESCRIPT)]
        [DataRow(ParsingErrorCode.GENERAL_ERROR)]
        [DataRow(ParsingErrorCode.PARSING)]
        [DataRow((ParsingErrorCode)1234)]
        public async Task ExecuteAnalysis_ResponseWithParsingError_ParsingErrorLogged(int errorCode)
        {
            var logger = new TestLogger();

            var parsingError = new ParsingError
            {
                Code = (ParsingErrorCode)errorCode,
                Line = 5,
                Message = "some message"
            };

            await SetupAnalysisWithParsingError(parsingError, logger);

            logger.AssertPartialOutputStringExists(parsingError.Code.ToString());
            logger.AssertPartialOutputStringExists(parsingError.Message);
            logger.AssertPartialOutputStringExists(parsingError.Line.ToString());
        }

        [TestMethod]
        public async Task ExecuteAnalysis_ResponseWithParsingError_IssuesIgnoredAndConsumerNotCalled()
        {
            var response = new AnalysisResponse
            {
                ParsingError = new ParsingError
                {
                    Code = ParsingErrorCode.PARSING,
                    Line = 5,
                    Message = "some message"
                },
                Issues = new List<Issue>
                {
                    new Issue {Message = "issue1"},
                    new Issue {Message = "issue2"}
                }
            };

            var eslintBridgeClient = SetupEslintBridgeClient(response: response);
            var consumer = new Mock<IIssueConsumer>();
            var issueConverter = new Mock<IEslintBridgeIssueConverter>();

            var testSubject = CreateTestSubject(eslintBridgeClient.Object, issueConverter.Object);
            await testSubject.ExecuteAnalysis("some path", consumer.Object, CancellationToken.None);

            issueConverter.VerifyNoOtherCalls();
            consumer.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ExecuteAnalysis_ResponseWithIssues_ConsumerCalled()
        {
            var response = new AnalysisResponse
            {
                Issues = new List<Issue>
                {
                    new Issue {Message = "issue1"},
                    new Issue {Message = "issue2"}
                }
            };

            var convertedIssues = new[] { Mock.Of<IAnalysisIssue>(), Mock.Of<IAnalysisIssue>() };

            var issueConverter = new Mock<IEslintBridgeIssueConverter>();
            SetupConvertedIssue(issueConverter, "some path", response.Issues.First(), convertedIssues[0]);
            SetupConvertedIssue(issueConverter, "some path", response.Issues.Last(), convertedIssues[1]);

            var eslintBridgeClient = SetupEslintBridgeClient(response: response);
            var consumer = new Mock<IIssueConsumer>();
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(eslintBridgeClient.Object, issueConverter.Object, logger: logger);
            await testSubject.ExecuteAnalysis("some path", consumer.Object, CancellationToken.None);

            consumer.Verify(x => x.Accept("some path", convertedIssues));
        }

        [TestMethod]
        public void ExecuteAnalysis_CriticalException_ExceptionThrown()
        {
            var eslintBridgeClient = new Mock<IEslintBridgeClient>();
            eslintBridgeClient
                .Setup(x => x.AnalyzeJs(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new StackOverflowException());

            var testSubject = CreateTestSubject(eslintBridgeClient.Object);

            Func<Task> act = async () => await testSubject.ExecuteAnalysis("test", Mock.Of<IIssueConsumer>(), CancellationToken.None);
            act.Should().ThrowExactly<StackOverflowException>();
        }

        [TestMethod]
        public void Dispose_AnalysisNeverRan_DisposesEslintBridgeClient()
        {
            var client = SetupEslintBridgeClient(null);

            var testSubject = CreateTestSubject(client.Object);

            Action act = () => testSubject.Dispose();
            act.Should().NotThrow();

            client.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        public async Task Dispose_AnalysisRan_DisposesEslintBridgeClient()
        {
            var client = SetupEslintBridgeClient(null);

            var testSubject = CreateTestSubject(client.Object);

            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), CancellationToken.None);
            testSubject.Dispose();

            client.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        public void Dispose_UnregisterFromSolutionChangedEvent()
        {
            var activeSolutionTracker = SetupActiveSolutionTracker();
            var testSubject = CreateTestSubject(activeSolutionTracker: activeSolutionTracker.Object);

            activeSolutionTracker.VerifyRemove(x => x.ActiveSolutionChanged -= It.IsAny<EventHandler<ActiveSolutionChangedEventArgs>>(), Times.Never);

            testSubject.Dispose();

            activeSolutionTracker.VerifyRemove(x => x.ActiveSolutionChanged -= It.IsAny<EventHandler<ActiveSolutionChangedEventArgs>>(), Times.Once);
        }

        [TestMethod]
        public void Dispose_UnregisterFromConfigChangedEvent()
        {
            var analysisConfigMonitor = SetupAnalysisConfigMonitor();
            var testSubject = CreateTestSubject(analysisConfigMonitor: analysisConfigMonitor.Object);

            analysisConfigMonitor.VerifyRemove(x => x.ConfigChanged -= It.IsAny<EventHandler>(), Times.Never);

            testSubject.Dispose();

            analysisConfigMonitor.VerifyRemove(x => x.ConfigChanged -= It.IsAny<EventHandler>(), Times.Once);
        }

        [TestMethod]
        public async Task OnSolutionChanged_NextAnalysisCallsInitLinter()
        {
            var activeSolutionTracker = SetupActiveSolutionTracker();
            var client = SetupEslintBridgeClient(new AnalysisResponse {Issues = Enumerable.Empty<Issue>()});

            var testSubject = CreateTestSubject(client.Object, activeSolutionTracker: activeSolutionTracker.Object);

            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), CancellationToken.None);
            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), CancellationToken.None);

            client.Verify(x=> x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Once());

            activeSolutionTracker.Raise(x => x.ActiveSolutionChanged += null, new ActiveSolutionChangedEventArgs(true));

            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), CancellationToken.None);

            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task OnConfigChanged_NextAnalysisCallsInitLinter()
        {
            var analysisConfigMonitor = SetupAnalysisConfigMonitor();
            var client = SetupEslintBridgeClient(new AnalysisResponse { Issues = Enumerable.Empty<Issue>() });

            var testSubject = CreateTestSubject(client.Object, analysisConfigMonitor: analysisConfigMonitor.Object);

            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), CancellationToken.None);
            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), CancellationToken.None);

            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Once());

            analysisConfigMonitor.Raise(x => x.ConfigChanged += null, EventArgs.Empty);

            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), CancellationToken.None);

            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task ExecuteAnalysis_AnalysisFailed_NotifiesThatAnalysisFailed()
        {
            var statusNotifier = new Mock<IAnalysisStatusNotifier>();
            var exception = new NotImplementedException("this is a test");
            var serverProcess = SetupEslintBridgeClient(exceptionToThrow: exception);

            var testSubject = CreateTestSubject(serverProcess.Object, statusNotifier: statusNotifier.Object);
            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), CancellationToken.None);

            statusNotifier.Verify(x => x.AnalysisStarted("some path"), Times.Once);
            statusNotifier.Verify(x => x.AnalysisFailed("some path", exception), Times.Once);
            statusNotifier.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ExecuteAnalysis_TaskCancelled_NotifiesThatAnalysisWasCancelled()
        {
            var statusNotifier = new Mock<IAnalysisStatusNotifier>();
            var client = SetupEslintBridgeClient(exceptionToThrow: new TaskCanceledException());

            var testSubject = CreateTestSubject(client.Object, statusNotifier: statusNotifier.Object);
            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), CancellationToken.None);

            statusNotifier.Verify(x => x.AnalysisStarted("some path"), Times.Once);
            statusNotifier.Verify(x => x.AnalysisCancelled("some path"), Times.Once);
            statusNotifier.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ExecuteAnalysis_AnalysisFinished_NotifiesThatAnalysisFinished()
        {
            var statusNotifier = new Mock<IAnalysisStatusNotifier>();
            var client = SetupEslintBridgeClient(new AnalysisResponse { Issues = new[] { new Issue(), new Issue() } });

            var testSubject = CreateTestSubject(client.Object, statusNotifier: statusNotifier.Object);
            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), CancellationToken.None);

            statusNotifier.Verify(x => x.AnalysisStarted("some path"), Times.Once);
            statusNotifier.Verify(x => x.AnalysisFinished("some path", 2, It.IsAny<TimeSpan>()), Times.Once);
            statusNotifier.VerifyNoOtherCalls();
        }

        private async Task SetupAnalysisWithParsingError(ParsingError parsingError, ILogger logger)
        {
            var response = new AnalysisResponse { ParsingError = parsingError };
            var eslintBridgeClient = SetupEslintBridgeClient(response: response);
            var consumer = new Mock<IIssueConsumer>();

            var testSubject = CreateTestSubject(eslintBridgeClient.Object, logger: logger);
            await testSubject.ExecuteAnalysis("some path", consumer.Object, CancellationToken.None);

            consumer.VerifyNoOtherCalls();
        }

        private static void SetupConvertedIssue(Mock<IEslintBridgeIssueConverter> issueConverter,
            string filePath,
            Issue eslintBridgeIssue,
            IAnalysisIssue convertedIssue)
        {
            issueConverter
                .Setup(x => x.Convert(filePath, eslintBridgeIssue))
                .Returns(convertedIssue);
        }

        private Mock<IEslintBridgeClient> SetupEslintBridgeClient(AnalysisResponse response = null, Exception exceptionToThrow = null)
        {
            var eslintBridgeClient = new Mock<IEslintBridgeClient>();
            var setup = eslintBridgeClient.Setup(x => x.AnalyzeJs(It.IsAny<string>(), It.IsAny<CancellationToken>()));

            if (exceptionToThrow == null)
            {
                setup.ReturnsAsync(response);
            }
            else
            {
                setup.ThrowsAsync(exceptionToThrow);
            }

            return eslintBridgeClient;
        }

        private static Mock<IRulesProvider> SetupActiveRulesProvider(Rule[] activeRules)
        {
            var rulesProvider = new Mock<IRulesProvider>();
            rulesProvider.Setup(x => x.GetActiveRulesConfiguration()).Returns(activeRules);

            return rulesProvider;
        }

        private Mock<IEslintBridgeClientFactory> SetupEslintBridgeClientFactory(IEslintBridgeClient client)
        {
            var factory = new Mock<IEslintBridgeClientFactory>();
            factory.Setup(x => x.Create()).Returns(client);

            return factory;
        }

        private static Mock<IActiveSolutionTracker> SetupActiveSolutionTracker()
        {
            var activeSolutionTracker = new Mock<IActiveSolutionTracker>();
            activeSolutionTracker.SetupAdd(x => x.ActiveSolutionChanged += null);
            activeSolutionTracker.SetupRemove(x => x.ActiveSolutionChanged -= null);

            return activeSolutionTracker;
        }

        private static Mock<IAnalysisConfigMonitor> SetupAnalysisConfigMonitor()
        {
            var analysisConfigMonitor = new Mock<IAnalysisConfigMonitor>();
            analysisConfigMonitor.SetupAdd(x => x.ConfigChanged += null);
            analysisConfigMonitor.SetupRemove(x => x.ConfigChanged -= null);

            return analysisConfigMonitor;
        }

        private JavaScriptAnalyzer CreateTestSubject(IEslintBridgeClient client = null,
            IEslintBridgeIssueConverter issueConverter = null,
            IRulesProvider rulesProvider = null,
            ITelemetryManager telemetryManager = null,
            IAnalysisStatusNotifier statusNotifier = null,
            IActiveSolutionTracker activeSolutionTracker = null,
            IAnalysisConfigMonitor analysisConfigMonitor = null,
            ILogger logger = null)
        {
            client ??= SetupEslintBridgeClient().Object;

            var eslintBridgeClientFactory = SetupEslintBridgeClientFactory(client);

            return CreateTestSubject(
                eslintBridgeClientFactory.Object,
                rulesProvider,
                issueConverter,
                telemetryManager,
                statusNotifier,
                activeSolutionTracker,
                analysisConfigMonitor,
                logger);
        }

        private JavaScriptAnalyzer CreateTestSubject(
            IEslintBridgeClientFactory eslintBridgeClientFactory,
            IRulesProvider rulesProvider = null,
            IEslintBridgeIssueConverter issueConverter = null,
            ITelemetryManager telemetryManager = null,
            IAnalysisStatusNotifier statusNotifier = null,
            IActiveSolutionTracker activeSolutionTracker = null,
            IAnalysisConfigMonitor analysisConfigMonitor = null,
            ILogger logger = null)
        {
            issueConverter ??= Mock.Of<IEslintBridgeIssueConverter>();
            logger ??= Mock.Of<ILogger>();
            telemetryManager ??= Mock.Of<ITelemetryManager>();
            statusNotifier ??= Mock.Of<IAnalysisStatusNotifier>();
            rulesProvider ??= Mock.Of<IRulesProvider>();
            activeSolutionTracker ??= Mock.Of<IActiveSolutionTracker>();
            analysisConfigMonitor ??= Mock.Of<IAnalysisConfigMonitor>();

            return new JavaScriptAnalyzer(eslintBridgeClientFactory,
                rulesProvider,
                telemetryManager,
                statusNotifier,
                activeSolutionTracker,
                analysisConfigMonitor,
                logger,
                issueConverter);
        }
    }
}
