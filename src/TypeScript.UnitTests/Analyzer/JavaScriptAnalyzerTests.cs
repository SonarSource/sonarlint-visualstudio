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
using SonarLint.VisualStudio.Core.Analysis;
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
                MefTestHelpers.CreateExport<IEslintBridgeProcess>(Mock.Of<IEslintBridgeProcess>()),
                MefTestHelpers.CreateExport<IActiveJavaScriptRulesProvider>(Mock.Of<IActiveJavaScriptRulesProvider>()),
                MefTestHelpers.CreateExport<IJavaScriptRuleKeyMapper>(Mock.Of<IJavaScriptRuleKeyMapper>()),
                MefTestHelpers.CreateExport<IJavaScriptRuleDefinitionsProvider>(Mock.Of<IJavaScriptRuleDefinitionsProvider>()),
                MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>())
            });
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
        public async Task ExecuteAnalysis_AlwaysStartsEslintBridgeServer()
        {
            var serverProcess = SetupServerProcess(123);
            var client = SetupEslintBridgeClient(null);
            var clientFactory = SetupEslintBridgeClientFactory(123, client.Object);
            
            var testSubject = CreateTestSubject(clientFactory.Object, serverProcess.Object);

            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), CancellationToken.None);

            serverProcess.Verify(x => x.Start(), Times.Once);
            serverProcess.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ExecuteAnalysis_FailsToStartsEslintBridgeServer_ExceptionCaughtAndLogged()
        {
            var serverProcess = SetupServerProcess(exceptionToThrow: new NotImplementedException("this is a test"));
            var clientFactory = new Mock<IEslintBridgeClientFactory>();
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(clientFactory.Object, serverProcess.Object, logger: logger);

            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), CancellationToken.None);

            logger.AssertPartialOutputStringExists("this is a test");
            clientFactory.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ExecuteAnalysis_EslintBridgePortChanged_PreviousClientIsDisposed()
        {
            const int firstPort = 123;
            const int secondPort = 456;

            var serverProcess = new Mock<IEslintBridgeProcess>();
            serverProcess.SetupSequence(x => x.Start())
                .ReturnsAsync(firstPort)
                .ReturnsAsync(secondPort);

            var client1 = SetupEslintBridgeClient(null);
            var client2 = SetupEslintBridgeClient(null);

            var clientFactory = new Mock<IEslintBridgeClientFactory>();
            clientFactory.Setup(x => x.Create(firstPort)).Returns(client1.Object);
            clientFactory.Setup(x => x.Create(secondPort)).Returns(client2.Object);

            var testSubject = CreateTestSubject(clientFactory.Object, serverProcess.Object);

            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), CancellationToken.None);

            clientFactory.Verify(x=> x.Create(firstPort), Times.Once);
            clientFactory.Verify(x=> x.Create(secondPort), Times.Never);

            client1.Verify(x => x.Dispose(), Times.Never);
            client2.Invocations.Should().BeEmpty();

            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), CancellationToken.None);

            clientFactory.Verify(x => x.Create(secondPort), Times.Once);

            client1.Verify(x => x.Dispose(), Times.Once);
            client2.Verify(x => x.Dispose(), Times.Never);
        }

        [TestMethod]
        public async Task ExecuteAnalysis_EslintBridgePortUnchanged_DoesNotCreateNewClient()
        {
            const int port = 123;

            var serverProcess = SetupServerProcess(port);
            var client = SetupEslintBridgeClient(null);
            var clientFactory = SetupEslintBridgeClientFactory(port, client.Object);

            var testSubject = CreateTestSubject(clientFactory.Object, serverProcess.Object);

            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), CancellationToken.None);

            clientFactory.Verify(x => x.Create(port), Times.Once);
            clientFactory.Verify(x => x.Create(It.IsAny<int>()), Times.Once);

            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), CancellationToken.None);
           
            clientFactory.Verify(x => x.Create(port), Times.Once);
            clientFactory.Verify(x => x.Create(It.IsAny<int>()), Times.Once);
            client.Verify(x => x.Dispose(), Times.Never);
        }

        [TestMethod]
        public async Task ExecuteAnalysis_EslintBridgePortUnchanged_DoesNotCallInitLinterAgain()
        {
            var activeRules = new[] { new Rule { Key = "rule1" }, new Rule { Key = "rule2" } };
            var activeRulesProvider = SetupActiveRulesProvider(activeRules);
            var client = SetupEslintBridgeClient(null);

            var testSubject = CreateTestSubject(client.Object, activeRulesProvider: activeRulesProvider.Object);

            var cancellationToken = new CancellationToken();
            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), cancellationToken);

            client.VerifyAll();
            client.Verify(x=> x.InitLinter(activeRules, cancellationToken), Times.Once);
            client.VerifyNoOtherCalls();

            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), cancellationToken);

            client.VerifyAll();
            client.Verify(x => x.InitLinter(activeRules, cancellationToken), Times.Once);
            client.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ExecuteAnalysis_EslintBridgeClientCalledWithCorrectParams()
        {
            var activeRules = new[] { new Rule { Key = "rule1" }, new Rule { Key = "rule2" } };
            var activeRulesProvider = SetupActiveRulesProvider(activeRules);
            var eslintBridgeClient = SetupEslintBridgeClient(response: null);
            
            var testSubject = CreateTestSubject(eslintBridgeClient.Object, activeRulesProvider: activeRulesProvider.Object);

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
        public async Task ExecuteAnalysis_NonCriticalException_ExceptionCaughtAndLogged()
        {
            var eslintBridgeClient = new Mock<IEslintBridgeClient>();
            eslintBridgeClient
                .Setup(x => x.AnalyzeJs(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new NotImplementedException("this is a test"));

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(eslintBridgeClient.Object, logger: logger);
            await testSubject.ExecuteAnalysis("test", Mock.Of<IIssueConsumer>(), CancellationToken.None);

            logger.AssertPartialOutputStringExists("this is a test");
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
        public void Dispose_AnalysisNeverRan_NoException()
        {
            var serverProcess = SetupServerProcess(123);
            var client = SetupEslintBridgeClient(null);
            var clientFactory = SetupEslintBridgeClientFactory(123, client.Object);

            var testSubject = CreateTestSubject(clientFactory.Object, serverProcess.Object);

            Action act = () => testSubject.Dispose();
            act.Should().NotThrow();

            client.VerifyNoOtherCalls();
            serverProcess.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        public async Task Dispose_AnalysisRan_DisposesServerProcess()
        {
            var serverProcess = SetupServerProcess(123);
            var client = SetupEslintBridgeClient(null);
            var clientFactory = SetupEslintBridgeClientFactory(123, client.Object);

            var testSubject = CreateTestSubject(clientFactory.Object, serverProcess.Object);

            await testSubject.ExecuteAnalysis("some path", Mock.Of<IIssueConsumer>(), CancellationToken.None);
            testSubject.Dispose();

            client.Verify(x => x.Dispose(), Times.Once);
            serverProcess.Verify(x => x.Dispose(), Times.Once);
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

        private Mock<IEslintBridgeClient> SetupEslintBridgeClient(AnalysisResponse response)
        {
            var eslintBridgeClient = new Mock<IEslintBridgeClient>();
            eslintBridgeClient
                .Setup(x => x.AnalyzeJs(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            return eslintBridgeClient;
        }

        private Mock<IEslintBridgeProcess> SetupServerProcess(int port = 123, Exception exceptionToThrow = null)
        {
            var serverProcess = new Mock<IEslintBridgeProcess>();
            var setup = serverProcess.Setup(x => x.Start());

            if (exceptionToThrow == null)
            {
                setup.ReturnsAsync(port);
            }
            else
            {
                setup.ThrowsAsync(exceptionToThrow);
            }

            return serverProcess;
        }

        private static Mock<IActiveJavaScriptRulesProvider> SetupActiveRulesProvider(Rule[] activeRules)
        {
            var rulesProvider = new Mock<IActiveJavaScriptRulesProvider>();
            rulesProvider.Setup(x => x.Get()).Returns(activeRules);

            return rulesProvider;
        }

        private Mock<IEslintBridgeClientFactory> SetupEslintBridgeClientFactory(int port, IEslintBridgeClient client)
        {
            var factory = new Mock<IEslintBridgeClientFactory>();
            factory.Setup(x => x.Create(port)).Returns(client);

            return factory;
        }

        private JavaScriptAnalyzer CreateTestSubject(IEslintBridgeClient client = null, 
            IEslintBridgeIssueConverter issueConverter = null,
            IActiveJavaScriptRulesProvider activeRulesProvider = null,
            ILogger logger = null)
        {
            client ??= SetupEslintBridgeClient(null).Object;
            activeRulesProvider ??= Mock.Of<IActiveJavaScriptRulesProvider>();

            var serverProcess = SetupServerProcess(123);
            var eslintBridgeClientFactory = SetupEslintBridgeClientFactory(123, client);

            return CreateTestSubject(eslintBridgeClientFactory.Object, serverProcess.Object, activeRulesProvider, issueConverter, logger);
        }

        private JavaScriptAnalyzer CreateTestSubject(IEslintBridgeClientFactory eslintBridgeClientFactory,
            IEslintBridgeProcess eslintBridgeProcess,
            IActiveJavaScriptRulesProvider activeRulesProvider = null,
            IEslintBridgeIssueConverter issueConverter = null,
            ILogger logger = null)
        {
            issueConverter ??= Mock.Of<IEslintBridgeIssueConverter>();
            logger ??= Mock.Of<ILogger>();

            return new JavaScriptAnalyzer(eslintBridgeClientFactory, eslintBridgeProcess, activeRulesProvider, issueConverter, logger);
        }
    }
}
