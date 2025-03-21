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

using System;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode.Install;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;

using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Install
{
    [TestClass]
    public class ImportBeforeInstallTriggerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ImportBeforeInstallTrigger, ImportBeforeInstallTrigger>(
                MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(),
                MefTestHelpers.CreateExport<IImportBeforeFileGenerator>(),
                MefTestHelpers.CreateExport<IThreadHandling>());
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
            => MefTestHelpers.CheckIsSingletonMefComponent<ImportBeforeInstallTrigger>();

        [TestMethod]
        public void Ctor_SubscribesToEvents()
        {
            var activeSolutionTracker = new Mock<IActiveSolutionBoundTracker>();

            _ = CreateTestSubject(activeSolutionTracker.Object, Mock.Of<IImportBeforeFileGenerator>());

            activeSolutionTracker.VerifyAdd(x => x.PreSolutionBindingChanged += It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>(), Times.Once);
            activeSolutionTracker.VerifyAdd(x => x.PreSolutionBindingUpdated += It.IsAny<EventHandler>(), Times.Once);

            activeSolutionTracker.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Ctor_DoesNotCallAnyServices()
        {
            var activeSolutionBoundTracker = new Mock<IActiveSolutionBoundTracker>();
            var importBeforeFileGenerator = new Mock<IImportBeforeFileGenerator>();
            var threadHandling = new Mock<IThreadHandling>();

            _ = CreateTestSubject(activeSolutionBoundTracker.Object, importBeforeFileGenerator.Object, threadHandling.Object);

            // The MEF constructor should be free-threaded, which it will be if
            // it doesn't make any external calls.
            importBeforeFileGenerator.Invocations.Should().BeEmpty();
            threadHandling.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected, true)]
        [DataRow(SonarLintMode.LegacyConnected, true)]
        [DataRow(SonarLintMode.Standalone, false)]
        public async Task TriggersImportBeforeFileDependingOnMode(SonarLintMode mode, bool shouldTrigger)
        {
            var activeSolutionTracker = CreateActiveSolutionBoundTrackerWihtBindingConfig(mode);
            var importBeforeFileGenerator = new Mock<IImportBeforeFileGenerator>();

            var testSubject = CreateTestSubject(activeSolutionTracker.Object, importBeforeFileGenerator.Object);

            await testSubject.TriggerUpdateAsync();

            importBeforeFileGenerator.Verify(x => x.WriteTargetsFileToDiskIfNotExists(), shouldTrigger ? Times.Once : Times.Never);
        }

        [TestMethod]
        public async Task TriggersImportBeforeFile_SwitchesThread()
        {
            var activeSolutionTracker = CreateActiveSolutionBoundTrackerWihtBindingConfig(SonarLintMode.Connected);
            var threadHandling = new Mock<IThreadHandling>();
            threadHandling.Setup(th => th.SwitchToBackgroundThread()).Returns(new NoOpThreadHandler.NoOpAwaitable());

            var testSubject = CreateTestSubject(activeSolutionTracker.Object, threadHandling: threadHandling.Object);

            await testSubject.TriggerUpdateAsync();

            threadHandling.Verify(x => x.SwitchToBackgroundThread(), Times.Once);
        }

        [TestMethod]
        public void InvokeBindingChanged_Standalone_ImportBeforeFileGeneratorIsNotCalled()
        {
            var activeSolutionTracker = new Mock<IActiveSolutionBoundTracker>();
            var importBeforeFileGenerator = new Mock<IImportBeforeFileGenerator>();

            _ = CreateTestSubject(activeSolutionTracker.Object, importBeforeFileGenerator.Object);

            activeSolutionTracker.Raise(x => x.PreSolutionBindingChanged += null, new ActiveSolutionBindingEventArgs(BindingConfiguration.Standalone));
            importBeforeFileGenerator.Verify(x => x.WriteTargetsFileToDiskIfNotExists(), Times.Never);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public void InvokeBindingChanged_ConnectedMode_ImportBeforeFileGeneratorIsCalled(SonarLintMode mode)
        {
            var activeSolutionTracker = CreateActiveSolutionBoundTrackerWihtBindingConfig(SonarLintMode.Standalone);
            var importBeforeFileGenerator = new Mock<IImportBeforeFileGenerator>();

            _ = CreateTestSubject(activeSolutionTracker.Object, importBeforeFileGenerator.Object);

            importBeforeFileGenerator.Verify(x => x.WriteTargetsFileToDiskIfNotExists(), Times.Never);

            var newBindingConfig = CreateBindingConfiguration(mode);
            activeSolutionTracker.Setup(x => x.CurrentConfiguration).Returns(newBindingConfig);

            activeSolutionTracker.Raise(x => x.PreSolutionBindingChanged += null, new ActiveSolutionBindingEventArgs(newBindingConfig));
            importBeforeFileGenerator.Verify(x => x.WriteTargetsFileToDiskIfNotExists(), Times.Once);
        }

        [TestMethod]
        public void InvokeBindingUpdated_ImportBeforeFileGeneratorIsCalled()
        {
            var activeSolutionTracker = CreateActiveSolutionBoundTrackerWihtBindingConfig(SonarLintMode.Standalone);
            var importBeforeFileGenerator = new Mock<IImportBeforeFileGenerator>();

            _ = CreateTestSubject(activeSolutionTracker.Object, importBeforeFileGenerator.Object);

            importBeforeFileGenerator.Verify(x => x.WriteTargetsFileToDiskIfNotExists(), Times.Never);

            var newBindingConfig = CreateBindingConfiguration(SonarLintMode.Connected);
            activeSolutionTracker.Setup(x => x.CurrentConfiguration).Returns(newBindingConfig);

            activeSolutionTracker.Raise(x => x.PreSolutionBindingUpdated += null, EventArgs.Empty);
            importBeforeFileGenerator.Verify(x => x.WriteTargetsFileToDiskIfNotExists(), Times.Once);
        }

        [TestMethod]
        public void Dispose_UnsubscribesToEvent()
        {
            var activeSolutionTracker = new Mock<IActiveSolutionBoundTracker>();

            var testSubject = CreateTestSubject(activeSolutionTracker.Object, Mock.Of<IImportBeforeFileGenerator>());

            testSubject.Dispose();

            activeSolutionTracker.VerifyRemove(x => x.PreSolutionBindingChanged -= It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>(), Times.Once);
            activeSolutionTracker.VerifyRemove(x => x.PreSolutionBindingUpdated -= It.IsAny<EventHandler>(), Times.Once);
        }

        private Mock<IActiveSolutionBoundTracker> CreateActiveSolutionBoundTrackerWihtBindingConfig(SonarLintMode mode)
        {
            var activeSolutionTracker = new Mock<IActiveSolutionBoundTracker>();

            var bindingConfig = CreateBindingConfiguration(mode);

            activeSolutionTracker.Setup(x => x.CurrentConfiguration).Returns(bindingConfig);

            return activeSolutionTracker;
        }

        private BindingConfiguration CreateBindingConfiguration(SonarLintMode mode)
        {
            return new BindingConfiguration(new BoundServerProject("test", "test", new ServerConnection.SonarQube(new Uri("http://localhost"))), mode, "");
        }

        private ImportBeforeInstallTrigger CreateTestSubject(IActiveSolutionBoundTracker activeSolutionBoundTracker, IImportBeforeFileGenerator importBeforeFileGenerator = null, IThreadHandling threadHandling = null)
        {
            importBeforeFileGenerator ??= Mock.Of<IImportBeforeFileGenerator>();
            threadHandling ??= new NoOpThreadHandler();

            return new ImportBeforeInstallTrigger(activeSolutionBoundTracker, importBeforeFileGenerator, threadHandling);
        }
    }
}
