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

using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests.Helpers;

[TestClass]
public class FileExtensionExtractorTests
{
    [DataTestMethod]
    [DataRow("file.cs", "cs")]
    [DataRow("File.ScsS", "scss")]
    [DataRow("file.json", "json")]
    [DataRow("file", "")]
    [DataRow("<inva>lid?filename.exe", null)]
    public void GetNormalizedExtension_Returns(string fileName, string expectedExtension)
    {
        FileExtensionExtractor.GetNormalizedExtension(fileName).Should().Be(expectedExtension);
    }
}
