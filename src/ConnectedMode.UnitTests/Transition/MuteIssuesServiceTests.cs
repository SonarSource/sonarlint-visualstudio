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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.ConnectedMode.Transition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Transition;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Transition
{
    [TestClass]
    public class MuteIssuesServiceTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<MuteIssuesService, IMuteIssuesService>(
                MefTestHelpers.CreateExport<IConfigurationProvider>(),
                MefTestHelpers.CreateExport<ILogger>(),
                MefTestHelpers.CreateExport<IMuteIssuesWindowService>(),
                MefTestHelpers.CreateExport<ISonarQubeService>(),
                MefTestHelpers.CreateExport<IServerIssuesStoreWriter>());
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<MuteIssuesService>();
        }

        [TestMethod]
        public async Task Mute_NotInConnectedMode_Logs()
        {
            var configurationProvider = CreateConfigurationProvider(false);
            var logger = new Mock<ILogger>();

            var testSubject = CreateTestSubject(configurationProvider: configurationProvider, logger: logger.Object);

            await testSubject.Mute("anyKEy", CancellationToken.None);

            logger.Verify(l => l.LogVerbose("[Transition]Issue muting is only supported in connected mode"), Times.Once);
        }

        [TestMethod]
        public async Task Mute_WindowOK_CallService()
        {
            var muteIssuesWindowService = CreateMuteIssuesWindowService("issueKey", true, SonarQubeIssueTransition.FalsePositive, "some comment");

            var sonarQubeService = new Mock<ISonarQubeService>();

            sonarQubeService.Setup(s => s.TransitionIssueAsync(It.IsAny<string>(), It.IsAny<SonarQubeIssueTransition>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(SonarQubeIssueTransitionResult.FailedToTransition);
            sonarQubeService.Setup(s => s.TransitionIssueAsync("issueKey", SonarQubeIssueTransition.FalsePositive, "some comment", CancellationToken.None)).ReturnsAsync(SonarQubeIssueTransitionResult.Success);

            var serverIssuesStore = new Mock<IServerIssuesStoreWriter>();

            var testSubject = CreateTestSubject(muteIssuesWindowService: muteIssuesWindowService.Object, sonarQubeService: sonarQubeService.Object, serverIssuesStore: serverIssuesStore.Object);

            await testSubject.Mute("issueKey", CancellationToken.None);

            muteIssuesWindowService.Verify(s => s.Show("issueKey"), Times.Once);
            sonarQubeService.Verify(s => s.TransitionIssueAsync("issueKey", SonarQubeIssueTransition.FalsePositive, "some comment", CancellationToken.None), Times.Once);
            serverIssuesStore.Verify(s => s.UpdateIssues(true, It.Is<IEnumerable<string>>(p => p.SequenceEqual(new[] { "issueKey" }))), Times.Once);
        }

        [TestMethod]
        public async Task Mute_WindowCancel_DontCallService()
        {
            var muteIssuesWindowService = CreateMuteIssuesWindowService("issueKey", false, SonarQubeIssueTransition.FalsePositive, "some comment");

            var sonarQubeService = new Mock<ISonarQubeService>();

            var testSubject = CreateTestSubject(muteIssuesWindowService: muteIssuesWindowService.Object, sonarQubeService: sonarQubeService.Object);

            await testSubject.Mute("issueKey", CancellationToken.None);

            muteIssuesWindowService.Verify(s => s.Show("issueKey"), Times.Once);
            sonarQubeService.VerifyNoOtherCalls();
        }

        private Mock<IMuteIssuesWindowService> CreateMuteIssuesWindowService(string issueKey, bool result, SonarQubeIssueTransition transition = default, string comment = default)
        {
            var muteIssuesWindowResponse = CreateMuteIssuesWindowResponse(result, transition, comment);

            var service = new Mock<IMuteIssuesWindowService>();
            service.Setup(s => s.Show(issueKey)).Returns(muteIssuesWindowResponse);

            return service;

            static MuteIssuesWindowResponse CreateMuteIssuesWindowResponse(bool result, SonarQubeIssueTransition transition, string comment)
            {
                return new MuteIssuesWindowResponse
                {
                    Result = result,
                    IssueTransition = transition,
                    Comment = comment
                };
            }
        }

        private Mock<IThreadHandling> CreateThreadHandling()
        {
            var threadHandling = new Mock<IThreadHandling>();
            threadHandling
                .Setup(x => x.RunOnUIThreadAsync(It.IsAny<Action>()))
                .Callback<Action>(callbackAction =>
                {
                    callbackAction();
                });

            return threadHandling;
        }

        private IConfigurationProvider CreateConfigurationProvider(bool isConnectedMode = true)
        {
            var configurationProvider = new ConfigurableConfigurationProvider();
            configurationProvider.ModeToReturn = isConnectedMode ? SonarLintMode.Connected : SonarLintMode.Standalone;
            configurationProvider.ProjectToReturn = new BoundSonarQubeProject();
            configurationProvider.FolderPathToReturn = "someFolder";

            return configurationProvider;
        }

        private MuteIssuesService CreateTestSubject(IConfigurationProvider configurationProvider = null,
            ILogger logger = null,
            IMuteIssuesWindowService muteIssuesWindowService = null,
            ISonarQubeService sonarQubeService = null,
            IServerIssuesStoreWriter serverIssuesStore = null,
            IThreadHandling threadHandling = null)
        {
            configurationProvider ??= CreateConfigurationProvider();
            logger ??= Mock.Of<ILogger>();
            muteIssuesWindowService ??= Mock.Of<IMuteIssuesWindowService>();
            sonarQubeService ??= Mock.Of<ISonarQubeService>();
            serverIssuesStore ??= Mock.Of<IServerIssuesStoreWriter>();
            threadHandling ??= CreateThreadHandling().Object;

            return new MuteIssuesService(configurationProvider, logger, muteIssuesWindowService, sonarQubeService, serverIssuesStore, threadHandling);
        }
    }
}
