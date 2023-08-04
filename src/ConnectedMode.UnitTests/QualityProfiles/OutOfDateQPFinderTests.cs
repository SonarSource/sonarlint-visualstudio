/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System;
using System.Collections.Generic;
using System.Threading;
using SonarLint.VisualStudio.ConnectedMode.QualityProfiles;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Models;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.QualityProfiles
{
    [TestClass]
    public class OutOfDateQPFinderTests
    {
        private static readonly Uri ValidUri = new Uri("http://server");
        private static readonly SonarQubeQualityProfile ValidServerResponse = new SonarQubeQualityProfile("any key", "any name", "any language", true, DateTime.UtcNow);
        private static readonly ApplicableQualityProfile ValidApplicableQualityProfile = new ApplicableQualityProfile
        {
            ProfileKey = "any", ProfileTimestamp = DateTime.UtcNow,
        };

        [TestMethod]
        public void MefCtor_CheckIsExported()
            => MefTestHelpers.CheckTypeCanBeImported<OutOfDateQPFinder, IOutOfDateQPFinder>(
                MefTestHelpers.CreateExport<IConfigurationProvider>(),
                MefTestHelpers.CreateExport<ISonarQubeService>(),
                MefTestHelpers.CreateExport<ILogger>());

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
            => MefTestHelpers.CheckIsSingletonMefComponent<OutOfDateQPFinder>();

        [TestMethod]
        [DataRow("project1", null, "ts")] // Null org key should be handled correctly
        [DataRow("project2", "my org", "secrets")] // Null org key should be handled correctly
        public async Task GetAsync_CorrectParametersPassedToServer(string projectKey, string orgKey, string languageKey)
        {
            var expectedLanguage = Language.GetLanguageFromLanguageKey(languageKey);
            var org = orgKey == null ? null :  new SonarQubeOrganization(orgKey, "org name");

            var boundProject = new BoundSonarQubeProject(ValidUri, projectKey, "any name", null, org)
            {
                Profiles = new Dictionary<Language, ApplicableQualityProfile>
                {
                    { expectedLanguage, ValidApplicableQualityProfile}
                }
            };

            var sqServer = CreateSonarQubeService(ValidServerResponse);
            var configProvider = CreateConfigProvider(boundProject);

            var testSubject = CreateTestSubject(configProvider.Object, sqServer.Object);

            await testSubject.GetAsync(CancellationToken.None);

            sqServer.Verify(x => x.GetQualityProfileAsync(
                projectKey, orgKey, expectedLanguage.ServerLanguage, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task GetAsync_AllProfilesChecked()
        {
            var boundProject = new BoundSonarQubeProject(ValidUri, "any", null, null)
            {
                Profiles = new Dictionary<Language, ApplicableQualityProfile>
                {
                    { Language.CSharp, ValidApplicableQualityProfile},
                    { Language.VBNET, ValidApplicableQualityProfile},
                    { Language.Secrets, ValidApplicableQualityProfile}
                }
            };

            var checkedLanguages = new List<SonarQubeLanguage>();

            var sqServer = new Mock<ISonarQubeService>();
            sqServer.Setup(x => x.GetQualityProfileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SonarQubeLanguage>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, SonarQubeLanguage, CancellationToken>((project, org, lang, token) => checkedLanguages.Add(lang))
                .ReturnsAsync(ValidServerResponse);

            var configProvider = CreateConfigProvider(boundProject);

            var testSubject = CreateTestSubject(configProvider.Object, sqServer.Object);

            await testSubject.GetAsync(CancellationToken.None);

            checkedLanguages.Should().BeEquivalentTo(
                SonarQubeLanguage.CSharp,
                SonarQubeLanguage.VbNet,
                SonarQubeLanguage.Secrets);
        }

        [TestMethod]
        [DataRow("same key", "same key", 0, false)] // profiles match -> no update
        [DataRow("key", "KEY", 0, true)] // key are case-sensitive -> no match -> update
        [DataRow("same key", "same key", 1, true)]  // different timestamps -> update
        [DataRow("key 1", "key 2", 0, true)]        // different keys -> update
        [DataRow("key 1", "key 1", 2, true)]        // different keys and timestamps -> update
        public async Task GetAsync_CompareProfileData(
            string localProfileKey,
            string serverProfileKey,
            int serverTimeOffsetInHours, // we can't use DateTime values in attributes, so we're using an integer time offset instead
            bool isUpdateExpected)
        {
            var localProfileDateTime = DateTime.UtcNow;
            var serverProfileDateTime = DateTime.UtcNow.Subtract(TimeSpan.FromHours(serverTimeOffsetInHours));

            var localProfile = CreateLocalProfile(localProfileKey, localProfileDateTime);
            var serverProfile = CreateServerProfile(serverProfileKey, serverProfileDateTime);

            var boundProject = new BoundSonarQubeProject(ValidUri, "any", null, null)
            {
                Profiles = new Dictionary<Language, ApplicableQualityProfile>
                {
                    { Language.Cpp, localProfile }
                }
            };

            var sqServer = CreateSonarQubeService(serverProfile);
            var configProvider = CreateConfigProvider(boundProject);
            var testSubject = CreateTestSubject(configProvider.Object, sqServer.Object);

            var actual = await testSubject.GetAsync(CancellationToken.None);

            if (isUpdateExpected)
            {
                actual.Should().BeEquivalentTo(Language.Cpp);
            }
            else
            {
                actual.Should().BeEmpty();
            }
        }

        private static ApplicableQualityProfile CreateLocalProfile(string profileKey, DateTime timestamp)
            => new ApplicableQualityProfile
            {
                ProfileKey = profileKey,
                ProfileTimestamp = timestamp
            };

        private static SonarQubeQualityProfile CreateServerProfile(string profileKey, DateTime timestamp)
            => new SonarQubeQualityProfile(profileKey, "any name", "any language", true, timestamp);

        private static Mock<IConfigurationProvider> CreateConfigProvider(BoundSonarQubeProject boundProject)
        {
            var bindingConfig = new BindingConfiguration(boundProject, SonarLintMode.Connected, "any");

            var configProvider = new Mock<IConfigurationProvider>();
            configProvider.Setup(x => x.GetConfiguration()).Returns(bindingConfig);
            return configProvider;
        }

        private static Mock<ISonarQubeService> CreateSonarQubeService(SonarQubeQualityProfile response)
        {
            var sqService = new Mock<ISonarQubeService>();
            sqService.Setup(x => x.GetQualityProfileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SonarQubeLanguage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            return sqService;
        }

        private static OutOfDateQPFinder CreateTestSubject(
            IConfigurationProvider configProvider = null,
            ISonarQubeService sonarQubeService = null,
            ILogger logger = null)
            => new OutOfDateQPFinder(
                configProvider ?? Mock.Of<IConfigurationProvider>(),
                sonarQubeService ?? Mock.Of<ISonarQubeService>(),
                logger ?? new TestLogger(logToConsole: true));
    }
}
