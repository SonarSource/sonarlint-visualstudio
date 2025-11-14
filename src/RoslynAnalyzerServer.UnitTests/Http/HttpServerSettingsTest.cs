/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using SonarLint.VisualStudio.RoslynAnalyzerServer.Http;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Http;

[TestClass]
public class HttpServerSettingsTest
{
    private HttpServerSettings config = null!;

    [TestInitialize]
    public void TestInitialize() => config = new HttpServerSettings();

    [TestMethod]
    public void MefCtor_CheckExports() => MefTestHelpers.CheckTypeCanBeImported<HttpServerSettings, IHttpServerSettings>();

    [TestMethod]
    public void Mef_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<HttpServerSettings>();

    [TestMethod]
    public void MaxStartAttempts_ReturnsTen() => config.MaxStartAttempts.Should().Be(10);

    [TestMethod]
    public void RequestMillisecondsTimeout_Returns30SecondsTimeoutInMilliseconds() => config.RequestMillisecondsTimeout.Should().Be(30000);

    [TestMethod]
    public void MaxRequestBodyBytes_ReturnsOneMegabyte() => config.MaxRequestBodyBytes.Should().Be(1024 * 1024);

    [TestMethod]
    public void MaxConcurrentRequests_ReturnsTwenty() => config.MaxConcurrentRequests.Should().Be(20);
}
