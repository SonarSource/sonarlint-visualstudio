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
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.QualityProfiles;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Binding
{
    [TestClass]
    public class BindingProcessImplTests
    {
        #region Tests

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
            // TODO duncanp

            //var bindingArgs = CreateBindCommandArgs("key", ProjectName, new ConnectionInformation(new Uri("http://connected")));
            //var testSubject = CreateTestSubject(bindingArgs,
            //    configProviderMock.Object,
            //    sonarQubeService: sonarQubeService.Object,
            //    configurationPersister: configPersister,
            //    languagesToBind: new []{ language });

            //// Act
            //await testSubject.DownloadQualityProfileAsync(progressAdapter, CancellationToken.None);

        }

        [TestMethod]
        public void EmitBindingCompleteMessage()
        {
            // Arrange
            var testSubject = this.CreateTestSubject();

            // Test case 1: Default state is 'true'
            testSubject.InternalState.BindingOperationSucceeded.Should().BeTrue($"Initial state of {nameof(BindingProcessImpl.InternalState.BindingOperationSucceeded)} should be true");
        }

        #endregion Tests

        #region Helpers

        private BindingProcessImpl CreateTestSubject(BindCommandArgs bindingArgs = null,
            IExclusionSettingsStorage exclusionSettingsStorage = null,
            ISonarQubeService sonarQubeService = null,
            IQualityProfileDownloader qpUpdater = null,
            ILogger logger = null)
        {
            bindingArgs = bindingArgs ?? CreateBindCommandArgs();
            sonarQubeService ??= Mock.Of<ISonarQubeService>();
            exclusionSettingsStorage ??= Mock.Of<IExclusionSettingsStorage>();
            qpUpdater ??= Mock.Of<IQualityProfileDownloader>();
            logger ??= new TestLogger(logToConsole: true);

            return new BindingProcessImpl(bindingArgs,
                exclusionSettingsStorage,
                sonarQubeService,
                qpUpdater,
                logger,
                false);
        }

        private BindCommandArgs CreateBindCommandArgs(string projectKey = "key", string projectName = "name", ConnectionInformation connection = null)
        {
            connection = connection ?? new ConnectionInformation(new Uri("http://connected"));
            return new BindCommandArgs(projectKey, projectName, connection);
        }

        #endregion Helpers
    }
}
