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
using System.Threading;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.UnitTests;

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
        }
    }
}
