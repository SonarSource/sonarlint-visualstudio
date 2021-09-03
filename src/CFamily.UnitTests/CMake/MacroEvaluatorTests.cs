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
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.CFamily.UnitTests.CMake
{
    [TestClass]
    public class MacroEvaluatorTests
    {
        [TestMethod]
        [DataRow("", "not recognised")]
        [DataRow("wrongPrefix", "name")]
        [DataRow("wrongPrefix", "projectDir")]
        [DataRow("projectDir", "")] // projectDir is a name, not a prefix
        public void Evaluate_ParametersReplaced(string macroPrefix, string macroName)
        {
            var testSubject = new MacroEvaluator();
            var context = new EvaluationContext("activeConfig", "rootDir");

            var result = testSubject.TryEvaluate(macroPrefix, macroName, context);

            result.Should().BeNull();
        }

        [TestMethod]
        public void Evaluate_ProjectDir_ExpectedValueReturned()
        {
            var testSubject = new MacroEvaluator();
            var context = new EvaluationContext("any", "rootDir");

            var result = testSubject.TryEvaluate(string.Empty, "projectDir", context);

            result.Should().Be("rootDir");
        }

        [TestMethod]
        public void Evaluate_Name_ExpectedValueReturned()
        {
            var testSubject = new MacroEvaluator();
            var context = new EvaluationContext("activeConfig", "any");

            var result = testSubject.TryEvaluate(string.Empty, "name", context);

            result.Should().Be("activeConfig");
        }

        [TestMethod]
        [DataRow("env")]
        [DataRow("ENV")]
        [DataRow("Env")]
        public void Evaluate_EnvPrefix_EnvVariableReturned(string prefix)
        {
            var envVariableProvider = new Mock<IEnvironmentVariableProvider>();
            envVariableProvider.Setup(x => x.TryGet("var name")).Returns("some value");

            var testSubject = new MacroEvaluator(envVariableProvider.Object);
            var context = new EvaluationContext("any", "any");

            var result = testSubject.TryEvaluate(prefix, "var name", context);

            result.Should().Be("some value");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void Evaluate_EnvPrefix_NoMacroName_Null(string macroName)
        {
            var envVariableProvider = new Mock<IEnvironmentVariableProvider>();

            var testSubject = new MacroEvaluator(envVariableProvider.Object);
            var context = new EvaluationContext("any", "any");

            var result = testSubject.TryEvaluate("env", macroName, context);

            result.Should().BeNull();

            envVariableProvider.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void Evaluate_NoMacroName_Null(string macroName)
        {
            var testSubject = new MacroEvaluator();
            var context = new EvaluationContext("any", "any");

            var result = testSubject.TryEvaluate(string.Empty, macroName, context);

            result.Should().BeNull();
        }
    }
}
