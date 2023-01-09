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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class UnboundSolutionCheckerTests
    {
        private const string ServerProjectKey = "my proj";

        [TestMethod]
        public async Task IsBindingUpdateRequired_SettingsDoNotExist_True()
        {
            var exclusionsSettingStorage = SetupLocalExclusions(null);

            var testSubject = CreateTestSubject(exclusionsSettingStorage.Object);

            var result = await testSubject.IsBindingUpdateRequired(CancellationToken.None);

            result.Should().BeTrue();
            exclusionsSettingStorage.VerifyAll();
            exclusionsSettingStorage.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task IsBindingUpdateRequired_SettingsExist_ServerSettingsAreDifferent_True()
        {
            var localExclusions = new ServerExclusions(
                exclusions: new[] { "1" },
                globalExclusions: new[] { "2" },
                inclusions: new[] { "3" });

            var serverExclusions = new ServerExclusions(
                exclusions: new[] { "4" },
                globalExclusions: new[] { "5" },
                inclusions: new[] { "6" });

            var exclusionsSettingStorage = SetupLocalExclusions(localExclusions);
            var sonarQubeServer = SetupServerExclusions(serverExclusions);

            var testSubject = CreateTestSubject(exclusionsSettingStorage.Object, sonarQubeServer.Object);

            var result = await testSubject.IsBindingUpdateRequired(CancellationToken.None);

            result.Should().BeTrue();
            exclusionsSettingStorage.VerifyAll();
            exclusionsSettingStorage.VerifyNoOtherCalls();
            sonarQubeServer.VerifyAll();
            sonarQubeServer.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task IsBindingUpdateRequired_SettingsExist_ServerSettingsAreTheSame_False()
        {
            var localExclusions = new ServerExclusions(
                exclusions: new[] { "1" },
                globalExclusions: new[] { "2" },
                inclusions: new[] { "3" });

            var serverExclusions = new ServerExclusions(
                exclusions: new[] { "1" },
                globalExclusions: new[] { "2" },
                inclusions: new[] { "3" });

            var exclusionsSettingStorage = SetupLocalExclusions(localExclusions);
            var sonarQubeServer = SetupServerExclusions(serverExclusions);

            var testSubject = CreateTestSubject(exclusionsSettingStorage.Object, sonarQubeServer.Object);

            var result = await testSubject.IsBindingUpdateRequired(CancellationToken.None);

            result.Should().BeFalse();
            exclusionsSettingStorage.VerifyAll();
            exclusionsSettingStorage.VerifyNoOtherCalls();
            sonarQubeServer.VerifyAll();
            sonarQubeServer.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task IsBindingUpdateRequired_FailedToFetchServerSettings_False()
        {
            var exclusionsSettingStorage = new Mock<IExclusionSettingsStorage>();
            exclusionsSettingStorage
                .Setup(x => x.GetSettings())
                .Throws(new NotImplementedException("this is a test"));

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(exclusionsSettingStorage.Object, logger:logger);

            var result = await testSubject.IsBindingUpdateRequired(CancellationToken.None);

            result.Should().BeFalse();
            logger.AssertPartialOutputStringExists("this is a test");
        }

        [TestMethod]
        public void IsBindingUpdateRequired_FailedToFetchServerSettings_CriticalException_ExceptionNotCaught()
        {
            var exclusionsSettingStorage = new Mock<IExclusionSettingsStorage>();
            exclusionsSettingStorage
                .Setup(x => x.GetSettings())
                .Throws(new StackOverflowException());

            var testSubject = CreateTestSubject(exclusionsSettingStorage.Object);

            Func<Task> act = async () => await testSubject.IsBindingUpdateRequired(CancellationToken.None);

            act.Should().Throw<StackOverflowException>();
        }

        private UnboundSolutionChecker CreateTestSubject(IExclusionSettingsStorage exclusionsSettingStorage,
            ISonarQubeService sonarQubeService = null,
            ILogger logger = null)
        {
            logger ??= Mock.Of<ILogger>();
            var bindingConfigurationProvider = SetupBindingConfiguration();

            return new UnboundSolutionChecker(exclusionsSettingStorage, bindingConfigurationProvider.Object, sonarQubeService, logger);
        }

        private static Mock<IExclusionSettingsStorage> SetupLocalExclusions(ServerExclusions localExclusions)
        {
            var exclusionsSettingStorage = new Mock<IExclusionSettingsStorage>();
            exclusionsSettingStorage.Setup(x => x.GetSettings()).Returns(localExclusions);
            
            return exclusionsSettingStorage;
        }

        private static Mock<ISonarQubeService> SetupServerExclusions(ServerExclusions serverExclusions)
        {
            var sonarQubeServer = new Mock<ISonarQubeService>();
            sonarQubeServer.Setup(x => x.GetServerExclusions(ServerProjectKey, CancellationToken.None))
                .ReturnsAsync(serverExclusions);
           
            return sonarQubeServer;
        }

        private Mock<IConfigurationProvider> SetupBindingConfiguration()
        {
            var bindingConfiguration =
                new BindingConfiguration(
                    new BoundSonarQubeProject(new Uri("http://localhost"), ServerProjectKey, null),
                    SonarLintMode.Connected,
                    null);

            var bindingConfigProvider = new Mock<IConfigurationProvider>();
            bindingConfigProvider.Setup(x => x.GetConfiguration()).Returns(bindingConfiguration);

            return bindingConfigProvider;
        }
    }
}
