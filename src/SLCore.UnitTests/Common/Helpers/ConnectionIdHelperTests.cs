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

using SonarLint.VisualStudio.SLCore.Common.Helpers;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Common.Helpers;

[TestClass]
public class ConnectionIdHelperTests
{
    [DataTestMethod]
    [DataRow(null, null)]
    [DataRow("", null)]
    [DataRow("http://someuri.abcdef", null)]
    [DataRow("sq", null)]
    [DataRow("sq|", null)]
    [DataRow("sq|http://someuri.abcdef", "http://someuri.abcdef")]
    [DataRow("sq|https://someuri.abcdef", "https://someuri.abcdef")]
    [DataRow("sq|https://someuri.abcdef/123", "https://someuri.abcdef/123")]
    [DataRow("sc", null)]
    [DataRow("sc|", null)]
    [DataRow("sc|myorganization", "https://sonarcloud.io")]
    [DataRow("sc|https://someuri.abcdef/123", "https://sonarcloud.io")] // should not happen, but we ignore any value after "sc|"
    public void GetUriFromConnectionId_ReturnsAsExpected(string connectionId, string expectedUri)
    {
        var uri = new ConnectionIdHelper().GetUriFromConnectionId(connectionId);

        var stringUri = uri?.ToString().Trim('/'); //trim is because uri.ToString sometimes adds / at the end. This won't be a problem in real code since we use Uri-s and not strings, but for simplicity this tests asserts string equality

        stringUri.Should().BeEquivalentTo(expectedUri);
    }

    [TestMethod]
    [DataRow(null, null, null)]
    [DataRow("", "something", null)]
    [DataRow("not a uri", "something", null)]
    [DataRow("http://someuri.com", null, "sq|http://someuri.com")]
    [DataRow("http://someuri.com", "something", "sq|http://someuri.com")]
    [DataRow("https://sonarcloud.io", "something", "sc|something")]
    [DataRow("https://sonarcloud.io", "", null)]
    [DataRow("https://sonarcloud.io", null, null)]
    public void GetConnectionIdFromUri_PassUri_ReturnsAsExpected(string uriString, string organisation, string expectedConnectionId)
    {
        var testSubject = new ConnectionIdHelper();

        var actualConnectionId = testSubject.GetConnectionIdFromUri(uriString, organisation);

        actualConnectionId.Should().Be(expectedConnectionId);
    }
}
