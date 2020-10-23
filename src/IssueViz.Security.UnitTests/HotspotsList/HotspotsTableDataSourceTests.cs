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

using System.Collections.Generic;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.HotspotsList
{
    [TestClass]
    public class HotspotsTableDataSourceTests
    {
        [TestMethod]
        public void Ctor_RegisterAsSource()
        {
            var tableManagerMock = new Mock<ITableManager>();
            var testSubject = CreateTestSubject(tableManagerMock);

            tableManagerMock.Verify(x=> x.AddSource(testSubject, HotspotsTableColumns.Names), Times.Once);
            tableManagerMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_UnregisterAsSource()
        {
            var tableManagerMock = new Mock<ITableManager>();
            var testSubject = CreateTestSubject(tableManagerMock);

            tableManagerMock.Verify(x => x.RemoveSource(testSubject), Times.Never);

            testSubject.Dispose();

            tableManagerMock.Verify(x => x.RemoveSource(testSubject), Times.Once);
        }

        [TestMethod]
        public void Subscribe_AddsEntriesToSink()
        {
            var sink = new Mock<ITableDataSink>();

            var testSubject = CreateTestSubject();
            testSubject.Subscribe(sink.Object);

            sink.Verify(x=> x.AddEntries(It.IsAny<IReadOnlyList<ITableEntry>>(), true), Times.Once);
            sink.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Unsubscribe_RemovesEntriesFromSink()
        {
            var sink = new Mock<ITableDataSink>();

            var testSubject = CreateTestSubject();
            var unSubscribeCallback = testSubject.Subscribe(sink.Object);

            sink.Verify(x => x.RemoveAllEntries(), Times.Never);

            unSubscribeCallback.Dispose();

            sink.Verify(x => x.RemoveAllEntries(), Times.Once);
        }

        private static HotspotsTableDataSource CreateTestSubject(Mock<ITableManager> tableManagerMock = null)
        {
            tableManagerMock ??= new Mock<ITableManager>();

            var tableManagerProviderMock = new Mock<ITableManagerProvider>();
            tableManagerProviderMock
                .Setup(x => x.GetTableManager(HotspotsTableConstants.TableManagerIdentifier))
                .Returns(tableManagerMock.Object);

            var testSubject = new HotspotsTableDataSource(tableManagerProviderMock.Object);

            return testSubject;
        }
    }
}
