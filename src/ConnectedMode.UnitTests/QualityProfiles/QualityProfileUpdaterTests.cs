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
using SonarLint.VisualStudio.ConnectedMode.QualityProfiles;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.QualityProfiles
{
    [TestClass]
    public class QualityProfileUpdaterTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
            => MefTestHelpers.CheckTypeCanBeImported<QualityProfileUpdater, IQualityProfileUpdater>(
                MefTestHelpers.CreateExport<IConfigurationProvider>(),
                MefTestHelpers.CreateExport<IQualityProfileDownloader>(),
                MefTestHelpers.CreateExport<IScheduler>(),
                MefTestHelpers.CreateExport<ILogger>());

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
            => MefTestHelpers.CheckIsSingletonMefComponent<QualityProfileUpdater>();

        [TestMethod]
        [DataRow(SonarLintMode.Standalone)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public async Task UpdateBoundSolutionAsync_NotNewConnectedMode_DoesNotUpdateQP(SonarLintMode mode)
        {
            var configProvider = CreateConfigProvider(mode);
            var qpDownloader = new Mock<IQualityProfileDownloader>();
            var scheduler = CreatePassthroughScheduler();

            var testSubject = CreateTestSubject(configProvider.Object, qpDownloader.Object, scheduler.Object);

            await testSubject.UpdateAsync();

            configProvider.Verify(x => x.GetConfiguration(), Times.Once);
            scheduler.Invocations.Should().BeEmpty();
            qpDownloader.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public async Task UpdateBoundSolutionAsync_IsNewConnectedMode_UpdateIsDoneByScheduler()
        {
            var boundProject = new BoundSonarQubeProject();
            var configProvider = CreateConfigProvider(SonarLintMode.Connected, boundProject);
            var qpDownloader = new Mock<IQualityProfileDownloader>();
            // Using a pass-through scheduler so we can test the action passed to the scheduler
            var scheduler = CreatePassthroughScheduler();

            var testSubject = CreateTestSubject(configProvider.Object, qpDownloader.Object, scheduler.Object);

            await testSubject.UpdateAsync();

            configProvider.Verify(x => x.GetConfiguration(), Times.Once);
            scheduler.Verify(x => x.Schedule(QualityProfileUpdater.QPUpdateJobId,
                It.IsAny<Action<CancellationToken>>(),
                QualityProfileUpdater.QPUpdateJobTimeoutInMilliseconds), Times.Once);
            qpDownloader.Verify(x => x.UpdateAsync(boundProject, null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task UpdateBoundSolutionAsync_IsNewConnectedMode_UpdaterDoesNotCallDownloaderDirectly()
        {
            var boundProject = new BoundSonarQubeProject();
            var configProvider = CreateConfigProvider(SonarLintMode.Connected, boundProject);
            var qpDownloader = new Mock<IQualityProfileDownloader>();
            // Here, we're not using a pass-through scheduler, so we're not expecting the 
            // downloader to be invoked
            var scheduler = new Mock<IScheduler>();

            var testSubject = CreateTestSubject(configProvider.Object, qpDownloader.Object, scheduler.Object);

            await testSubject.UpdateAsync();

            configProvider.Verify(x => x.GetConfiguration(), Times.Once);
            scheduler.Verify(x => x.Schedule(QualityProfileUpdater.QPUpdateJobId,
                It.IsAny<Action<CancellationToken>>(),
                QualityProfileUpdater.QPUpdateJobTimeoutInMilliseconds), Times.Once);

            // The updater should not be calling the downloader directly, only via the scheduler
            qpDownloader.Invocations.Should().BeEmpty();
        }

        private Mock<IConfigurationProvider> CreateConfigProvider(SonarLintMode mode, BoundSonarQubeProject boundProject = null)
        {
            var config = new BindingConfiguration(boundProject ?? new BoundSonarQubeProject(), mode, "any directory");

            var configProvider = new Mock<IConfigurationProvider>();
            configProvider.Setup(x => x.GetConfiguration()).Returns(config);

            return configProvider;
        }

        private static Mock<IScheduler> CreatePassthroughScheduler(CancellationToken? token = null)
        {
            // If we want to test any of the code in the action passed to the scheduler we need
            // to configure the scheduler to call it
            var schedulerMock = new Mock<IScheduler>();
            schedulerMock
                .Setup(x => x.Schedule(It.IsAny<string>(), It.IsAny<Action<CancellationToken>>(), It.IsAny<int>()))
                .Callback((string jobId, Action<CancellationToken> action, int timeout) => action(token ?? CancellationToken.None));

            return schedulerMock;
        }

        private static QualityProfileUpdater CreateTestSubject(
            IConfigurationProvider configProvider = null,
            IQualityProfileDownloader qpDownloader = null,
            IScheduler scheduler = null)
            => new QualityProfileUpdater(
                configProvider,
                qpDownloader,
                scheduler ?? CreatePassthroughScheduler().Object,
                new TestLogger(logToConsole: true));
    }
}
