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

using System.Linq;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.EslintBridgeClient
{
    [TestClass]
    public class AnalysisConfigurationTests
    {
        [TestMethod]
        public void GetGlobals_NoEnvVar_ReturnsDefaults()
        {
            using var environmentsScope = new EnvironmentVariableScope();
            environmentsScope.SetVariable(AnalysisConfiguration.GlobalsVarName, null);

            var testSubject = CreateTestSubject();
            var result = testSubject.GetGlobals();

            result.Should().BeEquivalentTo(AnalysisConfiguration.GlobalsDefaultValue.Split(',').Select(x => x.Trim()));
        }

        [TestMethod]
        public void GetGlobals_HasEnvVar_ReturnsFromEnvVar()
        {
            using var environmentsScope = new EnvironmentVariableScope();
            environmentsScope.SetVariable(AnalysisConfiguration.GlobalsVarName, "test1,test2");
            
            var testSubject = CreateTestSubject();
            var result = testSubject.GetGlobals();

            result.Should().BeEquivalentTo("test1", "test2");
        }

        [TestMethod]
        public void GetEnvironments_NoEnvVar_ReturnsDefaults()
        {
            using var environmentsScope = new EnvironmentVariableScope();
            environmentsScope.SetVariable(AnalysisConfiguration.EnvironmentsVarName, null);

            var testSubject = CreateTestSubject();
            var result = testSubject.GetEnvironments();

            result.Should().BeEquivalentTo(AnalysisConfiguration.EnvironmentsDefaultValue.Split(',').Select(x=> x.Trim()));
        }

        [TestMethod]
        public void GetEnvironments_HasEnvVar_ReturnsFromEnvVar()
        {
            using var environmentsScope = new EnvironmentVariableScope();
            environmentsScope.SetVariable(AnalysisConfiguration.EnvironmentsVarName, "test1,test2");

            var testSubject = CreateTestSubject();
            var result = testSubject.GetEnvironments();

            result.Should().BeEquivalentTo("test1", "test2");
        }

        private AnalysisConfiguration CreateTestSubject() => new AnalysisConfiguration();
    }
}
