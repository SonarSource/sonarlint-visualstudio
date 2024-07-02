/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
    [TestMethod]
    public void SourceFile_EncodingDefaultsToUTF8()
    {
        var sourceFile = new SourceFile(@"C:\Users\Project\JustASourceFile.cs");

        sourceFile.Encoding.Should().Be("utf-8");
    }

    [DataTestMethod]
    [DataRow("windows-1250")]
    [DataRow("windows-1251")]
    [DataRow("windows-1252")]
    [DataRow("iso-8859-15")]
    public void SourceFile_StoresFileEncoding(string encoding)
    {
        var sourceFile = new SourceFile(@"C:\Users\Project\JustASourceFile.cs", encoding);

        sourceFile.Encoding.Should().Be(encoding);
    }
}
