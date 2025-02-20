/*
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

using System.Net.Http;
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

        var result = httpClientHandlerFactory.Create(baseAddress);

        result.Should().NotBeNull();
        proxyDetector.Received(1).ConfigureProxy(Arg.Any<HttpClientHandler>(), proxyUri);
        logger.Received(1).Debug($"System proxy detected and configured: {proxyUri}");
    }

    [TestMethod]
    public void NoSystemProxyConfigured_DoesNotConfigureHttpClientHandler()
    {
        var baseAddress = new Uri("http://localhost");
        proxyDetector.GetProxyUri(baseAddress).Returns(baseAddress);

        var result = httpClientHandlerFactory.Create(baseAddress);

        result.Should().NotBeNull();
        proxyDetector.DidNotReceive().ConfigureProxy(Arg.Any<HttpClientHandler>(), Arg.Any<Uri>());
        logger.Received(1).Debug("No system proxy detected");
    }
}
