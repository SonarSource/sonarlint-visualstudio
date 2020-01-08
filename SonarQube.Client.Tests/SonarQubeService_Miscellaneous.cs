/*
 * SonarQube Client
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
using System.Net.Http;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarQube.Client.Requests;

namespace SonarQube.Client.Tests
{
    [TestClass]
    public class SonarQubeService_Miscellaneous
    {
        [TestMethod]
        public void SonarQubeService_Ctor_ArgumentChecks()
        {
            Action action;

            var logger = new TestLogger();

            action = () => new SonarQubeService(null, new RequestFactory(logger), string.Empty, logger);
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("messageHandler");

            action = () => new SonarQubeService(new Mock<HttpClientHandler>().Object, null, string.Empty, logger);
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("requestFactory");

            action = () => new SonarQubeService(new Mock<HttpClientHandler>().Object, new RequestFactory(logger), null, logger);
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("userAgent");

            action = () => new SonarQubeService(new Mock<HttpClientHandler>().Object, new RequestFactory(logger), string.Empty, null);
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void SonarQubeService_GetOrganizationKeyForWebApiCalls()
        {
            // 1. Doesn't fail with missing keys
            CheckSameKeyReturnedAndNoLogOutput(null);
            CheckSameKeyReturnedAndNoLogOutput(string.Empty);

            // 2. No change for normal keys
            CheckSameKeyReturnedAndNoLogOutput("my.org.key");
            CheckSameKeyReturnedAndNoLogOutput("sonar.internal.testing.no.org.XXX");
            CheckSameKeyReturnedAndNoLogOutput("aaa.sonar.internal.testing.no.org");

            // 3. Special key is recognised
            CheckSpecialKeyIsRecognisedAndNullReturned("sonar.internal.testing.no.org");
            CheckSpecialKeyIsRecognisedAndNullReturned("SONAR.INTERNAL.TESTING.NO.ORG");
        }

        private static void CheckSameKeyReturnedAndNoLogOutput(string key)
        {
            var testLogger = new TestLogger();
            SonarQubeService.GetOrganizationKeyForWebApiCalls(key, testLogger).Should().Be(key);
            testLogger.DebugMessages.Should().HaveCount(0);
        }

        private static void CheckSpecialKeyIsRecognisedAndNullReturned(string key)
        {
            var testLogger = new TestLogger();
            SonarQubeService.GetOrganizationKeyForWebApiCalls(key, testLogger).Should().BeNull();
            testLogger.DebugMessages.Should().HaveCount(1);
            testLogger.DebugMessages[0].Should().Contain("sonar.internal.testing.no.org");
        }
    }
}
