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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.Service.Log;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Log;

[TestClass]
public class SlCoreBackendLogLevelTests
{
    [TestMethod]
    [DataRow(DaemonLogLevel.Verbose, SlCoreBackendLogLevel.Debug)]
    [DataRow(DaemonLogLevel.Info, SlCoreBackendLogLevel.Info)]
    [DataRow(DaemonLogLevel.Minimal, SlCoreBackendLogLevel.Info)]
    public void FromDaemonLogLevel_MapsAsExpected(DaemonLogLevel input, string expected)
    {
        SlCoreBackendLogLevel.FromDaemonLogLevel(input).Should().Be(expected);
    }
}
