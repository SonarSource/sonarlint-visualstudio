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

using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Listener.Binding;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.Binding;

[TestClass]
public class SonarQubeConnectionParamsTests
{
    [TestMethod]
    public void DeserializesCorrectly()
    {
        var expected = new SonarQubeConnectionParams(new Uri("http://localhost:9000"), "myToken", "89D385F9-88CC-4AF5-B34B-7DAAE7FFB24A");
        var serialized =
            """
            {
                "serverUrl": "http://localhost:9000",
                "tokenName": "myToken",
                "tokenValue": "89D385F9-88CC-4AF5-B34B-7DAAE7FFB24A"
            }
            """;

        var actual = JsonConvert.DeserializeObject<SonarQubeConnectionParams>(serialized);

        actual.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public void SerializesCorrectly()
    {
        var sonarQubeConnection = new SonarQubeConnectionParams(new Uri("http://localhost:9000"), "myToken", "89D385F9-88CC-4AF5-B34B-7DAAE7FFB24A");
        var expected =
            """
            {
              "serverUrl": "http://localhost:9000",
              "tokenName": "myToken",
              "tokenValue": "89D385F9-88CC-4AF5-B34B-7DAAE7FFB24A"
            }
            """;

        var actual = JsonConvert.SerializeObject(sonarQubeConnection, Formatting.Indented);

        actual.Should().BeEquivalentTo(expected);
    }
}
