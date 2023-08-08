﻿/*
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.QualityProfiles;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Progress.Controller;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.QualityProfiles
{
    [TestClass]
    public class QualityProfileDownloaderTests
    {
        [TestMethod]
        public async Task UpdateAsync_AllSupportedLanguageAreChecked()
        {
            var boundProject = CreateBoundProject();

            var sonarQubeService = new Mock<ISonarQubeService>();

            // Note: calling the public constructor here to check that it uses
            // the expected set of languages
            var testSubject = new QualityProfileDownloader(sonarQubeService.Object,
                Mock.Of<IBindingConfigProvider>(),
                Mock.Of<IConfigurationPersister>(),
                Mock.Of<ISolutionBindingOperation>(),
                new TestLogger(logToConsole: true));
            
            await testSubject.UpdateAsync(boundProject, null, CancellationToken.None);

            foreach (var langauge in Language.KnownLanguages)
            {
                CheckLanguageQpRequested(sonarQubeService, langauge);
            }
            sonarQubeService.Invocations.Should().HaveCount(Language.KnownLanguages.Count());
        }

        [TestMethod]
        public async Task UpdateAsync_MultipleQPs_ProgressEventsAreRaised()
        {
            // Arrange
            var logger = new TestLogger(logToConsole: true);
            var boundProject = CreateBoundProject();

            var languagesToBind = new[] {
                Language.Cpp,
                Language.CSharp,
                Language.Secrets,
                Language.VBNET
            };

            var testSubject = CreateTestSubject(
                languagesToBind: languagesToBind,
                logger: logger);

            var notifications = new ConfigurableProgressStepExecutionEvents();
            var progressAdapter = new Mock<IProgress<FixedStepsProgress>>();
            progressAdapter.Setup(x => x.Report(It.IsAny<FixedStepsProgress>()))
                .Callback<FixedStepsProgress>(x => ((IProgressStepExecutionEvents)notifications).ProgressChanged(x.Message, x.CurrentStep));


            // Act
            var result = await testSubject.UpdateAsync(boundProject, progressAdapter.Object, CancellationToken.None);

            // Assert
            result.Should().BeTrue();

            // Progess notifications - percentage complete and messages
            notifications.AssertProgress(1.0, 2.0, 3.0, 4.0);
            CheckProgressMessages(languagesToBind);

            void CheckProgressMessages(params Language[] languages)
            {
                var expected = languages.Select(GetDownloadProgressMessages).ToArray();
                notifications.AssertProgressMessages(expected);
            }

            static string GetDownloadProgressMessages(Language language)
                => string.Format(BindingStrings.DownloadingQualityProfileProgressMessage, language.Name);
        }

        [TestMethod]
        public async Task UpdateAsync_WhenQualityProfileIsNotAvailable_OtherLanguagesDownloadedSuccessfully()
        {
            // Arrange
            var logger = new TestLogger(logToConsole: true);
            var languagesToBind = new[] {
                Language.Cpp,       // unavailable
                Language.CSharp,
                Language.Secrets,   // unavailable
                Language.VBNET
            };

            // Configure available languages on the server
            var sonarQubeService = new Mock<ISonarQubeService>();
            SetupAvailableLanguages(sonarQubeService,
                Language.CSharp,
                Language.VBNET,
                Language.Css /* available on server, but shouldn't be requested*/ );

            var configProvider = new Mock<IBindingConfigProvider>();
            var cppConfig = SetupConfigProvider(configProvider, Language.Cpp);
            var csharpConfig = SetupConfigProvider(configProvider, Language.CSharp);
            var secretsConfig = SetupConfigProvider(configProvider, Language.Secrets);
            var vbnetConfig = SetupConfigProvider(configProvider, Language.VBNET);

            var configPersister = new DummyConfigPersister();
            var solutionBindingOperation = new Mock<ISolutionBindingOperation>();

            var testSubject = CreateTestSubject(
                bindingConfigProvider: configProvider.Object,
                configurationPersister: configPersister,
                languagesToBind: languagesToBind,
                sonarQubeService: sonarQubeService.Object,
                solutionBindingOperation: solutionBindingOperation.Object,
                logger: logger);

            var notifications = new ConfigurableProgressStepExecutionEvents();
            var progressAdapter = new Mock<IProgress<FixedStepsProgress>>();
            progressAdapter.Setup(x => x.Report(It.IsAny<FixedStepsProgress>()))
                .Callback<FixedStepsProgress>(x => ((IProgressStepExecutionEvents)notifications).ProgressChanged(x.Message, x.CurrentStep));

            var boundProject = CreateBoundProject();

            // Act
            var result = await testSubject.UpdateAsync(boundProject, progressAdapter.Object, CancellationToken.None);

            // Assert
            result.Should().BeTrue();

            solutionBindingOperation.Invocations.Count().Should().Be(1);
            var savedConfigs = solutionBindingOperation.Invocations[0].Arguments[0] as IEnumerable<IBindingConfig>;

            savedConfigs.Should().BeEquivalentTo(csharpConfig, vbnetConfig);

            // Check output messages
            var missingPluginMessageCFamily = string.Format(Strings.SubTextPaddingFormat,
                string.Format(BindingStrings.CannotDownloadQualityProfileForLanguage, Language.Cpp.Name));
            var missingPluginMessageSecrets = string.Format(Strings.SubTextPaddingFormat,
                string.Format(BindingStrings.CannotDownloadQualityProfileForLanguage, Language.Secrets.Name));

            logger.AssertOutputStringExists(missingPluginMessageCFamily);
            logger.AssertOutputStringExists(missingPluginMessageSecrets);
            logger.AssertPartialOutputStringDoesNotExist(Language.Css.Name);
        }

        [TestMethod]
        public async Task UpdateAsync_WhenBindingConfigIsNull_Fails()
        {
            // Arrange
            var logger = new TestLogger(logToConsole: true);

            var language = Language.VBNET;
            var sonarQubeService = new Mock<ISonarQubeService>();
            SonarQubeQualityProfile profile = ConfigureQualityProfile(sonarQubeService, language);

            var solutionBindingOperation = new Mock<ISolutionBindingOperation>();
            var boundProject = CreateBoundProject("key", "any project name", new Uri("http://connected"));
            var testSubject = CreateTestSubject(
                languagesToBind: new[] { language },
                sonarQubeService: sonarQubeService.Object,
                solutionBindingOperation: solutionBindingOperation.Object,
                logger: logger);

            // Act
            var result = await testSubject.UpdateAsync(boundProject, null, CancellationToken.None);

            // Assert
            result.Should().BeFalse();
            solutionBindingOperation.Invocations.Should().BeEmpty();

            logger.AssertOutputStrings(1);
            var expectedOutput = string.Format(Strings.SubTextPaddingFormat,
                string.Format(BindingStrings.FailedToCreateBindingConfigForLanguage, language.Name));
            logger.AssertOutputStrings(expectedOutput);
        }

        [TestMethod]
        public async Task UpdateAsync_SavesConfiguration()
        {
            var configPersister = new DummyConfigPersister();

            // Arrange
            var bindingConfig = new Mock<IBindingConfig>().Object;

            var language = Language.VBNET;
            var serverQpTimestamp = DateTime.UtcNow.AddHours(-2);
            var sonarQubeService = new Mock<ISonarQubeService>();
            SonarQubeQualityProfile profile = ConfigureQualityProfile(sonarQubeService, language,
                "my profile key", serverQpTimestamp);

            var configProviderMock = new Mock<IBindingConfigProvider>();
            configProviderMock.Setup(x => x.GetConfigurationAsync(profile, language, It.IsAny<BindingConfiguration>(), CancellationToken.None))
                .ReturnsAsync(bindingConfig);

            var boundProject = CreateBoundProject("key", "any", new Uri("http://myserver"));

            var testSubject = CreateTestSubject(
                configProviderMock.Object,
                sonarQubeService: sonarQubeService.Object,
                configurationPersister: configPersister,
                languagesToBind: new[] { language });

            // Act
            await testSubject.UpdateAsync(boundProject, null, CancellationToken.None);

            // Assert
            configPersister.SavedProject.Should().NotBeNull();

            var savedProject = configPersister.SavedProject;
            savedProject.ServerUri.Should().Be(boundProject.ServerUri);
            savedProject.Profiles.Should().HaveCount(1);
            savedProject.Profiles[Language.VBNET].ProfileKey.Should().Be("my profile key");
            savedProject.Profiles[Language.VBNET].ProfileTimestamp.Should().Be(serverQpTimestamp);
        }

        #region Helpers

        private static QualityProfileDownloader CreateTestSubject(IBindingConfigProvider bindingConfigProvider = null,
            ISonarQubeService sonarQubeService = null,
            DummyConfigPersister configurationPersister = null,
            ISolutionBindingOperation solutionBindingOperation = null,
            ILogger logger = null,
            Language[] languagesToBind = null)
        {
            return new QualityProfileDownloader(
                sonarQubeService ?? Mock.Of<ISonarQubeService>(),
                bindingConfigProvider ?? Mock.Of<IBindingConfigProvider>(),
                configurationPersister ?? new DummyConfigPersister(),
                solutionBindingOperation ?? Mock.Of<ISolutionBindingOperation>(),
                logger ?? new TestLogger(logToConsole: true),
                languagesToBind ?? Language.KnownLanguages);
        }

        private static void SetupAvailableLanguages(
            Mock<ISonarQubeService> sonarQubeService,
            params Language[] languages)
        {
            foreach (var language in languages)
            {
                ConfigureQualityProfile(sonarQubeService, language, "Profile" + language.Name);
            }
        }

        private static IBindingConfig SetupConfigProvider(Mock<IBindingConfigProvider> bindingConfigProvider, Language language)
        {
            var bindingConfig = Mock.Of<IBindingConfig>();

            bindingConfigProvider.Setup(x => x.GetConfigurationAsync(It.IsAny<SonarQubeQualityProfile>(), language, It.IsAny<BindingConfiguration>(), CancellationToken.None))
                .ReturnsAsync(bindingConfig);
            return bindingConfig;
        }

        private static BoundSonarQubeProject CreateBoundProject(string projectKey = "key", string projectName = "name", Uri uri = null)
            => new BoundSonarQubeProject(
                uri ?? new Uri("http://any"),
                projectKey,
                projectName,
                null,
                null);

        private static SonarQubeQualityProfile ConfigureQualityProfile(Mock<ISonarQubeService> sonarQubeService, Language language,
            string profileKey = null,
            DateTime? serverQPTimestamp = null)
        {
            var profile = new SonarQubeQualityProfile(profileKey, "any profile name", "", false, serverQPTimestamp ?? DateTime.Now);
            sonarQubeService
                .Setup(x => x.GetQualityProfileAsync(It.IsAny<string>(), It.IsAny<string>(), language.ServerLanguage, It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            return profile;
        }

        private static void CheckLanguageQpRequested(Mock<ISonarQubeService> sonarQubeService, Language language)
            => sonarQubeService.Verify(x => x.GetQualityProfileAsync(
                It.IsAny<string>(), It.IsAny<string>(), language.ServerLanguage, It.IsAny<CancellationToken>()),
                Times.Once);

        private class DummyConfigPersister : IConfigurationPersister
        {
            public BoundSonarQubeProject SavedProject { get; private set; }

            BindingConfiguration IConfigurationPersister.Persist(BoundSonarQubeProject project)
            {
                SavedProject = project;
                return new BindingConfiguration(new BoundSonarQubeProject(), SonarLintMode.Connected, "c:\\any");
            }
        }

        #endregion Helpers
    }
}
