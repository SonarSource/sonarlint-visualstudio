﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests.Helpers;

[TestClass]
public class SonarLintSettingsLoggerSettingsProviderTests
{
    private ISonarLintSettings sonarLintSettings;
    private SonarLintSettingsLoggerSettingsProvider testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        sonarLintSettings = Substitute.For<ISonarLintSettings>();
        testSubject = new SonarLintSettingsLoggerSettingsProvider(sonarLintSettings);
    }

    [DataRow(DaemonLogLevel.Verbose, true)]
    [DataRow(DaemonLogLevel.Info, false)]
    [DataRow(DaemonLogLevel.Minimal, false)]
    [DataTestMethod]
    public void IsVerboseEnabled_IsThreadIdEnabled_ReturnsFor(DaemonLogLevel logLevel, bool isVerbose)
    {
        sonarLintSettings.DaemonLogLevel.Returns(logLevel);

        testSubject.IsVerboseEnabled.Should().Be(isVerbose);
        testSubject.IsThreadIdEnabled.Should().Be(isVerbose);
    }
}
