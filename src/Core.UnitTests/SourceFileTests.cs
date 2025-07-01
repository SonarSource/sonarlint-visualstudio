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

namespace SonarLint.VisualStudio.Core.UnitTests;

[TestClass]
public class SourceFileTests
{
    private const string path = @"C:\Users\Project\JustASourceFile.cs";

    [TestMethod]
    public void SourceFile_NoContent_ContentAndEncodingAreNull()
    {
        var sourceFile = new SourceFile(path);

        sourceFile.FilePath.Should().Be(path);
        sourceFile.Encoding.Should().BeNull();
        sourceFile.Content.Should().BeNull();
    }

    [DataTestMethod]
    [DataRow("a", "windows-1250")]
    [DataRow("b", "windows-1251")]
    [DataRow("c", "windows-1252")]
    [DataRow("d", "iso-8859-15")]
    public void SourceFile_WithContent(string content, string encoding)
    {
        var sourceFile = new SourceFile(path, new SourceFileContent(content, encoding));

        sourceFile.FilePath.Should().Be(path);
        sourceFile.Encoding.Should().Be(encoding);
        sourceFile.Content.Should().Be(content);
    }
}
