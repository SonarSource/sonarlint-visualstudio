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
using SonarLint.VisualStudio.ConnectedMode.Helpers;
using SonarLint.VisualStudio.ConnectedMode.QualityProfiles;
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
                MefTestHelpers.CreateExport<ICancellableActionRunner>(),
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
            var runner = CreatePassthroughRunner();

            var testSubject = CreateTestSubject(configProvider.Object, qpDownloader.Object, runner.Object);
            var eventListener = new QPUpdatedEventListener(testSubject);

            await testSubject.UpdateAsync();

            configProvider.Verify(x => x.GetConfiguration(), Times.Once);
            runner.Invocations.Should().BeEmpty();
            qpDownloader.Invocations.Should().BeEmpty();
            eventListener.EventCount.Should().Be(0);
        }

        [TestMethod]
        public async Task UpdateBoundSolutionAsync_IsNewConnectedMode_UpdateIsDoneThroughRunner()
        {
            var cancellationToken = new CancellationToken();
            var boundProject = new BoundSonarQubeProject();
            var configProvider = CreateConfigProvider(SonarLintMode.Connected, boundProject);
            var qpDownloader = new Mock<IQualityProfileDownloader>();
            SetUpDownloader(qpDownloader);
            // Using a pass-through runner so we can test the action passed to the runner
            var runner = CreatePassthroughRunner(cancellationToken);

            var testSubject = CreateTestSubject(configProvider.Object, qpDownloader.Object, runner.Object);
            var eventListener = new QPUpdatedEventListener(testSubject);

            await testSubject.UpdateAsync();

            configProvider.Verify(x => x.GetConfiguration(), Times.Once);
            runner.Verify(x => x.RunAsync(It.IsAny<Func<CancellationToken, Task>>()), Times.Once);
            qpDownloader.Verify(x => x.UpdateAsync(boundProject, null, cancellationToken), Times.Once);
            eventListener.EventCount.Should().Be(1);
        }

        [TestMethod]
        public async Task UpdateBoundSolutionAsync_IsNewConnectedMode_NoUpdates_EventIsNotRaised()
        {
            var boundProject = new BoundSonarQubeProject();
            var configProvider = CreateConfigProvider(SonarLintMode.Connected, boundProject);
            var qpDownloader = new Mock<IQualityProfileDownloader>();
            SetUpDownloader(qpDownloader, false);
            // Using a pass-through runner so we can test the action passed to the runner
            var runner = CreatePassthroughRunner();

            var testSubject = CreateTestSubject(configProvider.Object, qpDownloader.Object, runner.Object);
            var eventListener = new QPUpdatedEventListener(testSubject);

            await testSubject.UpdateAsync();

            configProvider.Verify(x => x.GetConfiguration(), Times.Once);
            runner.Verify(x => x.RunAsync(It.IsAny<Func<CancellationToken, Task>>()), Times.Once);
            qpDownloader.Verify(x => x.UpdateAsync(boundProject, null, It.IsAny<CancellationToken>()), Times.Once);
            eventListener.EventCount.Should().Be(0);
        }
        
        [TestMethod]
        public async Task UpdateBoundSolutionAsync_IsNewConnectedMode_UpdaterDoesNotCallDownloaderDirectly()
        {
            var boundProject = new BoundSonarQubeProject();
            var configProvider = CreateConfigProvider(SonarLintMode.Connected, boundProject);
            var qpDownloader = new Mock<IQualityProfileDownloader>();
            // Here, we're not using a pass-through runner, so we're not expecting the 
            // downloader to be invoked
            var runner = new Mock<ICancellableActionRunner>();

            var testSubject = CreateTestSubject(configProvider.Object, qpDownloader.Object, runner.Object);

            await testSubject.UpdateAsync();

            configProvider.Verify(x => x.GetConfiguration(), Times.Once);
            runner.Verify(x => x.RunAsync(It.IsAny<Func<CancellationToken, Task>>()), Times.Once);

            // The updater should not be calling the downloader directly, only via the runner
            qpDownloader.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void Dispose_RunnerIsDisposed()
        {
            var runner = new Mock<ICancellableActionRunner>();
            var testSubject = CreateTestSubject(runner: runner.Object);
            runner.Invocations.Should().BeEmpty();

            testSubject.Dispose();

            runner.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        public async Task UpdateBoundSolutionAsync_JobIsCancelled_EventIsNotRaised()
        {
            // Simulate the runner cancelling a task
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var runner = new Mock<ICancellableActionRunner>();
            runner.Setup(x => x.RunAsync(It.IsAny<Func<CancellationToken, Task>>()))
                .Callback<Func<CancellationToken, Task>>(func => cts.Token.ThrowIfCancellationRequested());

            var boundProject = new BoundSonarQubeProject();
            var configProvider = CreateConfigProvider(SonarLintMode.Connected, boundProject);
            var qpDownloader = new Mock<IQualityProfileDownloader>();

            var testSubject = CreateTestSubject(configProvider.Object, qpDownloader.Object, runner.Object);
            var eventListener = new QPUpdatedEventListener(testSubject);

            await testSubject.UpdateAsync();

            configProvider.Verify(x => x.GetConfiguration(), Times.Once);
            runner.Verify(x => x.RunAsync(It.IsAny<Func<CancellationToken, Task>>()), Times.Once);
            eventListener.EventCount.Should().Be(0);
        }
        
        [TestMethod]
        public async Task UpdateBoundSolutionAsync_InvalidOperationException_EventIsNotRaised()
        {
            var runner = new Mock<ICancellableActionRunner>();
            runner
                .Setup(x =>
                    x.RunAsync(It.IsAny<Func<CancellationToken, Task>>()))
                .Throws(new InvalidOperationException());

            var boundProject = new BoundSonarQubeProject();
            var configProvider = CreateConfigProvider(SonarLintMode.Connected, boundProject);

            var testSubject = CreateTestSubject(configProvider.Object, runner: runner.Object);
            var eventListener = new QPUpdatedEventListener(testSubject);

            await testSubject.UpdateAsync();

            configProvider.Verify(x => x.GetConfiguration(), Times.Once);
            runner.Verify(x => x.RunAsync(It.IsAny<Func<CancellationToken, Task>>()), Times.Once);
            eventListener.EventCount.Should().Be(0);
        }

        private static void SetUpDownloader(Mock<IQualityProfileDownloader> qpDownloader, bool result = true)
        {
            qpDownloader
                .Setup(x =>
                    x.UpdateAsync(It.IsAny<BoundSonarQubeProject>(),
                        It.IsAny<IProgress<FixedStepsProgress>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);
        }

        private Mock<IConfigurationProvider> CreateConfigProvider(SonarLintMode mode, BoundSonarQubeProject boundProject = null)
        {
            var config = new BindingConfiguration(boundProject ?? new BoundSonarQubeProject(), mode, "any directory");

            var configProvider = new Mock<IConfigurationProvider>();
            configProvider.Setup(x => x.GetConfiguration()).Returns(config);

            return configProvider;
        }

        private static Mock<ICancellableActionRunner> CreatePassthroughRunner(CancellationToken? token = null)
        {
            // If we want to test any of the code in the action passed to the runner we need
            // to configure the runner to call it
            var runner = new Mock<ICancellableActionRunner>();
            runner
                .Setup(x => x.RunAsync(It.IsAny<Func<CancellationToken, Task>>()))
                .Callback((Func<CancellationToken, Task> callback) => callback(token ?? CancellationToken.None));

            return runner;
        }

        private static QualityProfileUpdater CreateTestSubject(
            IConfigurationProvider configProvider = null,
            IQualityProfileDownloader qpDownloader = null,
            ICancellableActionRunner runner = null)
            => new QualityProfileUpdater(
                configProvider,
                qpDownloader,
                runner ?? CreatePassthroughRunner().Object,
                new TestLogger(logToConsole: true));

        private class QPUpdatedEventListener
        {
            public QPUpdatedEventListener(IQualityProfileUpdater qpUpdater)
                => qpUpdater.QualityProfilesChanged += (sender, args) => EventCount++;

            public int EventCount { get; private set; }
        }
    }
}
