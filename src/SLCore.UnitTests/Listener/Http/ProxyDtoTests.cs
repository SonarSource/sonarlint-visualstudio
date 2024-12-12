/*
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

using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Listener.Http;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.Http;

[TestClass]
public class ProxyDtoTests
{
    [TestMethod]
    [DataRow(ProxyType.DIRECT, "localhost", 0)]
    [DataRow(ProxyType.HTTP, "localhost", 80)]
    [DataRow(ProxyType.SOCKS, "example.com", 3128)]
    public void Deserialized_AsExpected(ProxyType expectedType, string expectedHost, int expectedPort)
    {
        var expected = new ProxyDto(expectedType, expectedHost, expectedPort);
        string serialized = $@"
            {{
              ""type"": ""{expectedType}"",
              ""hostname"": ""{expectedHost}"",
              ""port"": {expectedPort}
            }}";

        var actual = JsonConvert.DeserializeObject<ProxyDto>(serialized);

        actual.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public void NoProxy_Deserialized_AsExpected()
    {
        var expected = new ProxyDto(ProxyType.DIRECT, null, 0);
        const string serialized =
            """
            {
              "type": "DIRECT",
              "hostname": null,
              "port": 0
            }
            """;

        var actual = JsonConvert.DeserializeObject<ProxyDto>(serialized);

        actual.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public void NoProxy_HasExpectedValues()
    {
        var expected = new ProxyDto(ProxyType.DIRECT, null, 0);

        ProxyDto.NO_PROXY.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public void Serialize_AsExpected()
    {
        var proxyDto = new ProxyDto(ProxyType.HTTP, "localhost", 3128);
        const string expectedString =
            """
            {
              "type": "HTTP",
              "hostname": "localhost",
              "port": 3128
            }
            """;

        var actual = JsonConvert.SerializeObject(proxyDto, Formatting.Indented);

        actual.Should().BeEquivalentTo(expectedString);
    }

    [TestMethod]
    public void NoProxy_Serialize_AsExpected()
    {
        const string expectedString =
            """
            {
              "type": "DIRECT",
              "hostname": null,
              "port": 0
            }
            """;

        var actual = JsonConvert.SerializeObject(ProxyDto.NO_PROXY, Formatting.Indented);

        actual.Should().BeEquivalentTo(expectedString);
    }
}
