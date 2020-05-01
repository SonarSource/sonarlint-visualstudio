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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.Core.UnitTests.Binding
{
    [TestClass]
    public class SolutionBindingFilePathGeneratorTests
    {
        [DataTestMethod]
        [DataRow("", "", "", "")]
        [DataRow("c:\\", "", "", "c:\\")]
        [DataRow("", "key", "", "key")]
        [DataRow("", "", "test.txt", "test.txt")]
        [DataRow("c:\\", "MY_KEY", "NAME.txt", "c:\\my_keyname.txt")]
        [DataRow("c:\\", "My<Key>", "N|a<m>e.txt", "c:\\my_key_n_a_m_e.txt")]
        public void Generate_GeneratesCorrectFilePath(string rootDirectory, string projectKey, string fileNameSuffixAndExtension, string expectedPath)
        {
            var testSubject = new SolutionBindingFilePathGenerator();
            var result = testSubject.Generate(rootDirectory, projectKey, fileNameSuffixAndExtension);

            result.Should().Be(expectedPath);
        }
    }
}
