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

using System.IO;
using SonarLint.VisualStudio.TypeScript.NodeJSLocator.LocationProviders;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.NodeJSLocator.LocationProviders
{
    [TestClass]
    public class GlobalPathNodeLocationsProviderTests
    {
        private readonly string programFilesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs\\node.exe");
        private readonly string programFiles86Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "nodejs\\node.exe");

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void Get_NullPathEnvVar_ReturnsProgramFilesPath(string pathVar)
        {
            using var scope = CreateEnvironmentVariableScope(pathVar);

            var testSubject = CreateTestSubject();
            var result = testSubject.Get();

            result.Should().BeEquivalentTo(
                programFilesPath,
                programFiles86Path
            );
        }

        [TestMethod]
        public void Get_HasPathEnvVar_ReturnsFoldersUnderPathAndProgramFilesPath()
        {
            using var scope = CreateEnvironmentVariableScope("some folder1;some folder2");

            var testSubject = CreateTestSubject();
            var result = testSubject.Get();

            result.Should().BeEquivalentTo(
                "some folder1\\node.exe",
                "some folder2\\node.exe",
                programFilesPath,
                programFiles86Path
            );
        }

        private GlobalPathNodeLocationsProvider CreateTestSubject()
        {
            return new GlobalPathNodeLocationsProvider();
        }

        private EnvironmentVariableScope CreateEnvironmentVariableScope(string path = null)
        {
            var scope = new EnvironmentVariableScope();

            // remove any existing node.exe from machine's PATH for testing purposes
            scope.SetPath(path);

            return scope;
        }
    }
}
