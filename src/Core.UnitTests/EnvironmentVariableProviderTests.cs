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

namespace SonarLint.VisualStudio.Core.UnitTests
{
    [TestClass]
    public class EnvironmentVariableProviderTests
    {
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void TryGet_NoVariableName_ArgumentNullException(string variableName)
        {
            var testSubject = EnvironmentVariableProvider.Instance;

            Action act = () => testSubject.TryGet(variableName);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("variableName");
        }

        [TestMethod]
        public void TryGet_VariableDoesNotExist_Null()
        {
            var testSubject = EnvironmentVariableProvider.Instance;

            var result = testSubject.TryGet(Guid.NewGuid().ToString());

            result.Should().BeNull();
        }

        [TestMethod]
        public void TryGet_VariableExistsButEmpty_Null()
        {
            using var scope = new EnvironmentVariableScope();
            scope.SetVariable("test", "");

            var testSubject = EnvironmentVariableProvider.Instance;

            var result = testSubject.TryGet("test");

            result.Should().BeNull();
        }

        [TestMethod]
        public void TryGet_VariableExistsButWhitespace_ValueReturned()
        {
            using var scope = new EnvironmentVariableScope();
            scope.SetVariable("test", " ");

            var testSubject = EnvironmentVariableProvider.Instance;

            var result = testSubject.TryGet("test");

            result.Should().Be(" ");
        }

        [TestMethod]
        public void TryGet_VariableExists_ValueReturned()
        {
            using var scope = new EnvironmentVariableScope();
            scope.SetVariable("test name", "some value");

            var testSubject = EnvironmentVariableProvider.Instance;

            var result = testSubject.TryGet("TEST NAME"); // should be case insensitive

            result.Should().Be("some value");
        }

        [TestMethod]
        [DataRow(Environment.SpecialFolder.ApplicationData)]
        [DataRow(Environment.SpecialFolder.LocalApplicationData)]
        public void GetFolderPath_ReturnsValue(Environment.SpecialFolder folder)
        {
            var testSubject = EnvironmentVariableProvider.Instance;

            var actual = testSubject.GetFolderPath(folder);

            actual.Should().Be(Environment.GetFolderPath(folder));
        }
    }
}
