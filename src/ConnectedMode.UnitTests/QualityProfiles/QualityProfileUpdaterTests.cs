﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.Helpers;
using SonarLint.VisualStudio.ConnectedMode.QualityProfiles;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.QualityProfiles;

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
        var configProvider = CreateConfigProvider(mode, CreateDefaultProject());
        var qpDownloader = new Mock<IQualityProfileDownloader>();
        var runner = CreatePassthroughRunner();

        var testSubject = CreateTestSubject(configProvider.Object, qpDownloader.Object, runner.Object);

        await testSubject.UpdateAsync();

        configProvider.Verify(x => x.GetConfiguration(), Times.Once);
        runner.Invocations.Should().BeEmpty();
        qpDownloader.Invocations.Should().BeEmpty();
    }

    [TestMethod]
    public async Task UpdateBoundSolutionAsync_IsNewConnectedMode_UpdateIsDoneThroughRunner()
    {
        var cancellationToken = CancellationToken.None;
        var boundProject = CreateDefaultProject();
        var configProvider = CreateConfigProvider(SonarLintMode.Connected, boundProject);
        var qpDownloader = new Mock<IQualityProfileDownloader>();
        SetUpDownloader(qpDownloader);
        // Using a pass-through runner so we can test the action passed to the runner
        var runner = CreatePassthroughRunner(cancellationToken);

        var testSubject = CreateTestSubject(configProvider.Object, qpDownloader.Object, runner.Object);

        await testSubject.UpdateAsync();

        configProvider.Verify(x => x.GetConfiguration(), Times.Once);
        runner.Verify(x => x.RunAsync(It.IsAny<Func<CancellationToken, Task>>()), Times.Once);
        qpDownloader.Verify(x => x.UpdateAsync(boundProject, null, cancellationToken), Times.Once);
    }

    [TestMethod]
    public async Task UpdateBoundSolutionAsync_IsNewConnectedMode_NoUpdates_EventIsNotRaised()
    {
        var boundProject = CreateDefaultProject();
        var configProvider = CreateConfigProvider(SonarLintMode.Connected, boundProject);
        var qpDownloader = new Mock<IQualityProfileDownloader>();
        SetUpDownloader(qpDownloader, false);
        // Using a pass-through runner so we can test the action passed to the runner
        var runner = CreatePassthroughRunner();

        var testSubject = CreateTestSubject(configProvider.Object, qpDownloader.Object, runner.Object);

        await testSubject.UpdateAsync();

        configProvider.Verify(x => x.GetConfiguration(), Times.Once);
        runner.Verify(x => x.RunAsync(It.IsAny<Func<CancellationToken, Task>>()), Times.Once);
        qpDownloader.Verify(x => x.UpdateAsync(boundProject, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task UpdateBoundSolutionAsync_IsNewConnectedMode_UpdaterDoesNotCallDownloaderDirectly()
    {
        var configProvider = CreateConfigProvider(SonarLintMode.Connected, CreateDefaultProject());
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
            .Callback<Func<CancellationToken, Task>>(_ => cts.Token.ThrowIfCancellationRequested());

        var configProvider = CreateConfigProvider(SonarLintMode.Connected, CreateDefaultProject());
        var qpDownloader = new Mock<IQualityProfileDownloader>();

        var testSubject = CreateTestSubject(configProvider.Object, qpDownloader.Object, runner.Object);

        await testSubject.UpdateAsync();

        configProvider.Verify(x => x.GetConfiguration(), Times.Once);
        runner.Verify(x => x.RunAsync(It.IsAny<Func<CancellationToken, Task>>()), Times.Once);
        cts.Dispose();
    }

    [TestMethod]
    public async Task UpdateBoundSolutionAsync_InvalidOperationException_EventIsNotRaised()
    {
        var runner = new Mock<ICancellableActionRunner>();
        runner
            .Setup(x =>
                x.RunAsync(It.IsAny<Func<CancellationToken, Task>>()))
            .Throws(new InvalidOperationException());

        var configProvider = CreateConfigProvider(SonarLintMode.Connected, CreateDefaultProject());

        var testSubject = CreateTestSubject(configProvider.Object, runner: runner.Object);

        await testSubject.UpdateAsync();

        configProvider.Verify(x => x.GetConfiguration(), Times.Once);
        runner.Verify(x => x.RunAsync(It.IsAny<Func<CancellationToken, Task>>()), Times.Once);
    }

    private static void SetUpDownloader(Mock<IQualityProfileDownloader> qpDownloader, bool result = true)
    {
        qpDownloader
            .Setup(x =>
                x.UpdateAsync(It.IsAny<BoundServerProject>(),
                    It.IsAny<IProgress<FixedStepsProgress>>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    private BoundServerProject CreateDefaultProject() => new("solution", "project", new ServerConnection.SonarCloud("org"));

    private Mock<IConfigurationProvider> CreateConfigProvider(SonarLintMode mode, BoundServerProject boundProject)
    {
        var config = new BindingConfiguration(boundProject, mode, "any directory");

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
        => new(
            configProvider,
            qpDownloader,
            runner ?? CreatePassthroughRunner().Object,
            new TestLogger(logToConsole: true));
}
