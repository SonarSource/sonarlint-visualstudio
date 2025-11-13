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

using Newtonsoft.Json;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Persistence;

[TestClass]
public class BindingJsonModelSerializationTests
{
    private static readonly DateTime Date = new DateTime(2020, 12, 31, 23, 59, 59);

    private static readonly Dictionary<Language, ApplicableQualityProfile> QualityProfiles = new() { { Language.C, new ApplicableQualityProfile { ProfileKey = "qpkey", ProfileTimestamp = Date } } };
    private readonly BoundSonarQubeProject boundSonarQubeProject = new(new Uri("http://next.sonarqube.com/sonarqube"),
        "my_project_123",
        "My Project",
        /* ignored */ null,
        new SonarQubeOrganization("org_key_123", "My Org"));

    private readonly BindingJsonModel bindingJsonModel = new()
    {
        ServerUri = new Uri("http://next.sonarqube.com/sonarqube"),
        ProjectKey = "my_project_123",
        ProjectName = "My Project",
        Organization = new SonarQubeOrganization("org_key_123", "My Org"),
        ServerConnectionId = "some_connection_id_123",
    };

    private readonly BoundServerProject boundSonarCloudServerProject = new("solution123", "my_project_123", new ServerConnection.SonarCloud("org_key_123"));
    private readonly BoundServerProject boundSonarQubeServerProject
        = new("solution123", "my_project_123", new ServerConnection.SonarQube(new Uri("http://next.sonarqube.com/sonarqube")));

    private readonly BindingJsonModelConverter bindingJsonModelConverter = new();

    [TestMethod]
    public void JsonModel_SerializedAsExpected()
    {
        var serializeObject = JsonConvert.SerializeObject(bindingJsonModel, Formatting.Indented);

        serializeObject.Should().BeEquivalentTo(
            """
            {
              "ServerConnectionId": "some_connection_id_123",
              "ServerUri": "http://next.sonarqube.com/sonarqube",
              "Organization": {
                "Key": "org_key_123",
                "Name": "My Org"
              },
              "ProjectKey": "my_project_123",
              "ProjectName": "My Project"
            }
            """);
    }

    [TestMethod]
    public void JsonModel_FromSonarCloudBinding_SerializedAsExpected()
    {
        var serializeObject = JsonConvert.SerializeObject(bindingJsonModelConverter.ConvertToModel(boundSonarCloudServerProject), Formatting.Indented);

        serializeObject.Should().BeEquivalentTo(
            """
            {
              "ServerConnectionId": "https://sonarcloud.io/organizations/org_key_123",
              "ServerUri": "https://sonarcloud.io/",
              "Organization": {
                "Key": "org_key_123",
                "Name": null
              },
              "ProjectKey": "my_project_123"
            }
            """);
    }

    [TestMethod]
    public void JsonModel_FromSonarQubeBinding_SerializedAsExpected()
    {
        var serializeObject = JsonConvert.SerializeObject(bindingJsonModelConverter.ConvertToModel(boundSonarQubeServerProject), Formatting.Indented);

        serializeObject.Should().BeEquivalentTo(
            """
            {
              "ServerConnectionId": "http://next.sonarqube.com/sonarqube",
              "ServerUri": "http://next.sonarqube.com/sonarqube",
              "ProjectKey": "my_project_123"
            }
            """);
    }

    [TestMethod]
    public void Legacy_ToJson_ToJsonModel_ToLegacy_IsCorrect()
    {
        var serialized = JsonConvert.SerializeObject(boundSonarQubeProject);
        var deserializeBindingModel = JsonConvert.DeserializeObject<BindingJsonModel>(serialized);

        var convertedFromJsonToLegacy = bindingJsonModelConverter.ConvertFromModelToLegacy(deserializeBindingModel, null);

        convertedFromJsonToLegacy.Should().BeEquivalentTo(boundSonarQubeProject);
    }

    [TestMethod]
    public void Legacy_ToJson_ToLegacyDirectly_And_ToJsonModel_ToLegacy_IsCorrect()
    {
        var serializedLegacy = JsonConvert.SerializeObject(boundSonarQubeProject);
        var legacyDirect = JsonConvert.DeserializeObject<BoundSonarQubeProject>(serializedLegacy);
        var deserializedBindingModel = JsonConvert.DeserializeObject<BindingJsonModel>(serializedLegacy);

        var convertedFromJsonToLegacy = bindingJsonModelConverter.ConvertFromModelToLegacy(deserializedBindingModel, null);

        convertedFromJsonToLegacy.Should().BeEquivalentTo(legacyDirect);
    }

    [TestMethod]
    public void JsonModel_ToJson_ToLegacy_IsCorrect()
    {
        var serializedBindingModel = JsonConvert.SerializeObject(bindingJsonModel);
        var convertedFromJsonToLegacy = JsonConvert.DeserializeObject<BoundSonarQubeProject>(serializedBindingModel);

        convertedFromJsonToLegacy.Should().BeEquivalentTo(boundSonarQubeProject);
    }

    [TestMethod]
    public void JsonModel_ToJson_ToJsonModel_ToLegacy_IsCorrect()
    {
        var serializedBindingModel = JsonConvert.SerializeObject(bindingJsonModel);
        var deserializedBindingModel = JsonConvert.DeserializeObject<BindingJsonModel>(serializedBindingModel);

        var convertedFromJsonToLegacy = bindingJsonModelConverter.ConvertFromModelToLegacy(deserializedBindingModel, null);

        convertedFromJsonToLegacy.Should().BeEquivalentTo(boundSonarQubeProject);
    }

    [TestMethod]
    public void CurrentSonarCloud_ToJsonModel_ToJson_ToJsonModel_ToLegacy_IsCorrect()
    {
        var convertedBindingJsonFromLegacy = bindingJsonModelConverter.ConvertToModel(boundSonarCloudServerProject);
        var serializedBindingModel = JsonConvert.SerializeObject(convertedBindingJsonFromLegacy);
        var deserializedBindingModel = JsonConvert.DeserializeObject<BindingJsonModel>(serializedBindingModel);

        var legacyBinding = bindingJsonModelConverter.ConvertFromModelToLegacy(deserializedBindingModel, null);

        legacyBinding.Should().BeEquivalentTo(boundSonarQubeProject, options => options.Excluding(x => x.ProjectName).Excluding(x => x.ServerUri).Excluding(x => x.Organization.Name));
        legacyBinding.ServerUri.Should().BeEquivalentTo(boundSonarCloudServerProject.ServerConnection.ServerUri);
        legacyBinding.ProjectName.Should().BeNull();
        legacyBinding.Organization.Name.Should().BeNull();
    }

    [TestMethod]
    public void CurrentSonarQube_ToJsonModel_ToJson_ToJsonModel_ToLegacy_IsCorrect()
    {
        var convertedBindingJsonFromLegacy = bindingJsonModelConverter.ConvertToModel(boundSonarQubeServerProject);
        var serializedBindingModel = JsonConvert.SerializeObject(convertedBindingJsonFromLegacy);
        var deserializedBindingModel = JsonConvert.DeserializeObject<BindingJsonModel>(serializedBindingModel);

        var legacyBinding = bindingJsonModelConverter.ConvertFromModelToLegacy(deserializedBindingModel, null);

        legacyBinding.Should().BeEquivalentTo(boundSonarQubeProject, options => options.Excluding(x => x.ProjectName).Excluding(x => x.Organization));
        legacyBinding.ProjectName.Should().BeNull();
        legacyBinding.Organization.Should().BeNull();
    }

    [TestMethod]
    public void CurrentSonarCloud_ToJsonModel_ToJson_ToJsonModel_ToCurrent_IsCorrect()
    {
        var convertedBindingJsonFromLegacy = bindingJsonModelConverter.ConvertToModel(boundSonarCloudServerProject);
        var serializedBindingModel = JsonConvert.SerializeObject(convertedBindingJsonFromLegacy);
        var deserializedBindingModel = JsonConvert.DeserializeObject<BindingJsonModel>(serializedBindingModel);
        deserializedBindingModel.ServerConnectionId.Should().BeEquivalentTo(boundSonarCloudServerProject.ServerConnection.Id);

        var binding = bindingJsonModelConverter.ConvertFromModel(deserializedBindingModel, boundSonarCloudServerProject.ServerConnection, "solution123");

        binding.Should().BeEquivalentTo(boundSonarCloudServerProject);
    }

    [TestMethod]
    public void CurrentSonarQube_ToJsonModel_ToJson_ToJsonModel_ToCurrent_IsCorrect()
    {
        var convertedBindingJsonFromLegacy = bindingJsonModelConverter.ConvertToModel(boundSonarQubeServerProject);
        var serializedBindingModel = JsonConvert.SerializeObject(convertedBindingJsonFromLegacy);
        var deserializedBindingModel = JsonConvert.DeserializeObject<BindingJsonModel>(serializedBindingModel);
        deserializedBindingModel.ServerConnectionId.Should().BeEquivalentTo(boundSonarQubeServerProject.ServerConnection.Id);

        var binding = bindingJsonModelConverter.ConvertFromModel(deserializedBindingModel, boundSonarQubeServerProject.ServerConnection, "solution123");

        binding.Should().BeEquivalentTo(boundSonarQubeServerProject);
    }
}
