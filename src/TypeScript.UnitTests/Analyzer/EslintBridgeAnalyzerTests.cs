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
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.TypeScript.Analyzer;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;
using SonarLint.VisualStudio.TypeScript.Rules;
using Resources = SonarLint.VisualStudio.TypeScript.Analyzer.Resources;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.Analyzer
{
    [TestClass]
    public class EslintBridgeAnalyzerTests
    {
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
        public void Analyze_Exception_ExceptionThrown()
        {
            var eslintBridgeClient = SetupEslintBridgeClient(exceptionToThrow: new NotImplementedException());

            var testSubject = CreateTestSubject(eslintBridgeClient.Object);

            Func<Task> act = async () => await testSubject.Analyze("some path", "some config", CancellationToken.None);
            act.Should().ThrowExactly<NotImplementedException>();
        }

        [TestMethod]
        public void Analyze_InitLinterCallFails_NextCallCallsInitLinterAgain()
        {
            var client = SetupEslintBridgeClient(null);
            client.Setup(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()))
                .Throws<NotImplementedException>();

            var testSubject = CreateTestSubject(client.Object);

            Func<Task> act = async () => await testSubject.Analyze("some path", "some config", CancellationToken.None);
            act.Should().ThrowExactly<NotImplementedException>();

            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Once);

            act = async () => await testSubject.Analyze("some path", "some config", CancellationToken.None);
            act.Should().ThrowExactly<NotImplementedException>();

            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task Analyze_InitLinterCallSucceeds_NextCallDoesNotCallInitLinterAgain()
        {
            var activeRules = new[] { new Rule { Key = "rule1" }, new Rule { Key = "rule2" } };
            var activeRulesProvider = SetupActiveRulesProvider(activeRules);
            var client = SetupEslintBridgeClient(new JsTsAnalysisResponse { Issues = Enumerable.Empty<Issue>() });

            var testSubject = CreateTestSubject(client.Object, activeRulesProvider.Object);

            var cancellationToken = new CancellationToken();
            await testSubject.Analyze("some path", "some config", CancellationToken.None);

            client.VerifyAll();
            client.Verify(x => x.InitLinter(activeRules, cancellationToken), Times.Once);
            client.VerifyNoOtherCalls();

            await testSubject.Analyze("some path", "some config", CancellationToken.None);

            client.VerifyAll();
            client.Verify(x => x.InitLinter(activeRules, cancellationToken), Times.Once);
            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Once);
            client.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task Analyze_EslintBridgeClientNotInitializedResponse_CallsInitLinter()
        {
            var validResponse = new JsTsAnalysisResponse { Issues = new List<Issue>() };
            var linterNotInitializedResponse = CreateLinterNotInitializedResponse();
            var client = new Mock<IEslintBridgeClient>();

            // First analysis: response is valid, InitLinter should be called once (first initialization)
            // Second analysis: response is still valid, InitLinter should not be called
            // Third analysis: response is invalid, InitLinter should be called. Next response is valid, InitLinter not should be called.
            // Forth analysis: because previous response succeeded, init InitLinter should not be called
            client
                .SetupSequence(x => x.Analyze("some path", "some config", CancellationToken.None))
                .ReturnsAsync(validResponse)
                .ReturnsAsync(validResponse)
                .ReturnsAsync(linterNotInitializedResponse)
                .ReturnsAsync(validResponse)
                .ReturnsAsync(validResponse);

            var activeRules = new[] { new Rule { Key = "rule1" }, new Rule { Key = "rule2" } };
            var activeRulesProvider = SetupActiveRulesProvider(activeRules);

            var testSubject = CreateTestSubject(client.Object, rulesProvider: activeRulesProvider.Object);

            await testSubject.Analyze("some path", "some config", CancellationToken.None);
            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Once);

            await testSubject.Analyze("some path", "some config", CancellationToken.None);
            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Once);

            await testSubject.Analyze("some path", "some config", CancellationToken.None);
            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

            await testSubject.Analyze("some path", "some config", CancellationToken.None);
            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

            activeRulesProvider.Verify(x => x.GetActiveRulesConfiguration(), Times.Exactly(2));
        }

        [TestMethod]
        public async Task Analyze_EslintBridgeClientNotInitializedResponse_FollowedByParsingError_ParsingErrorIsLogged()
        {
            var logger = new TestLogger();
            var linterNotInitializedResponse = CreateLinterNotInitializedResponse();
            var parsingErrorResponse = new JsTsAnalysisResponse
            {
                ParsingError = new ParsingError
                {
                    Code = ParsingErrorCode.MISSING_TYPESCRIPT,
                    Line = 5,
                    Message = "some message"
                }
            };

            var client = new Mock<IEslintBridgeClient>();

            client
                .SetupSequence(x => x.Analyze("some path", "some config", CancellationToken.None))
                .ReturnsAsync(linterNotInitializedResponse)
                .ReturnsAsync(parsingErrorResponse);

            var testSubject = CreateTestSubject(client.Object, logger: logger);
            await testSubject.Analyze("some path", "some config", CancellationToken.None);

            logger.AssertPartialOutputStringExists(Resources.ERR_ParsingError_MissingTypescript);
        }

        [TestMethod]
        public async Task Analyze_EslintBridgeClientNotInitializedResponse_FollowedByValidResponse_ConvertedIssuesReturnedAndParsingErrorNotLogged()
        {
            var linterNotInitializedResponse = CreateLinterNotInitializedResponse();
            var validResponse = new JsTsAnalysisResponse { Issues = new List<Issue> { new Issue() } };
            var logger = new TestLogger();
            var client = new Mock<IEslintBridgeClient>();

            client
                .SetupSequence(x => x.Analyze("some path", "some config", CancellationToken.None))
                .ReturnsAsync(linterNotInitializedResponse)
                .ReturnsAsync(validResponse);

            var convertedIssues = new[] { Mock.Of<IAnalysisIssue>() };
            var issueConverter = new Mock<IEslintBridgeIssueConverter>();
            SetupConvertedIssue(issueConverter, "some path", validResponse.Issues.First(), convertedIssues[0]);

            var testSubject = CreateTestSubject(client.Object, issueConverter: issueConverter.Object, logger: logger);
            var result = await testSubject.Analyze("some path", "some config", CancellationToken.None);

            result.Should().BeEquivalentTo(convertedIssues);
            logger.AssertNoOutputMessages();
            client.Verify(x => x.Analyze("some path", "some config", CancellationToken.None), Times.Exactly(2));
        }

        [TestMethod]
        public async Task Analyze_EslintBridgeClientNotInitializedResponse_FollowedByAnotherNotInitializedResponse_GeneralParsingError()
        {
            var logger = new TestLogger();
            var linterNotInitializedResponse = CreateLinterNotInitializedResponse();
            var client = new Mock<IEslintBridgeClient>();

            client
                .SetupSequence(x => x.Analyze("some path", "some config", CancellationToken.None))
                .ReturnsAsync(linterNotInitializedResponse)
                .ReturnsAsync(linterNotInitializedResponse);

            var testSubject = CreateTestSubject(client.Object, logger: logger);
            var result = await testSubject.Analyze("some path", "some config", CancellationToken.None);

            logger.AssertPartialOutputStringExists(linterNotInitializedResponse.ParsingError.Code.ToString());
        }

        [TestMethod]
        public async Task Analyze_EslintBridgeClientCalledWithCorrectParams()
        {
            var activeRules = new[] { new Rule { Key = "rule1" }, new Rule { Key = "rule2" } };
            var activeRulesProvider = SetupActiveRulesProvider(activeRules);
            var eslintBridgeClient = SetupEslintBridgeClient(response: new JsTsAnalysisResponse());

            var testSubject = CreateTestSubject(eslintBridgeClient.Object, activeRulesProvider.Object);

            var cancellationToken = new CancellationToken();
            await testSubject.Analyze("some path", "some config", cancellationToken);

            eslintBridgeClient.Verify(x => x.InitLinter(activeRules, cancellationToken), Times.Once);
            eslintBridgeClient.Verify(x => x.Analyze("some path", "some config", cancellationToken), Times.Once);
            eslintBridgeClient.VerifyNoOtherCalls();
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
        [DataRow(true)]
        [DataRow(false)]
        public async Task OnSolutionChanged_NextAnalysisCallsInitLinter_OnlyIfSolutionClosed(bool isSolutionClosed)
        {
            var activeSolutionTracker = SetupActiveSolutionTracker();
            var client = SetupEslintBridgeClient(new JsTsAnalysisResponse { Issues = Enumerable.Empty<Issue>() });

            var testSubject = CreateTestSubject(client.Object, activeSolutionTracker: activeSolutionTracker.Object);

            await testSubject.Analyze("some path", "some config", CancellationToken.None);

            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

            activeSolutionTracker.Raise(x => x.ActiveSolutionChanged += null, new ActiveSolutionChangedEventArgs(!isSolutionClosed));

            await testSubject.Analyze("some path", "some config", CancellationToken.None);

            var expectedCallCount = isSolutionClosed ? 2 : 1;
            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Exactly(expectedCallCount));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void OnSolutionChanged_EslintBridgeClientStopped_OnlyIfSolutionClosed(bool isSolutionClosed)
        {
            var activeSolutionTracker = SetupActiveSolutionTracker();
            var client = SetupEslintBridgeClient(new JsTsAnalysisResponse { Issues = Enumerable.Empty<Issue>() });

            CreateTestSubject(client.Object, activeSolutionTracker: activeSolutionTracker.Object);

            activeSolutionTracker.Raise(x => x.ActiveSolutionChanged += null, new ActiveSolutionChangedEventArgs(!isSolutionClosed));

            var expectedCallCount = isSolutionClosed ? 1 : 0;
            client.Verify(x => x.Close(), Times.Exactly(expectedCallCount));
        }

        [TestMethod]
        public void OnSolutionChanged_SolutionClosed_EslintBridgeClientStoppedOnBackgroundThread()
        {
            // Regression test for #3161 - UI freeze when closing a folder/solution after a JS/TS analysis was done
            var activeSolutionTracker = SetupActiveSolutionTracker();

            var callOrder = new List<string>();

            var threadHandling = new Mock<IThreadHandling>();
            threadHandling.Setup(x => x.SwitchToBackgroundThread())
                .Returns(() => new NoOpThreadHandler.NoOpAwaitable())
                .Callback(() => callOrder.Add("SwitchToBackgroundThread"));

            var client = new Mock<IEslintBridgeClient>();
            client.Setup(x => x.Close())
                .Returns(() => Task.CompletedTask)
                .Callback(() => callOrder.Add("Close"));

            CreateTestSubject(client.Object, activeSolutionTracker: activeSolutionTracker.Object, threadHandling: threadHandling.Object);

            activeSolutionTracker.Raise(x => x.ActiveSolutionChanged += null, new ActiveSolutionChangedEventArgs(false));

            // Order is important so using Equal rather than IsEquivalentTo
            callOrder.Should().Equal("SwitchToBackgroundThread", "Close");
        }

        [TestMethod]
        public async Task OnConfigChanged_NextAnalysisCallsInitLinter()
        {
            var analysisConfigMonitor = SetupAnalysisConfigMonitor();
            var client = SetupEslintBridgeClient(new JsTsAnalysisResponse { Issues = Enumerable.Empty<Issue>() });

            var testSubject = CreateTestSubject(client.Object, analysisConfigMonitor: analysisConfigMonitor.Object);

            await testSubject.Analyze("some path", "some config", CancellationToken.None);
            await testSubject.Analyze("some path", "some config", CancellationToken.None);

            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Once());

            analysisConfigMonitor.Raise(x => x.ConfigChanged += null, EventArgs.Empty);

            await testSubject.Analyze("some path", "some config", CancellationToken.None);

            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public void OnConfigChanged_DoesNotStopEslintBridgeClient()
        {
            var analysisConfigMonitor = SetupAnalysisConfigMonitor();
            var client = SetupEslintBridgeClient(new JsTsAnalysisResponse { Issues = Enumerable.Empty<Issue>() });

            CreateTestSubject(client.Object, analysisConfigMonitor: analysisConfigMonitor.Object);

            analysisConfigMonitor.Raise(x => x.ConfigChanged += null, EventArgs.Empty);

            client.Verify(x => x.Close(), Times.Never);
        }

        [TestMethod]
        public async Task Analyze_ResponseWithParsingError_MissingTypescript_ParsingErrorLogged()
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
        public async Task Analyze_ResponseWithParsingError_UnsupportedTypescript_ParsingErrorLogged()
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
        public async Task Analyze_ResponseWithParsingError_ParsingErrorLogged(int errorCode)
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
        public async Task Analyze_ResponseWithParsingError_IssuesIgnoredAndEmptyListReturned()
        {
            var response = new JsTsAnalysisResponse
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
            var issueConverter = new Mock<IEslintBridgeIssueConverter>();

            var testSubject = CreateTestSubject(eslintBridgeClient.Object, issueConverter: issueConverter.Object);
            var result = await testSubject.Analyze("some path", "some config", CancellationToken.None);

            issueConverter.VerifyNoOtherCalls();
            result.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Analyze_ResponseWithNoIssues_ReturnsEmptyList()
        {
            var response = new JsTsAnalysisResponse { Issues = null };
            var eslintBridgeClient = SetupEslintBridgeClient(response: response);
            var issueConverter = new Mock<IEslintBridgeIssueConverter>();

            var testSubject = CreateTestSubject(eslintBridgeClient.Object, issueConverter: issueConverter.Object);
            var result = await testSubject.Analyze("some path", "some config", CancellationToken.None);

            issueConverter.VerifyNoOtherCalls();
            result.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Analyze_ResponseWithIssues_ReturnsConvertedIssues()
        {
            var response = new JsTsAnalysisResponse
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
            var testSubject = CreateTestSubject(eslintBridgeClient.Object, issueConverter: issueConverter.Object);
            var result = await testSubject.Analyze("some path", "some config", CancellationToken.None);

            result.Should().BeEquivalentTo(convertedIssues);
        }

        [TestMethod]
        public async Task Analyze_HasCssSyntaxError_IgnoresNull()
        {
            var response = new AnalysisResponse
            {
                Issues = new List<Issue>
                {
                    new Issue {Message = "issue1"},
                    new Issue {Message = "issue2"}
                }
            };
            var analysisIssue = Mock.Of<IAnalysisIssue>();
            var convertedIssues = new[] { analysisIssue, null };

            var issueConverter = new Mock<IEslintBridgeIssueConverter>();
            SetupConvertedIssue(issueConverter, "some path", response.Issues.First(), convertedIssues[0]);
            SetupConvertedIssue(issueConverter, "some path", response.Issues.Last(), convertedIssues[1]);

            var eslintBridgeClient = SetupEslintBridgeClient(response: response);
            var testSubject = CreateTestSubject(eslintBridgeClient.Object, issueConverter: issueConverter.Object);
            var result = await testSubject.Analyze("some path", "some config", CancellationToken.None);

            result.Should().ContainSingle();
            result.Should().HaveElementAt(0, analysisIssue);
        }

        [TestMethod]
        public async Task Analyze_HasCssAnalysisError_ReturnsEmptyList()
        {
            var response = new CssAnalysisResponse()
            {
                Error = "errorrr"
            };
            var issueConverter = new Mock<IEslintBridgeIssueConverter>();
            var eslintBridgeClient = SetupEslintBridgeClient(response: response);
            var testLogger = new TestLogger();

            var testSubject = CreateTestSubject(eslintBridgeClient.Object, issueConverter: issueConverter.Object, logger: testLogger);

            var result = await testSubject.Analyze("some path", "some config", CancellationToken.None);

            result.Should().HaveCount(0);
            testLogger.AssertOutputStrings(2);
            testLogger.AssertOutputStringExists("[CssAnalyzer] Failed to analyze CSS in some path");
            testLogger.AssertOutputStringExists("[Verbose] Reason for failed css analysis: errorrr");
        }

        private static JsTsAnalysisResponse CreateLinterNotInitializedResponse() =>
            new JsTsAnalysisResponse { ParsingError = new ParsingError { Code = ParsingErrorCode.LINTER_INITIALIZATION } };

        private static void SetupConvertedIssue(Mock<IEslintBridgeIssueConverter> issueConverter,
            string filePath,
            Issue eslintBridgeIssue,
            IAnalysisIssue convertedIssue)
        {
            issueConverter
                .Setup(x => x.Convert(filePath, eslintBridgeIssue))
                .Returns(convertedIssue);
        }

        private async Task SetupAnalysisWithParsingError(ParsingError parsingError, ILogger logger)
        {
            var response = new JsTsAnalysisResponse { ParsingError = parsingError };
            var eslintBridgeClient = SetupEslintBridgeClient(response: response);
            var testSubject = CreateTestSubject(eslintBridgeClient.Object, logger: logger);
            await testSubject.Analyze("some path", "some config", CancellationToken.None);
        }

        private Mock<IEslintBridgeClient> SetupEslintBridgeClient(AnalysisResponse response = null, Exception exceptionToThrow = null)
        {
            var eslintBridgeClient = new Mock<IEslintBridgeClient>();
            var setup = eslintBridgeClient.Setup(x => x.Analyze(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));

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

        private EslintBridgeAnalyzer CreateTestSubject(
            IEslintBridgeClient eslintBridgeClient = null,
            IRulesProvider rulesProvider = null,
            IActiveSolutionTracker activeSolutionTracker = null,
            IAnalysisConfigMonitor analysisConfigMonitor = null,
            IEslintBridgeIssueConverter issueConverter = null,
            IThreadHandling threadHandling = null,
            ILogger logger = null)
        {
            eslintBridgeClient ??= SetupEslintBridgeClient().Object;
            rulesProvider ??= Mock.Of<IRulesProvider>();
            activeSolutionTracker ??= Mock.Of<IActiveSolutionTracker>();
            analysisConfigMonitor ??= Mock.Of<IAnalysisConfigMonitor>();
            issueConverter ??= Mock.Of<IEslintBridgeIssueConverter>();
            threadHandling ??= new NoOpThreadHandler();
            logger ??= Mock.Of<ILogger>();

            return new EslintBridgeAnalyzer(rulesProvider,
                eslintBridgeClient,
                activeSolutionTracker,
                analysisConfigMonitor,
                issueConverter,
                threadHandling,
                logger);
        }
    }
}
