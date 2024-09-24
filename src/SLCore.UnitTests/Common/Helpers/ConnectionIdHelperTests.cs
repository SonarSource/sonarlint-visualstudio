
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

using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.SLCore.Common.Helpers;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Common.Helpers;

[TestClass]
public class ConnectionIdHelperTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<ConnectionIdHelper, IConnectionIdHelper>();
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<ConnectionIdHelper>();
    }
    
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
    [DataRow("http://someuri.com", null, "sq|http://someuri.com/")]
    [DataRow("https://sonarcloud.io", "something", "sc|https://sonarcloud.io/organizations/something")]
    public void GetConnectionIdFromServerConnection_PassUri_ReturnsAsExpected(string uriString, string organisation, string expectedConnectionId)
    {
        var uri = new Uri(uriString);

        var testSubject = new ConnectionIdHelper();

        var actualConnectionId = testSubject.GetConnectionIdFromServerConnection(organisation is null ? new ServerConnection.SonarQube(new Uri(uriString)) : new ServerConnection.SonarCloud(organisation));

        actualConnectionId.Should().Be(expectedConnectionId);
    }

    [TestMethod]
    public void GetConnectionIdFromServerConnection_ConnectionIsNull_ReturnsNull()
    {
        var testSubject = new ConnectionIdHelper();

        var actualConnectionId = testSubject.GetConnectionIdFromServerConnection(null);

        actualConnectionId.Should().BeNull();
    }

    [TestMethod]
    public void MethodsBackToBack_SonarQube_ShouldCreateSameUri()
    {
        var uri = new Uri("http://someuri.com");

        var testSubject = new ConnectionIdHelper();

        var resultUri = testSubject.GetUriFromConnectionId(testSubject.GetConnectionIdFromServerConnection(new ServerConnection.SonarQube(uri)));

        resultUri.Should().Be(uri);
    }
    
    [TestMethod]
    public void MethodsBackToBack_SonarCloud_ShouldCreateSameUri()
    {
        var uri = new Uri("https://sonarcloud.io");

        var testSubject = new ConnectionIdHelper();

        var resultUri = testSubject.GetUriFromConnectionId(testSubject.GetConnectionIdFromServerConnection(new ServerConnection.SonarCloud("my org")));

        resultUri.Should().Be(uri);
    }
}
