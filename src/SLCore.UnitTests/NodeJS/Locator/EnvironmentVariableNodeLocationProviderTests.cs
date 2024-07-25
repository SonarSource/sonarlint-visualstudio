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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.NodeJS.Locator;

namespace SonarLint.VisualStudio.SLCore.UnitTests.NodeJS.Locator
{
    [TestClass]
    public class EnvironmentVariableNodeLocationProviderTests
    {
        [TestMethod]
        public void Get_HasEnvironmentVariable_ReturnsValue()
        {
            using var scope = new EnvironmentVariableScope();
            scope.SetVariable(EnvironmentVariableNodeLocationProvider.NodeJsPathEnvVar, "some path");

            var testSubject = CreateTestSubject();
            var result = testSubject.Get();

            result.Should().BeEquivalentTo("some path");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void Get_NoEnvironmentVariable_Null(string envVarValue)
        {
            using var scope = new EnvironmentVariableScope();
            scope.SetVariable(EnvironmentVariableNodeLocationProvider.NodeJsPathEnvVar, envVarValue);

            var testSubject = CreateTestSubject();
            var result = testSubject.Get();

            result.Should().BeNull();
        }

        private EnvironmentVariableNodeLocationProvider CreateTestSubject()
        {
            return new EnvironmentVariableNodeLocationProvider(Mock.Of<ILogger>());
        }
    }
}
