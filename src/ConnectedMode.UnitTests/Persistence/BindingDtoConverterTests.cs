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

using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Persistence;

[TestClass]
public class BindingDtoConverterTests
{
    private BindingDtoConverter testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        testSubject = new BindingDtoConverter();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<BindingDtoConverter, IBindingDtoConverter>();
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<BindingDtoConverter>();
    }
    
    [TestMethod]
    public void ConvertFromDto_ConvertsCorrectly()
    {
        const string localBindingKey = "solution123";
        var bindingDto = new BindingDto
        {
            ProjectKey = "project123",
            Profiles = new Dictionary<Language, ApplicableQualityProfile>(),
            ProjectName = "ignored",
            Organization = new SonarQubeOrganization("ignored", "ignored"),
            ServerUri = new Uri("http://ignored"),
            ServerConnectionId = "ignored", // only used for connection extraction, delegates to the connection object during the conversion
        };
        var connection = new ServerConnection.SonarCloud("myorg");

        var boundServerProject = testSubject.ConvertFromDto(bindingDto, connection, localBindingKey);

        boundServerProject.ServerConnection.Should().BeSameAs(connection);
        boundServerProject.LocalBindingKey.Should().BeSameAs(localBindingKey);
        boundServerProject.ServerProjectKey.Should().BeSameAs(bindingDto.ProjectKey);
        boundServerProject.Profiles.Should().BeSameAs(bindingDto.Profiles);
    }

    [TestMethod]
    public void ConvertToDto_SonarCloudConnection_ConvertsCorrectly()
    {
        var boundServerProject = new BoundServerProject("localBinding", "serverProject", new ServerConnection.SonarCloud("myorg"))
        {
            Profiles = new Dictionary<Language, ApplicableQualityProfile>()
        };

        var bindingDto = testSubject.ConvertToDto(boundServerProject);

        bindingDto.ProjectKey.Should().BeSameAs(boundServerProject.ServerProjectKey);
        bindingDto.ProjectName.Should().BeNull();
        bindingDto.ServerUri.Should().BeEquivalentTo(boundServerProject.ServerConnection.ServerUri);
        bindingDto.Organization.Key.Should().BeSameAs(((ServerConnection.SonarCloud)boundServerProject.ServerConnection).OrganizationKey);
        bindingDto.ServerConnectionId.Should().BeSameAs(boundServerProject.ServerConnection.Id);
        bindingDto.Profiles.Should().BeSameAs(boundServerProject.Profiles);
    }
    
    [TestMethod]
    public void ConvertToDto_SonarQubeConnection_ConvertsCorrectly()
    {
        var boundServerProject = new BoundServerProject("localBinding", "serverProject", new ServerConnection.SonarQube(new Uri("http://mysq")))
        {
            Profiles = new Dictionary<Language, ApplicableQualityProfile>()
        };

        var bindingDto = testSubject.ConvertToDto(boundServerProject);

        bindingDto.ProjectKey.Should().BeSameAs(boundServerProject.ServerProjectKey);
        bindingDto.ProjectName.Should().BeNull();
        bindingDto.ServerUri.Should().BeEquivalentTo(boundServerProject.ServerConnection.ServerUri);
        bindingDto.Organization.Should().BeNull();
        bindingDto.ServerConnectionId.Should().BeSameAs(boundServerProject.ServerConnection.Id);
        bindingDto.Profiles.Should().BeSameAs(boundServerProject.Profiles);
    }

    [TestMethod]
    public void ConvertFromDtoToLegacy_ConvertsCorrectly()
    {
        var credentials = Substitute.For<ICredentials>();
        var bindingDto = new BindingDto
        {
            Organization = new SonarQubeOrganization("org", "my org"),
            ServerUri = new Uri("http://localhost"),
            ProjectKey = "project123",
            ProjectName = "project 123",
            Profiles = new Dictionary<Language, ApplicableQualityProfile>(),
            ServerConnectionId = "ignored",
        };

        var legacyBinding = testSubject.ConvertFromDtoToLegacy(bindingDto, credentials);

        legacyBinding.ProjectKey.Should().BeSameAs(bindingDto.ProjectKey);
        legacyBinding.ProjectName.Should().BeSameAs(bindingDto.ProjectName);
        legacyBinding.ServerUri.Should().BeSameAs(bindingDto.ServerUri);
        legacyBinding.Organization.Should().BeSameAs(bindingDto.Organization);
        legacyBinding.Profiles.Should().BeSameAs(legacyBinding.Profiles);
        legacyBinding.Credentials.Should().BeSameAs(credentials);
    }
}
