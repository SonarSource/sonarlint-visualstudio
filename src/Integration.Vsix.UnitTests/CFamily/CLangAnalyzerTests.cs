/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.Integration.UnitTests.CFamily;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.UnitTests
{
    [TestClass]
    public class CLangAnalyzerTests
    {
        private Mock<ITelemetryManager> telemetryManagerMock;
        private TestLogger testLogger;
        private Mock<ICFamilyRulesConfigProvider> rulesConfigProviderMock;
        private Mock<IServiceProvider> serviceProviderWithValidProjectItem;

        private readonly ProjectItem ValidProjectItem = Mock.Of<ProjectItem>();

        [TestInitialize]
        public void TestInitialize()
        {
            telemetryManagerMock = new Mock<ITelemetryManager>();
            testLogger = new TestLogger();
            rulesConfigProviderMock = new Mock<ICFamilyRulesConfigProvider>();
            serviceProviderWithValidProjectItem = CreateServiceProviderReturningProjectItem(ValidProjectItem);
        }

        [TestMethod]
        public void IsSupported()
        {
            var testSubject = new CLangAnalyzer(telemetryManagerMock.Object, new ConfigurableSonarLintSettings(),
                rulesConfigProviderMock.Object, serviceProviderWithValidProjectItem.Object, testLogger);

            testSubject.IsAnalysisSupported(new[] { AnalysisLanguage.CFamily }).Should().BeTrue();
            testSubject.IsAnalysisSupported(new[] { AnalysisLanguage.Javascript }).Should().BeFalse();
            testSubject.IsAnalysisSupported(new[] { AnalysisLanguage.Javascript, AnalysisLanguage.CFamily }).Should().BeTrue();
        }

        [TestMethod]
        public void ExecuteAnalysis_MissingProjectItem_NoAnalysis()
        {
            serviceProviderWithValidProjectItem = CreateServiceProviderReturningProjectItem(null);

            var testSubject = new TestableCLangAnalyzer(telemetryManagerMock.Object, new ConfigurableSonarLintSettings(),
                rulesConfigProviderMock.Object, serviceProviderWithValidProjectItem.Object, testLogger);

            testSubject.ExecuteAnalysis("path", "charset", new[] { AnalysisLanguage.CFamily }, Mock.Of<IIssueConsumer>(), null, CancellationToken.None);

            testSubject.CreateRequestCallCount.Should().Be(0);
            testSubject.TriggerAnalysisCallCount.Should().Be(0);
        }

        [TestMethod]
        public void ExecuteAnalysis_ValidProjectItem_RequestCannotBeCreated_NoAnalysis()
        {
            var testSubject = new TestableCLangAnalyzer(telemetryManagerMock.Object, new ConfigurableSonarLintSettings(),
                rulesConfigProviderMock.Object, serviceProviderWithValidProjectItem.Object, testLogger);
            testSubject.RequestToReturn = null;

            testSubject.ExecuteAnalysis("path", "charset", new[] { AnalysisLanguage.CFamily }, Mock.Of<IIssueConsumer>(), null, CancellationToken.None);

            testSubject.CreateRequestCallCount.Should().Be(1);
            testSubject.TriggerAnalysisCallCount.Should().Be(0);
        }

        [TestMethod]
        public void ExecuteAnalysis_ValidProjectItem_RequestCanBeCreated_AnalysisIsTriggered()
        {
            var testSubject = new TestableCLangAnalyzer(telemetryManagerMock.Object, new ConfigurableSonarLintSettings(),
                rulesConfigProviderMock.Object, serviceProviderWithValidProjectItem.Object, testLogger);
            testSubject.RequestToReturn = new Request();

            testSubject.ExecuteAnalysis("path", "charset", new[] { AnalysisLanguage.CFamily }, Mock.Of<IIssueConsumer>(), null, CancellationToken.None);

            testSubject.CreateRequestCallCount.Should().Be(1);
            testSubject.TriggerAnalysisCallCount.Should().Be(1);
        }

        [TestMethod]
        public void TriggerAnalysisAsync_StreamsIssuesFromSubProcessToConsumer()
        {
            const string fileName = "c:\\data\\aaa\\bbb\file.txt";
            var rulesConfig = new DummyCFamilyRulesConfig("c")
                .AddRule("rule1", isActive: true)
                .AddRule("rule2", isActive: true);

            var request = new Request
            {
                File = fileName,
                RulesConfiguration = rulesConfig
            };

            var message1 = new Message("rule1", fileName, 1, 1, 1, 1, "message one", false, Array.Empty<MessagePart>());
            var message2 = new Message("rule2", fileName, 2, 2, 2, 2, "message two", false, Array.Empty<MessagePart>());

            var mockConsumer = new Mock<IIssueConsumer>();
            var subProcess = new SubProcessSimulator();

            var testSubject = new TestableCLangAnalyzer(telemetryManagerMock.Object, new ConfigurableSonarLintSettings(),
                rulesConfigProviderMock.Object, serviceProviderWithValidProjectItem.Object, testLogger);
            testSubject.SetCallSubProcessBehaviour(subProcess.CallSubProcess);

            try
            {
                // Call the CLangAnalyzer on another thread (that thread is blocked by subprocess wrapper)
                var analysisTask = Task.Run(() => testSubject.TriggerAnalysisAsync(request, mockConsumer.Object, CancellationToken.None));
                subProcess.WaitUntilSubProcessCalledByAnalyzer();

                // Stream the first message to the analyzer
                subProcess.PassMessageToCLangAnalyzer(message1);

                mockConsumer.Verify(x => x.Accept(fileName, It.IsAny<IEnumerable<IAnalysisIssue>>()), Times.Once);
                var suppliedIssues = (IEnumerable<IAnalysisIssue>)mockConsumer.Invocations[0].Arguments[1];
                suppliedIssues.Count().Should().Be(1);
                suppliedIssues.First().Message.Should().Be("message one");

                // Stream the second message to the analyzer
                subProcess.PassMessageToCLangAnalyzer(message2);

                mockConsumer.Verify(x => x.Accept(fileName, It.IsAny<IEnumerable<IAnalysisIssue>>()), Times.Exactly(2));
                suppliedIssues = (IEnumerable<IAnalysisIssue>)mockConsumer.Invocations[1].Arguments[1];
                suppliedIssues.Count().Should().Be(1);
                suppliedIssues.First().Message.Should().Be("message two");

                // Tell the subprocess mock there are no more messages and wait for the analyzer method to complete
                subProcess.SignalNoMoreIssues();
                bool succeeded = analysisTask.Wait(10000);
                succeeded.Should().BeTrue();

                testLogger.AssertOutputStringExists($"Found 2 issue(s) for {fileName}");
            }
            finally
            {
                // Unblock the subprocess wrapper in case of errors so it can finish
                subProcess.SignalNoMoreIssues();
            }
        }

        [TestMethod]
        public void TriggerAnalysisAsync_IssuesForInactiveRulesAreNotStreamed()
        {
            const string fileName = "c:\\data\\aaa\\bbb\file.txt";
            var rulesConfig = new DummyCFamilyRulesConfig("c")
                .AddRule("inactiveRule", isActive: false)
                .AddRule("activeRule", isActive: true);

            var request = new Request
            {
                File = fileName,
                RulesConfiguration = rulesConfig
            };

            var inactiveRuleMessage = new Message("inactiveRule", fileName, 1, 1, 1, 1, "inactive message", false, Array.Empty<MessagePart>());
            var activeRuleMessage = new Message("activeRule", fileName, 2, 2, 2, 2, "active message", false, Array.Empty<MessagePart>());

            var mockConsumer = new Mock<IIssueConsumer>();
            var subProcess = new SubProcessSimulator();

            var testSubject = new TestableCLangAnalyzer(telemetryManagerMock.Object, new ConfigurableSonarLintSettings(),
                rulesConfigProviderMock.Object, serviceProviderWithValidProjectItem.Object, testLogger);
            testSubject.SetCallSubProcessBehaviour(subProcess.CallSubProcess);

            try
            {
                // Call the CLangAnalyzer on another thread (that thread is blocked by subprocess wrapper)
                var analysisTask = Task.Run(() => testSubject.TriggerAnalysisAsync(request, mockConsumer.Object, CancellationToken.None));
                subProcess.WaitUntilSubProcessCalledByAnalyzer();

                // Stream the inactive rule message to the analyzer
                subProcess.PassMessageToCLangAnalyzer(inactiveRuleMessage);
                mockConsumer.Verify(x => x.Accept(fileName, It.IsAny<IEnumerable<IAnalysisIssue>>()), Times.Never);

                // Now stream an active rule message
                subProcess.PassMessageToCLangAnalyzer(activeRuleMessage);

                mockConsumer.Verify(x => x.Accept(fileName, It.IsAny<IEnumerable<IAnalysisIssue>>()), Times.Once);
                var suppliedIssues = (IEnumerable<IAnalysisIssue>)mockConsumer.Invocations[0].Arguments[1];
                suppliedIssues.Count().Should().Be(1);
                suppliedIssues.First().Message.Should().Be("active message");

                // Tell the subprocess mock there are no more messages and wait for the analyzer method to complete
                subProcess.SignalNoMoreIssues();
                bool succeeded = analysisTask.Wait(10000);
                succeeded.Should().BeTrue();

                testLogger.AssertOutputStringExists($"Found 1 issue(s) for {fileName}");
            }
            finally
            {
                // Unblock the subprocess wrapper in case of errors so it can finish
                subProcess.SignalNoMoreIssues();
            }
        }

        [TestMethod]
        public void TriggerAnalysisAsync_AnalysisIsCancelled_NoIssuesFoundMessage()
        {
            var mockConsumer = new Mock<IIssueConsumer>();
            var subProcess = new SubProcessSimulator();

            var testSubject = new TestableCLangAnalyzer(telemetryManagerMock.Object, new ConfigurableSonarLintSettings(),
                rulesConfigProviderMock.Object, serviceProviderWithValidProjectItem.Object, testLogger);
            testSubject.SetCallSubProcessBehaviour(subProcess.CallSubProcess);

            using var cts = new CancellationTokenSource();
            try
            {
                // Call the CLangAnalyzer on another thread (that thread is blocked by subprocess wrapper)
                var analysisTask = Task.Run(() => testSubject.TriggerAnalysisAsync(new Request(), mockConsumer.Object, cts.Token));
                subProcess.WaitUntilSubProcessCalledByAnalyzer();

                cts.Cancel();

                // Tell the subprocess mock there are no more messages and wait for the analyzer method to complete
                subProcess.SignalNoMoreIssues();
                bool succeeded = analysisTask.Wait(10000);
                succeeded.Should().BeTrue();

                testLogger.AssertPartialOutputStringDoesNotExist("Found");
            }
            finally
            {
                // Unblock the subprocess wrapper in case of errors so it can finish
                subProcess.SignalNoMoreIssues();
            }
        }

        private static Mock<IServiceProvider> CreateServiceProviderReturningProjectItem(ProjectItem projectItemToReturn)
        {
            var mockSolution = new Mock<Solution>();
            mockSolution.Setup(s => s.FindProjectItem(It.IsAny<string>())).Returns(projectItemToReturn);
            var solution = mockSolution.Object;

            var mockDTE = new Mock<DTE>();
            mockDTE.Setup(d => d.Solution).Returns(solution);
            var dte = mockDTE.Object;

            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(s => s.GetService(typeof(DTE))).Returns(dte);

            return mockServiceProvider;
        }

        private class TestableCLangAnalyzer : CLangAnalyzer
        {
            public delegate void HandleCallSubProcess(Action<Message> handleMessage, Request request,
                ISonarLintSettings settings, ILogger logger, CancellationToken cancellationToken);

            private HandleCallSubProcess onCallSubProcess;
            public void SetCallSubProcessBehaviour(HandleCallSubProcess onCallSubProcess)
                => this.onCallSubProcess = onCallSubProcess;

            public Request RequestToReturn { get; set; }
            public int CreateRequestCallCount { get; private set; }
            public int TriggerAnalysisCallCount { get; private set; }

            public TestableCLangAnalyzer(ITelemetryManager telemetryManager, ISonarLintSettings settings, ICFamilyRulesConfigProvider cFamilyRulesConfigProvider,
                IServiceProvider serviceProvider, ILogger logger)
                : base(telemetryManager, settings, cFamilyRulesConfigProvider, serviceProvider, logger)
            {}

            protected override Request CreateRequest(ILogger logger, ProjectItem projectItem, string absoluteFilePath, ICFamilyRulesConfigProvider cFamilyRulesConfigProvider, IAnalyzerOptions analyzerOptions)
            {
                CreateRequestCallCount++;
                return RequestToReturn;
            }

            protected override void TriggerAnalysis(Request request, IIssueConsumer consumer, CancellationToken cancellationToken)
            {
                TriggerAnalysisCallCount++;
            }

            protected override void CallSubProcess(Action<Message> handleMessage, Request request, ISonarLintSettings settings, ILogger logger, CancellationToken cancellationToken)
            {
                if (onCallSubProcess == null)
                {
                    base.CallSubProcess(handleMessage, request, settings, logger, cancellationToken);
                }
                else
                {
                    onCallSubProcess(handleMessage, request, settings, logger, cancellationToken);
                }
            }
        }

        private class SubProcessSimulator
        {
            private Action<Message> handleMessageCallback;

            private readonly AutoResetEvent callbackFromCLangReceived = new AutoResetEvent(false);
            private readonly AutoResetEvent noMoreIssues = new AutoResetEvent(false);

            public void CallSubProcess(Action<Message> handleMessage, Request request, ISonarLintSettings settings,
                ILogger logger, CancellationToken cancellationToken)
            {
                // When this method exits the analyzer will finish processing, so we need to
                // block until we we want that to happen.

                // Store the callback passed to us from the CLangAnalyzer
                handleMessageCallback = handleMessage;

                // Tell the calling test we're ready and the test can continue
                callbackFromCLangReceived.Set();

                // Block until the test tells us we can finish
                noMoreIssues.WaitOne();
            }

            public void WaitUntilSubProcessCalledByAnalyzer()
                => callbackFromCLangReceived.WaitOne(20000);

            public void PassMessageToCLangAnalyzer(Message message)
                => handleMessageCallback(message);

            public void SignalNoMoreIssues()
                => noMoreIssues.Set();
        }
    }
}
