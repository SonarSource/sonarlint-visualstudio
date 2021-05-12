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
using SonarLint.VisualStudio.TypeScript.Analyzer;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;
using SonarLint.VisualStudio.TypeScript.Rules;

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
        public async Task Analyze_Success_ReturnsAnalysisResponse()
        {
            var response = new AnalysisResponse { Issues = new List<Issue> { new Issue() } };
            var client = SetupEslintBridgeClient(response);

            var testSubject = CreateTestSubject(client.Object);
            var result = await testSubject.Analyze("some path", "some config", CancellationToken.None);

            result.Should().Be(response);
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
            var client = SetupEslintBridgeClient(new AnalysisResponse { Issues = Enumerable.Empty<Issue>() });

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
        public async Task Analyze_EslintBridgeClientNotInitializedException_CallsInitLinter()
        {
            var validResponse = new AnalysisResponse { Issues = new List<Issue>() };
            var client = new Mock<IEslintBridgeClient>();

            // First analysis: response is valid, InitLinter should be called once (first initialization)
            // Second analysis: response is still valid, InitLinter should not be called
            // Third analysis: response is invalid, InitLinter should be called. Next response is valid, InitLinter not should be called.
            // Forth analysis: because previous response succeeded, init InitLinter should not be called
            client
                .SetupSequence(x => x.Analyze("some path", "some config", CancellationToken.None))
                .ReturnsAsync(validResponse)
                .ReturnsAsync(validResponse)
                .ThrowsAsync(new EslintBridgeClientNotInitializedException())
                .ReturnsAsync(validResponse)
                .ReturnsAsync(validResponse);

            var testSubject = CreateTestSubject(client.Object);

            await testSubject.Analyze("some path", "some config", CancellationToken.None);
            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Once);

            await testSubject.Analyze("some path", "some config", CancellationToken.None);
            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Once);

            await testSubject.Analyze("some path", "some config", CancellationToken.None);
            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

            await testSubject.Analyze("some path", "some config", CancellationToken.None);
            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task Analyze_EslintBridgeClientCalledWithCorrectParams()
        {
            var activeRules = new[] { new Rule { Key = "rule1" }, new Rule { Key = "rule2" } };
            var activeRulesProvider = SetupActiveRulesProvider(activeRules);
            var eslintBridgeClient = SetupEslintBridgeClient(response: null);

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
        public async Task OnSolutionChanged_NextAnalysisCallsInitLinter()
        {
            var activeSolutionTracker = SetupActiveSolutionTracker();
            var client = SetupEslintBridgeClient(new AnalysisResponse { Issues = Enumerable.Empty<Issue>() });

            var testSubject = CreateTestSubject(client.Object, activeSolutionTracker: activeSolutionTracker.Object);

            await testSubject.Analyze("some path", "some config", CancellationToken.None);
            await testSubject.Analyze("some path", "some config", CancellationToken.None);

            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Once());

            activeSolutionTracker.Raise(x => x.ActiveSolutionChanged += null, new ActiveSolutionChangedEventArgs(true));

            await testSubject.Analyze("some path", "some config", CancellationToken.None);

            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public void OnSolutionChanged_StopsEslintBridgeClient()
        {
            var activeSolutionTracker = SetupActiveSolutionTracker();
            var client = SetupEslintBridgeClient(new AnalysisResponse { Issues = Enumerable.Empty<Issue>() });

            CreateTestSubject(client.Object, activeSolutionTracker: activeSolutionTracker.Object);

            activeSolutionTracker.Raise(x => x.ActiveSolutionChanged += null, new ActiveSolutionChangedEventArgs(true));

            client.Verify(x => x.Close(), Times.Once);
        }

        [TestMethod]
        public async Task OnConfigChanged_NextAnalysisCallsInitLinter()
        {
            var analysisConfigMonitor = SetupAnalysisConfigMonitor();
            var client = SetupEslintBridgeClient(new AnalysisResponse { Issues = Enumerable.Empty<Issue>() });

            var testSubject = CreateTestSubject(client.Object, analysisConfigMonitor: analysisConfigMonitor.Object);

            await testSubject.Analyze("some path", "some config", CancellationToken.None);
            await testSubject.Analyze("some path", "some config", CancellationToken.None);

            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Once());

            analysisConfigMonitor.Raise(x => x.ConfigChanged += null, EventArgs.Empty);

            await testSubject.Analyze("some path", "some config", CancellationToken.None);

            client.Verify(x => x.InitLinter(It.IsAny<IEnumerable<Rule>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public void OnConfigChanged_StopsEslintBridgeClient()
        {
            var analysisConfigMonitor = SetupAnalysisConfigMonitor();
            var client = SetupEslintBridgeClient(new AnalysisResponse { Issues = Enumerable.Empty<Issue>() });

            CreateTestSubject(client.Object, analysisConfigMonitor: analysisConfigMonitor.Object);

            analysisConfigMonitor.Raise(x => x.ConfigChanged += null, EventArgs.Empty);

            client.Verify(x => x.Close(), Times.Once);
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
            IAnalysisConfigMonitor analysisConfigMonitor = null)
        {
            eslintBridgeClient ??= SetupEslintBridgeClient().Object;
            rulesProvider ??= Mock.Of<IRulesProvider>();
            activeSolutionTracker ??= Mock.Of<IActiveSolutionTracker>();
            analysisConfigMonitor ??= Mock.Of<IAnalysisConfigMonitor>();

            return new EslintBridgeAnalyzer(rulesProvider,
                eslintBridgeClient,
                activeSolutionTracker,
                analysisConfigMonitor);
        }
    }
}
