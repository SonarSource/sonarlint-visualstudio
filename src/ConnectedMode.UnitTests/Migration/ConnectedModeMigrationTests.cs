﻿/*
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

using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.Migration;
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration.ConnectedModeMigrationTestsExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration
{
    [TestClass]
    public class ConnectedModeMigrationTests
    {
        private static readonly LegacySettings DefaultTestLegacySettings = new LegacySettings("folder", "cs ruleset", "cs xml", "vb ruleset", "vb xml");

        private static BoundSonarQubeProject AnyBoundProject = new BoundSonarQubeProject(new Uri("http://localhost:9000"), "any-key", "any-name");

        [TestMethod]
        public void MefCtor_CheckTypeIsNonShared()
            => MefTestHelpers.CheckIsNonSharedMefComponent<ConnectedModeMigration>();

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ConnectedModeMigration, IConnectedModeMigration>(
                MefTestHelpers.CreateExport<IMigrationSettingsProvider>(),
                MefTestHelpers.CreateExport<IFileProvider>(),
                MefTestHelpers.CreateExport<IFileCleaner>(),
                MefTestHelpers.CreateExport<IVsAwareFileSystem>(),
                MefTestHelpers.CreateExport<ISonarQubeService>(),
                MefTestHelpers.CreateExport<IUnintrusiveBindingController>(),
                MefTestHelpers.CreateExport<ISuppressionIssueStoreUpdater>(),
                MefTestHelpers.CreateExport<ISharedBindingConfigProvider>(),
                MefTestHelpers.CreateExport<ILogger>(),
                MefTestHelpers.CreateExport<IThreadHandling>(),
                MefTestHelpers.CreateExport<ISolutionInfoProvider>(),
                MefTestHelpers.CreateExport<IServerConnectionsRepository>(),
                MefTestHelpers.CreateExport<IUnintrusiveBindingPathProvider>());
        }

        [TestMethod]
        public async Task Migrate_ExpectedOldBindingIsPassedSettingsProvider()
        {
            var fileProvider = CreateFileProvider();

            var settings = new LegacySettings("expected folder", "any", "any", "any", "any");
            var oldBinding = new BoundSonarQubeProject(new Uri("http://any"), "expected project key", "any");
            var settingsProvider = CreateSettingsProvider(settings);

            var testSubject = CreateTestSubject(fileProvider.Object, settingsProvider: settingsProvider.Object);

            await testSubject.MigrateAsync(oldBinding, null, false, CancellationToken.None);

            settingsProvider.Verify(x => x.GetAsync("expected project key"), Times.Once);
        }

        [TestMethod]
        public async Task Migrate_NoFilesToClean_DirectoryIsDeleted()
        {
            var fileProvider = CreateFileProvider();
            var fileSystem = new Mock<IVsAwareFileSystem>();
            var fileCleaner = new Mock<IFileCleaner>();

            var settings = new LegacySettings("expected folder", "any", "any", "any", "any");
            var settingsProvider = CreateSettingsProvider(settings);

            var testSubject = CreateTestSubject(fileProvider.Object, fileCleaner.Object, fileSystem.Object, settingsProvider.Object);

            await testSubject.MigrateAsync(AnyBoundProject, null, false, CancellationToken.None);

            fileProvider.Verify(x => x.GetFilesAsync(It.IsAny<CancellationToken>()), Times.Once);

            fileCleaner.Invocations.Should().HaveCount(0);
            fileSystem.VerifyNoFilesSaved();
            fileSystem.VerifyDirectoryDeleted("expected folder");
        }

        [DataRow(true, 1)]
        [DataRow(false, 0)]
        [TestMethod]
        public async Task Migrate_SharedBinding_CalledCorrectly(bool isSharedBinding, int callCount)
        {
            var sharedBindingConfigProvider = new Mock<ISharedBindingConfigProvider>();

            var testSubject = CreateTestSubject(sharedBindingConfigProvider: sharedBindingConfigProvider.Object);

            await testSubject.MigrateAsync(AnyBoundProject, null, isSharedBinding, CancellationToken.None);

            sharedBindingConfigProvider.Verify(p => p.SaveSharedBinding(It.IsAny<SharedBindingConfigModel>()), Times.Exactly(callCount));
            sharedBindingConfigProvider.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task Migrate_FilesExist_AllFilesCleanedAndSaved_AndDirectoryDeleted()
        {
            var fileProvider = CreateFileProvider("file1", "file2", "file3");
            var fileSystem = new Mock<IVsAwareFileSystem>();
            fileSystem.AddFile("file1", "content1");
            fileSystem.AddFile("file2", "content2");
            fileSystem.AddFile("file3", "content3");

            var fileCleaner = new Mock<IFileCleaner>();
            fileCleaner.SetupFileToClean("content1", "cleaned1");
            fileCleaner.SetupFileToClean("content2", "cleaned2");
            fileCleaner.SetupFileToClean("content3", "cleaned3");

            var settings = new LegacySettings("root folder", "any", "any", "any", "any");
            var settingsProvider = CreateSettingsProvider(settings);

            var testSubject = CreateTestSubject(fileProvider.Object, fileCleaner.Object, fileSystem.Object, settingsProvider.Object);

            await testSubject.MigrateAsync(AnyBoundProject, null, false, CancellationToken.None);

            fileProvider.Verify(x => x.GetFilesAsync(It.IsAny<CancellationToken>()), Times.Once);
            fileSystem.VerifyFileLoaded("file1");
            fileSystem.VerifyFileLoaded("file2");
            fileSystem.VerifyFileLoaded("file3");

            fileCleaner.Invocations.Should().HaveCount(3);
            fileCleaner.VerifyFileCleaned("content1");
            fileCleaner.VerifyFileCleaned("content2");
            fileCleaner.VerifyFileCleaned("content3");

            fileSystem.VerifyFileSaved("file1", "cleaned1");
            fileSystem.VerifyFileSaved("file2", "cleaned2");
            fileSystem.VerifyFileSaved("file3", "cleaned3");

            fileSystem.VerifyDirectoryDeleted("root folder");
        }

        [TestMethod]
        public async Task Migrate_FilesExist_OnlyChangedFilesAreSaved()
        {
            // Setup - only odd-numbered files are changed -> only they should be saved
            var fileProvider = CreateFileProvider("file1", "file2", "file3", "file4");
            var fileSystem = new Mock<IVsAwareFileSystem>();
            fileSystem.AddFile("file1", "1 original content");
            fileSystem.AddFile("file2", "2 will not change");
            fileSystem.AddFile("file3", "3 original content");
            fileSystem.AddFile("file4", "4 will not change");

            var fileCleaner = new Mock<IFileCleaner>();
            fileCleaner.SetupFileToClean("1 original content", "1 new content");
            fileCleaner.SetupFileToClean("2 will not change", null); // null = unchanged
            fileCleaner.SetupFileToClean("3 original content", "3 new content");
            fileCleaner.SetupFileToClean("4 will not change", null); // null = unchanged

            var testSubject = CreateTestSubject(fileProvider.Object, fileCleaner.Object, fileSystem.Object);

            await testSubject.MigrateAsync(AnyBoundProject, null, false, CancellationToken.None);

            fileProvider.Verify(x => x.GetFilesAsync(It.IsAny<CancellationToken>()), Times.Once);

            fileCleaner.Invocations.Should().HaveCount(4);
            fileCleaner.VerifyFileCleaned("1 original content");
            fileCleaner.VerifyFileCleaned("2 will not change");
            fileCleaner.VerifyFileCleaned("3 original content");
            fileCleaner.VerifyFileCleaned("4 will not change");

            fileSystem.VerifyFileSaved("file1", "1 new content");
            fileSystem.VerifyFileNotSaved("file2");
            fileSystem.VerifyFileSaved("file3", "3 new content");
            fileSystem.VerifyFileNotSaved("file4");

            fileSystem.VerifyDirectoryDeleted("folder"); // TODO - check for the correct path. Depends on #4362
        }

        [TestMethod]
        public void Migrate_NoProgressListener_NoError()
        {
            var testSubject = CreateTestSubject();

            Func<Task> act = async () => await testSubject.MigrateAsync(AnyBoundProject, null, false, CancellationToken.None);

            act.Should().NotThrow();
        }

        [TestMethod]
        public async Task Migrate_HasProgressListener_ReportsProgress()
        {
            var progressMessages = new List<MigrationProgress>();
            var progressListener = new Mock<IProgress<MigrationProgress>>();
            progressListener.Setup(x => x.Report(It.IsAny<MigrationProgress>()))
                .Callback<MigrationProgress>(progressMessages.Add);

            var testSubject = CreateTestSubject();

            await testSubject.MigrateAsync(AnyBoundProject, progressListener.Object, false, CancellationToken.None);

            progressMessages.Should().NotBeEmpty();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Migrate_ConnectToSonarQubeIfNeeded_MigrationSucceeds_DisconnectedNotCalled(bool isAlreadyConnectedToServer)
        {
            var sonarQubeService = CreateSonarQubeService(isAlreadyConnectedToServer);

            var testSubject = CreateTestSubject(sonarQubeService: sonarQubeService.Object);
            await testSubject.MigrateAsync(AnyBoundProject, null, false, CancellationToken.None);

            sonarQubeService.Verify(x => x.ConnectAsync(It.IsAny<ConnectionInformation>(), CancellationToken.None), !isAlreadyConnectedToServer ? Times.Once : Times.Never);
            sonarQubeService.Verify(x => x.Disconnect(), Times.Never);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Migrate_DisconnectToSonarQubeIfNeeded__MigrationFails_NonCritical_HandledAndThrown(bool isAlreadyConnectedToServer)
        {
            var sonarQubeService = CreateSonarQubeService(isAlreadyConnectedToServer);
            var logger = new Mock<ILogger>();
            logger.Setup(x => x.WriteLine(It.IsAny<string>())).Throws(new InvalidCastException("thrown from test"));

            var testSubject = CreateTestSubject(sonarQubeService: sonarQubeService.Object, logger: logger.Object);
            Func<Task> act = async () => { await testSubject.MigrateAsync(AnyBoundProject, null, false, CancellationToken.None); };

            await act.Should().ThrowAsync<InvalidCastException>();
            sonarQubeService.Verify(x => x.Disconnect(), !isAlreadyConnectedToServer ? Times.Once : Times.Never);
        }

        [TestMethod]
        public async Task Migrate_ThrowsCritical_NotHandled()
        {
            var logger = new Mock<ILogger>();
            logger.Setup(x => x.WriteLine(It.IsAny<string>())).Throws(new StackOverflowException("thrown from test"));

            var testSubject = CreateTestSubject(logger: logger.Object);
            Func<Task> act = async () => { await testSubject.MigrateAsync(AnyBoundProject, null, false, CancellationToken.None); };

            await act.Should().ThrowAsync<StackOverflowException>();
        }

        [TestMethod]
        public async Task Migrate_CallBindAsync()
        {
            var unintrusiveBindingController = new Mock<IUnintrusiveBindingController>();
            var cancellationToken = CancellationToken.None;
            var testSubject = CreateTestSubject(unintrusiveBindingController: unintrusiveBindingController.Object);

            await testSubject.MigrateAsync(AnyBoundProject, Mock.Of<IProgress<MigrationProgress>>(), false, cancellationToken);

            unintrusiveBindingController.Verify(
                x => x.BindAsync(
                    It.Is<BoundServerProject>(proj => IsExpectedBoundServerProject(proj)), 
                    It.IsAny<IProgress<FixedStepsProgress>>(), cancellationToken), Times.Once);
        }

        [TestMethod]
        public async Task Migrate_BoundProjectCanNotBeConvertedToServerConnection_LogsAndDoesB()
        {
            var storedConnection = new ServerConnection.SonarQube(AnyBoundProject.ServerUri);
            var unintrusiveBindingControllerMock = new Mock<IUnintrusiveBindingController>();
            var serverConnectionsRepositoryMock = CreateServerConnectionsRepositoryMock();
            MockIServerConnectionsRepositoryTryGet(serverConnectionsRepositoryMock, storedConnection.Id, storedConnection);
            var solutionInfoProviderMock = CreateSolutionInfoProviderMock();
            var testSubject = CreateTestSubject(unintrusiveBindingController: unintrusiveBindingControllerMock.Object,
                serverConnectionsRepository: serverConnectionsRepositoryMock.Object, solutionInfoProvider: solutionInfoProviderMock.Object);

            await testSubject.MigrateAsync(AnyBoundProject, Mock.Of<IProgress<MigrationProgress>>(), false, CancellationToken.None);

            serverConnectionsRepositoryMock.Verify(mock => mock.TryGet(storedConnection.Id, out It.Ref<ServerConnection>.IsAny), Times.Once);
            serverConnectionsRepositoryMock.Verify(mock => mock.TryAdd(IsExpectedServerConnection(It.IsAny<ServerConnection>())), Times.Never);
            solutionInfoProviderMock.Verify(mock => mock.GetSolutionNameAsync(), Times.Once);
            unintrusiveBindingControllerMock.Verify(
                x => x.BindAsync(
                    It.Is<BoundServerProject>(proj => IsExpectedBoundServerProject(proj)),
                    It.IsAny<IProgress<FixedStepsProgress>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Migrate_ConnectionExists_EstablishesBinding()
        {
            var storedConnection = new ServerConnection.SonarQube(AnyBoundProject.ServerUri);
            var unintrusiveBindingControllerMock = new Mock<IUnintrusiveBindingController>();
            var serverConnectionsRepositoryMock = CreateServerConnectionsRepositoryMock();
            MockIServerConnectionsRepositoryTryGet(serverConnectionsRepositoryMock, storedConnection.Id, storedConnection);
            var solutionInfoProviderMock = CreateSolutionInfoProviderMock();
            var testSubject = CreateTestSubject(unintrusiveBindingController: unintrusiveBindingControllerMock.Object,
                serverConnectionsRepository: serverConnectionsRepositoryMock.Object, solutionInfoProvider: solutionInfoProviderMock.Object);

            await testSubject.MigrateAsync(AnyBoundProject, Mock.Of<IProgress<MigrationProgress>>(), false, CancellationToken.None);

            serverConnectionsRepositoryMock.Verify(mock => mock.TryGet(storedConnection.Id, out It.Ref<ServerConnection>.IsAny), Times.Once);
            serverConnectionsRepositoryMock.Verify(mock => mock.TryAdd(IsExpectedServerConnection(It.IsAny<ServerConnection>())), Times.Never);
            solutionInfoProviderMock.Verify(mock => mock.GetSolutionNameAsync(), Times.Once);
            unintrusiveBindingControllerMock.Verify(
                x => x.BindAsync(
                    It.Is<BoundServerProject>(proj => IsExpectedBoundServerProject(proj)),
                    It.IsAny<IProgress<FixedStepsProgress>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Migrate_ConnectionDoesNotExist_AddsConnectionAndEstablishesBinding()
        {
            var unintrusiveBindingControllerMock = new Mock<IUnintrusiveBindingController>();
            var convertedConnection = ServerConnection.FromBoundSonarQubeProject(AnyBoundProject);
            var serverConnectionsRepositoryMock = CreateServerConnectionsRepositoryMock();
            var solutionInfoProviderMock = CreateSolutionInfoProviderMock();
            var testSubject = CreateTestSubject(unintrusiveBindingController: unintrusiveBindingControllerMock.Object,
                serverConnectionsRepository: serverConnectionsRepositoryMock.Object, solutionInfoProvider: solutionInfoProviderMock.Object);
            serverConnectionsRepositoryMock.Setup(x => x.TryAdd(IsExpectedServerConnection(convertedConnection))).Returns(true);

            await testSubject.MigrateAsync(AnyBoundProject, Mock.Of<IProgress<MigrationProgress>>(), false, CancellationToken.None);

            serverConnectionsRepositoryMock.Verify(mock => mock.TryGet(convertedConnection.Id, out It.Ref<ServerConnection>.IsAny), Times.Once);
            serverConnectionsRepositoryMock.Verify(mock => mock.TryAdd(IsExpectedServerConnection(convertedConnection)), Times.Once);
            solutionInfoProviderMock.Verify(mock => mock.GetSolutionNameAsync(), Times.Once);
            unintrusiveBindingControllerMock.Verify(
                x => x.BindAsync(
                    It.Is<BoundServerProject>(proj => IsExpectedBoundServerProject(proj)),
                    It.IsAny<IProgress<FixedStepsProgress>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// If the connection can not be added in the new format (for example, credentials do not exist or are invalid), the old migration should still succeed.
        /// The user can add manually add the connection at a later time.
        /// </summary>
        [TestMethod]
        public async Task Migrate_ConnectionDoesNotExist_CannotAdd_DoesNotCreateConnection()
        {
            var unintrusiveBindingController = new Mock<IUnintrusiveBindingController>();
            var convertedConnection = ServerConnection.FromBoundSonarQubeProject(AnyBoundProject);
            var serverConnectionsRepositoryMock = new Mock<IServerConnectionsRepository>();
            serverConnectionsRepositoryMock.Setup(x => x.ConnectionsFileExists()).Returns(true);
            serverConnectionsRepositoryMock.Setup(x => x.TryAdd(convertedConnection)).Returns(false);
            var testSubject = CreateTestSubject(unintrusiveBindingController: unintrusiveBindingController.Object, serverConnectionsRepository: serverConnectionsRepositoryMock.Object);

            await testSubject.MigrateAsync(AnyBoundProject, Mock.Of<IProgress<MigrationProgress>>(), false, CancellationToken.None);

            serverConnectionsRepositoryMock.Verify(mock => mock.TryGet(convertedConnection.Id, out It.Ref<ServerConnection>.IsAny), Times.Once);
            serverConnectionsRepositoryMock.Verify(mock => mock.TryAdd(IsExpectedServerConnection(convertedConnection)), Times.Once);
            unintrusiveBindingController.Verify(
                x => x.BindAsync(
                    It.Is<BoundServerProject>(proj => IsExpectedBoundServerProject(proj)),
                    It.IsAny<IProgress<FixedStepsProgress>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// If the connection can not be added in the new format for whatever reason, the old migration should still succeed.
        /// The user can add manually add the connection at a later time.
        /// </summary>
        [TestMethod]
        public async Task Migrate_ConnectionDoesNotExist_Throws_DoesNotCreateConnection()
        {
            var unintrusiveBindingController = new Mock<IUnintrusiveBindingController>();
            var convertedConnection = ServerConnection.FromBoundSonarQubeProject(AnyBoundProject);
            var serverConnectionsRepositoryMock = new Mock<IServerConnectionsRepository>();
            serverConnectionsRepositoryMock.Setup(x => x.ConnectionsFileExists()).Returns(true);
            serverConnectionsRepositoryMock.Setup(x => x.TryAdd(convertedConnection)).Throws(new Exception());
            var testSubject = CreateTestSubject(unintrusiveBindingController: unintrusiveBindingController.Object, serverConnectionsRepository: serverConnectionsRepositoryMock.Object);

            await testSubject.MigrateAsync(AnyBoundProject, Mock.Of<IProgress<MigrationProgress>>(), false, CancellationToken.None);

            serverConnectionsRepositoryMock.Verify(mock => mock.TryGet(convertedConnection.Id, out It.Ref<ServerConnection>.IsAny), Times.Once);
            serverConnectionsRepositoryMock.Verify(mock => mock.TryAdd(IsExpectedServerConnection(convertedConnection)), Times.Once);
            unintrusiveBindingController.Verify(
                x => x.BindAsync(
                    It.Is<BoundServerProject>(proj => IsExpectedBoundServerProject(proj)),
                    It.IsAny<IProgress<FixedStepsProgress>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// If bindings exist in the new format (in the Bindings folder), it means that the old migration is being executed before the new migration. But we expect it the other way around
        /// </summary>
        [TestMethod]
        public async Task Migrate_ConnectionsJsonFileDoesNotExistAndNewBindingsExist_DoesNotMigrateServerConnection()
        {
            var unintrusiveBindingControllerMock = new Mock<IUnintrusiveBindingController>();
            var serverConnectionsRepositoryMock = new Mock<IServerConnectionsRepository>();
            var bindingPathProvider = new Mock<IUnintrusiveBindingPathProvider>();
            var logger = new Mock<ILogger>();
            var testSubject = CreateTestSubject(
                serverConnectionsRepository: serverConnectionsRepositoryMock.Object,
                unintrusiveBindingController: unintrusiveBindingControllerMock.Object,
                unintrusiveBindingPathProvider: bindingPathProvider.Object,
                logger:logger.Object);
            serverConnectionsRepositoryMock.Setup(mock => mock.ConnectionsFileExists()).Returns(false);
            bindingPathProvider.Setup(mock => mock.GetBindingPaths()).Returns(["binding1"]);

            await testSubject.MigrateAsync(AnyBoundProject, Mock.Of<IProgress<MigrationProgress>>(), false, CancellationToken.None);

            logger.Verify(x=> x.WriteLine(MigrationStrings.ConnectionsJson_DoesNotExist), Times.Once);
            serverConnectionsRepositoryMock.Verify(mock => mock.TryAdd(It.IsAny<ServerConnection>()), Times.Never);
            unintrusiveBindingControllerMock.Verify(
                x => x.BindAsync(
                    It.Is<BoundServerProject>(proj => IsExpectedBoundServerProject(proj)),
                    It.IsAny<IProgress<FixedStepsProgress>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// If no bindings exist in the new format (in the Bindings folder), even if the new migration is executed first no connections.json will be created, so we can safely proceed with the old migration
        /// </summary>
        [TestMethod]
        public async Task Migrate_ConnectionsJsonFileDoesNotExistAndNoNewBindingsExist_MigratesConnection()
        {
            var unintrusiveBindingControllerMock = new Mock<IUnintrusiveBindingController>();
            var serverConnectionsRepositoryMock = new Mock<IServerConnectionsRepository>();
            var bindingPathProvider = new Mock<IUnintrusiveBindingPathProvider>();
            var logger = new Mock<ILogger>();
            var testSubject = CreateTestSubject(
                serverConnectionsRepository: serverConnectionsRepositoryMock.Object,
                unintrusiveBindingController: unintrusiveBindingControllerMock.Object,
                unintrusiveBindingPathProvider:bindingPathProvider.Object,
                logger: logger.Object);
            serverConnectionsRepositoryMock.Setup(mock => mock.ConnectionsFileExists()).Returns(false);
            bindingPathProvider.Setup(mock => mock.GetBindingPaths()).Returns([]);

            await testSubject.MigrateAsync(AnyBoundProject, Mock.Of<IProgress<MigrationProgress>>(), false, CancellationToken.None);

            logger.Verify(x => x.WriteLine(MigrationStrings.ConnectionsJson_DoesNotExist), Times.Never);
            serverConnectionsRepositoryMock.Verify(mock => mock.TryAdd(It.IsAny<ServerConnection>()), Times.Once);
            unintrusiveBindingControllerMock.Verify(
                x => x.BindAsync(
                    It.Is<BoundServerProject>(proj => IsExpectedBoundServerProject(proj)),
                    It.IsAny<IProgress<FixedStepsProgress>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void Migrate_InvalidServerInformation_Throws()
        {
            var testSubject = CreateTestSubject();

            Func<Task> act = async () => await testSubject.MigrateAsync(new BoundSonarQubeProject(), Mock.Of<IProgress<MigrationProgress>>(), false, CancellationToken.None);

            act.Should().Throw<Exception>();
        }

        [TestMethod]
        public async Task Migrate_RoslynSuppressionUpdateIsTriggered()
        {
            var suppressionsUpdater = new Mock<ISuppressionIssueStoreUpdater>();

            var testSubject = CreateTestSubject(suppressionIssueStoreUpdater: suppressionsUpdater.Object);
            await testSubject.MigrateAsync(AnyBoundProject, null, false, CancellationToken.None);

            suppressionsUpdater.Verify(x => x.UpdateAllServerSuppressionsAsync(), Times.Once);
        }

        [TestMethod]
        public async Task Migrate_SwitchToBackgroundThread()
        {
            var threadHandling = new Mock<IThreadHandling>();
            threadHandling.Setup(x => x.SwitchToBackgroundThread())
               .Returns(() => new NoOpThreadHandler.NoOpAwaitable());

            var testSubject = CreateTestSubject(threadHandling: threadHandling.Object);
            await testSubject.MigrateAsync(AnyBoundProject, It.IsAny<IProgress<MigrationProgress>>(), false, CancellationToken.None);

            threadHandling.Verify(x => x.SwitchToBackgroundThread(), Times.Once);
        }

        private static ConnectedModeMigration CreateTestSubject(
            IFileProvider fileProvider = null,
            IFileCleaner fileCleaner = null,
            IVsAwareFileSystem fileSystem = null,
            IMigrationSettingsProvider settingsProvider = null,
            ISonarQubeService sonarQubeService = null,
            IUnintrusiveBindingController unintrusiveBindingController = null,
            ISuppressionIssueStoreUpdater suppressionIssueStoreUpdater = null,
            ISharedBindingConfigProvider sharedBindingConfigProvider = null,
            ILogger logger = null,
            IThreadHandling threadHandling = null, 
            ISolutionInfoProvider solutionInfoProvider = null, 
            IServerConnectionsRepository serverConnectionsRepository = null,
            IUnintrusiveBindingPathProvider unintrusiveBindingPathProvider = null)
        {
            fileProvider ??= Mock.Of<IFileProvider>();
            fileCleaner ??= Mock.Of<IFileCleaner>();
            fileSystem ??= Mock.Of<IVsAwareFileSystem>();
            sonarQubeService ??= Mock.Of<ISonarQubeService>();
            unintrusiveBindingController ??= Mock.Of<IUnintrusiveBindingController>();
            suppressionIssueStoreUpdater ??= Mock.Of<ISuppressionIssueStoreUpdater>();
            settingsProvider ??= CreateSettingsProvider(DefaultTestLegacySettings).Object;
            sharedBindingConfigProvider ??= Mock.Of<ISharedBindingConfigProvider>();
            solutionInfoProvider ??= CreateSolutionInfoProviderMock().Object;
            serverConnectionsRepository ??= CreateServerConnectionsRepositoryMock().Object;
            unintrusiveBindingPathProvider ??= Mock.Of<IUnintrusiveBindingPathProvider>();

            logger ??= new TestLogger(logToConsole: true);
            threadHandling ??= new NoOpThreadHandler();

            return new ConnectedModeMigration(settingsProvider,
                fileProvider,
                fileCleaner,
                fileSystem,
                sonarQubeService,
                unintrusiveBindingController,
                suppressionIssueStoreUpdater,
                sharedBindingConfigProvider,
                logger,
                threadHandling,
                solutionInfoProvider,
                serverConnectionsRepository, 
                unintrusiveBindingPathProvider);
        }

        private static Mock<IFileProvider> CreateFileProvider(params string[] filesToReturn)
        {
            var fileProvider = new Mock<IFileProvider>();
            fileProvider.Setup(x => x.GetFilesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IEnumerable<string>>(filesToReturn));

            return fileProvider;
        }

        private static Mock<ISonarQubeService> CreateSonarQubeService(bool isAlreadyConnectedToServer)
        {
            var sonarQubeService = new Mock<ISonarQubeService>();
            sonarQubeService.Setup(x => x.IsConnected).Returns(isAlreadyConnectedToServer);

            return sonarQubeService;
        }

        private static Mock<IMigrationSettingsProvider> CreateSettingsProvider(LegacySettings settingsToReturn = null)
        {
            settingsToReturn ??= DefaultTestLegacySettings;

            var settingsProvider = new Mock<IMigrationSettingsProvider>();
            settingsProvider.Setup(x => x.GetAsync(It.IsAny<string>())).Returns(Task.FromResult(settingsToReturn));
            return settingsProvider;
        }

        private static Mock<IServerConnectionsRepository> CreateServerConnectionsRepositoryMock()
        {
            var serverConnectionsRepositoryMock = new Mock<IServerConnectionsRepository>();
            serverConnectionsRepositoryMock.Setup(x => x.TryAdd(It.IsAny<ServerConnection>())).Returns(true);
            serverConnectionsRepositoryMock.Setup(x => x.ConnectionsFileExists()).Returns(true);

            return serverConnectionsRepositoryMock;
        }

        private static void MockIServerConnectionsRepositoryTryGet(Mock<IServerConnectionsRepository> serverConnectionsRepositoryMock, string id = null, ServerConnection.SonarQube storedConnection = null)
        {
            serverConnectionsRepositoryMock.Setup(service => service.TryGet(id ?? It.IsAny<string>(), out It.Ref<ServerConnection>.IsAny))
                .Returns((string _, out ServerConnection value) =>
                {
                    value = storedConnection;
                    return storedConnection != null;
                });
        }

        private static Mock<ISolutionInfoProvider> CreateSolutionInfoProviderMock()
        {
            var createSolutionInfoProviderMock = new Mock<ISolutionInfoProvider>();
            createSolutionInfoProviderMock.Setup(mock => mock.GetSolutionNameAsync()).ReturnsAsync("solution");
            return createSolutionInfoProviderMock;
        }

        private static bool IsExpectedBoundServerProject(BoundServerProject proj)
        {
            return proj.ServerProjectKey == AnyBoundProject.ProjectKey && proj.ServerConnection.ServerUri == AnyBoundProject.ServerUri;
        }

        private static ServerConnection IsExpectedServerConnection(ServerConnection convertedConnection)
        {
            return It.Is<ServerConnection>(conn => conn.Id == convertedConnection.Id);
        }
    }

    // Extension methods to make the mocks easier to work with.
    // In a separate namespace so we don't pollute IntelliSense for other tests.
    namespace ConnectedModeMigrationTestsExtensions
    {
        internal static class MockExtensions
        {
            public static void SetupFileToClean(this Mock<IFileCleaner> fileCleaner, string input, string output)
                => fileCleaner.Setup(x => x.Clean(input, It.IsAny<LegacySettings>(), It.IsAny<CancellationToken>()))
                    .Returns(output);

            public static void VerifyFileCleaned(this Mock<IFileCleaner> fileCleaner, string expectedContent)
                => fileCleaner.Verify(x => x.Clean(expectedContent,
                    It.IsAny<LegacySettings>(),
                    It.IsAny<CancellationToken>()), Times.Once);

            public static void AddFile(this Mock<IVsAwareFileSystem> fileSystem, string filePath, string content)
                => fileSystem.Setup(x => x.LoadAsTextAsync(filePath)).Returns(Task.FromResult(content));

            public static void VerifyFileLoaded(this Mock<IVsAwareFileSystem> fileSystem, string filePath)
                => fileSystem.Verify(x => x.LoadAsTextAsync(filePath), Times.Once);

            public static void VerifyFileSaved(this Mock<IVsAwareFileSystem> fileSystem, string filePath, string content)
                => fileSystem.Verify(x => x.SaveAsync(filePath, content), Times.Once);

            public static void VerifyFileNotSaved(this Mock<IVsAwareFileSystem> fileSystem, string filePath)
                => fileSystem.Verify(x => x.SaveAsync(filePath, It.IsAny<string>()), Times.Never);

            public static void VerifyNoFilesSaved(this Mock<IVsAwareFileSystem> fileSystem)
                => fileSystem.Verify(x => x.SaveAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            public static void VerifyDirectoryDeleted(this Mock<IVsAwareFileSystem> fileSystem, string folderPath)
                => fileSystem.Verify(x => x.DeleteFolderAsync(folderPath), Times.Once);
        }
    }
}
