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
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Persistence;

[TestClass]
public class BindingDtoSerializationTests
{
    private static readonly DateTime Date = new DateTime(2020, 12, 31, 23, 59, 59);
    
    private static readonly Dictionary<Language, ApplicableQualityProfile> QualityProfiles = new()
    {
        { Language.C, new ApplicableQualityProfile { ProfileKey = "qpkey", ProfileTimestamp = Date } }
    };
    private readonly BoundSonarQubeProject boundSonarQubeProject = new(new Uri("http://next.sonarqube.com/sonarqube"),
        "my_project_123",
        "My Project",
        /* ignored */ null,
        new SonarQubeOrganization("org_key_123", "My Org"))
    {
        Profiles = QualityProfiles
    };

    private readonly BindingDto bindingDto = new()
    {
        ServerUri = new Uri("http://next.sonarqube.com/sonarqube"),
        ProjectKey = "my_project_123",
        ProjectName = "My Project",
        Organization = new SonarQubeOrganization("org_key_123", "My Org"),
        ServerConnectionId = "some_connection_id_123",
        Profiles = QualityProfiles
    };
    
    private readonly BoundServerProject boundSonarCloudServerProject = new("solution123", "my_project_123", new ServerConnection.SonarCloud("org_key_123")){ Profiles = QualityProfiles };
    private readonly BoundServerProject boundSonarQubeServerProject = new("solution123", "my_project_123", new ServerConnection.SonarQube(new Uri("http://next.sonarqube.com/sonarqube"))){ Profiles = QualityProfiles };
    
    private readonly BindingDtoConverter bindingDtoConverter = new();

    [TestMethod]
    public void Dto_SerializedAsExpected()
    {
        var serializeObject = JsonConvert.SerializeObject(bindingDto, Formatting.Indented);

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
              "ProjectName": "My Project",
              "Profiles": {
                "C": {
                  "ProfileKey": "qpkey",
                  "ProfileTimestamp": "2020-12-31T23:59:59"
                }
              }
            }
            """);
    }
    
    [TestMethod]
    public void Dto_FromSonarCloudBinding_SerializedAsExpected()
    {
        var serializeObject = JsonConvert.SerializeObject(bindingDtoConverter.ConvertToDto(boundSonarCloudServerProject), Formatting.Indented);

        serializeObject.Should().BeEquivalentTo(
            """
            {
              "ServerConnectionId": "org_key_123",
              "ServerUri": "https://sonarcloud.io",
              "Organization": {
                "Key": "org_key_123",
                "Name": null
              },
              "ProjectKey": "my_project_123",
              "Profiles": {
                "C": {
                  "ProfileKey": "qpkey",
                  "ProfileTimestamp": "2020-12-31T23:59:59"
                }
              }
            }
            """);
    }
    
    [TestMethod]
    public void Dto_FromSonarQubeBinding_SerializedAsExpected()
    {
        var serializeObject = JsonConvert.SerializeObject(bindingDtoConverter.ConvertToDto(boundSonarQubeServerProject), Formatting.Indented);

        serializeObject.Should().BeEquivalentTo(
            """
            {
              "ServerConnectionId": "http://next.sonarqube.com/sonarqube",
              "ServerUri": "http://next.sonarqube.com/sonarqube",
              "ProjectKey": "my_project_123",
              "Profiles": {
                "C": {
                  "ProfileKey": "qpkey",
                  "ProfileTimestamp": "2020-12-31T23:59:59"
                }
              }
            }
            """);
    }
    
    [TestMethod]
    public void Legacy_ToJson_ToDto_ToLegacy_IsCorrect()
    {
        var serialized = JsonConvert.SerializeObject(boundSonarQubeProject);
        var convertedFromDtoToLegacy = bindingDtoConverter.ConvertFromDtoToLegacy(JsonConvert.DeserializeObject<BindingDto>(serialized), null);
        
        convertedFromDtoToLegacy.Should().BeEquivalentTo(boundSonarQubeProject);
    }
    
    [TestMethod]
    public void Legacy_ToJson_ToLegacyDirectly_And_ToDto_ToLegacy_IsCorrect()
    {
        var serialized = JsonConvert.SerializeObject(boundSonarQubeProject);
        var legacyDirect = JsonConvert.DeserializeObject<BoundSonarQubeProject>(serialized);
        var convertedFromDtoToLegacy = bindingDtoConverter.ConvertFromDtoToLegacy(JsonConvert.DeserializeObject<BindingDto>(serialized), null);
        
        convertedFromDtoToLegacy.Should().BeEquivalentTo(legacyDirect);
    }
    
    [TestMethod]
    public void Dto_ToJson_ToLegacy_IsCorrect()
    {
        var convertedFromDtoToLegacy = JsonConvert.DeserializeObject<BoundSonarQubeProject>(JsonConvert.SerializeObject(bindingDto));
        
        convertedFromDtoToLegacy.Should().BeEquivalentTo(boundSonarQubeProject);
    }
    
    [TestMethod]
    public void Dto_ToJson_ToDto_ToLegacy_IsCorrect()
    {
        var convertedFromDtoToLegacy = bindingDtoConverter.ConvertFromDtoToLegacy(JsonConvert.DeserializeObject<BindingDto>(JsonConvert.SerializeObject(bindingDto)), null);
        
        convertedFromDtoToLegacy.Should().BeEquivalentTo(boundSonarQubeProject);
    }
    
    [TestMethod]
    public void CurrentSonarCloud_ToDto_ToJson_ToDto_ToLegacy_IsCorrect()
    {
        var legacyBinding = bindingDtoConverter.ConvertFromDtoToLegacy(JsonConvert.DeserializeObject<BindingDto>(JsonConvert.SerializeObject(bindingDtoConverter.ConvertToDto(boundSonarCloudServerProject))), null);
        
        legacyBinding.Should().BeEquivalentTo(boundSonarQubeProject, options => options.Excluding(x => x.ProjectName).Excluding(x => x.ServerUri).Excluding(x => x.Organization.Name));
        legacyBinding.ServerUri.Should().BeEquivalentTo(boundSonarCloudServerProject.ServerConnection.ServerUri);
        legacyBinding.ProjectName.Should().BeNull();
        legacyBinding.Organization.Name.Should().BeNull();
    }
    
    [TestMethod]
    public void CurrentSonarQube_ToDto_ToJson_ToDto_ToLegacy_IsCorrect()
    {
        var legacyBinding = bindingDtoConverter.ConvertFromDtoToLegacy(JsonConvert.DeserializeObject<BindingDto>(JsonConvert.SerializeObject(bindingDtoConverter.ConvertToDto(boundSonarQubeServerProject))), null);
        
        legacyBinding.Should().BeEquivalentTo(boundSonarQubeProject, options => options.Excluding(x => x.ProjectName).Excluding(x => x.Organization));
        legacyBinding.ProjectName.Should().BeNull();
        legacyBinding.Organization.Should().BeNull();
    }
    
    [TestMethod]
    public void CurrentSonarCloud_ToDto_ToJson_ToDto_ToCurrent_IsCorrect()
    {
        var dto = JsonConvert.DeserializeObject<BindingDto>(JsonConvert.SerializeObject(bindingDtoConverter.ConvertToDto(boundSonarCloudServerProject)));
        dto.ServerConnectionId.Should().BeEquivalentTo(boundSonarCloudServerProject.ServerConnection.Id);
        
        var binding = bindingDtoConverter.ConvertFromDto(dto, boundSonarCloudServerProject.ServerConnection, "solution123");
        
        binding.Should().BeEquivalentTo(boundSonarCloudServerProject);
    }
    
    [TestMethod]
    public void CurrentSonarQube_ToDto_ToJson_ToDto_ToCurrent_IsCorrect()
    {
        var dto = JsonConvert.DeserializeObject<BindingDto>(JsonConvert.SerializeObject(bindingDtoConverter.ConvertToDto(boundSonarQubeServerProject)));
        dto.ServerConnectionId.Should().BeEquivalentTo(boundSonarQubeServerProject.ServerConnection.Id);
        
        var binding = bindingDtoConverter.ConvertFromDto(dto, boundSonarQubeServerProject.ServerConnection, "solution123");
        
        binding.Should().BeEquivalentTo(boundSonarQubeServerProject);
    }
}
