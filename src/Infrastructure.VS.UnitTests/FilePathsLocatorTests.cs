/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests
{
    [TestClass]
    public class FilePathsLocatorTests
    {
        [TestMethod]
        public void Locate_NullHierarchy_ArgumentNullException()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.Locate(null, "some file");

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("hierarchyToSearch");
        }

        [TestMethod]
        public void Locate_NullFileName_ArgumentNullException()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.Locate(Mock.Of<IVsHierarchy>(), null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileName");
        }

        [TestMethod]
        public void Locate_VsHierarchyHasNoItems_EmptyList()
        {
            var project = new ProjectMock("c:\\test\\test.csproj");
            var testSubject = CreateTestSubject();

            var result = testSubject.Locate(project, "some file");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void Locate_VsHierarchyDoesNotContainTheFile_EmptyList()
        {
            var project = new ProjectMock("c:\\test\\test.csproj");
            var file1 = project.AddOrGetFile("file1.cs");
            var file2 = project.AddOrGetFile("file2.cs");
            var file3 = project.AddOrGetFile("matching-file.json"); // same name, different extension
            var file4 = project.AddOrGetFile("not-matching-file.cs"); // not an exact match
            var folder1 = project.AddOrGetFile("folder1");
            var folder2 = project.AddOrGetFile("folder2");

            /*
             * file1
             * folder1
             *    file2
             *    folder2
             *       file3
             * file4
             */
            project.SetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_FirstChild, (int)file1);
            project.SetProperty(file1, (int)__VSHPROPID.VSHPROPID_NextSibling, (int)folder1);
            project.SetProperty(folder1, (int)__VSHPROPID.VSHPROPID_FirstChild, (int)file2);
            project.SetProperty(folder1, (int)__VSHPROPID.VSHPROPID_NextSibling, (int)file4);
            project.SetProperty(file2, (int)__VSHPROPID.VSHPROPID_NextSibling, (int)folder2);
            project.SetProperty(folder2, (int)__VSHPROPID.VSHPROPID_FirstChild, (int)file3);

            var testSubject = CreateTestSubject();

            var result = testSubject.Locate(project, "matching-file.cs");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void Locate_VsHierarchyContainsTheFile_ListWithAllMatchingFiles()
        {
            var project = new ProjectMock("c:\\test\\test.csproj");
            var file1 = project.AddOrGetFile("file1.cs");
            var file2 = project.AddOrGetFile("file2.cs");
            var file3 = project.AddOrGetFile("matching-file.json"); // same name, different extension
            var file4 = project.AddOrGetFile("not-matching-file.cs"); // not an exact match
            var folder1 = project.AddOrGetFile("folder1");
            var folder2 = project.AddOrGetFile("folder2");
            var matchingFile1 = project.AddOrGetFile("folder2\\matching-FILE.cs"); // check case-sensitivity
            var matchingFile2 = project.AddOrGetFile("matching-file.CS"); // check case-sensitivity

            /*
             * file1
             * folder1
             *    file2
             *    folder2
             *       file3
             *       matchingFile1
             * matchingFile2
             * file4
             */
            project.SetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_FirstChild, (int)file1);
            project.SetProperty(file1, (int)__VSHPROPID.VSHPROPID_NextSibling, (int)folder1);
            project.SetProperty(folder1, (int)__VSHPROPID.VSHPROPID_FirstChild, (int)file2);
            project.SetProperty(file2, (int)__VSHPROPID.VSHPROPID_NextSibling, (int)folder2);
            project.SetProperty(folder2, (int)__VSHPROPID.VSHPROPID_FirstChild, (int)file3);
            project.SetProperty(file3, (int)__VSHPROPID.VSHPROPID_NextSibling, (int)matchingFile1);
            project.SetProperty(folder1, (int)__VSHPROPID.VSHPROPID_NextSibling, (int)matchingFile2);
            project.SetProperty(matchingFile2, (int)__VSHPROPID.VSHPROPID_NextSibling, (int)file4);

            var testSubject = CreateTestSubject();

            var result = testSubject.Locate(project, "matching-file.cs");

            var expected = new[] { "c:\\test\\matching-file.CS", "c:\\test\\folder2\\matching-FILE.cs" };
            result.Should().BeEquivalentTo(expected);
        }

        private FilePathsLocator CreateTestSubject() => new FilePathsLocator();
    }
}
