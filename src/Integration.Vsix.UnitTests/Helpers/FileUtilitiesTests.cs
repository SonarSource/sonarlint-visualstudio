/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class FileUtilitiesTests
    {
        [TestMethod]
        public void Path_Relative_InSameFolder()
        {
            string path = FileUtilities.GetRelativePath("c:\\proj1.csproj", "c:\\file1.cs");
            path.Should().Be("file1.cs");

            path = FileUtilities.GetRelativePath("c:\\aaa\\proj2.csproj", "c:\\aaa\\file2.cs");
            path.Should().Be("file2.cs");
        }

        [TestMethod]
        public void Path_Relative_InChildFolders()
        {
            string path = FileUtilities.GetRelativePath("c:\\sub1\\proj1.proj", "c:\\sub1\\sub2\\sub3\\myfile.cs");
            path.Should().Be("sub2/sub3/myfile.cs");

            path = FileUtilities.GetRelativePath("c:\\proj1.proj", "c:\\sub1\\sub2\\sub3\\myfile.cs");
            path.Should().Be("sub1/sub2/sub3/myfile.cs");
        }

        [TestMethod]
        public void Path_NotRelative_InNonChildFolders()
        {
            string path = FileUtilities.GetRelativePath("c:\\aaaa\\myproject.csproj", "c:\\bbbb\\file1.cs");
            path.Should().Be("../bbbb/file1.cs");
        }

        [TestMethod]
        public void Path_NotRelative_OnDifferentDrives()
        {
            string path = FileUtilities.GetRelativePath("c:\\folder1\\project1.vbproj", "d:\\folder1\\file2.vb");
            path.Should().Be("d:/folder1/file2.vb");
        }

        [TestMethod]
        public void Path_Relative_IsNotCaseSensitive()
        {
            string path = FileUtilities.GetRelativePath("c:\\aaa\\proj1.csproj", "C:\\AAA\\file1.cs");
            path.Should().Be("file1.cs");
        }

    }
}
