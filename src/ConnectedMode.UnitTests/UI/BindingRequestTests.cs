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

using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.SLCore.Listener.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI;

[TestClass]
public class BindingRequestTests
{
    [DataRow("some project", "some connection")]
    [DataRow(null, "some connection 2")]
    [DataRow("some project 2", null)]
    [DataTestMethod]
    public void Manual_AllPropertiesReturnExpectedValues(string projectKey, string connectionId)
    {
        var testSubject = new BindingRequest.Manual(projectKey, connectionId);

        testSubject.TypeName.Should().Be(Resources.BindingType_Manual);
        testSubject.ProjectKey.Should().Be(projectKey);
        testSubject.ConnectionId.Should().Be(connectionId);
    }

    [DynamicData(nameof(SharedBindings))]
    [DataTestMethod]
    public void Shared_AllPropertiesReturnExpectedValues(SharedBindingConfigModel model, string expectedProjectKey, string expectedConnectionId)
    {
        var testSubject = new BindingRequest.Shared(model);

        testSubject.TypeName.Should().Be(Resources.BindingType_Shared);
        testSubject.Model.Should().Be(model);
        testSubject.ProjectKey.Should().Be(expectedProjectKey);
        testSubject.ConnectionId.Should().Be(expectedConnectionId);
    }

    [DynamicData(nameof(AssistedBindingSubtypes))]
    [DataTestMethod]
    public void Assisted_AllPropertiesReturnExpectedValues(string connectionId, string projectKey, bool isShared, string expectedTypeName)
    {
        var dto = new AssistBindingParams(connectionId, projectKey, default, isShared);

        var testSubject = new BindingRequest.Assisted(dto);

        testSubject.ConnectionId.Should().Be(connectionId);
        testSubject.ProjectKey.Should().Be(projectKey);
        testSubject.TypeName.Should().Be(expectedTypeName);
        testSubject.Dto.Should().Be(dto);
    }

    public static object[][] AssistedBindingSubtypes =>
    [
        ["some connection", "some project", true, Resources.BindingType_AssistedShared],
        [null, "some project 2", true, Resources.BindingType_AssistedShared],
        ["some connection2", null, true, Resources.BindingType_AssistedShared],
        ["some connection 3", "some project 3", false, Resources.BindingType_Assisted],
        [null, "some project 4", false, Resources.BindingType_Assisted],
        ["some connection 4", null, false, Resources.BindingType_Assisted],
    ];

    public static object[][] SharedBindings =>
    [
        [new SharedBindingConfigModel{Uri = new("http://anyhost/"), ProjectKey = "project key"}, "project key", "http://anyhost/"],
        [new SharedBindingConfigModel{Organization = "orgkey", Region = "EU", ProjectKey = "project key 2"}, "project key 2", "https://sonarcloud.io/organizations/orgkey"],
        [new SharedBindingConfigModel{Organization = "orgkey", Region = "US", ProjectKey = "project key 3"}, "project key 3", "https://sonarqube.us/organizations/orgkey"],
    ];
}
