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

using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;

namespace SonarLint.VisualStudio.Integration.UnitTests.Analysis
{
    [TestClass]
    public class AnalysisRequestListenerTests
    {
        [TestMethod]
        [DataRow(null)]
        [DataRow(new string[] { })]
        public void FilterIssueTrackersByPath_NullOrEmptyPaths_AllTrackersReturned(string[] filePaths)
        {
            var trackers = CreateMockedIssueTrackers("any", "any2");

            var actual = AnalysisRequestListener.FilterRequestHandlersByPath(trackers, filePaths);

            actual.Should().BeEquivalentTo(trackers);
        }

        [TestMethod]
        public void FilterIssueTrackersByPath_WithPaths_NoMatches_EmptyListReturned()
        {
            var trackers = CreateMockedIssueTrackers("file1.txt", "c:\\aaa\\file2.cpp");

            var actual = AnalysisRequestListener.FilterRequestHandlersByPath(trackers,
                new string[] { "no matches", "file1.wrongextension" });

            actual.Should().BeEmpty();
        }

        [TestMethod]
        public void FilterIssueTrackersByPath_WithPaths_SingleMatch_SingleTrackerReturned()
        {
            var trackers = CreateMockedIssueTrackers("file1.txt", "c:\\aaa\\file2.cpp", "d:\\bbb\\file3.xxx");

            var actual = AnalysisRequestListener.FilterRequestHandlersByPath(trackers,
                new string[] { "file1.txt" });

            actual.Should().BeEquivalentTo(trackers[0]);
        }

        [TestMethod]
        public void FilterIssueTrackersByPath_WithPaths_MultipleMatches_MultipleTrackersReturned()
        {
            var trackers = CreateMockedIssueTrackers("file1.txt", "c:\\aaa\\file2.cpp", "d:\\bbb\\file3.xxx");

            var actual = AnalysisRequestListener.FilterRequestHandlersByPath(trackers,
                new string[]
                {
                    "file1.txt",
                    "D:\\BBB\\FILE3.xxx" // match should be case-insensitive
                });

            actual.Should().BeEquivalentTo(trackers[0], trackers[2]);
        }

        [TestMethod]
        public void FilterIssueTrackersByPath_WithPaths_AllMatched_AllTrackersReturned()
        {
            var trackers = CreateMockedIssueTrackers("file1.txt", "c:\\aaa\\file2.cpp", "d:\\bbb\\file3.xxx");

            var actual = AnalysisRequestListener.FilterRequestHandlersByPath(trackers,
                new string[]
                {
                    "unmatchedFile1.cs",
                    "file1.txt",
                    "c:\\aaa\\file2.cpp",
                    "unmatchedfile2.cpp",
                    "d:\\bbb\\file3.xxx"
                });

            actual.Should().BeEquivalentTo(trackers);
        }

        private IAnalysisRequestHandler[] CreateMockedIssueTrackers(params string[] filePaths) =>
            filePaths.Select(CreateMockedIssueTracker).ToArray();

        private static IAnalysisRequestHandler CreateMockedIssueTracker(string filePath)
        {
            var mock = new Mock<IAnalysisRequestHandler>();
            mock.Setup(x => x.FilePath).Returns(filePath);
            return mock.Object;
        }
    }
}
