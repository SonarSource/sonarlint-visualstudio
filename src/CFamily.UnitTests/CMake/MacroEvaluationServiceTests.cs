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
using SonarLint.VisualStudio.CFamily.CMake;

namespace SonarLint.VisualStudio.CFamily.UnitTests.CMake
{
    [TestClass]
    public class MacroEvaluationServiceTests
    {
        [TestMethod]
        [DataRow(null, null)]
        [DataRow("no macros", "no macros")]
        [DataRow("X ${name} X", "X my-config X")] // active config once
        [DataRow("Y${projectDir}Y", "Ydummy rootY")] // workspace once
        [DataRow("${projectDir}\\somefolder\\${name}\\sub", "dummy root\\somefolder\\my-config\\sub")]
        [DataRow("${name}${name}", "my-configmy-config")]  // multiple instances
        public void Evaluate_ParametersReplaced(string input, string expectedOutput)
        {
            const string ActiveConfiguration = "my-config";
            const string WorkspaceRootDir = "dummy root";

            var testSubject = new MacroEvaluationService();

            var result = testSubject.Evaluate(input, ActiveConfiguration, WorkspaceRootDir);

            result.Should().Be(expectedOutput);
        }
    }
}
