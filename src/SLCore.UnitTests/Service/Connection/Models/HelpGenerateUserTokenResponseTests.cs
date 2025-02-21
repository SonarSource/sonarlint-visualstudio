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
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Connection.Models;

[TestClass]
public class HelpGenerateUserTokenResponseTests
{
    [TestMethod]
    [DataRow("""
             {
               "token": "A186C072-9D8F-404C-9BA8-31EC450409B2"
             }
             """, "A186C072-9D8F-404C-9BA8-31EC450409B2")]
    [DataRow("""
             {
               "token": null
             }
             """, null)]
    public void Serialize_AsExpected(string expected, string token)
    {
        var helpGenerateUserTokenParams = new HelpGenerateUserTokenResponse(token);

        var actual = JsonConvert.SerializeObject(helpGenerateUserTokenParams, Formatting.Indented);

        actual.Should().Be(expected);
    }

    [TestMethod]
    [DataRow("""
             {
               "token": "A186C072-9D8F-404C-9BA8-31EC450409B2"
             }
             """, "A186C072-9D8F-404C-9BA8-31EC450409B2")]
    [DataRow("""
             {
               "token": null
             }
             """, null)]
    public void Deserialize_AsExpected(string serialized, string expectedToken)
    {
        var expected = new HelpGenerateUserTokenResponse(expectedToken);

        var actual = JsonConvert.DeserializeObject<HelpGenerateUserTokenResponse>(serialized);

        actual.Should().BeEquivalentTo(expected, options => options.ComparingByMembers<HelpGenerateUserTokenResponse>());
    }
}
