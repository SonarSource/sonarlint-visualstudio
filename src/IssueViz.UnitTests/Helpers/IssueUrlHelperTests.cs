/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.IssueVisualization.Helpers;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Helpers
{
    [TestClass]
    public class IssueUrlHelperTests
    {
        [TestMethod]
        public void GetViewIssueUrl_ReturnsExpectedUrl() =>
            NewMethod(new ServerConnection.SonarQube(new Uri("http://localhost:9000/")), "http://localhost:9000/project/issues?id=projectKey0&issues=myIssue0&open=myIssue0", "projectKey0", "myIssue0");

        [TestMethod]
        public void GetViewIssueUrl_SonarQubeHttpsWithSubPath_ReturnsExpectedUrl() =>
            NewMethod(new ServerConnection.SonarQube(new Uri("https://next.sonarqube.com/next/")), "https://next.sonarqube.com/next/project/issues?id=projectKey1&issues=myIssue1&open=myIssue1", "projectKey1", "myIssue1");


        [TestMethod]
        public void GetViewIssueUrl_SonarCloud_ReturnsExpectedUrl() =>
            NewMethod(new ServerConnection.SonarCloud("any, not used", CloudServerRegion.Eu), "https://sonarcloud.io/project/issues?id=projectKey2&issues=myIssue2&open=myIssue2", "projectKey2", "myIssue2");

        [TestMethod]
        public void GetViewIssueUrl_SonarCloudUs_ReturnsExpectedUrl() =>
            NewMethod(new ServerConnection.SonarCloud("any, not used", CloudServerRegion.Us), "https://sonarqube.us/project/issues?id=projectKey3&issues=myIssue3&open=myIssue3", "projectKey3", "myIssue3");


        [TestMethod]
        public void GetViewIssueUrl_ThrowsOnNullProject()
        {
            Action act = () => IssueUrlHelper.GetViewIssueUrl(null, "issue");

            act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("boundServerProject");
        }

        [TestMethod]
        public void GetViewIssueUrl_ThrowsOnNullIssueKey()
        {
            var serverUri = new Uri("http://localhost:9000/");
            var serverConnection = new ServerConnection.SonarQube(serverUri);
            var boundProject = new BoundServerProject("localKey", "projectKey", serverConnection);

            Action act = () => IssueUrlHelper.GetViewIssueUrl(boundProject, null);

            act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("issueKey");
        }

        private static void NewMethod(
            ServerConnection serverConnection,
            string fullExpectedUri,
            string serverProjectKey,
            string issueKey)
        {
            var boundProject = new BoundServerProject("any", serverProjectKey, serverConnection);

            var result = IssueUrlHelper.GetViewIssueUrl(boundProject, issueKey);

            result.Should().NotBeNull();
            result.Host.Should().Be(serverConnection.ServerUri.Host);
            result.LocalPath.Should().EndWith("/project/issues");
            result.Query.Should().Be($"?id={serverProjectKey}&issues={issueKey}&open={issueKey}");
            result.ToString().Should().Be(fullExpectedUri);
        }
    }
}
