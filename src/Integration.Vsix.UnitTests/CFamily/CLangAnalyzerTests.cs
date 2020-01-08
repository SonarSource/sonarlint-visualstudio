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
using Moq;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.UnitTests
{
    [TestClass]
    public class CLangAnalyzerTests
    {
        [TestMethod]
        public void IsSupported()
        {
            // Arrange
            var telemetryManagerMock = new Mock<ITelemetryManager>();
            var testLogger = new TestLogger();
            var rulesConfigProviderMock = new Mock<ICFamilyRulesConfigProvider>();

            var analyzer = new CLangAnalyzer(telemetryManagerMock.Object, new ConfigurableSonarLintSettings(),
                rulesConfigProviderMock.Object, testLogger);

            // Act and Assert
            analyzer.IsAnalysisSupported(new[] { AnalysisLanguage.CFamily }).Should().BeTrue();
            analyzer.IsAnalysisSupported(new[] { AnalysisLanguage.Javascript }).Should().BeFalse();
            analyzer.IsAnalysisSupported(new[] { AnalysisLanguage.Javascript, AnalysisLanguage.CFamily }).Should().BeTrue();
        }
    }
}
