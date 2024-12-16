﻿/*
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

using SonarLint.VisualStudio.SLCore.Listeners.Implementation.Http;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation.Http;

[TestClass]
public class SystemProxyDetectorTests
{
    private SystemProxyDetector testSubject;

    [TestInitialize]
    public void TestInitialize() => testSubject = new SystemProxyDetector();

    [TestMethod]
    public void MefCtor_CheckIsExported() => MefTestHelpers.CheckTypeCanBeImported<SystemProxyDetector, ISystemProxyDetector>();

    [TestMethod]
    public void Mef_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<SystemProxyDetector>();

    [TestMethod]
    public void GetProxyUri_NullUri_ReturnsNull()
    {
        var result = testSubject.GetProxyUri(null);

        result.Should().BeNull();
    }

    [TestMethod]
    public void GetProxyUri_UriNotNull_ReturnsUri()
    {
        var result = testSubject.GetProxyUri(new Uri("https://sonarcloud.io"));

        result.Should().NotBeNull();
    }
}
