/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Editor;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor
{
    [TestClass]
    public class FileRenamesEventSourceTests
    {
        private FileRenamesEventSource testSubject;
        private Mock<IVsTrackProjectDocuments2> trackProjectDocumentsMock;
        private uint cookie;

        [TestInitialize]
        public void TestInitialize()
        {
            trackProjectDocumentsMock = new Mock<IVsTrackProjectDocuments2>();

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(SVsTrackProjectDocuments)))
                .Returns(trackProjectDocumentsMock.Object);

            testSubject = new FileRenamesEventSource(serviceProviderMock.Object);

            cookie = 0;
            trackProjectDocumentsMock.Setup(x => x.AdviseTrackProjectDocumentsEvents(testSubject, out cookie));
        }

        [TestMethod]
        public void Ctor_RegisterToAdviseTrackProjectDocumentsEvents()
        {
            trackProjectDocumentsMock.Verify(x => x.AdviseTrackProjectDocumentsEvents(testSubject, out cookie), Times.Once);
        }

        [TestMethod]
        public void Dispose_UnregisterFromAdviseTrackProjectDocumentsEvents()
        {
            trackProjectDocumentsMock.Verify(x=> x.UnadviseTrackProjectDocumentsEvents(It.IsAny<uint>()), Times.Never);

            testSubject.Dispose();

            trackProjectDocumentsMock.Verify(x => x.UnadviseTrackProjectDocumentsEvents(cookie), Times.Once);
        }

        [TestMethod]
        public void AfterDocumentsRenamed_NoSubscribers_NoException()
        {
            Action act = () => RaiseDocumentsRenamed(new Dictionary<string, string> {{"old name", "new name"}});
            act.Should().NotThrow();
        }

        [TestMethod]
        public void AfterDocumentsRenamed_HasSubscribers_RaisesEvent()
        {
            var eventHandler = new Mock<EventHandler<FilesRenamedEventArgs>>();
            testSubject.FilesRenamed += eventHandler.Object;

            var renamedFiles = new Dictionary<string, string>
            {
                {"old name1", "new name1"},
                {"old name2", "new name2"}
            };

            RaiseDocumentsRenamed(renamedFiles);

            eventHandler.Verify(
                x => x(testSubject, It.Is((FilesRenamedEventArgs args) =>
                        args.OldNewFilePaths.Count == renamedFiles.Count &&
                        args.OldNewFilePaths.All(arg => renamedFiles.ContainsKey(arg.Key) && renamedFiles[arg.Key] == arg.Value))),
                    Times.Once());
        }

        private void RaiseDocumentsRenamed(IDictionary<string, string> oldNewFilePaths)
        {
            (testSubject as IVsTrackProjectDocumentsEvents2).OnAfterRenameFiles(0, 0, null, new int[0],
                oldNewFilePaths.Keys.ToArray(), oldNewFilePaths.Values.ToArray(), null);
        }
    }
}
