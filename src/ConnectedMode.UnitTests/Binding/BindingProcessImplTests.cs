/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.ConnectedMode.QualityProfiles;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Models;
using SonarQube.Client.Helpers;
using System.Security;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Binding
{
    [TestClass]
    public class BindingProcessImplTests
    {
        #region Tests

        [TestMethod]
        public void Ctor_ArgChecks()
        {
            var bindingArgs = CreateBindCommandArgs(connection: new ConnectionInformation(new Uri("http://server")));
            var exclusionSettingsStorage = Mock.Of<IExclusionSettingsStorage>();
            var qpDownloader = Mock.Of<IQualityProfileDownloader>();
            var sonarQubeService = Mock.Of<ISonarQubeService>();
            var logger = Mock.Of<ILogger>();

            // 1. Null binding args
            Action act = () => new BindingProcessImpl(null, exclusionSettingsStorage, sonarQubeService, qpDownloader, logger);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("bindingArgs");

            // 2. Null exclusion settings storage
            act = () => new BindingProcessImpl(bindingArgs, null, sonarQubeService, qpDownloader, logger);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("exclusionSettingsStorage");

            // 3. Null SonarQube service
            act = () => new BindingProcessImpl(bindingArgs, exclusionSettingsStorage, null, qpDownloader, logger);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("sonarQubeService");

            // 4. Null QP downloader
            act = () => new BindingProcessImpl(bindingArgs, exclusionSettingsStorage, sonarQubeService, null, logger);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("qualityProfileDownloader");

            // 5. Null logger
            act = () => new BindingProcessImpl(bindingArgs, exclusionSettingsStorage, sonarQubeService, qpDownloader, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public async Task SaveServerExclusionsAsync_ReturnsTrue()
        {
            var bindingArgs = CreateBindCommandArgs(projectKey: "projectKey");
            var logger = new TestLogger(logToConsole: true);

            ServerExclusions settings = CreateSettings();

            var sonarQubeService = new Mock<ISonarQubeService>();
            sonarQubeService.Setup(s => s.GetServerExclusions("projectKey", It.IsAny<CancellationToken>())).Returns(Task.FromResult(settings));

            var exclusionSettingsStorage = new Mock<IExclusionSettingsStorage>();

            var testSubject = CreateTestSubject(bindingArgs: bindingArgs,
                sonarQubeService: sonarQubeService.Object,
                exclusionSettingsStorage: exclusionSettingsStorage.Object,
                logger: logger);

            await testSubject.SaveServerExclusionsAsync(CancellationToken.None);

            exclusionSettingsStorage.Verify(fs => fs.SaveSettings(settings), Times.Once);
            logger.AssertOutputStrings(0);
        }

        [TestMethod]
        public async Task SaveServerExclusionsAsync_HasError_ReturnsFalse()
        {
            var logger = new TestLogger(logToConsole: true);
            var bindingArgs = CreateBindCommandArgs(projectKey: "projectKey");

            var sonarQubeService = new Mock<ISonarQubeService>();
            sonarQubeService.Setup(s => s.GetServerExclusions("projectKey", It.IsAny<CancellationToken>())).Throws(new Exception("Expected Error"));

            var testSubject = CreateTestSubject(
                bindingArgs: bindingArgs,
                sonarQubeService: sonarQubeService.Object,
                logger: logger);

            var result = await testSubject.SaveServerExclusionsAsync(CancellationToken.None);

            result.Should().BeFalse();
            logger.AssertOutputStrings(1);
            logger.AssertPartialOutputStrings("Expected Error");
        }

        [TestMethod]
        public void SaveServerExclusionsAsync_HasCriticalError_Throws()
        {
            var logger = new TestLogger(logToConsole: true);
            var bindingArgs = CreateBindCommandArgs(projectKey: "projectKey");

            var sonarQubeService = new Mock<ISonarQubeService>();
            sonarQubeService.Setup(s => s.GetServerExclusions("projectKey", It.IsAny<CancellationToken>())).Throws(new StackOverflowException("Critical Error"));

            var testSubject = CreateTestSubject(bindingArgs: bindingArgs,
                sonarQubeService: sonarQubeService.Object,
                logger: logger);

            Func<Task<bool>> act = async () => await testSubject.SaveServerExclusionsAsync(CancellationToken.None);

            act.Should().ThrowExactly<StackOverflowException>().WithMessage("Critical Error");
            logger.AssertOutputStrings(0);
        }

        private static ServerExclusions CreateSettings()
        {
            return new ServerExclusions
            {
                Inclusions = new string[] { "inclusion1", "inclusion2" },
                Exclusions = new string[] { "exclusion" },
                GlobalExclusions = new string[] { "globalExclusion" }
            };
        }

        [TestMethod]
        public async Task DownloadQualityProfile_CreatesBoundProjectAndCallsQPDownloader()
        {
            var qpDownloader = new Mock<IQualityProfileDownloader>();
            var progress = Mock.Of<IProgress<FixedStepsProgress>>();

            var connectionInfo = new ConnectionInformation(new Uri("http://theServer"));
            connectionInfo.Organization = new SonarQubeOrganization("the org key", "the org name");
            var bindingArgs = CreateBindCommandArgs("the project key", "the project name", connectionInfo);

            var testSubject = CreateTestSubject(bindingArgs,
                qpDownloader: qpDownloader.Object);

            // Act
            var result = await testSubject.DownloadQualityProfileAsync(progress, CancellationToken.None);

            result.Should().BeTrue();
            
            qpDownloader.Verify(x => x.UpdateAsync(It.IsAny<BoundSonarQubeProject>(), progress, It.IsAny<CancellationToken>()),
                Times.Once);

            var actualProject = (BoundSonarQubeProject)qpDownloader.Invocations[0].Arguments[0];

            // Check the bound project was correctly constructed from the BindCommandArgs
            actualProject.Should().NotBeNull();
            actualProject.ServerUri.Should().Be("http://theServer");
            actualProject.ProjectKey.Should().Be("the project key");
            actualProject.ProjectName.Should().Be("the project name");
            actualProject.Organization.Key.Should().Be("the org key");
            actualProject.Organization.Name.Should().Be("the org name");
        }

        [TestMethod]
        [DataRow("the user name", null)]
        [DataRow("the user name", "a password")]
        [DataRow(null, null)]
        [DataRow(null, "should be ignored")]
        public async Task DownloadQualityProfile_HandlesBoundProjectCredentialsCorrectly(string userName, string rawPassword)
        {
            var qpDownloader = new Mock<IQualityProfileDownloader>();
            var password = rawPassword == null ?  null : rawPassword.ToSecureString();

            var connectionInfo = new ConnectionInformation(new Uri("http://any"), userName, password);
            var bindingArgs = CreateBindCommandArgs(connection: connectionInfo);

            var testSubject = CreateTestSubject(bindingArgs,
                qpDownloader: qpDownloader.Object);

            // Act
            var result = await testSubject.DownloadQualityProfileAsync(Mock.Of<IProgress<FixedStepsProgress>>(), CancellationToken.None);

            result.Should().BeTrue();
            
            qpDownloader.Verify(x => x.UpdateAsync(It.IsAny<BoundSonarQubeProject>(),
                It.IsAny<IProgress<FixedStepsProgress>>(),
                It.IsAny<CancellationToken>()),
                Times.Once);

            var actualProject = (BoundSonarQubeProject)qpDownloader.Invocations[0].Arguments[0];

            // Check the credentials were handled correctly
            if (userName == null)
            {
                actualProject.Credentials.Should().BeNull();
            }
            else
            {
                actualProject.Credentials.Should().BeOfType<BasicAuthCredentials>();
                var actualCreds = (BasicAuthCredentials)actualProject.Credentials;

                actualCreds.UserName.Should().Be(userName);
                CheckIsExpectedPassword(rawPassword, actualCreds.Password);
            }
        }

        [TestMethod] 
        public async Task DownloadQualityProfile_HandlesInvalidOperationException()
        {
            var qpDownloader = new Mock<IQualityProfileDownloader>();
            qpDownloader
                .Setup(x =>
                    x.UpdateAsync(It.IsAny<BoundSonarQubeProject>(),
                        It.IsAny<IProgress<FixedStepsProgress>>(),
                        It.IsAny<CancellationToken>()))
                .Throws(new InvalidOperationException());

            var testSubject = CreateTestSubject(
                qpDownloader: qpDownloader.Object);

            // Act
            var result =
                await testSubject.DownloadQualityProfileAsync(Mock.Of<IProgress<FixedStepsProgress>>(), CancellationToken.None);

            result.Should().BeFalse();
            qpDownloader.Verify(x => x.UpdateAsync(It.IsAny<BoundSonarQubeProject>(),
                    It.IsAny<IProgress<FixedStepsProgress>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion Tests

        #region Helpers

        private BindingProcessImpl CreateTestSubject(BindCommandArgs bindingArgs = null,
            IExclusionSettingsStorage exclusionSettingsStorage = null,
            ISonarQubeService sonarQubeService = null,
            IQualityProfileDownloader qpDownloader = null,
            ILogger logger = null)
        {
            bindingArgs = bindingArgs ?? CreateBindCommandArgs();
            sonarQubeService ??= Mock.Of<ISonarQubeService>();
            exclusionSettingsStorage ??= Mock.Of<IExclusionSettingsStorage>();
            qpDownloader ??= Mock.Of<IQualityProfileDownloader>();
            logger ??= new TestLogger(logToConsole: true);

            return new BindingProcessImpl(bindingArgs,
                exclusionSettingsStorage,
                sonarQubeService,
                qpDownloader,
                logger);
        }

        private BindCommandArgs CreateBindCommandArgs(string projectKey = "key", string projectName = "name", ConnectionInformation connection = null)
        {
            connection = connection ?? new ConnectionInformation(new Uri("http://connected"));
            return new BindCommandArgs(projectKey, projectName, connection);
        }

        private static void CheckIsExpectedPassword(string expectedRawPassword, SecureString actualPassword)
        {
            // The SecureString extension methods in SonarQube.Client.Helpers.SecureStringHelper throw for
            // nulls
            if (expectedRawPassword == null)
            {
                actualPassword.Should().BeNull();
            }
            else
            {
                actualPassword.ToUnsecureString().Should().Be(expectedRawPassword);
            }
        }
        #endregion Helpers
    }
}
