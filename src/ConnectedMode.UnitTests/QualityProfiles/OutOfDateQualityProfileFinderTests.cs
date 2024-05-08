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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.QualityProfiles;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.QualityProfiles;

[TestClass]
public class OutOfDateQualityProfileFinderTests
{
    private static readonly Uri AnyUri = new("http://localhost");
    private const string Project = "project";
    private const string Organization = "organization";
    
    [TestMethod]
    public void MefCtor_CheckExports()
    {
        MefTestHelpers.CheckTypeCanBeImported<OutOfDateQualityProfileFinder, IOutOfDateQualityProfileFinder>(
            MefTestHelpers.CreateExport<ISonarQubeService>());
    }

    [TestMethod]
    public void Mef_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<OutOfDateQualityProfileFinder>();
    }
    
    [TestMethod]
    public async Task GetAsync_SkipsUnknownLanguage()
    {
        var testSubject = CreateTestSubject(out var sonarQubeServiceMock, Project, Organization,
            new SonarQubeQualityProfile("key", "name", "unsupportedlanguage", false, DateTime.UtcNow));

        var profiles = await testSubject.GetAsync(CreateArgument(Project, Organization, null), CancellationToken.None);

        profiles.Should().BeEmpty();
        sonarQubeServiceMock.Verify(x => x.GetAllQualityProfilesAsync(Project, Organization, CancellationToken.None), Times.Once);
    }
    
    [TestMethod]
    public async Task GetAsync_SkipsUpToDateQualityProfile()
    {
        const string qpKey = "key";
        var timestamp = DateTime.UtcNow;
        
        var testSubject = CreateTestSubject(out var sonarQubeServiceMock, Project, Organization,
            new SonarQubeQualityProfile(qpKey, "name", Language.Ts.ServerLanguage.Key, false, timestamp));

        var profiles = await testSubject.GetAsync(
            CreateArgument(Project,
                Organization, 
                new Dictionary<Language, ApplicableQualityProfile>{{Language.Ts, new ApplicableQualityProfile{ProfileKey = qpKey, ProfileTimestamp = timestamp}}}),
            CancellationToken.None);

        profiles.Should().BeEmpty();
        sonarQubeServiceMock.Verify(x => x.GetAllQualityProfilesAsync(Project, Organization, CancellationToken.None), Times.Once);
    }
    
    [TestMethod]
    public async Task GetAsync_DifferentKey_ReturnsQP()
    {
        const string serverQpKey = "key1";
        const string localQpKey = "key2";
        var timestampServer = DateTime.UtcNow;
        var timestampLocal = timestampServer.AddHours(1); // local timestamp is greater, but the key is more important
        var sonarQubeQualityProfile = new SonarQubeQualityProfile(serverQpKey, "name", Language.Ts.ServerLanguage.Key, false, timestampServer);

        var testSubject = CreateTestSubject(out var sonarQubeServiceMock, Project, Organization,
            sonarQubeQualityProfile);

        var profiles = await testSubject.GetAsync(
            CreateArgument(Project,
                Organization, 
                new Dictionary<Language, ApplicableQualityProfile>
                {
                    {Language.Ts, new ApplicableQualityProfile{ProfileKey = localQpKey, ProfileTimestamp = timestampLocal}}
                }),
            CancellationToken.None);

        profiles.Should().BeEquivalentTo((Language.Ts, sonarQubeQualityProfile));
        sonarQubeServiceMock.Verify(x => x.GetAllQualityProfilesAsync(Project, Organization, CancellationToken.None), Times.Once);
    }
    
    [TestMethod]
    public async Task GetAsync_OutdatedLocalTimestamp_ReturnsQP()
    {
        const string qpKey = "key";
        var localTimestamp = DateTime.UtcNow;
        var serverTimestamp = localTimestamp.AddHours(1);
        var sonarQubeQualityProfile = new SonarQubeQualityProfile(qpKey, "name", Language.Ts.ServerLanguage.Key, false, serverTimestamp);

        var testSubject = CreateTestSubject(out var sonarQubeServiceMock, Project, Organization,
            sonarQubeQualityProfile);

        var profiles = await testSubject.GetAsync(
            CreateArgument(Project,
                Organization, 
                new Dictionary<Language, ApplicableQualityProfile>
                {
                    {Language.Ts, new ApplicableQualityProfile{ProfileKey = qpKey, ProfileTimestamp = localTimestamp}}
                }),
            CancellationToken.None);

        profiles.Should().BeEquivalentTo((Language.Ts, sonarQubeQualityProfile));
        sonarQubeServiceMock.Verify(x => x.GetAllQualityProfilesAsync(Project, Organization, CancellationToken.None), Times.Once);
    }
    
    [TestMethod]
    public async Task GetAsync_NullLocalQP_ReturnsQP()
    {
        const string qpKey = "key";
        var serverTimestamp = DateTime.UtcNow;
        var sonarQubeQualityProfile = new SonarQubeQualityProfile(qpKey, "name", Language.Ts.ServerLanguage.Key, false, serverTimestamp);

        var testSubject = CreateTestSubject(out var sonarQubeServiceMock, Project, Organization,
            sonarQubeQualityProfile);

        var profiles = await testSubject.GetAsync(
            CreateArgument(Project,
                Organization, 
                new Dictionary<Language, ApplicableQualityProfile>
                {
                    {Language.Ts, new ApplicableQualityProfile{ProfileKey = null, ProfileTimestamp = DateTime.MinValue}}
                }),
            CancellationToken.None);

        profiles.Should().BeEquivalentTo((Language.Ts, sonarQubeQualityProfile));
        sonarQubeServiceMock.Verify(x => x.GetAllQualityProfilesAsync(Project, Organization, CancellationToken.None), Times.Once);
    }
    
    [TestMethod]
    public async Task GetAsync_NullOrganization_DoesNotThrow()
    {
        var testSubject = CreateTestSubject(out var sonarQubeServiceMock, Project, null);

        var act = () => testSubject.GetAsync(
            CreateArgument(Project,
                null, 
                new Dictionary<Language, ApplicableQualityProfile>()),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
        sonarQubeServiceMock.Verify(
            x =>
                x.GetAllQualityProfilesAsync(Project, null, CancellationToken.None),
            Times.Once);
    }
    
    [TestMethod]
    public async Task GetAsync_MultipleQualityProfiles_ReturnsQP()
    {
        var serverKey = "key";
        var localKey = "localKey";
        var timestamp = DateTime.UtcNow;
        var csharpQp = new SonarQubeQualityProfile(serverKey, "name", Language.CSharp.ServerLanguage.Key, false, timestamp);
        var cssQp = new SonarQubeQualityProfile(serverKey, "name", Language.Css.ServerLanguage.Key, false, timestamp);
        var jsQp = new SonarQubeQualityProfile(serverKey, "name", Language.Js.ServerLanguage.Key, false, timestamp);

        var testSubject = CreateTestSubject(out var sonarQubeServiceMock, Project, Organization,
            csharpQp,
            cssQp,
            jsQp);

        var profiles = await testSubject.GetAsync(
            CreateArgument(Project,
                Organization, 
                new Dictionary<Language, ApplicableQualityProfile>
                {
                    {Language.Js, new ApplicableQualityProfile{ProfileKey = localKey, ProfileTimestamp = timestamp}},
                    {Language.CSharp, new ApplicableQualityProfile{ProfileKey = localKey, ProfileTimestamp = timestamp}},
                    {Language.Css, new ApplicableQualityProfile{ProfileKey = serverKey, ProfileTimestamp = timestamp}} // same qp
                }),
            CancellationToken.None);

        profiles.Should().BeEquivalentTo((Language.CSharp, csharpQp), (Language.Js, jsQp));
        sonarQubeServiceMock.Verify(x => x.GetAllQualityProfilesAsync(Project, Organization, CancellationToken.None), Times.Once);
    }
    
    private static BoundSonarQubeProject CreateArgument(string project,
        string organization,
        Dictionary<Language, ApplicableQualityProfile> profiles) =>
        new(AnyUri, 
            project,
            null, 
            null,
            organization == null ? null : new(organization, null))
        {
            Profiles = profiles
        };

    private IOutOfDateQualityProfileFinder CreateTestSubject(out Mock<ISonarQubeService> sonarQubeServiceMock,
        string project,
        string organization,
        params SonarQubeQualityProfile[] qualityProfiles)
    {
        sonarQubeServiceMock = new Mock<ISonarQubeService>();
        sonarQubeServiceMock
            .Setup(x => x.GetAllQualityProfilesAsync(project, organization, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IList<SonarQubeQualityProfile>>(qualityProfiles));

        return new OutOfDateQualityProfileFinder(sonarQubeServiceMock.Object);
    }
}
