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
using FluentAssertions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests.Helpers
{
    [TestClass]
    public class AbsoluteFilePathLocatorTests
    {
        private const string TestedRelativePath = "src\\solution\\project\\folder\\test.cpp";
        private const string NonMatchingPath = "c:\\some\\root\\src\\solution\\PROJECT\\Test.cpp";
        private const string MatchingAbsolutePath = "c:\\some\\root\\src\\solution\\PROJECT\\FOLDER\\Test.cpp";

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsSolution))).Returns(Mock.Of<IVsSolution>());

            MefTestHelpers.CheckTypeCanBeImported<AbsoluteFilePathLocator, IAbsoluteFilePathLocator>(null, new[]
            {
                MefTestHelpers.CreateExport<SVsServiceProvider>(serviceProvider.Object)
            });
        }

        [TestMethod]
        public void Locate_RelativePathIsNull_ArgumentNullException()
        {
            var testSubject = new AbsoluteFilePathLocator(Mock.Of<IProjectSystemHelper>());

            Action act = () => testSubject.Locate(null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("relativeFilePath");
        }

        [TestMethod]
        public void Locate_SolutionHasNoProjectItems_Null()
        {
            var projectSystemHelper = new Mock<IProjectSystemHelper>();
            SetupProjects(projectSystemHelper, Array.Empty<ITestSolutionItem>());

            var testSubject = new AbsoluteFilePathLocator(projectSystemHelper.Object);
            var result = testSubject.Locate(TestedRelativePath);

            result.Should().BeNull();

            projectSystemHelper.Verify(x=> x.GetAllItems(It.IsAny<IVsHierarchy>()), Times.Never);
        }

        [TestMethod]
        public void Locate_CannotRetrieveProjectItemFilePath_Null()
        {
            var projectItem = Mock.Of<ITestSolutionItem>();
            var projectSystemHelper = new Mock<IProjectSystemHelper>();
            SetupProjects(projectSystemHelper, projectItem);
            SetupItemFilePath(projectSystemHelper, projectItem, VSConstants.VSITEMID.Root, null);

            var testSubject = new AbsoluteFilePathLocator(projectSystemHelper.Object);
            var result = testSubject.Locate(TestedRelativePath);

            result.Should().BeNull();

            projectSystemHelper.Verify(x => x.GetAllItems(projectItem), Times.Never);
        }

        [TestMethod]
        public void Locate_ProjectItemHasNoSubItems_Null()
        {
            var projectItem = Mock.Of<ITestSolutionItem>();
            var projectSystemHelper = new Mock<IProjectSystemHelper>();
            SetupProjects(projectSystemHelper, projectItem);
            SetupProjectItems(projectSystemHelper, projectItem, Array.Empty<VSConstants.VSITEMID>());

            var testSubject = new AbsoluteFilePathLocator(projectSystemHelper.Object);
            var result = testSubject.Locate(TestedRelativePath);

            result.Should().BeNull();

            projectSystemHelper.Verify(x => x.GetAllItems(projectItem), Times.Once);
        }

        [TestMethod]
        public void Locate_ProjectItemHasSubItems_NoMatches_Null()
        {
            var projectItem = Mock.Of<ITestSolutionItem>();
            var projectSystemHelper = new Mock<IProjectSystemHelper>();
            SetupProjects(projectSystemHelper, projectItem);

            const VSConstants.VSITEMID subItemId1 = (VSConstants.VSITEMID) 123;
            const VSConstants.VSITEMID subItemId2 = (VSConstants.VSITEMID) 456;
            SetupItemFilePath(projectSystemHelper, projectItem, subItemId1, null);
            SetupItemFilePath(projectSystemHelper, projectItem, subItemId1, NonMatchingPath);
            SetupProjectItems(projectSystemHelper, projectItem, subItemId1, subItemId2);
            
            var testSubject = new AbsoluteFilePathLocator(projectSystemHelper.Object);
            var result = testSubject.Locate(TestedRelativePath);

            result.Should().BeNull();

            projectSystemHelper.Verify(x => x.GetAllItems(projectItem), Times.Once);
        }

        [TestMethod]
        public void Locate_MultipleProjects_HasMatch_MatchReturnedAndFollowingItemsNotQueried()
        {
            var projectItem1 = Mock.Of<ITestSolutionItem>();
            var projectItem2 = Mock.Of<ITestSolutionItem>();

            var projectSystemHelper = new Mock<IProjectSystemHelper>();
            SetupProjects(projectSystemHelper, projectItem1, projectItem2);

            const VSConstants.VSITEMID subItemId = (VSConstants.VSITEMID)123;
            SetupProjectItems(projectSystemHelper, projectItem1, subItemId);
            SetupItemFilePath(projectSystemHelper, projectItem1, subItemId, MatchingAbsolutePath);

            var testSubject = new AbsoluteFilePathLocator(projectSystemHelper.Object);
            var result = testSubject.Locate(TestedRelativePath);

            result.Should().Be(MatchingAbsolutePath);

            projectSystemHelper.Verify(x => x.GetAllItems(projectItem2), Times.Never);
        }

        [TestMethod]
        public void Locate_ProjectItemHasSubItems_HasMatch_MatchReturnedAndFollowingItemsNotQueried()
        {
            var projectItem = Mock.Of<ITestSolutionItem>();

            var projectSystemHelper = new Mock<IProjectSystemHelper>();
            SetupProjects(projectSystemHelper, projectItem);
            SetupItemFilePath(projectSystemHelper, projectItem, VSConstants.VSITEMID.Root, "some path");

            const VSConstants.VSITEMID subItemId1 = (VSConstants.VSITEMID)123;
            const VSConstants.VSITEMID subItemId2 = (VSConstants.VSITEMID)456;
            const VSConstants.VSITEMID subItemId3 = (VSConstants.VSITEMID)789;
            SetupProjectItems(projectSystemHelper, projectItem, subItemId1, subItemId2, subItemId3);
            SetupItemFilePath(projectSystemHelper, projectItem, subItemId1, null);
            SetupItemFilePath(projectSystemHelper, projectItem, subItemId2, MatchingAbsolutePath);
            SetupItemFilePath(projectSystemHelper, projectItem, subItemId3, NonMatchingPath);

            var testSubject = new AbsoluteFilePathLocator(projectSystemHelper.Object);
            var result = testSubject.Locate(TestedRelativePath);

            result.Should().Be(MatchingAbsolutePath);

            projectSystemHelper.Verify(x=> x.GetItemFilePath(projectItem, subItemId3), Times.Never);
        }

        public interface ITestSolutionItem : IVsHierarchy, IVsProject
        {
        }

        private static void SetupProjects(Mock<IProjectSystemHelper> projectSystemHelper, params ITestSolutionItem[] projectItems)
        {
            projectSystemHelper
                .Setup(x => x.EnumerateProjects())
                .Returns(projectItems);

            foreach (var projectItem in projectItems)
            {
                SetupItemFilePath(projectSystemHelper, projectItem, VSConstants.VSITEMID.Root, Guid.NewGuid().ToString());
            }
        }

        private static void SetupItemFilePath(Mock<IProjectSystemHelper> projectSystemHelper, IVsProject projectItem, VSConstants.VSITEMID itemId, string filePath)
        {
            projectSystemHelper
                .Setup(x => x.GetItemFilePath(projectItem, itemId))
                .Returns(filePath);
        }

        private void SetupProjectItems(Mock<IProjectSystemHelper> projectSystemHelper, ITestSolutionItem projectItem, params VSConstants.VSITEMID[] itemIds)
        {
            projectSystemHelper
                .Setup(x => x.GetAllItems(projectItem))
                .Returns(itemIds);
        }
    }
}
