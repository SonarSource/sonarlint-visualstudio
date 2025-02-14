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

using System.IO;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Roslyn.Suppressions.InProcess;
using SonarLint.VisualStudio.Roslyn.Suppressions.SettingsFile;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models;
using EventHandler = System.EventHandler;
using static SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.TestHelper;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.InProcess;

[TestClass]
public class RoslynSettingsFileSynchronizerTests
{
    private const string DefaultSln = "DefaultSolution.sln";
    private IConfigurationProvider configProvider;
    private TestLogger logger;
    private IRoslynSettingsFileStorage roslynSettingsFileStorage;
    private IServerIssuesStore serverIssuesStore;
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
        serverIssuesStore = Substitute.For<IServerIssuesStore>();
        roslynSettingsFileStorage = Substitute.For<IRoslynSettingsFileStorage>();
        configProvider = Substitute.For<IConfigurationProvider>();
        solutionInfoProvider = Substitute.For<ISolutionInfoProvider>();
        solutionBindingRepository = Substitute.For<ISolutionBindingRepository>();
        roslynSuppressionUpdater = Substitute.For<IRoslynSuppressionUpdater>();
        threadHandling = Substitute.For<IThreadHandling>();
        logger = new TestLogger();

        testSubject = new RoslynSettingsFileSynchronizer(serverIssuesStore,
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
    public void Ctor_RegisterToEvents()
    {
        serverIssuesStore.Received(1).ServerIssuesChanged += Arg.Any<EventHandler>();
        VerifyServerIssuesStoreNoOtherCalls();

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

        serverIssuesStore.Received(1).ServerIssuesChanged -= Arg.Any<EventHandler>();
        solutionBindingRepository.Received(1).BindingDeleted -= Arg.Any<EventHandler<LocalBindingKeyEventArgs>>();
        roslynSuppressionUpdater.Received(1).SuppressedIssuesReloaded -= Arg.Any<EventHandler<SuppressionsEventArgs>>();
        roslynSuppressionUpdater.Received(1).NewIssuesSuppressed -= Arg.Any<EventHandler<SuppressionsEventArgs>>();
        roslynSuppressionUpdater.Received(1).SuppressionsRemoved -= Arg.Any<EventHandler<SuppressionsRemovedEventArgs>>();
    }

    [TestMethod]
    public void OnSuppressionsUpdateRequested_StandaloneMode_StorageFileDeleted()
    {
        var fullSolutionFilePath = "c:\\aaa\\MySolution1.sln";
        MockSolutionInfoProvider(fullSolutionFilePath);
        MockConfigProvider(BindingConfiguration.Standalone);

        serverIssuesStore.ServerIssuesChanged += Raise.EventWith<EventArgs>();

        configProvider.Received(1).GetConfiguration();
        roslynSettingsFileStorage.Received(1).Delete(Path.GetFileNameWithoutExtension(fullSolutionFilePath));
        roslynSettingsFileStorage.ReceivedCalls().Should().HaveCount(1);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)] // should update storage even when there are no issues
    public void OnSuppressionsUpdateRequested_ConnectedMode_StorageUpdated(bool hasIssues)
    {
        MockConfigProvider(connectedBindingConfiguration);
        MockSolutionInfoProvider("c:\\aaa\\MySolution1.sln");
        var issues = hasIssues ? new[] { CreateSonarQubeIssue(), CreateSonarQubeIssue() } : Array.Empty<SonarQubeIssue>();
        MockServerIssuesStore(issues);

        serverIssuesStore.ServerIssuesChanged += Raise.EventWith<EventArgs>();

        roslynSettingsFileStorage.Received(1).Update(Arg.Any<RoslynSettings>(), "MySolution1");
    }

    [TestMethod]
    public async Task UpdateFileStorage_FileStorageIsUpdatedOnBackgroundThread()
    {
        MockConfigProvider(connectedBindingConfiguration);
        var issues = new[] { CreateSonarQubeIssue() };
        MockServerIssuesStore(issues);

        await testSubject.UpdateFileStorageAsync();

        threadHandling.Received(1).SwitchToBackgroundThread();
        roslynSettingsFileStorage.ReceivedCalls().Should().HaveCount(1);
        configProvider.ReceivedCalls().Should().HaveCount(1);
        roslynSettingsFileStorage.Received(1).Update(Arg.Any<RoslynSettings>(), Arg.Any<string>());
    }

    [TestMethod]
    public async Task UpdateFileStorage_NoSolution_DoesNothing()
    {
        MockSolutionInfoProvider(null);

        await testSubject.UpdateFileStorageAsync();

        threadHandling.Received(1).SwitchToBackgroundThread();
        solutionInfoProvider.Received(1).GetFullSolutionFilePathAsync();
        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Update(default, default);
        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Delete(default);
        configProvider.DidNotReceiveWithAnyArgs().GetConfiguration();
    }

    [TestMethod]
    public async Task UpdateFileStorage_IssuesAreConvertedAndFiltered()
    {
        MockConfigProvider(connectedBindingConfiguration);
        var sonarIssues = new[]
        {
            CreateSonarQubeIssue("csharpsquid:S111"), // C# issue
            CreateSonarQubeIssue("vbnet:S222"), // VB issue
            CreateSonarQubeIssue("cpp:S333"), // C++ issue - ignored
            CreateSonarQubeIssue("xxx:S444"), // unrecognised repo - ignored
            CreateSonarQubeIssue("xxxS555"), // invalid repo key - ignored
            CreateSonarQubeIssue("xxx:") // invalid repo key (no rule id) - ignored
        };
        MockServerIssuesStore(sonarIssues);
        RoslynSettings actualSettings = null;
        roslynSettingsFileStorage.When(x => x.Update(Arg.Any<RoslynSettings>(), Arg.Any<string>()))
            .Do(callInfo => actualSettings = callInfo.Arg<RoslynSettings>());

        await testSubject.UpdateFileStorageAsync();

        actualSettings.Should().NotBeNull();
        actualSettings.SonarProjectKey.Should().Be("some project key");
        actualSettings.Suppressions.Should().NotBeNull();

        var actualSuppressions = actualSettings.Suppressions.ToList();
        actualSuppressions.Count.Should().Be(2);
        actualSuppressions[0].RoslynLanguage.Should().Be(RoslynLanguage.CSharp);
        actualSuppressions[0].RoslynRuleId.Should().Be("S111");
        actualSuppressions[1].RoslynLanguage.Should().Be(RoslynLanguage.VB);
        actualSuppressions[1].RoslynRuleId.Should().Be("S222");
    }

    [TestMethod]
    public async Task UpdateFileStorage_OnlySuppressedIssuesAreInSettings()
    {
        MockConfigProvider(connectedBindingConfiguration);
        var sonarIssues = new[]
        {
            CreateSonarQubeIssue("csharpsquid:S111", isSuppressed: false), CreateSonarQubeIssue("vbnet:S222", isSuppressed: true), CreateSonarQubeIssue("csharpsquid:S333", isSuppressed: true),
            CreateSonarQubeIssue("vbnet:S444", isSuppressed: false)
        };
        MockServerIssuesStore(sonarIssues);
        RoslynSettings actualSettings = null;
        roslynSettingsFileStorage.When(x => x.Update(Arg.Any<RoslynSettings>(), Arg.Any<string>()))
            .Do(callInfo => actualSettings = callInfo.Arg<RoslynSettings>());

        await testSubject.UpdateFileStorageAsync();

        actualSettings.Should().NotBeNull();
        actualSettings.SonarProjectKey.Should().Be("some project key");
        actualSettings.Suppressions.Should().NotBeNull();

        var actualSuppressions = actualSettings.Suppressions.ToList();
        actualSuppressions.Count.Should().Be(2);
        actualSuppressions[0].RoslynLanguage.Should().Be(RoslynLanguage.VB);
        actualSuppressions[0].RoslynRuleId.Should().Be("S222");
        actualSuppressions[1].RoslynLanguage.Should().Be(RoslynLanguage.CSharp);
        actualSuppressions[1].RoslynRuleId.Should().Be("S333");
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
    }

    [TestMethod]
    public void SuppressedIssuesReloaded_OnlySuppressedIssuesAreInSettings()
    {
        MockConfigProvider(connectedBindingConfiguration);
        var allSonarQubeIssues = new[] { csharpIssueSuppressed, vbNetIssueSuppressed, csharpIssueNotSuppressed, vbNetIssueNotSuppressed };

        RaiseSuppressedIssuesReloaded(allSonarQubeIssues);

        VerifyExpectedSuppressionsSaved(csharpIssueSuppressed, vbNetIssueSuppressed);
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
    }

    [TestMethod]
    public void NewIssuesSuppressed_NoIssues_StorageNotUpdated()
    {
        MockConfigProvider(connectedBindingConfiguration);
        MockSolutionInfoProvider("c:\\aaa\\MySolution1.sln");

        RaiseNewIssuesSuppressed([]);

        roslynSettingsFileStorage.DidNotReceive().Update(Arg.Any<RoslynSettings>(), "MySolution1");
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
    }

    [TestMethod]
    public void NewIssuesSuppressed_IssueDoNotExist_OnlySuppressedIssuesAreInSettings()
    {
        MockConfigProvider(connectedBindingConfiguration);
        var newSonarQubeIssues = new[] { csharpIssueSuppressed, vbNetIssueSuppressed, csharpIssueNotSuppressed, vbNetIssueNotSuppressed };

        RaiseNewIssuesSuppressed(newSonarQubeIssues);

        VerifyExpectedSuppressionsSaved(csharpIssueSuppressed, vbNetIssueSuppressed);
    }

    [TestMethod]
    public void NewIssuesSuppressed_IssuesExist_UpdatesCorrectly()
    {
        var newSonarQubeIssues = new[] { csharpIssueSuppressed, vbNetIssueSuppressed };
        MockConfigProvider(connectedBindingConfiguration);
        MockExistingSuppressionsOnSettingsFile(newSonarQubeIssues);

        RaiseNewIssuesSuppressed(newSonarQubeIssues);

        VerifyExpectedSuppressionsSaved(newSonarQubeIssues);
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
    }

    [TestMethod]
    public void NewIssuesResolved_StandaloneMode_StorageFileDeleted()
    {
        var fullSolutionFilePath = "c:\\aaa\\MySolution1.sln";
        MockSolutionInfoProvider(fullSolutionFilePath);
        MockConfigProvider(BindingConfiguration.Standalone);
        var newSonarQubeIssues = new[] { csharpIssueSuppressed.IssueKey };

        RaiseNewIssuesResolved(newSonarQubeIssues);

        configProvider.Received(1).GetConfiguration();
        roslynSettingsFileStorage.Received(1).Delete(Path.GetFileNameWithoutExtension(fullSolutionFilePath));
        roslynSettingsFileStorage.ReceivedCalls().Should().HaveCount(1);
    }

    [TestMethod]
    public void NewIssuesResolved_NoIssues_StorageNotUpdated()
    {
        MockConfigProvider(connectedBindingConfiguration);
        MockSolutionInfoProvider("c:\\aaa\\MySolution1.sln");

        RaiseNewIssuesResolved([]);

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

        RaiseNewIssuesResolved(newSonarQubeIssues);

        threadHandling.Received(1).SwitchToBackgroundThread();
    }

    [TestMethod]
    public void NewIssuesResolved_NoSolution_DoesNothing()
    {
        MockSolutionInfoProvider(null);
        var newSonarQubeIssues = new[] { csharpIssueSuppressed.IssueKey };

        RaiseNewIssuesResolved(newSonarQubeIssues);

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

        RaiseNewIssuesResolved(newSonarQubeIssues);

        roslynSettingsFileStorage.DidNotReceiveWithAnyArgs().Update(default, default);
    }

    [TestMethod]
    public void NewIssuesResolved_IssueKeysExistInFile_RemovesIssues()
    {
        var newSonarQubeIssues = new[] { csharpIssueSuppressed, vbNetIssueSuppressed };
        MockConfigProvider(connectedBindingConfiguration);
        MockExistingSuppressionsOnSettingsFile(newSonarQubeIssues);

        RaiseNewIssuesResolved(newSonarQubeIssues.Select(x => x.IssueKey).ToArray());

        VerifyExpectedSuppressionsSaved([]);
    }

    [TestMethod]
    public void NewIssuesResolved_OneNewIssueResolved_DoesNotRemoveExistingOne()
    {
        MockConfigProvider(connectedBindingConfiguration);
        var existingIssues = new[] { csharpIssueSuppressed, vbNetIssueSuppressed, };
        MockExistingSuppressionsOnSettingsFile(existingIssues);

        RaiseNewIssuesResolved([csharpIssueSuppressed.IssueKey]);

        VerifyExpectedSuppressionsSaved([vbNetIssueSuppressed]);
    }

    [TestMethod]
    public void NewIssuesResolved_MultipleIssuesWithSameIssueServerKeyExistsInFile_RemovesThemAll()
    {
        MockConfigProvider(connectedBindingConfiguration);
        var existingIssues = new[] { csharpIssueSuppressed, CreateSonarQubeIssue(issueKey: csharpIssueSuppressed.IssueKey), CreateSonarQubeIssue(issueKey: csharpIssueSuppressed.IssueKey) };
        MockExistingSuppressionsOnSettingsFile(existingIssues);

        RaiseNewIssuesResolved([csharpIssueSuppressed.IssueKey]);

        VerifyExpectedSuppressionsSaved([]);
    }

    private void MockConfigProvider(BindingConfiguration configuration) => configProvider.GetConfiguration().Returns(configuration);

    private void MockServerIssuesStore(IEnumerable<SonarQubeIssue> issues = null) => serverIssuesStore.Get().Returns(issues);

    private static BindingConfiguration CreateConnectedConfiguration(string projectKey)
    {
        var project = new BoundServerProject("solution", projectKey, new ServerConnection.SonarQube(new Uri("http://bound")));

        return BindingConfiguration.CreateBoundConfiguration(project, SonarLintMode.Connected, "some directory");
    }

    private void MockSolutionInfoProvider(string fullSolutionNameToReturn) => solutionInfoProvider.GetFullSolutionFilePathAsync().Returns(fullSolutionNameToReturn);

    private void VerifyServerIssuesStoreNoOtherCalls() => serverIssuesStore.ReceivedCalls().Should().HaveCount(1); // no other calls

    private void VerifyExpectedSuppressionsSaved(params SonarQubeIssue[] expectedIssues) =>
        roslynSettingsFileStorage.Received(1).Update(Arg.Is<RoslynSettings>(x => VerifyActualSettingsHasSuppressions(x, expectedIssues)), Arg.Any<string>());

    private bool VerifyActualSettingsHasSuppressions(RoslynSettings actualSettings, params SonarQubeIssue[] expectedIssues)
    {
        actualSettings.Should().NotBeNull();
        actualSettings.SonarProjectKey.Should().Be(connectedBindingConfiguration.Project.ServerProjectKey);
        actualSettings.Suppressions.Should().NotBeNull();

        var actualSuppressions = actualSettings.Suppressions.ToList();
        actualSuppressions.Count.Should().Be(expectedIssues.Length);
        actualSuppressions.Should().BeEquivalentTo(expectedIssues.Select(RoslynSettingsFileSynchronizer.IssueConverter.Convert));
        return true;
    }

    private void RaiseSuppressedIssuesReloaded(SonarQubeIssue[] issues) => roslynSuppressionUpdater.SuppressedIssuesReloaded += Raise.EventWith(null, new SuppressionsEventArgs(issues));

    private void RaiseNewIssuesSuppressed(SonarQubeIssue[] issues) => roslynSuppressionUpdater.NewIssuesSuppressed += Raise.EventWith(null, new SuppressionsEventArgs(issues));

    private void RaiseNewIssuesResolved(string[] issueKeys) => roslynSuppressionUpdater.SuppressionsRemoved += Raise.EventWith(null, new SuppressionsRemovedEventArgs(issueKeys));

    private void MockExistingSuppressionsOnSettingsFile(params SonarQubeIssue[] existingIssues) =>
        roslynSettingsFileStorage.Get(Arg.Any<string>()).Returns(existingIssues.Length == 0
            ? RoslynSettings.Empty
            : new RoslynSettings
            {
                SonarProjectKey = connectedBindingConfiguration.Project.ServerProjectKey, Suppressions = existingIssues.Select(RoslynSettingsFileSynchronizer.IssueConverter.Convert)
            });
}
