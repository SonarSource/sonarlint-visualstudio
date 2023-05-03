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
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Binding
{
    [TestClass]
    public class PreSolutionBoundHandlerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<PreSolutionBoundUpdateHandler, PreSolutionBoundUpdateHandler>(
                MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(),
                MefTestHelpers.CreateExport<IImportBeforeFileGenerator>());
        }

        [TestMethod]
        public void Ctor_SubscribesToEvents()
        {
            var activeSolutionTracker = new Mock<IActiveSolutionBoundTracker>();

            _ = new PreSolutionBoundUpdateHandler(activeSolutionTracker.Object, Mock.Of<IImportBeforeFileGenerator>());

            activeSolutionTracker.VerifyAdd(x => x.PreSolutionBindingChanged += It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>(), Times.Once);
            activeSolutionTracker.VerifyAdd(x => x.PreSolutionBindingUpdated += It.IsAny<EventHandler>(), Times.Once);
        }

        [TestMethod]
        public void InvokeBindingChanged_Standalone_ImportBeforeFileGeneratorIsNotCalled()
        {
            var activeSolutionTracker = new Mock<IActiveSolutionBoundTracker>();
            var importBeforeFileGenerator = new Mock<IImportBeforeFileGenerator>();

            _ = new PreSolutionBoundUpdateHandler(activeSolutionTracker.Object, importBeforeFileGenerator.Object);

            activeSolutionTracker.Raise(x => x.PreSolutionBindingChanged += null, new ActiveSolutionBindingEventArgs(BindingConfiguration.Standalone));
            importBeforeFileGenerator.Verify(x => x.WriteTargetsFileToDiskIfNotExists(), Times.Never);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public void InvokeBindingChanged_ConnectedMode_ImportBeforeFileGeneratorIsCalled(SonarLintMode mode)
        {
            var activeSolutionTracker = new Mock<IActiveSolutionBoundTracker>();
            var importBeforeFileGenerator = new Mock<IImportBeforeFileGenerator>();
            var bindingConfig = new BindingConfiguration(
                                    new BoundSonarQubeProject(new Uri("http://localhost"), "test", ""),
                                    mode, "");

            _ = new PreSolutionBoundUpdateHandler(activeSolutionTracker.Object, importBeforeFileGenerator.Object);

            activeSolutionTracker.Raise(x => x.PreSolutionBindingChanged += null, new ActiveSolutionBindingEventArgs(bindingConfig));
            importBeforeFileGenerator.Verify(x => x.WriteTargetsFileToDiskIfNotExists(), Times.Once);
        }

        [TestMethod]
        public void InvokeBindingUpdated_ImportBeforeFileGeneratorIsCalled()
        {
            var activeSolutionTracker = new Mock<IActiveSolutionBoundTracker>();
            var importBeforeFileGenerator = new Mock<IImportBeforeFileGenerator>();

            _ = new PreSolutionBoundUpdateHandler(activeSolutionTracker.Object, importBeforeFileGenerator.Object);

            activeSolutionTracker.Raise(x => x.PreSolutionBindingUpdated += null, EventArgs.Empty);
            importBeforeFileGenerator.Verify(x => x.WriteTargetsFileToDiskIfNotExists(), Times.Once);
        }

        [TestMethod]
        public void InvokeTwoEvents_ImportBeforeFileGeneratorIsCalledOnceAndEventsGetUnsubscribed()
        {
            var activeSolutionTracker = new Mock<IActiveSolutionBoundTracker>();
            var importBeforeFileGenerator = new Mock<IImportBeforeFileGenerator>();

            _ = new PreSolutionBoundUpdateHandler(activeSolutionTracker.Object, importBeforeFileGenerator.Object);

            activeSolutionTracker.Raise(x => x.PreSolutionBindingUpdated += null, EventArgs.Empty);
            importBeforeFileGenerator.Verify(x => x.WriteTargetsFileToDiskIfNotExists(), Times.Once);

            activeSolutionTracker.VerifyRemove(x => x.PreSolutionBindingChanged -= It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>(), Times.Once);
            activeSolutionTracker.VerifyRemove(x => x.PreSolutionBindingUpdated -= It.IsAny<EventHandler>(), Times.Once);

            activeSolutionTracker.Raise(x => x.PreSolutionBindingUpdated += null, EventArgs.Empty);
            importBeforeFileGenerator.Verify(x => x.WriteTargetsFileToDiskIfNotExists(), Times.Once);
        }

        [TestMethod]
        public void Dispose_UnsubscribesToEvent()
        {
            var activeSolutionTracker = new Mock<IActiveSolutionBoundTracker>();

            var testSubject = new PreSolutionBoundUpdateHandler(activeSolutionTracker.Object, Mock.Of<IImportBeforeFileGenerator>());

            testSubject.Dispose();

            activeSolutionTracker.VerifyRemove(x => x.PreSolutionBindingChanged -= It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>(), Times.Once);
            activeSolutionTracker.VerifyRemove(x => x.PreSolutionBindingUpdated -= It.IsAny<EventHandler>(), Times.Once);
        }
    }
}
