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
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.Migration;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration
{
    [TestClass]
    public class ConnectedModeMigrationTests
    {
        [TestMethod]
        public void MefCtor_CheckTypeIsNonShared()
            => MefTestHelpers.CheckIsNonSharedMefComponent<ConnectedModeMigration>();

        [TestMethod]
        public void MefCtor_CheckIsExported_NoProjectCleaners()
        {
            MefTestHelpers.CheckTypeCanBeImported<ConnectedModeMigration, IConnectedModeMigration>(
                MefTestHelpers.CreateExport<IFileProvider>(),
                MefTestHelpers.CreateExport<IFileCleaner>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void Migrate_FilesExist_AllFilesCleaned()
        {
            var fileProvider = CreateFileProvider("file1", "file2", "file3");
            var fileCleaner = new Mock<IFileCleaner>();

            var testSubject = CreateTestSubject(fileProvider.Object, fileCleaner.Object);

            Func<Task> act = async () => await testSubject.MigrateAsync(null, CancellationToken.None);

            act.Should().NotThrow();

            fileProvider.Verify(x => x.GetFilesAsync(It.IsAny<CancellationToken>()), Times.Once);
            fileCleaner.Invocations.Should().HaveCount(3);
            VerifyFileCleaned(fileCleaner, "file1");
            VerifyFileCleaned(fileCleaner, "file2");
            VerifyFileCleaned(fileCleaner, "file3");
        }

        [TestMethod]
        public void Migrate_NoProgressListener_NoError()
        {
            var testSubject = CreateTestSubject();

            Func<Task> act = async () => await testSubject.MigrateAsync(null, CancellationToken.None);

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

            await testSubject.MigrateAsync(progressListener.Object, CancellationToken.None);

            progressMessages.Should().NotBeEmpty();
        }

        private static void VerifyFileCleaned(Mock<IFileCleaner> fileCleaner, string filePath)
            => fileCleaner.Verify(x => x.CleanAsync(filePath, It.IsAny<LegacySettings>(), It.IsAny<CancellationToken>()), Times.Once);

        private static ConnectedModeMigration CreateTestSubject(
            IFileProvider fileProvider = null,
            IFileCleaner fileCleaner = null,
            ILogger logger = null)
        {
            fileProvider ??= Mock.Of<IFileProvider>();
            fileCleaner ??= Mock.Of<IFileCleaner>();

            logger ??= new TestLogger(logToConsole: true);

            return new ConnectedModeMigration(fileProvider, fileCleaner, logger);
        }

        private static Mock<IFileProvider> CreateFileProvider(params string[] filesToReturn)
        {
            var fileProvider = new Mock<IFileProvider>();
            fileProvider.Setup(x => x.GetFilesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IEnumerable<string>>(filesToReturn));
        
            return fileProvider;
        }
    }
}
