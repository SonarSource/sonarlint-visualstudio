/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests
{
    [TestClass]
    public class SonarQubeService_GetIssuesForComponent : SonarQubeService_GetIssuesBase
    {
        [TestMethod]
        public void GetIssuesForComponentAsync_NotConnected()
        {
            // No calls to Connect
            // No need to setup request, the operation should fail

            Func<Task<IList<SonarQubeIssue>>> func = async () =>
                await service.GetIssuesForComponentAsync("simplcom", null, null, null, CancellationToken.None);

            func.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().Be("This operation expects the service to be connected.");

            logger.ErrorMessages.Should().Contain("The service is expected to be connected.");
        }

        [TestMethod]
        public async Task GetIssuesForComponentAsync_Connected_ReturnsExpexted()
        {
            await ConnectToSonarQube("9.9.0.0");

            string projectKey = "projectKey";
            string branch = "branch";
            string componentKey = "componentKey";
            string ruleId = "ruleId";

            SetupPageOfResponses(projectKey, ruleId, componentKey, branch, 1, 1, "CODE_SMELL");
            SetupPageOfResponses(projectKey, ruleId, componentKey, branch, 1, 1, "BUG");
            SetupPageOfResponses(projectKey, ruleId, componentKey, branch, 1, 1, "VULNERABILITY");

            var result = await service.GetIssuesForComponentAsync(projectKey, branch, componentKey, ruleId, CancellationToken.None);

            result.Should().HaveCount(3);
        }

        [TestMethod]
        public async Task GetIssuesForComponentAsync_MaxIssues_ShowsLog()
        {
            await ConnectToSonarQube("9.9.0.0");

            string projectKey = "projectKey";
            string branch = "branch";
            string componentKey = "componentKey";
            string ruleId = "ruleId";

            SetupPagesOfResponses(projectKey, ruleId, componentKey, branch, MaxAllowedIssues, "CODE_SMELL");
            SetupPagesOfResponses(projectKey, ruleId, componentKey, branch, MaxAllowedIssues, "BUG");
            SetupPagesOfResponses(projectKey, ruleId, componentKey, branch, MaxAllowedIssues, "VULNERABILITY");

            var result = await service.GetIssuesForComponentAsync(projectKey, branch, componentKey, ruleId, CancellationToken.None);

            result.Should().HaveCount(MaxAllowedIssues * 3);

            DumpWarningsToConsole();

            messageHandler.VerifyAll();

            checkForExpectedWarning(MaxAllowedIssues, "code smells");
            checkForExpectedWarning(MaxAllowedIssues, "bugs");
            checkForExpectedWarning(MaxAllowedIssues, "vulnerabilities");
        }
    }
}
