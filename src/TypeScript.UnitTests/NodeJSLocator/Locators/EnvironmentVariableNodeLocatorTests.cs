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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.TypeScript.NodeJSLocator.Locators;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.NodeJSLocator.Locators
{
    [TestClass]
    public class EnvironmentVariableNodeLocatorTests
    {
        [TestMethod]
        [DataRow(null, null)]
        [DataRow("", null)]
        [DataRow("some path", "some path")]
        public void Locate_ReturnsEnvironmentVariableValue(string envVarValue, string expected)
        {
            using var scope = new EnvironmentVariableScope();
            scope.SetVariable(EnvironmentVariableNodeLocator.NodeJsPathEnvVar, envVarValue);

            var testSubject = CreateTestSubject();
            var result = testSubject.Locate();

            result.Should().Be(expected);
        }

        [TestMethod]
        public void Locate_NoEnvironmentVariable_Null()
        {
            var testSubject = CreateTestSubject();
            var result = testSubject.Locate();

            result.Should().BeNull();
        }

        private EnvironmentVariableNodeLocator CreateTestSubject()
        {
            return new EnvironmentVariableNodeLocator(Mock.Of<ILogger>());
        }
    }
}
