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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Helpers;
using SonarQube.Client;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Helpers
{
    [TestClass]
    public class ShowInBrowserServiceTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ShowInBrowserService, IShowInBrowserService>(null, new[]
            {
                MefTestHelpers.CreateExport<ISonarQubeService>(Mock.Of<ISonarQubeService>()),
                MefTestHelpers.CreateExport<IConfigurationProvider>(Mock.Of<IConfigurationProvider>()),
                MefTestHelpers.CreateExport<IVsBrowserService>(Mock.Of<IVsBrowserService>())
            });
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void ShowIssue_NullIssueKey_ArgumentNullException(string issueKey)
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.ShowIssue(issueKey);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("issueKey");
        }

        [TestMethod]
        public void ShowIssue_NotInConnectedMode_BrowserNotOpened()
        {
            var configurationProvider = new Mock<IConfigurationProvider>();
            configurationProvider.Setup(x => x.GetConfiguration()).Returns(BindingConfiguration.Standalone);

            var sonarQubeService = new Mock<ISonarQubeService>();
            var browserService = new Mock<IVsBrowserService>();

            var testSubject = CreateTestSubject(sonarQubeService.Object, configurationProvider.Object, browserService.Object);
            testSubject.ShowIssue("issue");

            sonarQubeService.Invocations.Count.Should().Be(0);
            browserService.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public void ShowIssue_InConnectedMode_BrowserOpened()
        {
            const string projectKey = "project key";
            const string issueKey = "issue key";

            var configurationProvider = new Mock<IConfigurationProvider>();
            configurationProvider
                .Setup(x => x.GetConfiguration())
                .Returns(new BindingConfiguration(new BoundSonarQubeProject{ProjectKey = projectKey}, SonarLintMode.Connected, null));

            var sonarQubeService = new Mock<ISonarQubeService>();
            sonarQubeService
                .Setup(x => x.GetViewIssueUrl(projectKey, issueKey))
                .Returns(new Uri("http://localhost:123/expected/issue?id=1"));

            var browserService = new Mock<IVsBrowserService>();

            var testSubject = CreateTestSubject(sonarQubeService.Object, configurationProvider.Object, browserService.Object);
            testSubject.ShowIssue(issueKey);

            browserService.Verify(x=> x.Navigate("http://localhost:123/expected/issue?id=1"), Times.Once);
            browserService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ShowDocumentation_BrowserOpened()
        {
            var browserService = new Mock<IVsBrowserService>();

            var testSubject = CreateTestSubject(browserService: browserService.Object);

            testSubject.ShowDocumentation();

            browserService.Verify(x => x.Navigate("https://github.com/SonarSource/sonarlint-visualstudio/wiki"), Times.Once());
            browserService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ShowRuleDescription_BrowserOpened()
        {
            const string ruleKey = "some key";
            const string ruleUrl = "some url";

            var helpLinkProvider = new Mock<IRuleHelpLinkProvider>();
            var browserService = new Mock<IVsBrowserService>();
            helpLinkProvider.Setup(x => x.GetHelpLink(ruleKey)).Returns(ruleUrl);

            var testSubject = CreateTestSubject(browserService: browserService.Object, helpLinkProvider: helpLinkProvider.Object);

            testSubject.ShowRuleDescription(ruleKey);

            browserService.Verify(x => x.Navigate(ruleUrl), Times.Once());
            browserService.VerifyNoOtherCalls();
        }

        private ShowInBrowserService CreateTestSubject(ISonarQubeService sonarQubeService = null,
            IConfigurationProvider configurationProvider = null,
            IVsBrowserService browserService = null,
            IRuleHelpLinkProvider helpLinkProvider = null)
        {
            sonarQubeService ??= Mock.Of<ISonarQubeService>();
            configurationProvider ??= Mock.Of<IConfigurationProvider>();
            browserService ??= Mock.Of<IVsBrowserService>();
            helpLinkProvider ??= Mock.Of<IRuleHelpLinkProvider>();

            return new ShowInBrowserService(sonarQubeService, configurationProvider, browserService, helpLinkProvider);
        }
    }
}
