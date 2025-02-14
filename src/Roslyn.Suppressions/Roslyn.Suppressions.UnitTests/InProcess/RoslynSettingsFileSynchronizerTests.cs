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
    private const string DefaultSln = "DefaultSolution.sln";
    private IConfigurationProvider configProvider;
    private ILogger logger;
    private IRoslynSettingsFileStorage roslynSettingsFileStorage;
    private ISolutionInfoProvider solutionInfoProvider;
    private RoslynSettingsFileSynchronizer testSubject;
    private IThreadHandling threadHandling;
    private readonly BindingConfiguration connectedBindingConfiguration = CreateConnectedConfiguration("some project key");
    private ISolutionBindingRepository solutionBindingRepository;
    private IRoslynSuppressionUpdater roslynSuppressionUpdater;

    private readonly SonarQubeIssue csharpIssueSuppressed = CreateSonarQubeIssue("csharpsquid:S111");
    private readonly SonarQubeIssue vbNetIssueSuppressed = CreateSonarQubeIssue("vbnet:S222");
    private readonly SonarQubeIssue cppIssueSuppressed = CreateSonarQubeIssue("cpp:S333");
    private readonly SonarQubeIssue unknownRepoIssue = CreateSonarQubeIssue("xxx:S444");
    private readonly SonarQubeIssue invalidRepoKeyIssue = CreateSonarQubeIssue("xxxS555");
    private readonly SonarQubeIssue noRuleIdIssue = CreateSonarQubeIssue("xxx:");
    private readonly SonarQubeIssue csharpIssueNotSuppressed = CreateSonarQubeIssue("csharpsquid:S333", isSuppressed: false);
    private readonly SonarQubeIssue vbNetIssueNotSuppressed = CreateSonarQubeIssue("vbnet:S444", isSuppressed: false);

    [TestInitialize]
    public void TestInitialize()
    {
        roslynSettingsFileStorage = Substitute.For<IRoslynSettingsFileStorage>();
        configProvider = Substitute.For<IConfigurationProvider>();
        solutionInfoProvider = Substitute.For<ISolutionInfoProvider>();
        solutionBindingRepository = Substitute.For<ISolutionBindingRepository>();
        roslynSuppressionUpdater = Substitute.For<IRoslynSuppressionUpdater>();
        threadHandling = Substitute.For<IThreadHandling>();
        logger = Substitute.For<ILogger>();
        logger.ForContext(Arg.Any<string[]>()).Returns(logger);

        testSubject = new RoslynSettingsFileSynchronizer(
            roslynSettingsFileStorage,
            configProvider,
            solutionInfoProvider,
            solutionBindingRepository,
            roslynSuppressionUpdater,
            logger,
            threadHandling);
        threadHandling.SwitchToBackgroundThread().Returns(new NoOpThreadHandler.NoOpAwaitable());
        MockSolutionInfoProvider(DefaultSln);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RoslynSettingsFileSynchronizer, IRoslynSettingsFileSynchronizer>(
            MefTestHelpers.CreateExport<IServerIssuesStore>(),
            MefTestHelpers.CreateExport<IRoslynSettingsFileStorage>(),
            MefTestHelpers.CreateExport<IConfigurationProvider>(),
            MefTestHelpers.CreateExport<ISolutionInfoProvider>(),
            MefTestHelpers.CreateExport<ISolutionBindingRepository>(),
            MefTestHelpers.CreateExport<IRoslynSuppressionUpdater>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckTypeIsNonShared() => MefTestHelpers.CheckIsNonSharedMefComponent<RoslynSettingsFileSynchronizer>();

    [TestMethod]
    public void Ctor_SetsLogContext() => logger.Received(1).ForContext(nameof(RoslynSettingsFileSynchronizer));

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
        var fullSolutionFilePath = "c:\\aaa\\MySolution1.sln";
        MockSolutionInfoProvider(fullSolutionFilePath);
        MockConfigProvider(BindingConfiguration.Standalone);

        RaiseSuppressedIssuesReloaded([]);

        configProvider.Received(1).GetConfiguration();
        roslynSettingsFileStorage.Received(1).Delete(Path.GetFileNameWithoutExtension(fullSolutionFilePath));
        roslynSettingsFileStorage.ReceivedCalls().Should().HaveCount(1);
        logger.DidNotReceive().LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerReloadSuppressions);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)] // should update storage even when there are no issues
    public void SuppressedIssuesReloaded_ConnectedMode_StorageUpdated(bool hasIssues)
    {
        MockConfigProvider(connectedBindingConfiguration);
        MockSolutionInfoProvider("c:\\aaa\\MySolution1.sln");
        var issues = hasIssues ? new[] { csharpIssueSuppressed, vbNetIssueSuppressed } : Array.Empty<SonarQubeIssue>();

        RaiseSuppressedIssuesReloaded(issues);

        roslynSettingsFileStorage.Received(1).Update(Arg.Any<RoslynSettings>(), "MySolution1");
        logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerReloadSuppressions);
    }

    [TestMethod]
    public void SuppressedIssuesReloaded_FileStorageIsUpdatedOnBackgroundThread()
    {
        MockConfigProvider(connectedBindingConfiguration);
        var allSonarQubeIssues = new[] { CreateSonarQubeIssue() };

        RaiseSuppressedIssuesReloaded(allSonarQubeIssues);

        threadHandling.Received(1).SwitchToBackgroundThread();
    }

    [TestMethod]
    public void SuppressedIssuesReloaded_NoSolution_DoesNothing()
    {
        MockSolutionInfoProvider(null);

        RaiseSuppressedIssuesReloaded([]);

        threadHandling.Received(1).SwitchToBackgroundThread();
        solutionInfoProvider.Received(1).GetFullSolutionFilePathAsync();
        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Update(default, default);
        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Delete(default);
        configProvider.DidNotReceiveWithAnyArgs().GetConfiguration();
    }

    [TestMethod]
    public void SuppressedIssuesReloaded_IssuesAreConvertedAndFiltered()
    {
        MockConfigProvider(connectedBindingConfiguration);
        var allSonarQubeIssues = new[]
        {
            csharpIssueSuppressed, // C# issue
            vbNetIssueSuppressed, // VB issue
            cppIssueSuppressed, // C++ issue - ignored
            unknownRepoIssue, // unrecognised repo - ignored
            invalidRepoKeyIssue, // invalid repo key - ignored
            noRuleIdIssue // invalid repo key (no rule id) - ignored
        };

        RaiseSuppressedIssuesReloaded(allSonarQubeIssues);

        VerifyExpectedSuppressionsSaved(csharpIssueSuppressed, vbNetIssueSuppressed);
        logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerReloadSuppressions);
    }

    [TestMethod]
    public void SuppressedIssuesReloaded_OnlySuppressedIssuesAreInSettings()
    {
        MockConfigProvider(connectedBindingConfiguration);
        var allSonarQubeIssues = new[] { csharpIssueSuppressed, vbNetIssueSuppressed, csharpIssueNotSuppressed, vbNetIssueNotSuppressed };

        RaiseSuppressedIssuesReloaded(allSonarQubeIssues);

        VerifyExpectedSuppressionsSaved(csharpIssueSuppressed, vbNetIssueSuppressed);
        logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerReloadSuppressions);
    }

    [TestMethod]
    public void NewIssuesSuppressed_StandaloneMode_StorageFileDeleted()
    {
        var fullSolutionFilePath = "c:\\aaa\\MySolution1.sln";
        MockSolutionInfoProvider(fullSolutionFilePath);
        MockConfigProvider(BindingConfiguration.Standalone);
        var newSonarQubeIssues = new[] { csharpIssueSuppressed };

        RaiseNewIssuesSuppressed(newSonarQubeIssues);

        configProvider.Received(1).GetConfiguration();
        roslynSettingsFileStorage.Received(1).Delete(Path.GetFileNameWithoutExtension(fullSolutionFilePath));
        roslynSettingsFileStorage.ReceivedCalls().Should().HaveCount(1);
        logger.DidNotReceive().LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerAddNewSuppressions);
    }

    [TestMethod]
    public void NewIssuesSuppressed_NoIssues_StorageNotUpdated()
    {
        MockConfigProvider(connectedBindingConfiguration);
        MockSolutionInfoProvider("c:\\aaa\\MySolution1.sln");

        RaiseNewIssuesSuppressed([]);

        roslynSettingsFileStorage.DidNotReceive().Update(Arg.Any<RoslynSettings>(), "MySolution1");
        logger.DidNotReceive().LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerAddNewSuppressions);
    }

    [TestMethod]
    public void NewIssuesSuppressed_FileStorageIsUpdatedOnBackgroundThread()
    {
        MockConfigProvider(connectedBindingConfiguration);
        var newSonarQubeIssues = new[] { csharpIssueSuppressed };

        RaiseNewIssuesSuppressed(newSonarQubeIssues);

        threadHandling.Received(1).SwitchToBackgroundThread();
    }

    [TestMethod]
    public void NewIssuesSuppressed_NoSolution_DoesNothing()
    {
        MockSolutionInfoProvider(null);
        var newSonarQubeIssues = new[] { csharpIssueSuppressed };

        RaiseNewIssuesSuppressed(newSonarQubeIssues);

        threadHandling.Received(1).SwitchToBackgroundThread();
        solutionInfoProvider.Received(1).GetFullSolutionFilePathAsync();
        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Update(default, default);
        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Delete(default);
        configProvider.DidNotReceiveWithAnyArgs().GetConfiguration();
    }

    [TestMethod]
    public void NewIssuesSuppressed_IssueDoNotExist_IssuesAreConvertedAndFiltered()
    {
        MockConfigProvider(connectedBindingConfiguration);
        MockExistingSuppressionsOnSettingsFile();
        var newSonarQubeIssues = new[]
        {
            csharpIssueSuppressed, // C# issue
            vbNetIssueSuppressed, // VB issue
            cppIssueSuppressed, // C++ issue - ignored
            unknownRepoIssue, // unrecognised repo - ignored
            invalidRepoKeyIssue, // invalid repo key - ignored
            noRuleIdIssue // invalid repo key (no rule id) - ignored
        };

        RaiseNewIssuesSuppressed(newSonarQubeIssues);

        VerifyExpectedSuppressionsSaved(csharpIssueSuppressed, vbNetIssueSuppressed);
        logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerAddNewSuppressions);
    }

    [TestMethod]
    public void NewIssuesSuppressed_IssueDoNotExist_OnlySuppressedIssuesAreInSettings()
    {
        MockConfigProvider(connectedBindingConfiguration);
        var newSonarQubeIssues = new[] { csharpIssueSuppressed, vbNetIssueSuppressed, csharpIssueNotSuppressed, vbNetIssueNotSuppressed };

        RaiseNewIssuesSuppressed(newSonarQubeIssues);

        VerifyExpectedSuppressionsSaved(csharpIssueSuppressed, vbNetIssueSuppressed);
        logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerAddNewSuppressions);
    }

    [TestMethod]
    public void NewIssuesSuppressed_IssuesExist_UpdatesCorrectly()
    {
        var newSonarQubeIssues = new[] { csharpIssueSuppressed, vbNetIssueSuppressed };
        MockConfigProvider(connectedBindingConfiguration);
        MockExistingSuppressionsOnSettingsFile(newSonarQubeIssues);

        RaiseNewIssuesSuppressed(newSonarQubeIssues);

        VerifyExpectedSuppressionsSaved(newSonarQubeIssues);
        logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerAddNewSuppressions);
    }

    [TestMethod]
    public void NewIssuesSuppressed_TwoNewIssuesAdded_DoesNotRemoveExistingOnes()
    {
        MockConfigProvider(connectedBindingConfiguration);
        var newSonarQubeIssues = new[] { CreateSonarQubeIssue("csharpsquid:S666"), CreateSonarQubeIssue("vbnet:S666") };
        var existingIssues = new[] { csharpIssueSuppressed, vbNetIssueSuppressed, };
        MockExistingSuppressionsOnSettingsFile(existingIssues);

        RaiseNewIssuesSuppressed(newSonarQubeIssues);

        var expectedIssues = existingIssues.Union(newSonarQubeIssues).ToArray();
        VerifyExpectedSuppressionsSaved(expectedIssues);
        logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerAddNewSuppressions);
    }

    [TestMethod]
    public void NewIssuesResolved_StandaloneMode_StorageFileDeleted()
    {
        var fullSolutionFilePath = "c:\\aaa\\MySolution1.sln";
        MockSolutionInfoProvider(fullSolutionFilePath);
        MockConfigProvider(BindingConfiguration.Standalone);
        var newSonarQubeIssues = new[] { csharpIssueSuppressed.IssueKey };

        RaiseSuppressionsRemoved(newSonarQubeIssues);

        configProvider.Received(1).GetConfiguration();
        roslynSettingsFileStorage.Received(1).Delete(Path.GetFileNameWithoutExtension(fullSolutionFilePath));
        roslynSettingsFileStorage.ReceivedCalls().Should().HaveCount(1);
        logger.DidNotReceive().LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerRemoveSuppressions);
    }

    [TestMethod]
    public void NewIssuesResolved_NoIssues_StorageNotUpdated()
    {
        MockConfigProvider(connectedBindingConfiguration);
        MockSolutionInfoProvider("c:\\aaa\\MySolution1.sln");

        RaiseSuppressionsRemoved([]);

        solutionInfoProvider.DidNotReceive().GetFullSolutionFilePathAsync();
        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Update(default, default);
        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Delete(default);
        configProvider.DidNotReceiveWithAnyArgs().GetConfiguration();
    }

    [TestMethod]
    public void NewIssuesResolved_FileStorageIsUpdatedOnBackgroundThread()
    {
        MockConfigProvider(connectedBindingConfiguration);
        var newSonarQubeIssues = new[] { csharpIssueSuppressed.IssueKey };

        RaiseSuppressionsRemoved(newSonarQubeIssues);

        threadHandling.Received(1).SwitchToBackgroundThread();
    }

    [TestMethod]
    public void NewIssuesResolved_NoSolution_DoesNothing()
    {
        MockSolutionInfoProvider(null);
        var newSonarQubeIssues = new[] { csharpIssueSuppressed.IssueKey };

        RaiseSuppressionsRemoved(newSonarQubeIssues);

        threadHandling.Received(1).SwitchToBackgroundThread();
        solutionInfoProvider.Received(1).GetFullSolutionFilePathAsync();
        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Update(default, default);
        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Delete(default);
        configProvider.DidNotReceiveWithAnyArgs().GetConfiguration();
    }

    [TestMethod]
    public void NewIssuesResolved_IssueDoNotExistInFile_DoesNotUpdateFile()
    {
        MockConfigProvider(connectedBindingConfiguration);
        MockExistingSuppressionsOnSettingsFile();
        var newSonarQubeIssues = new[]
        {
            csharpIssueSuppressed.IssueKey, // C# issue
            vbNetIssueSuppressed.IssueKey, // VB issue
            cppIssueSuppressed.IssueKey, // C++ issue - ignored
            unknownRepoIssue.IssueKey, // unrecognised repo - ignored
            invalidRepoKeyIssue.IssueKey, // invalid repo key - ignored
            noRuleIdIssue.IssueKey // invalid repo key (no rule id) - ignored
        };

        RaiseSuppressionsRemoved(newSonarQubeIssues);

        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Update(default, default);
        logger.DidNotReceive().LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerRemoveSuppressions);
    }

    [TestMethod]
    public void NewIssuesResolved_IssueKeysExistInFile_RemovesIssues()
    {
        var newSonarQubeIssues = new[] { csharpIssueSuppressed, vbNetIssueSuppressed };
        MockConfigProvider(connectedBindingConfiguration);
        MockExistingSuppressionsOnSettingsFile(newSonarQubeIssues);

        RaiseSuppressionsRemoved(newSonarQubeIssues.Select(x => x.IssueKey).ToArray());

        VerifyExpectedSuppressionsSaved([]);
        logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerRemoveSuppressions);
    }

    [TestMethod]
    public void NewIssuesResolved_OneNewIssueResolved_DoesNotRemoveExistingOne()
    {
        MockConfigProvider(connectedBindingConfiguration);
        var existingIssues = new[] { csharpIssueSuppressed, vbNetIssueSuppressed, };
        MockExistingSuppressionsOnSettingsFile(existingIssues);

        RaiseSuppressionsRemoved([csharpIssueSuppressed.IssueKey]);

        VerifyExpectedSuppressionsSaved([vbNetIssueSuppressed]);
        logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerRemoveSuppressions);
    }

    [TestMethod]
    public void NewIssuesResolved_MultipleIssuesWithSameIssueServerKeyExistsInFile_RemovesThemAll()
    {
        MockConfigProvider(connectedBindingConfiguration);
        var existingIssues = new[] { csharpIssueSuppressed, CreateSonarQubeIssue(issueKey: csharpIssueSuppressed.IssueKey), CreateSonarQubeIssue(issueKey: csharpIssueSuppressed.IssueKey) };
        MockExistingSuppressionsOnSettingsFile(existingIssues);

        RaiseSuppressionsRemoved([csharpIssueSuppressed.IssueKey]);

        VerifyExpectedSuppressionsSaved([]);
        logger.Received(1).LogVerbose(Resources.Strings.RoslynSettingsFileSynchronizerRemoveSuppressions);
    }

    private void MockConfigProvider(BindingConfiguration configuration) => configProvider.GetConfiguration().Returns(configuration);

    private static BindingConfiguration CreateConnectedConfiguration(string projectKey)
    {
        var project = new BoundServerProject("solution", projectKey, new ServerConnection.SonarQube(new Uri("http://bound")));

        return BindingConfiguration.CreateBoundConfiguration(project, SonarLintMode.Connected, "some directory");
    }

    private void MockSolutionInfoProvider(string fullSolutionNameToReturn) => solutionInfoProvider.GetFullSolutionFilePathAsync().Returns(fullSolutionNameToReturn);

    private void VerifyExpectedSuppressionsSaved(params SonarQubeIssue[] expectedIssues) =>
        roslynSettingsFileStorage.Received(1).Update(Arg.Is<RoslynSettings>(x => VerifyActualSettingsHasSuppressions(x, expectedIssues)), Arg.Any<string>());

    private bool VerifyActualSettingsHasSuppressions(RoslynSettings actualSettings, params SonarQubeIssue[] expectedIssues)
    {
        actualSettings.Should().NotBeNull();
        actualSettings.SonarProjectKey.Should().Be(connectedBindingConfiguration.Project.ServerProjectKey);
        actualSettings.Suppressions.Should().NotBeNull();

        var actualSuppressions = actualSettings.Suppressions.ToList();
        actualSuppressions.Should().HaveCount(expectedIssues.Length);
        actualSuppressions.Should().BeEquivalentTo(expectedIssues.Select(IssueConverter.Convert));
        return true;
    }

    private void RaiseSuppressedIssuesReloaded(SonarQubeIssue[] issues) => roslynSuppressionUpdater.SuppressedIssuesReloaded += Raise.EventWith(null, new SuppressionsEventArgs(issues));

    private void RaiseNewIssuesSuppressed(SonarQubeIssue[] issues) => roslynSuppressionUpdater.NewIssuesSuppressed += Raise.EventWith(null, new SuppressionsEventArgs(issues));

    private void RaiseSuppressionsRemoved(string[] issueKeys) => roslynSuppressionUpdater.SuppressionsRemoved += Raise.EventWith(null, new SuppressionsRemovedEventArgs(issueKeys));

    private void MockExistingSuppressionsOnSettingsFile(params SonarQubeIssue[] existingIssues) =>
        roslynSettingsFileStorage.Get(Arg.Any<string>()).Returns(existingIssues.Length == 0
            ? RoslynSettings.Empty
            : new RoslynSettings { SonarProjectKey = connectedBindingConfiguration.Project.ServerProjectKey, Suppressions = existingIssues.Select(IssueConverter.Convert) });
}
