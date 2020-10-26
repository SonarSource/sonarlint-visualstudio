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
using FluentAssertions;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.HotspotsList
{
    [TestClass]
    public class HotspotsTableDataSourceTests
    {
        [TestMethod]
        public void Ctor_RegisterAsTableDataSource()
        {
            var tableManagerMock = new Mock<ITableManager>();
            var testSubject = CreateTestSubject(tableManagerMock);

            tableManagerMock.Verify(x => x.AddSource(testSubject, HotspotsTableColumns.Names), Times.Once);
            tableManagerMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_UnregisterAsTableDataSource()
        {
            var tableManagerMock = new Mock<ITableManager>();
            var testSubject = CreateTestSubject(tableManagerMock);

            tableManagerMock.Verify(x => x.RemoveSource(testSubject), Times.Never);

            testSubject.Dispose();

            tableManagerMock.Verify(x => x.RemoveSource(testSubject), Times.Once);
        }

        [TestMethod]
        public void Subscribe_NoExistingTableEntries_NoEntriesAddedToSink()
        {
            var testSubject = CreateTestSubject();

            var sink = new Mock<ITableDataSink>();
            testSubject.Subscribe(sink.Object);

            sink.Verify(x => x.AddEntries(new List<ITableEntry>(), true), Times.Once);
            sink.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Subscribe_HasExistingTableEntries_SetsSinkEntries()
        {
            var issueViz1 = CreateIssueViz();
            var issueViz2 = CreateIssueViz();

            var testSubject = CreateTestSubject();
            testSubject.Add(issueViz1);
            testSubject.Add(issueViz2);

            var sink = new Mock<ITableDataSink>();
            testSubject.Subscribe(sink.Object);

            const bool removeAllEntries = true;

            sink.Verify(x => x.AddEntries(It.Is((IReadOnlyList<ITableEntry> entries) =>
                entries.Count == 2 &&
                entries[0].Identity == issueViz1 &&
                entries[1].Identity == issueViz2
            ), removeAllEntries), Times.Once);

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

        [TestMethod]
        public void Add_NoSinks_NoSubscribers_NoException()
        {
            var testSubject = CreateTestSubject();
            
            Action act = () => testSubject.Add(CreateIssueViz());
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Add_HasSinks_AddsEntryToAllSinks()
        {
            var sink1 = new Mock<ITableDataSink>();
            var sink2 = new Mock<ITableDataSink>();

            var testSubject = CreateTestSubject();
            testSubject.Subscribe(sink1.Object);
            testSubject.Subscribe(sink2.Object);

            var issueViz = CreateIssueViz();
            testSubject.Add(issueViz);

            const bool removeAllEntries = false;

            sink1.Verify(x => x.AddEntries(It.Is((IReadOnlyList<ITableEntry> entries) =>
                entries.Count == 1 &&
                entries[0].Identity == issueViz
            ), removeAllEntries), Times.Once);

            sink2.Verify(x => x.AddEntries(It.Is((IReadOnlyList<ITableEntry> entries) =>
                entries.Count == 1 &&
                entries[0].Identity == issueViz
            ), removeAllEntries), Times.Once);
        }

        [TestMethod]
        public void Add_HasSubscribersToIssuesChangedEvent_SubscribersNotified()
        {
            var testSubject = CreateTestSubject();

            IssuesChangedEventArgs suppliedArgs = null;
            var eventCount = 0;
            testSubject.IssuesChanged += (sender, args) => { suppliedArgs = args; eventCount++; };

            testSubject.Add(CreateIssueViz("test1.cpp"));

            eventCount.Should().Be(1);
            suppliedArgs.Should().NotBeNull();
            suppliedArgs.AnalyzedFiles.Should().BeEquivalentTo("test1.cpp");
        }

        [TestMethod]
        public void GetLocations_NoTableEntries_EmptyList()
        {
            var testSubject = CreateTestSubject();

            var locations = testSubject.GetLocations("test.cpp");
            locations.Should().BeEmpty();
        }

        [TestMethod]
        public void GetLocations_NoTableEntriesForGivenFilePath_EmptyList()
        {
            var testSubject = CreateTestSubject();
            testSubject.Add(CreateIssueViz("file1.cpp"));

            var locations = testSubject.GetLocations("file2.cpp");
            locations.Should().BeEmpty();
        }

        [TestMethod]
        public void GetLocations_HasTableEntriesForGivenFilePath_ReturnsMatchingLocations()
        {
            var issueViz1 = CreateIssueViz("somefile.cpp");
            var issueViz2 = CreateIssueViz("someotherfile.cpp");
            var issueViz3 = CreateIssueViz("somefile.cpp");

            var locationViz = new Mock<IAnalysisIssueLocationVisualization>();
            locationViz.Setup(x => x.CurrentFilePath).Returns("somefile.cpp");
            var flowViz = Mock.Get(issueViz2.Flows[0]);
            flowViz.SetupGet(x => x.Locations).Returns(new[] {locationViz.Object});

            var testSubject = CreateTestSubject();
            testSubject.Add(issueViz1);
            testSubject.Add(issueViz2);
            testSubject.Add(issueViz3);

            var locations = testSubject.GetLocations("somefile.cpp");
            locations.Should().BeEquivalentTo(issueViz1, issueViz3, locationViz.Object);
        }

        [TestMethod]
        public void Refresh_NoSinks_NoException()
        {
            var testSubject = CreateTestSubject();
            testSubject.Add(CreateIssueViz("file1.cpp"));

            Action act = () => testSubject.Refresh(new[] {"file1.cpp"});
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Refresh_HasSinks_NoTableEntries_SinksNotChanged()
        {
            var sink = new Mock<ITableDataSink>();

            var testSubject = CreateTestSubject();
            testSubject.Subscribe(sink.Object);

            sink.Reset();

            testSubject.Refresh(new[] {"file.cpp"});

            sink.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Refresh_HasSinks_NoTableEntriesForGivenFiles_SinksNotChanged()
        {
            var issueViz = CreateIssueViz("somefile.cpp");
            var sink = new Mock<ITableDataSink>();

            var testSubject = CreateTestSubject();
            testSubject.Subscribe(sink.Object);
            testSubject.Add(issueViz);

            sink.Reset();

            testSubject.Refresh(new[] { "someotherfile.cpp" });

            sink.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Refresh_HasSinks_HasTableEntriesForGivenFiles_SinksRefreshed()
        {
            var issueViz = CreateIssueViz("file.cpp");
            var sink = new Mock<ITableDataSink>();

            var testSubject = CreateTestSubject();
            testSubject.Subscribe(sink.Object);
            testSubject.Add(issueViz);

            sink.Reset();

            testSubject.Refresh(new[] { "file.cpp" });

            sink.Verify(x => x.ReplaceEntries(
                    It.Is((IReadOnlyList<ITableEntry> entries) => entries.Count == 1 && entries[0].Identity == issueViz),
                    It.Is((IReadOnlyList<ITableEntry> entries) => entries.Count == 1 && entries[0].Identity == issueViz)),
                Times.Once());

            sink.VerifyNoOtherCalls();
        }

        private static IAnalysisIssueVisualization CreateIssueViz(string filePath = "test.cpp")
        {
            var flowViz = new Mock<IAnalysisIssueFlowVisualization>();
            flowViz.SetupGet(x => x.Locations).Returns(new List<IAnalysisIssueLocationVisualization>());

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(Mock.Of<IHotspot>());
            issueViz.Setup(x => x.CurrentFilePath).Returns(filePath);
            issueViz.Setup(x => x.Flows).Returns(new[] {flowViz.Object});

            return issueViz.Object;
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
