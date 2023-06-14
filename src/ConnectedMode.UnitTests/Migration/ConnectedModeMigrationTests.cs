﻿/*
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
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.Migration;
using SonarLint.VisualStudio.Core;
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
                MefTestHelpers.CreateExport<IRoslynProjectWalker>(),
                MefTestHelpers.CreateExport<ILogger>(),
                MefTestHelpers.CreateExport<IThreadHandling>());
        }

        [TestMethod]
        public void MefCtor_CheckIsExported_MultipleProjectCleaners()
        {
            var batch = new CompositionBatch();
            batch.AddExport(MefTestHelpers.CreateExport<ILogger>());
            batch.AddExport(MefTestHelpers.CreateExport<IRoslynProjectWalker>());
            batch.AddExport(MefTestHelpers.CreateExport<IThreadHandling>());

            batch.AddExport(MefTestHelpers.CreateExport<IProjectCleaner>());
            batch.AddExport(MefTestHelpers.CreateExport<IProjectCleaner>());
            batch.AddExport(MefTestHelpers.CreateExport<IProjectCleaner>());

            var importer = new SingleObjectImporter<IConnectedModeMigration>();
            batch.AddPart(importer);

            using var catalog = new TypeCatalog(typeof(ConnectedModeMigration));
            using var container = new CompositionContainer(catalog);
            Action act = () => container.Compose(batch);

            act.Should().NotThrow();
            importer.Import.Should().NotBeNull();

            var actualType = (ConnectedModeMigration)importer.Import;
            actualType.ProjectCleaners.Should().HaveCount(3);
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

        private static ConnectedModeMigration CreateTestSubject(
            IRoslynProjectWalker projectWalker = null,
            IThreadHandling threadHandling = null,
            ILogger logger = null,
            params IProjectCleaner[] projectCleaners)
        {
            projectWalker ??= Mock.Of<IRoslynProjectWalker>();
            threadHandling ??= new NoOpThreadHandler();
            logger ??= new TestLogger(logToConsole: true);

            return new ConnectedModeMigration(projectWalker, projectCleaners, logger, threadHandling);
        }
    }
}
