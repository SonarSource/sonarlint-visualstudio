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
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.Integration.UnitTests.CFamily;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.UnitTests
{
    [TestClass]
    public class CLangAnalyzerTests
    {
        private static readonly IIssueConsumer ValidIssueConsumer = Mock.Of<IIssueConsumer>();
        private static readonly IAnalysisStatusNotifier AnyStatusNotifier = Mock.Of<IAnalysisStatusNotifier>();

        [TestMethod]
        public void IsSupported()
        {
            var testSubject = new CLangAnalyzer(Mock.Of<ITelemetryManager>(),
                Mock.Of<ISonarLintSettings>(),
                Mock.Of<IAnalysisStatusNotifier>(),
                Mock.Of<ICFamilyIssueConverterFactory>(),
                Mock.Of<IRequestFactoryAggregate>(),
                Mock.Of<ILogger>());

            testSubject.IsAnalysisSupported(new[] { AnalysisLanguage.CFamily }).Should().BeTrue();
            testSubject.IsAnalysisSupported(new[] { AnalysisLanguage.Javascript }).Should().BeFalse();
            testSubject.IsAnalysisSupported(new[] { AnalysisLanguage.Javascript, AnalysisLanguage.CFamily }).Should().BeTrue();
        }

        [TestMethod]
        public async Task ExecuteAnalysis_RequestCannotBeCreated_NoAnalysis()
        {
            var analysisOptions = new CFamilyAnalyzerOptions();
            var requestFactory = CreateRequestFactory("path", analysisOptions, null);

            var testSubject = CreateTestableAnalyzer(requestFactory: requestFactory.Object);
            await testSubject.TriggerAnalysisAsync("path", new[] { AnalysisLanguage.CFamily }, ValidIssueConsumer, analysisOptions, AnyStatusNotifier, CancellationToken.None);

            requestFactory.Verify(x => x.TryCreateAsync("path", analysisOptions), Times.Once);

            // TODO - modify check to be more reliable
            Thread.Sleep(400); // delay in case the background thread has gone on to call the subprocess
            testSubject.SubProcessExecutedCount.Should().Be(0);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task ExecuteAnalysis_RequestCannotBeCreated_NotPCH_LogOutput(bool isNullOptions)
        {
            var analysisOptions = isNullOptions ? null : new CFamilyAnalyzerOptions { CreatePreCompiledHeaders = false };
            var requestFactory = CreateRequestFactory("path", analysisOptions, null);
            var testLogger = new TestLogger();

            var testSubject = CreateTestableAnalyzer(
                requestFactory: requestFactory.Object,
                logger: testLogger);

            await testSubject.TriggerAnalysisAsync("path", new[] { AnalysisLanguage.CFamily }, ValidIssueConsumer, analysisOptions, AnyStatusNotifier, CancellationToken.None);

            testLogger.AssertOutputStringExists(string.Format(CFamilyStrings.MSG_UnableToCreateConfig, "path"));
        }

        [TestMethod]
        public async Task ExecuteAnalysis_RequestCannotBeCreated_PCH_NoLogOutput()
        {
            var analysisOptions = new CFamilyAnalyzerOptions {CreatePreCompiledHeaders = true};
            var requestFactory = CreateRequestFactory("path", analysisOptions, null);
            var testLogger = new TestLogger();

            var testSubject = CreateTestableAnalyzer(
                requestFactory: requestFactory.Object,
                logger: testLogger);

            await testSubject.TriggerAnalysisAsync("path", new[] { AnalysisLanguage.CFamily }, ValidIssueConsumer, analysisOptions, AnyStatusNotifier, CancellationToken.None);

            testLogger.AssertNoOutputMessages();
        }

        [TestMethod]
        public async Task ExecuteAnalysis_RequestCanBeCreated_AnalysisIsTriggered()
        {
            var analysisOptions = new CFamilyAnalyzerOptions();
            var request = CreateRequest("path");
            var requestFactory = CreateRequestFactory("path", analysisOptions, request);


            var testSubject = CreateTestableAnalyzer(
                requestFactory: requestFactory.Object);

            await testSubject.TriggerAnalysisAsync("path", new[] { AnalysisLanguage.CFamily }, ValidIssueConsumer, analysisOptions, AnyStatusNotifier, CancellationToken.None);

            testSubject.SubProcessExecutedCount.Should().Be(1);
        }

        [TestMethod]
        public async Task TriggerAnalysisAsync_StreamsIssuesFromSubProcessToConsumer()
        {
            const string fileName = "c:\\data\\aaa\\bbb\\file.txt";
            var rulesConfig = new DummyCFamilyRulesConfig("c")
                .AddRule("rule1", isActive: true)
                .AddRule("rule2", isActive: true);

            var request = CreateRequest
            (
                file: fileName,
                rulesConfiguration: rulesConfig,
                language: rulesConfig.LanguageKey
            );
            var requestFactory = CreateRequestFactory(fileName, ValidAnalyzerOptions, request);

            var message1 = new Message("rule1", fileName, 1, 1, 1, 1, "message one", false, Array.Empty<MessagePart>());
            var message2 = new Message("rule2", fileName, 2, 2, 2, 2, "message two", false, Array.Empty<MessagePart>());

            var convertedMessage1 = Mock.Of<IAnalysisIssue>();
            var convertedMessage2 = Mock.Of<IAnalysisIssue>();

            var issueConverter = new Mock<ICFamilyIssueToAnalysisIssueConverter>();
            issueConverter
                .Setup(x => x.Convert(message1, request.Context.CFamilyLanguage, rulesConfig))
                .Returns(convertedMessage1);

            issueConverter
                .Setup(x => x.Convert(message2, request.Context.CFamilyLanguage, rulesConfig))
                .Returns(convertedMessage2);

            var issueConverterFactory = new Mock<ICFamilyIssueConverterFactory>();
            issueConverterFactory.Setup(x => x.Create()).Returns(issueConverter.Object);

            var mockConsumer = new Mock<IIssueConsumer>();
            var statusNotifier = new Mock<IAnalysisStatusNotifier>();

            var testSubject = CreateTestableAnalyzer(issueConverterFactory: issueConverterFactory.Object,
                requestFactory: requestFactory.Object);

            TestableCLangAnalyzer.HandleCallSubProcess subProcessOp = (handleMessage, _, _, _, _) =>
            {
                // NOTE: on a background thread so the assertions might be handled by the product code.
                // Must check testSubject.SubProcessCompleted on the "main" test thread.

                // Stream the first message to the analyzer
                handleMessage(message1);

                mockConsumer.Verify(x => x.Accept(fileName, It.IsAny<IEnumerable<IAnalysisIssue>>()), Times.Once);
                var suppliedIssues = (IEnumerable<IAnalysisIssue>)mockConsumer.Invocations[0].Arguments[1];
                suppliedIssues.Count().Should().Be(1);
                suppliedIssues.First().Should().Be(convertedMessage1);

                // Stream the second message to the analyzer
                handleMessage(message2);

                mockConsumer.Verify(x => x.Accept(fileName, It.IsAny<IEnumerable<IAnalysisIssue>>()), Times.Exactly(2));
                suppliedIssues = (IEnumerable<IAnalysisIssue>)mockConsumer.Invocations[1].Arguments[1];
                suppliedIssues.Count().Should().Be(1);
                suppliedIssues.First().Should().Be(convertedMessage2);
            };
            testSubject.SetCallSubProcessBehaviour(subProcessOp);

            await testSubject.TriggerAnalysisAsync(fileName, ValidDetectedLanguages, mockConsumer.Object, ValidAnalyzerOptions, statusNotifier.Object, CancellationToken.None);

            testSubject.SubProcessCompleted.Should().BeTrue();

            statusNotifier.Verify(x => x.AnalysisStarted(fileName), Times.Once);
            statusNotifier.Verify(x => x.AnalysisFinished(fileName, 2, It.IsAny<TimeSpan>()), Times.Once);
            statusNotifier.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task TriggerAnalysisAsync_AnalysisIsCancelled_NotifiesOfCancellation()
        {
            var mockConsumer = new Mock<IIssueConsumer>();
            var originalStatusNotifier = new Mock<IAnalysisStatusNotifier>();

            // Call the CLangAnalyzer on another thread (that thread is blocked by subprocess wrapper)
            var filePath = "c:\\test.cpp";
            var request = CreateRequest(filePath);
            var requestFactory = CreateRequestFactory(filePath, ValidAnalyzerOptions, request);

            var testSubject = CreateTestableAnalyzer(statusNotifier: originalStatusNotifier.Object,
                requestFactory: requestFactory.Object);

            using var cts = new CancellationTokenSource();

            TestableCLangAnalyzer.HandleCallSubProcess subProcessAction = (_, _, _, _, _) =>
            {
                cts.Cancel();
            };
            testSubject.SetCallSubProcessBehaviour(subProcessAction);

            // Expecting to use this status notifier, not the one supplied in the constructor
            var statusNotifier = new Mock<IAnalysisStatusNotifier>();

            await testSubject.TriggerAnalysisAsync(filePath, ValidDetectedLanguages, mockConsumer.Object, ValidAnalyzerOptions, statusNotifier.Object, cts.Token);

            testSubject.SubProcessCompleted.Should().BeTrue();

            statusNotifier.Verify(x=> x.AnalysisStarted(filePath), Times.Once);
            statusNotifier.Verify(x=> x.AnalysisCancelled(filePath), Times.Once);
            statusNotifier.VerifyNoOtherCalls();
            originalStatusNotifier.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public async Task TriggerAnalysisAsync_AnalysisFailsDueToException_NotifiesOfFailure()
        {
            void MockSubProcessCall(Action<Message> message, IRequest request, ISonarLintSettings settings, ILogger logger, CancellationToken token)
            {
                throw new NullReferenceException("test");
            }

            var statusNotifier = new Mock<IAnalysisStatusNotifier>();

            var filePath = "c:\\test.cpp";
            var request = CreateRequest(filePath);
            var requestFactory = CreateRequestFactory(filePath, ValidAnalyzerOptions, request);

            var testSubject = CreateTestableAnalyzer(requestFactory: requestFactory.Object);
            testSubject.SetCallSubProcessBehaviour(MockSubProcessCall);

            await testSubject.TriggerAnalysisAsync(filePath, ValidDetectedLanguages, ValidIssueConsumer, ValidAnalyzerOptions, statusNotifier.Object, CancellationToken.None);

            statusNotifier.Verify(x=> x.AnalysisStarted(filePath), Times.Once);
            statusNotifier.Verify(x=> x.AnalysisFailed(filePath, It.Is<NullReferenceException>(e => e.Message == "test")), Times.Once);
            statusNotifier.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task TriggerAnalysisAsync_AnalysisFailsDueToInternalMessage_NotifiesOfFailure()
        {
            const string fileName = "c:\\data\\aaa\\bbb\\file.txt";
            var request = CreateRequest(fileName);
            var requestFactory = CreateRequestFactory(fileName, ValidAnalyzerOptions, request);

            var internalErrorMessage = new Message("internal.UnexpectedFailure", "", 1, 1, 1, 1, "XXX Error in subprocess XXX", false, Array.Empty<MessagePart>());

            var issueConverterFactory = Mock.Of<ICFamilyIssueConverterFactory>();
            var mockConsumer = new Mock<IIssueConsumer>();
            var statusNotifier = new Mock<IAnalysisStatusNotifier>();

            var testSubject = CreateTestableAnalyzer(issueConverterFactory: issueConverterFactory,
                requestFactory: requestFactory.Object);

            TestableCLangAnalyzer.HandleCallSubProcess subProcessOp = (handleMessage, _, _, _, _) =>
            {
                handleMessage(internalErrorMessage);
            };
            testSubject.SetCallSubProcessBehaviour(subProcessOp);

            await testSubject.TriggerAnalysisAsync(fileName, ValidDetectedLanguages, mockConsumer.Object, ValidAnalyzerOptions, statusNotifier.Object, CancellationToken.None);

            testSubject.SubProcessCompleted.Should().BeTrue();

            statusNotifier.Verify(x => x.AnalysisStarted(fileName), Times.Once);
            statusNotifier.Verify(x => x.AnalysisFailed(fileName, CFamilyStrings.MSG_GenericAnalysisFailed), Times.Once);
            statusNotifier.VerifyNoOtherCalls();
        }

        private readonly AnalysisLanguage[] ValidDetectedLanguages = new[] { AnalysisLanguage.CFamily };
        private readonly CFamilyAnalyzerOptions ValidAnalyzerOptions = null;

        private static IRequest CreateRequest(string file = null, string language = null, ICFamilyRulesConfig rulesConfiguration = null)
        {
            var request = new Mock<IRequest>();
            var context = new RequestContext(language, rulesConfiguration, file, null, null);
            request.SetupGet(x => x.Context).Returns(context);
            return request.Object;
        }

        private static Mock<IRequestFactoryAggregate> CreateRequestFactory(string filePath, CFamilyAnalyzerOptions analysisOptions, IRequest request)
        {
            var factory = new Mock<IRequestFactoryAggregate>();
            factory.Setup(x => x.TryCreateAsync(filePath, analysisOptions))
                .Returns(Task.FromResult(request));
            return factory;
        }

        private static TestableCLangAnalyzer CreateTestableAnalyzer(ITelemetryManager telemetryManager = null,
            ISonarLintSettings settings = null,
            IAnalysisStatusNotifier statusNotifier = null,
            ICFamilyIssueConverterFactory issueConverterFactory = null,
            IRequestFactoryAggregate requestFactory = null,
            ILogger logger = null)
        {
            telemetryManager ??= Mock.Of<ITelemetryManager>();
            settings ??= new ConfigurableSonarLintSettings();
            statusNotifier ??= Mock.Of<IAnalysisStatusNotifier>();
            issueConverterFactory ??= Mock.Of<ICFamilyIssueConverterFactory>();
            requestFactory ??= Mock.Of<IRequestFactoryAggregate>();
            logger ??= new TestLogger();

            return new TestableCLangAnalyzer(telemetryManager, settings, statusNotifier, logger, issueConverterFactory, requestFactory);
        }

        private class TestableCLangAnalyzer : CLangAnalyzer
        {
            public delegate void HandleCallSubProcess(Action<Message> handleMessage, IRequest request, 
                ISonarLintSettings settings, ILogger logger, CancellationToken cancellationToken);

            private HandleCallSubProcess onCallSubProcess;

            public void SetCallSubProcessBehaviour(HandleCallSubProcess onCallSubProcess) =>
                this.onCallSubProcess = onCallSubProcess;

            public bool SubProcessCompleted { get; private set; }

            public int SubProcessExecutedCount { get; private set; }

            public TestableCLangAnalyzer(ITelemetryManager telemetryManager, ISonarLintSettings settings, 
                IAnalysisStatusNotifier analysisStatusNotifier, ILogger logger, 
                ICFamilyIssueConverterFactory cFamilyIssueConverterFactory, IRequestFactoryAggregate requestFactory)
                : base(telemetryManager, settings, analysisStatusNotifier, cFamilyIssueConverterFactory, requestFactory, logger)
            {}

            protected override void CallSubProcess(Action<Message> handleMessage, IRequest request,
                ISonarLintSettings settings, ILogger logger, CancellationToken cancellationToken)
            {
                SubProcessExecutedCount++;
                if (onCallSubProcess == null)
                {
                    base.CallSubProcess(handleMessage, request, settings, logger, cancellationToken);
                }
                else
                {
                    onCallSubProcess(handleMessage, request, settings, logger, cancellationToken);

                    // The sub process is executed on a separate thread, so any exceptions might be
                    // squashed by the product code. So, we'll set a flag to indicate whether it
                    // ran to completion.
                    SubProcessCompleted = true;
                }
            }
        }
    }
}
