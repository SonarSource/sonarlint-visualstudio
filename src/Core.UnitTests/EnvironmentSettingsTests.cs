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
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Core.UnitTests
{
    [TestClass]
    public class EnvironmentSettingsTests
    {
        private const int PchGenerationDefaultValue = 3;

        [TestMethod]
        [DataRow(null, false)]
        [DataRow("true", true)]
        [DataRow("TRUE", true)]
        [DataRow("false", false)]
        [DataRow("FALSE", false)]
        [DataRow("1", false)]
        [DataRow("0", false)]
        [DataRow("not a boolean", false)]
        public void TreatBlockerSeverityAsError_CorrectlyMapped(string envVarValue, bool expected)
        {
            using (var scope = new EnvironmentVariableScope())
            {
                scope.SetVariable(EnvironmentSettings.TreatBlockerAsErrorEnvVar, envVarValue);
                new EnvironmentSettings().TreatBlockerSeverityAsError().Should().Be(expected);
            }
        }

        [TestMethod]
        [DataRow(null, 0)]
        [DataRow("", 0)]
        [DataRow(" ", 0)]
        [DataRow("abc", 0)]
        [DataRow("1.23", 0)]
        [DataRow("2,000", 0)]
        [DataRow("2.001", 0)]
        [DataRow("-999", 0)]
        [DataRow("-1", 0)]
        [DataRow("0", 0)]
        [DataRow("1", 1)]
        [DataRow("9876", 9876)]
        public void AnalysisTimeoutInMs_ReturnsExpectedValue(string envVarValue, int expected)
        {
            using (var scope = new EnvironmentVariableScope())
            {
                scope.SetVariable(EnvironmentSettings.AnalysisTimeoutEnvVar, envVarValue);
                new EnvironmentSettings().AnalysisTimeoutInMs().Should().Be(expected);
            }
        }

        [TestMethod]
        [DataRow(null, PchGenerationDefaultValue)]
        [DataRow("", PchGenerationDefaultValue)]
        [DataRow(" ", PchGenerationDefaultValue)]
        [DataRow("abc", PchGenerationDefaultValue)]
        [DataRow("1.23", PchGenerationDefaultValue)]
        [DataRow("2,000", PchGenerationDefaultValue)]
        [DataRow("2.001", PchGenerationDefaultValue)]
        [DataRow("-999", PchGenerationDefaultValue)]
        [DataRow("-1", PchGenerationDefaultValue)]
        [DataRow("0", PchGenerationDefaultValue)]
        [DataRow("1", 1)]
        [DataRow("9876", 9876)]
        public void PchTimeoutInMs_ReturnsExpectedValue(string envVarValue, int expected)
        {
            using (var scope = new EnvironmentVariableScope())
            {
                scope.SetVariable(EnvironmentSettings.PchGenerationTimeoutEnvVar, envVarValue);
                new EnvironmentSettings().PCHGenerationTimeoutInMs(PchGenerationDefaultValue).Should().Be(expected);
            }
        }

        [TestMethod]
        [DataRow(null, null)]
        [DataRow("", null)]
        [DataRow("does not validate urls", "does not validate urls")]
        public void SonarLintDaemonDownloadUrl_ReturnsExpectedValue(string envVarValue, string expected)
        {
            using (var scope = new EnvironmentVariableScope())
            {
                scope.SetVariable(EnvironmentSettings.SonarLintDownloadUrlEnvVar, envVarValue);
                new EnvironmentSettings().SonarLintDaemonDownloadUrl().Should().Be(expected);
            }
        }
    }
}
