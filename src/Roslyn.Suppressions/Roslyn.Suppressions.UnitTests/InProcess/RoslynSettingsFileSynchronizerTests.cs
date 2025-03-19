/*
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

using System.IO;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Roslyn.Suppressions.InProcess;
using SonarLint.VisualStudio.Roslyn.Suppressions.SettingsFile;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models;
using static SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.TestHelper;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.InProcess;

[TestClass]
public class RoslynSettingsFileSynchronizerTests
{
    private const string DefaultSln = "DefaultSolution";
    private IConfigurationProvider configProvider;
    private ISuppressedIssuesCalculatorFactory suppressedIssuesCalculatorFactory;
    private IRoslynSettingsFileStorage roslynSettingsFileStorage;
    private ISolutionInfoProvider solutionInfoProvider;
    private RoslynSettingsFileSynchronizer testSubject;
    private IThreadHandling threadHandling;
    private readonly BindingConfiguration connectedBindingConfiguration = CreateConnectedConfiguration("some project key");
    private ISolutionBindingRepository solutionBindingRepository;
    private IRoslynSuppressionUpdater roslynSuppressionUpdater;
    private ISuppressedIssuesCalculator suppressedIssuesCalculator;

    private readonly SonarQubeIssue csharpIssueSuppressed = CreateSonarQubeIssue("csharpsquid:S111");
    private readonly SonarQubeIssue vbNetIssueSuppressed = CreateSonarQubeIssue("vbnet:S222");

    [TestInitialize]
    public void TestInitialize()
    {
        roslynSettingsFileStorage = Substitute.For<IRoslynSettingsFileStorage>();
        configProvider = Substitute.For<IConfigurationProvider>();
        solutionInfoProvider = Substitute.For<ISolutionInfoProvider>();
        solutionBindingRepository = Substitute.For<ISolutionBindingRepository>();
        roslynSuppressionUpdater = Substitute.For<IRoslynSuppressionUpdater>();
        threadHandling = new NoOpThreadHandler();
        MockSuppressedIssuesCalculator();

        testSubject = CreateTestSubject(threadHandling);
        MockSolutionInfoProvider(DefaultSln);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RoslynSettingsFileSynchronizer, IRoslynSettingsFileSynchronizer>(
            MefTestHelpers.CreateExport<IRoslynSettingsFileStorage>(),
            MefTestHelpers.CreateExport<IConfigurationProvider>(),
            MefTestHelpers.CreateExport<ISolutionInfoProvider>(),
            MefTestHelpers.CreateExport<ISolutionBindingRepository>(),
            MefTestHelpers.CreateExport<IRoslynSuppressionUpdater>(),
            MefTestHelpers.CreateExport<ISuppressedIssuesCalculatorFactory>());

    [TestMethod]
    public void MefCtor_CheckTypeIsNonShared() => MefTestHelpers.CheckIsNonSharedMefComponent<RoslynSettingsFileSynchronizer>();

    [TestMethod]
    public void Ctor_RegisterToEvents()
    {
        solutionBindingRepository.Received(1).BindingDeleted += Arg.Any<EventHandler<LocalBindingKeyEventArgs>>();

        roslynSuppressionUpdater.Received(1).SuppressedIssuesReloaded += Arg.Any<EventHandler<SuppressionsEventArgs>>();
        roslynSuppressionUpdater.Received(1).NewIssuesSuppressed += Arg.Any<EventHandler<SuppressionsEventArgs>>();
        roslynSuppressionUpdater.Received(1).SuppressionsRemoved += Arg.Any<EventHandler<SuppressionsRemovedEventArgs>>();
        roslynSuppressionUpdater.ReceivedCalls().Should().HaveCount(3);
    }

    [TestMethod]
    public void Dispose_UnregisterFromEvents()
    {
        testSubject.Dispose();

        solutionBindingRepository.Received(1).BindingDeleted -= Arg.Any<EventHandler<LocalBindingKeyEventArgs>>();
        roslynSuppressionUpdater.Received(1).SuppressedIssuesReloaded -= Arg.Any<EventHandler<SuppressionsEventArgs>>();
        roslynSuppressionUpdater.Received(1).NewIssuesSuppressed -= Arg.Any<EventHandler<SuppressionsEventArgs>>();
        roslynSuppressionUpdater.Received(1).SuppressionsRemoved -= Arg.Any<EventHandler<SuppressionsRemovedEventArgs>>();
    }

    [TestMethod]
    public void BindingDeleted_StorageFileDeleted()
    {
        var localBindingKey = "my solution name";
        solutionBindingRepository.BindingDeleted += Raise.EventWith(new LocalBindingKeyEventArgs(localBindingKey));

        roslynSettingsFileStorage.Received(1).Delete(localBindingKey);
    }

    [TestMethod]
    public void SuppressedIssuesReloaded_StandaloneMode_StorageFileDeleted()
    {
        MockConfigProvider(BindingConfiguration.Standalone);
        var sonarQubeIssues = new[] { csharpIssueSuppressed, vbNetIssueSuppressed };

        RaiseSuppressedIssuesReloaded(sonarQubeIssues);

        configProvider.Received(1).GetConfiguration();
        roslynSettingsFileStorage.Received(1).Delete(DefaultSln);
        roslynSettingsFileStorage.ReceivedCalls().Should().HaveCount(1);
        suppressedIssuesCalculatorFactory.Received(1).CreateAllSuppressedIssuesCalculator(sonarQubeIssues);
        suppressedIssuesCalculator.DidNotReceive().GetSuppressedIssuesOrNull(Arg.Any<string>());
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)] // should update storage even when there are no issues
    public void SuppressedIssuesReloaded_ConnectedMode_StorageUpdated(bool hasIssues)
    {
        MockConfigProvider(connectedBindingConfiguration);
        var issues = hasIssues ? new[] { csharpIssueSuppressed, vbNetIssueSuppressed } : Array.Empty<SonarQubeIssue>();

        RaiseSuppressedIssuesReloaded(issues);

        roslynSettingsFileStorage.Received(1).Update(Arg.Any<RoslynSettings>(), DefaultSln);
        suppressedIssuesCalculatorFactory.Received(1).CreateAllSuppressedIssuesCalculator(issues);
        suppressedIssuesCalculator.Received(1).GetSuppressedIssuesOrNull(DefaultSln);
    }

    [TestMethod]
    public void SuppressedIssuesReloaded_FileStorageIsUpdatedOnBackgroundThread()
    {
        MockConfigProvider(connectedBindingConfiguration);
        var threadHandlingMock = Substitute.For<IThreadHandling>();
        CreateTestSubject(threadHandlingMock);

        RaiseSuppressedIssuesReloaded([csharpIssueSuppressed]);

        threadHandlingMock.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<bool>>>());
    }

    [TestMethod]
    public void SuppressedIssuesReloaded_NoSolution_DoesNothing()
    {
        MockSolutionInfoProvider(null);
        var sonarQubeIssues = new[] { csharpIssueSuppressed, vbNetIssueSuppressed };

        RaiseSuppressedIssuesReloaded(sonarQubeIssues);

        solutionInfoProvider.Received(1).GetSolutionNameAsync();
        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Update(default, default);
        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Delete(default);
        configProvider.DidNotReceiveWithAnyArgs().GetConfiguration();
        suppressedIssuesCalculatorFactory.Received(1).CreateAllSuppressedIssuesCalculator(sonarQubeIssues);
        suppressedIssuesCalculator.DidNotReceive().GetSuppressedIssuesOrNull(Arg.Any<string>());
    }

    [TestMethod]
    public void SuppressedIssuesReloaded_ConnectedMode_UpdatesFileStorage()
    {
        MockConfigProvider(connectedBindingConfiguration);
        var sonarQubeIssues = new[] { csharpIssueSuppressed, vbNetIssueSuppressed };
        var expectedSonarQubeIssues = new[] { CreateIssue(issueServerKey: csharpIssueSuppressed.IssueKey) };
        suppressedIssuesCalculator.GetSuppressedIssuesOrNull(DefaultSln).Returns(expectedSonarQubeIssues);

        RaiseSuppressedIssuesReloaded(sonarQubeIssues);

        roslynSettingsFileStorage.Received(1)
            .Update(Arg.Is<RoslynSettings>(x => VerifyExpectedRoslynSettings(x, expectedSonarQubeIssues)), DefaultSln);
        suppressedIssuesCalculatorFactory.Received(1).CreateAllSuppressedIssuesCalculator(sonarQubeIssues);
        suppressedIssuesCalculator.Received(1).GetSuppressedIssuesOrNull(DefaultSln);
    }

    [TestMethod]
    public void SuppressedIssuesReloaded_SuppressedIssueCalculatorReturnsNull_DoesNothing()
    {
        MockConfigProvider(connectedBindingConfiguration);
        suppressedIssuesCalculator.GetSuppressedIssuesOrNull(DefaultSln).Returns((IEnumerable<SuppressedIssue>)null);

        RaiseSuppressedIssuesReloaded([csharpIssueSuppressed, vbNetIssueSuppressed]);

        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Update(default, default);
        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Delete(default);
    }

    [TestMethod]
    public void NewIssuesSuppressed_StandaloneMode_StorageFileDeleted()
    {
        MockConfigProvider(BindingConfiguration.Standalone);
        var newSonarQubeIssues = new[] { csharpIssueSuppressed };

        RaiseNewIssuesSuppressed(newSonarQubeIssues);

        configProvider.Received(1).GetConfiguration();
        roslynSettingsFileStorage.Received(1).Delete(DefaultSln);
        roslynSettingsFileStorage.ReceivedCalls().Should().HaveCount(1);
        suppressedIssuesCalculatorFactory.Received(1).CreateNewSuppressedIssuesCalculator(newSonarQubeIssues);
        suppressedIssuesCalculator.DidNotReceive().GetSuppressedIssuesOrNull(Arg.Any<string>());
    }

    [TestMethod]
    public void NewIssuesSuppressed_NoIssues_StorageNotUpdated()
    {
        MockConfigProvider(connectedBindingConfiguration);

        RaiseNewIssuesSuppressed([]);

        roslynSettingsFileStorage.DidNotReceive().Update(Arg.Any<RoslynSettings>(), DefaultSln);
        suppressedIssuesCalculatorFactory.DidNotReceive().CreateNewSuppressedIssuesCalculator(Arg.Any<SonarQubeIssue[]>());
        suppressedIssuesCalculator.DidNotReceive().GetSuppressedIssuesOrNull(Arg.Any<string>());
    }

    [TestMethod]
    public void NewIssuesSuppressed_FileStorageIsUpdatedOnBackgroundThread()
    {
        MockConfigProvider(connectedBindingConfiguration);
        var threadHandlingMock = Substitute.For<IThreadHandling>();
        CreateTestSubject(threadHandlingMock);

        RaiseNewIssuesSuppressed([csharpIssueSuppressed]);

        threadHandlingMock.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<bool>>>());
    }

    [TestMethod]
    public void NewIssuesSuppressed_NoSolution_DoesNothing()
    {
        MockSolutionInfoProvider(null);
        var newSonarQubeIssues = new[] { csharpIssueSuppressed };

        RaiseNewIssuesSuppressed(newSonarQubeIssues);

        solutionInfoProvider.Received(1).GetSolutionNameAsync();
        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Update(default, default);
        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Delete(default);
        configProvider.DidNotReceiveWithAnyArgs().GetConfiguration();
        suppressedIssuesCalculatorFactory.Received(1).CreateNewSuppressedIssuesCalculator(newSonarQubeIssues);
        suppressedIssuesCalculator.DidNotReceive().GetSuppressedIssuesOrNull(Arg.Any<string>());
    }

    [TestMethod]
    public void NewIssuesSuppressed_ConnectedMode_UpdatesFileStorage()
    {
        MockConfigProvider(connectedBindingConfiguration);
        var sonarQubeIssues = new[] { csharpIssueSuppressed, vbNetIssueSuppressed };
        var expectedSonarQubeIssues = new[] { CreateIssue(issueServerKey: csharpIssueSuppressed.IssueKey) };
        suppressedIssuesCalculator.GetSuppressedIssuesOrNull(DefaultSln).Returns(expectedSonarQubeIssues);

        RaiseNewIssuesSuppressed(sonarQubeIssues);

        roslynSettingsFileStorage.Received(1)
            .Update(Arg.Is<RoslynSettings>(x => VerifyExpectedRoslynSettings(x, expectedSonarQubeIssues)), DefaultSln);
        suppressedIssuesCalculatorFactory.Received(1).CreateNewSuppressedIssuesCalculator(sonarQubeIssues);
        suppressedIssuesCalculator.Received(1).GetSuppressedIssuesOrNull(DefaultSln);
    }

    [TestMethod]
    public void NewIssuesSuppressed_SuppressedIssueCalculatorReturnsNull_DoesNothing()
    {
        MockConfigProvider(connectedBindingConfiguration);
        suppressedIssuesCalculator.GetSuppressedIssuesOrNull(DefaultSln).Returns((IEnumerable<SuppressedIssue>)null);

        RaiseNewIssuesSuppressed([csharpIssueSuppressed, vbNetIssueSuppressed]);

        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Update(default, default);
        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Delete(default);
    }

    [TestMethod]
    public void NewIssuesResolved_StandaloneMode_StorageFileDeleted()
    {
        MockConfigProvider(BindingConfiguration.Standalone);
        var newSonarQubeIssues = new[] { csharpIssueSuppressed.IssueKey };

        RaiseSuppressionsRemoved(newSonarQubeIssues);

        configProvider.Received(1).GetConfiguration();
        roslynSettingsFileStorage.Received(1).Delete(Path.GetFileNameWithoutExtension(DefaultSln));
        roslynSettingsFileStorage.ReceivedCalls().Should().HaveCount(1);
        suppressedIssuesCalculatorFactory.Received(1).CreateSuppressedIssuesRemovedCalculator(newSonarQubeIssues);
        suppressedIssuesCalculator.DidNotReceive().GetSuppressedIssuesOrNull(Arg.Any<string>());
    }

    [TestMethod]
    public void NewIssuesResolved_NoIssues_StorageNotUpdated()
    {
        MockConfigProvider(connectedBindingConfiguration);

        RaiseSuppressionsRemoved([]);

        solutionInfoProvider.DidNotReceive().GetSolutionNameAsync();
        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Update(default, default);
        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Delete(default);
        configProvider.DidNotReceiveWithAnyArgs().GetConfiguration();
        suppressedIssuesCalculatorFactory.DidNotReceive().CreateSuppressedIssuesRemovedCalculator(Arg.Any<string[]>());
        suppressedIssuesCalculator.DidNotReceive().GetSuppressedIssuesOrNull(Arg.Any<string>());
    }

    [TestMethod]
    public void NewIssuesResolved_FileStorageIsUpdatedOnBackgroundThread()
    {
        MockConfigProvider(connectedBindingConfiguration);
        var threadHandlingMock = Substitute.For<IThreadHandling>();
        CreateTestSubject(threadHandlingMock);

        RaiseSuppressionsRemoved([csharpIssueSuppressed.IssueKey]);

        threadHandlingMock.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<bool>>>());
    }

    [TestMethod]
    public void NewIssuesResolved_NoSolution_DoesNothing()
    {
        MockSolutionInfoProvider(null);
        var newSonarQubeIssues = new[] { csharpIssueSuppressed.IssueKey };

        RaiseSuppressionsRemoved(newSonarQubeIssues);

        solutionInfoProvider.Received(1).GetSolutionNameAsync();
        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Update(default, default);
        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Delete(default);
        configProvider.DidNotReceiveWithAnyArgs().GetConfiguration();
        suppressedIssuesCalculatorFactory.Received().CreateSuppressedIssuesRemovedCalculator(newSonarQubeIssues);
        suppressedIssuesCalculator.DidNotReceive().GetSuppressedIssuesOrNull(Arg.Any<string>());
    }

    [TestMethod]
    public void NewIssuesResolved_ConnectedMode_UpdatesFileStorage()
    {
        MockConfigProvider(connectedBindingConfiguration);
        var sonarQubeIssues = new[] { csharpIssueSuppressed.IssueKey, vbNetIssueSuppressed.IssueKey };
        var expectedSonarQubeIssues = new[] { CreateIssue(issueServerKey: csharpIssueSuppressed.IssueKey) };
        suppressedIssuesCalculator.GetSuppressedIssuesOrNull(DefaultSln).Returns(expectedSonarQubeIssues);

        RaiseSuppressionsRemoved(sonarQubeIssues);

        roslynSettingsFileStorage.Received(1)
            .Update(Arg.Is<RoslynSettings>(x => VerifyExpectedRoslynSettings(x, expectedSonarQubeIssues)), DefaultSln);
        suppressedIssuesCalculatorFactory.Received(1).CreateSuppressedIssuesRemovedCalculator(sonarQubeIssues);
        suppressedIssuesCalculator.Received(1).GetSuppressedIssuesOrNull(DefaultSln);
    }

    [TestMethod]
    public void NewIssuesResolved_SuppressedIssueCalculatorReturnsNull_DoesNothing()
    {
        MockConfigProvider(connectedBindingConfiguration);
        suppressedIssuesCalculator.GetSuppressedIssuesOrNull(DefaultSln).Returns((IEnumerable<SuppressedIssue>)null);

        RaiseSuppressionsRemoved([csharpIssueSuppressed.IssueKey, vbNetIssueSuppressed.IssueKey]);

        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Update(default, default);
        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Delete(default);
    }

    private void MockConfigProvider(BindingConfiguration configuration) => configProvider.GetConfiguration().Returns(configuration);

    private static BindingConfiguration CreateConnectedConfiguration(string projectKey)
    {
        var project = new BoundServerProject("solution", projectKey, new ServerConnection.SonarQube(new Uri("http://bound")));

        return BindingConfiguration.CreateBoundConfiguration(project, SonarLintMode.Connected, "some directory");
    }

    private void MockSolutionInfoProvider(string solutionName) => solutionInfoProvider.GetSolutionNameAsync().Returns(solutionName);

    private void RaiseSuppressedIssuesReloaded(SonarQubeIssue[] issues) => roslynSuppressionUpdater.SuppressedIssuesReloaded += Raise.EventWith(null, new SuppressionsEventArgs(issues));

    private void RaiseNewIssuesSuppressed(SonarQubeIssue[] issues) => roslynSuppressionUpdater.NewIssuesSuppressed += Raise.EventWith(null, new SuppressionsEventArgs(issues));

    private void RaiseSuppressionsRemoved(string[] issueKeys) => roslynSuppressionUpdater.SuppressionsRemoved += Raise.EventWith(null, new SuppressionsRemovedEventArgs(issueKeys));

    private bool VerifyExpectedRoslynSettings(RoslynSettings roslynSettings, IEnumerable<SuppressedIssue> expectedSuppressedIssues) =>
        roslynSettings.SonarProjectKey == connectedBindingConfiguration.Project.ServerProjectKey
        && roslynSettings.Suppressions.SequenceEqual(expectedSuppressedIssues);

    private void MockSuppressedIssuesCalculator()
    {
        suppressedIssuesCalculator = Substitute.For<ISuppressedIssuesCalculator>();
        suppressedIssuesCalculatorFactory = Substitute.For<ISuppressedIssuesCalculatorFactory>();
        suppressedIssuesCalculatorFactory.CreateAllSuppressedIssuesCalculator(Arg.Any<SonarQubeIssue[]>()).Returns(suppressedIssuesCalculator);
        suppressedIssuesCalculatorFactory.CreateNewSuppressedIssuesCalculator(Arg.Any<SonarQubeIssue[]>()).Returns(suppressedIssuesCalculator);
        suppressedIssuesCalculatorFactory.CreateSuppressedIssuesRemovedCalculator(Arg.Any<string[]>()).Returns(suppressedIssuesCalculator);
    }

    private RoslynSettingsFileSynchronizer CreateTestSubject(IThreadHandling mockedThreadHandling) =>
        new(
            roslynSettingsFileStorage,
            configProvider,
            solutionInfoProvider,
            solutionBindingRepository,
            roslynSuppressionUpdater,
            suppressedIssuesCalculatorFactory,
            mockedThreadHandling);
}
