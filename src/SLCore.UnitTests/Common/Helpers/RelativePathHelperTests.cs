/*
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

using System.IO;
using SonarLint.VisualStudio.SLCore.Common.Helpers;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Common.Helpers;

[TestClass]
public class RelativePathHelperTests
{
    [DataRow("C:\\", "D:\\file.json", null)]
    [DataRow("C:\\", "C:\\file.json", "file.json")]
    [DataRow("C:\\one\\", "C:\\one\\file.json", "file.json")]
    [DataRow("C:\\one\\", "C:\\file.json", "..\\file.json")]
    [DataRow("C:\\one\\", "C:\\onetwo\\file.json", "..\\onetwo\\file.json")]
    [DataRow("C:\\one\\two\\", "C:\\one\\two\\file.json", "file.json")]
    [DataRow("C:\\one\\two\\", "C:\\one\\file.json", "..\\file.json")]
    [DataRow("C:\\one\\two\\", "C:\\one\\twothree\\file.json", "..\\twothree\\file.json")]
    [DataRow("C:\\one\\two\\", "C:\\twothree\\file.json", "..\\..\\twothree\\file.json")]
    [DataRow("C:\\one\\two\\", "C:\\oneone\\twothree\\file.json", "..\\..\\oneone\\twothree\\file.json")]
    [DataRow("C:\\one\\two\\three\\", "C:\\file.json", "..\\..\\..\\file.json")]
    [DataRow("C:\\one\\two\\", "D:\\one\\two\\file.json", null)]
    [DataRow("\\\\network\\one\\two\\three\\", "\\\\network\\file.json", "..\\..\\..\\file.json")]
    [DataTestMethod]
    public void GetRelativePathToRootFolder_ReturnsExpectedValues(string root, string file, string expected) => RelativePathHelper.GetRelativePathToRootFolder(root, file).Should().Be(expected);

    [TestMethod]
    public void GetRelativePathToRootFolder_RootPathNotEndsWithSeparator_Throws()
    {
        const string root = "C:\\dirwithoutseparatorattheend";
        var act = () => RelativePathHelper.GetRelativePathToRootFolder(root, "C:\\dirwithoutseparatorattheend\\file.json");

        act.Should().Throw<ArgumentException>().WithMessage(
            $"""
             {string.Format(SLCoreStrings.RelativePathHelper_RootDoesNotEndWithSeparator, Path.DirectorySeparatorChar)}
             Parameter name: root
             """
        );
    }

    [DataRow("path\\123")]
    [DataRow("\\path\\123")]
    [DataRow(".\\path\\123")]
    [DataRow("..\\path\\123")]
    [DataRow("notnetwork\\one\\two\\three\\")]
    [DataTestMethod]
    public void GetRelativePathToRootFolder_RootPathRelative_Throws(string path)
    {
        var act = () => RelativePathHelper.GetRelativePathToRootFolder(path, "C:\\path\\123\\file.json");

        act.Should().Throw<ArgumentException>().WithMessage(
            $"""
             {string.Format(SLCoreStrings.RelativePathHelper_NonAbsolutePath, path)}
             Parameter name: root
             """
        );
    }

    [DataRow("path\\123\\file.json")]
    [DataRow("\\path\\123\\file.json")]
    [DataRow(".\\path\\123\\file.json")]
    [DataRow("..\\path\\123\\file.json")]
    [DataRow("notnetwork\\one\\two\\three.json")]
    [DataTestMethod]
    public void GetRelativePathToRootFolder_FilePathRelative_Throws(string path)
    {
        var act = () => RelativePathHelper.GetRelativePathToRootFolder("C:\\path\\", path);

        act.Should().Throw<ArgumentException>().WithMessage(
            $"""
             {string.Format(SLCoreStrings.RelativePathHelper_NonAbsolutePath, path)}
             Parameter name: filePath
             """
        );
    }
}
