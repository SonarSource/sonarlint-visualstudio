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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Security.SharedUI;
using SonarQube.Client;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests
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
                MefTestHelpers.CreateExport<IConfigurationProvider>(Mock.Of<IConfigurationProvider>())
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
            var showInBrowserAction = new Mock<Action<string>>();

            var testSubject = CreateTestSubject(sonarQubeService.Object, configurationProvider.Object, showInBrowserAction.Object);
            testSubject.ShowIssue("issue");

            sonarQubeService.Invocations.Count.Should().Be(0);
            showInBrowserAction.Invocations.Count.Should().Be(0);
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

            var showInBrowserAction = new Mock<Action<string>>();

            var testSubject = CreateTestSubject(sonarQubeService.Object, configurationProvider.Object, showInBrowserAction.Object);
            testSubject.ShowIssue(issueKey);

            showInBrowserAction.Verify(x=> x("http://localhost:123/expected/issue?id=1"));
        }

        private ShowInBrowserService CreateTestSubject(ISonarQubeService sonarQubeService = null,
            IConfigurationProvider configurationProvider = null,
            Action<string> showInBrowserAction = null)
        {
            sonarQubeService ??= Mock.Of<ISonarQubeService>();
            configurationProvider ??= Mock.Of<IConfigurationProvider>();
            showInBrowserAction ??= s => { };

            return new ShowInBrowserService(sonarQubeService, configurationProvider, showInBrowserAction);
        }
    }
}
