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
        [TestMethod]
        [DataRow(null, false)]
        [DataRow("true", true)]
        [DataRow("TRUE", true)]
        [DataRow("false", true)]
        [DataRow("FALSE", true)]
        [DataRow("1", true)]
        [DataRow("0", true)]
        [DataRow("not a boolean", false)]
        public void GetVSSeverity_Blocker_CorrectlyMapped(string envVarValue, bool expected)
        {
            using (var scope = new EnvironmentVariableScope())
            {
                scope.SetVariable(EnvironmentSettings.TreatBlockerAsErrorEnvVar, envVarValue);
                new EnvironmentSettings().TreatBlockerSeverityAsError().Should().Be(expected);
            }
        }
    }
}
