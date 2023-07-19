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
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots
{
    [TestClass]
    public class DocumentClosedHandlerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
            =>  MefTestHelpers.CheckTypeCanBeImported<DocumentClosedHandler, DocumentClosedHandler>(
                    MefTestHelpers.CreateExport<IDocumentEvents>(),
                    MefTestHelpers.CreateExport<ILocalHotspotsStoreUpdater>(),
                    MefTestHelpers.CreateExport<IThreadHandling>());

        [TestMethod]
        public void CheckIsSharedMefComponent()
            => MefTestHelpers.CheckIsSingletonMefComponent<DocumentClosedHandler>();

        [TestMethod]
        public void DocumentIsClosed_StoreUpdateIsTriggered()
        {
            var docEvents = new Mock<IDocumentEvents>();
            var updater = new Mock<ILocalHotspotsStoreUpdater>();

            var testSubject = CreateTestSubject(docEvents.Object, updater.Object);
            updater.Invocations.Should().BeEmpty();

            // Act
            RaiseDocClosedEvent(docEvents, "c:\\a file.txt");
            updater.Verify(x => x.RemoveForFile("c:\\a file.txt"), Times.Once());
        }

        [TestMethod]
        public void DocumentIsClosed_UpdateIsOnBackgroundThread()
        {
            var callSequence = new List<string>();

            var docEvents = new Mock<IDocumentEvents>();
            var updater = new Mock<ILocalHotspotsStoreUpdater>();
            var threadHandling = new Mock<IThreadHandling>();

            updater.Setup(x => x.RemoveForFile(It.IsAny<string>()))
                .Callback<string>(x => callSequence.Add("RemoveForFile"));

            threadHandling.Setup(x => x.RunOnBackgroundThread(It.IsAny<Func<Task<bool>>>()))
                .Returns((Func<Task<bool>> action) =>
                {
                    callSequence.Add("RunOnBackgroundThread");
                    return action();
                });
            
            var testSubject = CreateTestSubject(docEvents.Object, updater.Object, threadHandling.Object);
            updater.Invocations.Should().BeEmpty();

            // Act
            RaiseDocClosedEvent(docEvents, "any");

            callSequence.Should().ContainInOrder("RunOnBackgroundThread", "RemoveForFile");
        }

        [TestMethod]
        public void Dispose_EventHandlerIsUnregistered()
        {
            var docEvents = new Mock<IDocumentEvents>();
            var updater = new Mock<ILocalHotspotsStoreUpdater>();

            var testSubject = CreateTestSubject(docEvents.Object, updater.Object);
            docEvents.VerifyAdd(x => x.DocumentClosed += It.IsAny<EventHandler<DocumentClosedEventArgs>>(), Times.Once);

            testSubject.Dispose();
            docEvents.VerifyRemove(x => x.DocumentClosed -= It.IsAny<EventHandler<DocumentClosedEventArgs>>(), Times.Once);
        }

        private void RaiseDocClosedEvent(Mock<IDocumentEvents> docEvents, string filePath)
            => docEvents.Raise(x => x.DocumentClosed += null, new DocumentClosedEventArgs(filePath));

        private DocumentClosedHandler CreateTestSubject(IDocumentEvents documentEvents = null,
            ILocalHotspotsStoreUpdater updater = null,
            IThreadHandling threadHandling = null)
        {
            documentEvents ??= Mock.Of<IDocumentEvents>();
            updater ??= Mock.Of<ILocalHotspotsStoreUpdater>();
            threadHandling ??= new NoOpThreadHandler();

            return new DocumentClosedHandler(documentEvents, updater, threadHandling);
        }
    }
}
