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

using System.Net;
using System.Net.Http;

namespace SonarQube.Client.Tests;

[TestClass]
public class ProxyDetectorTests
{
    private ProxyDetector testSubject;

    [TestInitialize]
    public void TestInitialize() => testSubject = new ProxyDetector();

    [TestMethod]
    public void ConfigureProxy_ReturnsSystemWebProxy()
    {
        var clientHandler = new HttpClientHandler();
        var uri = new Uri("http://localhost");

        testSubject.ConfigureProxy(clientHandler, uri);

        var webProxy = clientHandler.Proxy as WebProxy;
        webProxy.Should().NotBeNull();
        webProxy.Address.Should().Be(uri);
        clientHandler.UseProxy.Should().BeTrue();
    }
}
