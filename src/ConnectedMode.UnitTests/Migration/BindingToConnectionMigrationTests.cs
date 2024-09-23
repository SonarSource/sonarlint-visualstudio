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

using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.Migration;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration;

[TestClass]
public class BindingToConnectionMigrationTests
{
    private BindingToConnectionMigration testSubject;
    private IServerConnectionsRepository serverConnectionsRepository;
    private ILegacySolutionBindingRepository legacyBindingRepository;
    private IUnintrusiveBindingPathProvider unintrusiveBindingPathProvider;
    private IThreadHandling threadHandling;
    private ILogger logger;
    private ISolutionBindingRepository solutionBindingRepository;

    [TestInitialize]
    public void TestInitialize()
    {
        serverConnectionsRepository = Substitute.For<IServerConnectionsRepository>();
        legacyBindingRepository = Substitute.For<ILegacySolutionBindingRepository>();
        solutionBindingRepository = Substitute.For<ISolutionBindingRepository>();
        unintrusiveBindingPathProvider = Substitute.For<IUnintrusiveBindingPathProvider>();
        logger = Substitute.For<ILogger>();
        threadHandling = new NoOpThreadHandler();

        testSubject = new BindingToConnectionMigration(
            serverConnectionsRepository,
            legacyBindingRepository,
            solutionBindingRepository,
            unintrusiveBindingPathProvider,
            threadHandling,
            logger);
    }

    [TestMethod]
    public void MefCtor_CheckExports()
    {
        MefTestHelpers.CheckTypeCanBeImported<BindingToConnectionMigration, IBindingToConnectionMigration>(
            MefTestHelpers.CreateExport<IServerConnectionsRepository>(),
            MefTestHelpers.CreateExport<ILegacySolutionBindingRepository>(),
            MefTestHelpers.CreateExport<ISolutionBindingRepository>(),
            MefTestHelpers.CreateExport<IUnintrusiveBindingPathProvider>(),
            MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void Mef_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<BindingToConnectionMigration>();
    }

    [TestMethod]
    public async Task MigrateBindingToServerConnectionIfNeeded_RunsOnBackgroundThread()
    {
        var mockedThreadHandling = Substitute.For<IThreadHandling>();
        var migrateBindingToServer = new BindingToConnectionMigration(
            serverConnectionsRepository,
            legacyBindingRepository,
            solutionBindingRepository,
            unintrusiveBindingPathProvider,
            mockedThreadHandling,
            logger);

        await migrateBindingToServer.MigrateAllBindingsToServerConnectionsIfNeededAsync();

        mockedThreadHandling.ReceivedCalls().Should().Contain(call => call.GetMethodInfo().Name == nameof(IThreadHandling.RunOnBackgroundThread));
    }

    [TestMethod]
    public async Task MigrateBindingToServerConnectionIfNeeded_ConnectionsStorageFileExists_ShouldNotMigrate()
    {
        serverConnectionsRepository.IsConnectionsFileExisting().Returns(true);

        await testSubject.MigrateAllBindingsToServerConnectionsIfNeededAsync();

        serverConnectionsRepository.Received(1).IsConnectionsFileExisting();
        serverConnectionsRepository.DidNotReceiveWithAnyArgs().TryAdd(default);
        unintrusiveBindingPathProvider.DidNotReceive().GetBindingPaths();
    }

    [TestMethod]
    public async Task MigrateBindingToServerConnectionIfNeeded_ConnectionsStorageDoesNotExists_PerformsMigration()
    {
        CreateTwoBindingPathsToMockedBoundProject();

        await testSubject.MigrateAllBindingsToServerConnectionsIfNeededAsync();

        Received.InOrder(() =>
        {
            serverConnectionsRepository.IsConnectionsFileExisting();
            logger.WriteLine(MigrationStrings.ConnectionMigration_StartMigration);
            unintrusiveBindingPathProvider.GetBindingPaths();
            serverConnectionsRepository.TryAdd(Arg.Any<ServerConnection>());
            serverConnectionsRepository.TryAdd(Arg.Any<ServerConnection>());
        });
    }

    [TestMethod]
    public async Task MigrateBindingToServerConnectionIfNeeded_MigrationIsExecuted_CreatesServerConnectionsFromBinding()
    {
        var boundProjects = CreateTwoBindingPathsToMockedBoundProject().Values.ToList();

        await testSubject.MigrateAllBindingsToServerConnectionsIfNeededAsync();

        serverConnectionsRepository.Received(1).TryAdd(Arg.Is<ServerConnection>(conn => conn.Id == boundProjects[0].ServerUri.ToString()));
        serverConnectionsRepository.Received(1).TryAdd(Arg.Is<ServerConnection>(conn => conn.Id == boundProjects[1].ServerUri.ToString()));
    }

    [TestMethod]
    public async Task MigrateBindingToServerConnectionIfNeeded_MigrationIsExecuted_UpdatesBindingWithServerConnection()
    {
        var bindingPathToBoundProjectDictionary = CreateTwoBindingPathsToMockedBoundProject();

        await testSubject.MigrateAllBindingsToServerConnectionsIfNeededAsync();

        CheckBindingsWereMigrated(bindingPathToBoundProjectDictionary);
    }

    [TestMethod]
    public async Task MigrateBindingToServerConnectionIfNeeded_MigrationIsExecutedForTwoBindingsWithTheSameConnections_MigratesServerConnectionOnceAndUpdatesBothBindings()
    {
        var bindingPathToBoundProjectDictionary = CreateTwoBindingPathsToMockedBoundProject();
        var boundProjects = bindingPathToBoundProjectDictionary.Values.ToList();
        var expectedServerConnectionId = boundProjects[0].ServerUri.ToString();
        serverConnectionsRepository.TryGet(expectedServerConnectionId, out _).Returns(true);
        

        await testSubject.MigrateAllBindingsToServerConnectionsIfNeededAsync();

        logger.Received(1).WriteLine(string.Format(MigrationStrings.ConnectionMigration_ExistingServerConnectionNotMigrated, expectedServerConnectionId));
        serverConnectionsRepository.DidNotReceive().TryAdd(Arg.Is<ServerConnection>(conn => conn.Id == expectedServerConnectionId));
        CheckBindingsWereMigrated(bindingPathToBoundProjectDictionary);
    }

    [TestMethod]
    public async Task MigrateBindingToServerConnectionIfNeeded_ReadingBindingReturnsNull_SkipsAndLogs()
    {
        var boundProjects = CreateTwoBindingPathsToMockedBoundProject();
        var bindingPathToExclude = boundProjects.Keys.First();
        legacyBindingRepository.Read(Arg.Is<string>(path => path == bindingPathToExclude)).Returns((BoundSonarQubeProject)null);

        await testSubject.MigrateAllBindingsToServerConnectionsIfNeededAsync();

        logger.Received(1).WriteLine(string.Format(MigrationStrings.ConnectionMigration_BindingNotMigrated, bindingPathToExclude, "legacyBoundProject was not found"));
        serverConnectionsRepository.DidNotReceive().TryAdd(Arg.Is<ServerConnection>(conn => IsExpectedServerConnection(conn, boundProjects.First().Value)));
        solutionBindingRepository.DidNotReceive().Write(bindingPathToExclude, Arg.Any<BoundServerProject>());
    }

    [TestMethod]
    public async Task MigrateBindingToServerConnectionIfNeeded_MigratingConnectionFails_SkipsAndLogs()
    {
        var boundPathToBoundProject = CreateTwoBindingPathsToMockedBoundProject().First();
        serverConnectionsRepository.TryAdd(Arg.Is<ServerConnection>(conn => IsExpectedServerConnection(conn, boundPathToBoundProject.Value))).Returns(false);

        await testSubject.MigrateAllBindingsToServerConnectionsIfNeededAsync();

        logger.Received(1).WriteLine(string.Format(MigrationStrings.ConnectionMigration_ServerConnectionNotMigrated, boundPathToBoundProject.Value.ServerUri));
        serverConnectionsRepository.Received(1).TryAdd(Arg.Is<ServerConnection>(conn => IsExpectedServerConnection(conn, boundPathToBoundProject.Value)));
        solutionBindingRepository.DidNotReceive().Write(boundPathToBoundProject.Key, Arg.Any<BoundServerProject>());
    }

    [TestMethod]
    public async Task MigrateBindingToServerConnectionIfNeeded_MigrationIsExecuted_CredentialsAreMigrated()
    {
        var boundProjects = CreateTwoBindingPathsToMockedBoundProject().Values.ToList();

        await testSubject.MigrateAllBindingsToServerConnectionsIfNeededAsync();

        CheckCredentialsAreLoaded(boundProjects[0]);
        CheckCredentialsAreLoaded(boundProjects[1]);
    }

    [TestMethod]
    public async Task MigrateBindingToServerConnectionIfNeeded_MigrationThrowsException_ErrorIsLogged()
    {
        var boundProjects = CreateTwoBindingPathsToMockedBoundProject();
        var errorMessage = "loading failed";
        solutionBindingRepository.When(repo => repo.Write(Arg.Any<string>(), Arg.Any<BoundServerProject>())).Throw(new Exception(errorMessage));

        await testSubject.MigrateAllBindingsToServerConnectionsIfNeededAsync();

        logger.Received(1).WriteLine(string.Format(MigrationStrings.ConnectionMigration_BindingNotMigrated, boundProjects.First().Key, errorMessage));
        logger.Received(1).WriteLine(string.Format(MigrationStrings.ConnectionMigration_BindingNotMigrated, boundProjects.Last().Key, errorMessage));
    }

    private Dictionary<string, BoundSonarQubeProject> CreateTwoBindingPathsToMockedBoundProject()
    {
        Dictionary<string, BoundSonarQubeProject> pathToBindings = new()
        {
            {"bindings/proj1/binding.config", CreateBoundProject("http://server1", "proj1")},
            {"bindings/proj2/binding.config", CreateBoundProject("http://server2", "proj2")}
        };
        unintrusiveBindingPathProvider.GetBindingPaths().Returns(pathToBindings.Select(kvp => kvp.Key));
        foreach (var kvp in pathToBindings)
        {
            MockValidBinding(kvp.Key, kvp.Value);
        }
        return pathToBindings;
    }

    private void MockValidBinding(string bindingPath, BoundSonarQubeProject sonarQubeProject)
    {
        legacyBindingRepository.Read(bindingPath).Returns(sonarQubeProject);
        serverConnectionsRepository.TryAdd(Arg.Is<ServerConnection>(conn => IsExpectedServerConnection(conn, sonarQubeProject))).Returns(true);
    }

    private static BoundSonarQubeProject CreateBoundProject(string url, string projectKey)
    {
        return new BoundSonarQubeProject(new Uri(url), projectKey, "projectName", credentials: new BasicAuthCredentials("admin", "admin".ToSecureString()));
    }

    private static bool IsExpectedServerConnection(ServerConnection serverConnection, BoundSonarQubeProject boundProject)
    {
        return serverConnection.Id == boundProject.ServerUri.ToString();
    }

    private void CheckCredentialsAreLoaded(BoundSonarQubeProject boundProject)
    {
        serverConnectionsRepository.Received(1).TryAdd(Arg.Is<ServerConnection>(proj =>
            IsExpectedServerConnection(proj, boundProject) && proj.Credentials == boundProject.Credentials));
    }

    private void CheckBindingsWereMigrated(Dictionary<string, BoundSonarQubeProject> bindingPathToBoundProjectDictionary)
    {
        foreach (var boundSonarQubeProject in bindingPathToBoundProjectDictionary)
        {
            solutionBindingRepository.Received(1).Write(boundSonarQubeProject.Key,
                Arg.Is<BoundServerProject>(proj => IsExpectedServerConnection(proj.ServerConnection, boundSonarQubeProject.Value)));
        }
    }
}
