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

using System.Net;
using SonarQube.Client.Logging;

namespace SonarQube.Client.Tests;

[TestClass]
public class HttpClientHandlerFactoryTests
{
    private ILogger logger;
    private IProxyDetector proxyDetector;
    private HttpClientHandlerFactory httpClientHandlerFactory;

    [TestInitialize]
    public void TestInitialize()
    {
        logger = Substitute.For<ILogger>();
        proxyDetector = Substitute.For<IProxyDetector>();
        httpClientHandlerFactory = new HttpClientHandlerFactory(proxyDetector, logger);
    }

    [TestMethod]
    public void SystemProxyConfigured_ConfiguresHttpClientHandler()
    {
        var baseAddress = new Uri("http://localhost");
        var proxyUri = new Uri("http://proxy");
        proxyDetector.GetProxyUri(baseAddress).Returns(proxyUri);

        var httpClientHandler = httpClientHandlerFactory.Create(baseAddress);

        httpClientHandler.Should().NotBeNull();
        var webProxy = httpClientHandler.Proxy as WebProxy;
        webProxy.Should().NotBeNull();
        webProxy.Address.Should().Be(proxyUri);
        httpClientHandler.UseProxy.Should().BeTrue();
        logger.Received(1).Debug($"System proxy detected and configured: {proxyUri}");
    }

    [TestMethod]
    public void NoSystemProxyConfigured_DoesNotConfigureHttpClientHandler()
    {
        var baseAddress = new Uri("http://localhost");
        proxyDetector.GetProxyUri(baseAddress).Returns(baseAddress);

        var httpClientHandler = httpClientHandlerFactory.Create(baseAddress);

        httpClientHandler.Should().NotBeNull();
        httpClientHandler.Proxy.Should().BeNull();
        httpClientHandler.UseProxy.Should().BeTrue(); // default value is true
        logger.Received(1).Debug("No system proxy detected");
    }
}
