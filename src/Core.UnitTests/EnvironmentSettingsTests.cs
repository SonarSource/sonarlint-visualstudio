/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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
using SonarLint.VisualStudio.TestInfrastructure;

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
        [DataRow(null, false)]
        [DataRow("true", true)]
        [DataRow("TRUE", true)]
        [DataRow("false", false)]
        [DataRow("1", false)]
        [DataRow("not a boolean", false)]
        public void AreConnectedModeAutomationCommandsEnabled_InE2EInstance_FollowsTheEnvironmentVariable(string envVarValue, bool expected)
        {
            using (var scope = new EnvironmentVariableScope())
            {
                scope.SetVariable(EnvironmentSettings.ConnectedModeAutomationCommandsEnvVar, envVarValue);
                new EnvironmentSettings(() => ["devenv.exe", "/RootSuffix", "ExtensionE2E1a2b3c4d", "/NoSplash"])
                    .AreConnectedModeAutomationCommandsEnabled().Should().Be(expected);
            }
        }

        [TestMethod]
        [DataRow("devenv.exe /RootSuffix ExtensionE2E1a2b3c4d /NoSplash", true)]
        [DataRow("devenv.exe -rootsuffix ExtensionE2E", true)]
        [DataRow("devenv.exe", false)]
        [DataRow("devenv.exe /RootSuffix Exp", false)]
        [DataRow("devenv.exe /RootSuffix extensione2e1a2b3c4d", false)]
        [DataRow("devenv.exe /RootSuffix MyExtensionE2E", false)]
        [DataRow("devenv.exe /RootSuffix", false)]
        [DataRow("devenv.exe /Log ExtensionE2E", false)]
        public void AreConnectedModeAutomationCommandsEnabled_VariableSet_RequiresAnE2ERootSuffix(string commandLine, bool expected)
        {
            using (var scope = new EnvironmentVariableScope())
            {
                scope.SetVariable(EnvironmentSettings.ConnectedModeAutomationCommandsEnvVar, "true");
                new EnvironmentSettings(() => commandLine.Split(' ')).AreConnectedModeAutomationCommandsEnabled().Should().Be(expected);
            }
        }

        [TestMethod]
        public void AreConnectedModeAutomationCommandsEnabled_DefaultCtor_UsesTheProcessCommandLine()
        {
            using (var scope = new EnvironmentVariableScope())
            {
                scope.SetVariable(EnvironmentSettings.ConnectedModeAutomationCommandsEnvVar, "true");
                // The test runner is not an E2E experimental instance.
                new EnvironmentSettings().AreConnectedModeAutomationCommandsEnabled().Should().BeFalse();
            }
        }

        [TestMethod]
        [DataRow(null, false)]
        [DataRow("true", true)]
        [DataRow("TRUE", true)]
        [DataRow("false", false)]
        [DataRow("1", false)]
        [DataRow("not a boolean", false)]
        public void IsTelemetryDisabled_CorrectlyMapped(string envVarValue, bool expected)
        {
            using (var scope = new EnvironmentVariableScope())
            {
                scope.SetVariable(EnvironmentSettings.TelemetryDisabledEnvVar, envVarValue);
                new EnvironmentSettings().IsTelemetryDisabled().Should().Be(expected);
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
    }
}
