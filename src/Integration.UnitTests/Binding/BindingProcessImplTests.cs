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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Models;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class BindingProcessImplTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private Mock<ISonarQubeService> sonarQubeServiceMock;
        private ConfigurableHost host;
        private TestLogger logger;

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();

            logger = new TestLogger();
            serviceProvider.RegisterService(typeof(ILogger), logger);

            this.sonarQubeServiceMock = new Mock<ISonarQubeService>();

            this.host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);
            host.Logger = logger;
        }

        #region Tests

        [TestMethod]
        public void Ctor_ArgChecks()
        {
            var validHost = new ConfigurableHost();
            var bindingArgs = CreateBindCommandArgs(connection: new ConnectionInformation(new Uri("http://server")));
            var slnBindOp = new Mock<ISolutionBindingOperation>().Object;
            var bindingConfigProvider = new Mock<IBindingConfigProvider>().Object;
            var exclusionSettingsStorage = Mock.Of<IExclusionSettingsStorage>();

            // 1. Null host
            Action act = () => new BindingProcessImpl(null, bindingArgs, slnBindOp, bindingConfigProvider, exclusionSettingsStorage);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("host");

            // 2. Null binding args
            act = () => new BindingProcessImpl(validHost, null, slnBindOp, bindingConfigProvider, exclusionSettingsStorage);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("bindingArgs");

            // 3. Null solution binding operation
            act = () => new BindingProcessImpl(validHost, bindingArgs, null, bindingConfigProvider, exclusionSettingsStorage);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("solutionBindingOperation");

            // 4. Null rules configuration provider
            act = () => new BindingProcessImpl(validHost, bindingArgs, slnBindOp, null, exclusionSettingsStorage);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("bindingConfigProvider");

            // 5. Null exclusion settings storage
            act = () => new BindingProcessImpl(validHost, bindingArgs, slnBindOp, bindingConfigProvider, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("exclusionSettingsStorage");
        }

        [TestMethod]
        public async Task SaveServerExclusionsAsync_ReturnsTrue()
        {
            var bindingArgs = CreateBindCommandArgs(projectKey: "projectKey");

            ServerExclusions settings = CreateSettings();

            var sonarQubeService = new Mock<ISonarQubeService>();
            sonarQubeService.Setup(s => s.GetServerExclusions("projectKey", It.IsAny<CancellationToken>())).Returns(Task.FromResult(settings));

            var exclusionSettingsStorage = new Mock<IExclusionSettingsStorage>();

            var testSubject = CreateTestSubject(bindingArgs: bindingArgs, sonarQubeService: sonarQubeService.Object, exclusionSettingsStorage: exclusionSettingsStorage.Object);

            await testSubject.SaveServerExclusionsAsync(CancellationToken.None);

            exclusionSettingsStorage.Verify(fs => fs.SaveSettings(settings), Times.Once);
            logger.AssertOutputStrings(0);
        }

        [TestMethod]
        public async Task SaveServerExclusionsAsync_HasError_ReturnsFalse()
        {
            var bindingArgs = CreateBindCommandArgs(projectKey: "projectKey");

            var sonarQubeService = new Mock<ISonarQubeService>();
            sonarQubeService.Setup(s => s.GetServerExclusions("projectKey", It.IsAny<CancellationToken>())).Throws(new Exception("Expected Error"));

            var testSubject = CreateTestSubject(bindingArgs: bindingArgs, sonarQubeService: sonarQubeService.Object);

            var result = await testSubject.SaveServerExclusionsAsync(CancellationToken.None);

            result.Should().BeFalse();
            logger.AssertOutputStrings(1);
            logger.AssertPartialOutputStrings("Expected Error");
        }

        [TestMethod]
        public void SaveServerExclusionsAsync_HasCriticalError_Throws()
        {
            var bindingArgs = CreateBindCommandArgs(projectKey: "projectKey");

            var sonarQubeService = new Mock<ISonarQubeService>();
            sonarQubeService.Setup(s => s.GetServerExclusions("projectKey", It.IsAny<CancellationToken>())).Throws(new StackOverflowException("Critical Error"));

            var testSubject = CreateTestSubject(bindingArgs: bindingArgs, sonarQubeService: sonarQubeService.Object);

            Func<Task<bool>> act = async () => await testSubject.SaveServerExclusionsAsync(CancellationToken.None);

            act.Should().ThrowExactly<StackOverflowException>().WithMessage("Critical Error");
            logger.AssertOutputStrings(0);
        }

        private static ServerExclusions CreateSettings()
        {
            return new ServerExclusions
            {
                Inclusions = new string[] { "inclusion1", "inclusion2" },
                Exclusions = new string[] { "exclusion" },
                GlobalExclusions = new string[] { "globalExclusion" }
            };
        }

        [TestMethod]
        public async Task DownloadQualityProfile_SavesConfiguration()
        {
            var configPersister = new ConfigurableConfigurationProvider();
            this.serviceProvider.RegisterService(typeof(IConfigurationPersister), configPersister);

            // Arrange
            const string QualityProfileName = "SQQualityProfileName";
            const string ProjectName = "SQProjectName";

            var notifications = new ConfigurableProgressStepExecutionEvents();
            var progressAdapter = new FixedStepsProgressAdapter(notifications);

            var bindingConfig = new Mock<IBindingConfig>().Object;

            var language = Language.VBNET;
            SonarQubeQualityProfile profile = this.ConfigureQualityProfile(language, QualityProfileName);

            var configProviderMock = new Mock<IBindingConfigProvider>();
            configProviderMock.Setup(x => x.GetConfigurationAsync(profile, language, It.IsAny<BindingConfiguration>(), CancellationToken.None))
                .ReturnsAsync(bindingConfig);

            var bindingArgs = CreateBindCommandArgs("key", ProjectName, new ConnectionInformation(new Uri("http://connected")));
            var testSubject = this.CreateTestSubject(bindingArgs, configProviderMock.Object, languagesToBind: new []{ language });

            // Act
            await testSubject.DownloadQualityProfileAsync(progressAdapter, CancellationToken.None);

            // Assert
            configPersister.SavedProject.Should().NotBeNull();

            var savedProject = configPersister.SavedProject;
            savedProject.ServerUri.Should().Be(bindingArgs.Connection.ServerUri);
            savedProject.Profiles.Should().HaveCount(1);
            savedProject.Profiles[Language.VBNET].ProfileKey.Should().Be(profile.Key);
            savedProject.Profiles[Language.VBNET].ProfileTimestamp.Should().Be(profile.TimeStamp);
        }

        [TestMethod]
        public async Task DownloadQualityProfile_WhenQualityProfileIsNotAvailable_OtherLanguagesDownloadedSucceessfully()
        {
            // Arrange
            var languagesToBind = new[] {
                Language.Cpp,       // unavailable
                Language.CSharp,
                Language.Secrets,   // unavailable
                Language.VBNET
            };

            // Configure available languages on the server
            var configProvider = new Mock<IBindingConfigProvider>();
            SetupAvailableLanguages(configProvider,
                Language.CSharp,
                Language.VBNET,
                Language.Css /* available on server, but shouldn't be requested*/ );

            var configPersister = new ConfigurableConfigurationProvider();
            serviceProvider.RegisterService(typeof(IConfigurationPersister), configPersister);

            var testSubject = CreateTestSubject(
                configProvider: configProvider.Object,
                languagesToBind: languagesToBind);

            var notifications = new ConfigurableProgressStepExecutionEvents();
            var progressAdapter = new FixedStepsProgressAdapter(notifications);

            // Act
            var result = await testSubject.DownloadQualityProfileAsync(progressAdapter, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            testSubject.InternalState.BindingConfigs.Should().NotContainKey(Language.Cpp);
            testSubject.InternalState.BindingConfigs.Should().NotContainKey(Language.Secrets);
            testSubject.InternalState.BindingConfigs.Should().NotContainKey(Language.Css);

            testSubject.InternalState.BindingConfigs.Should().ContainKey(Language.CSharp);
            testSubject.InternalState.BindingConfigs.Should().ContainKey(Language.VBNET);

            // Progess notifications - percentage complete and messages
            notifications.AssertProgress(0.25, 0.5, 0.75, 1.0);
            CheckProgressMessages(languagesToBind);

            // Check output messages
            var missingPluginMessageCFamily = string.Format(Strings.SubTextPaddingFormat,
                string.Format(Strings.CannotDownloadQualityProfileForLanguage, Language.Cpp.Name));
            var missingPluginMessageSecrets = string.Format(Strings.SubTextPaddingFormat,
                string.Format(Strings.CannotDownloadQualityProfileForLanguage, Language.Secrets.Name));

            logger.AssertOutputStringExists(missingPluginMessageCFamily);
            logger.AssertOutputStringExists(missingPluginMessageSecrets);
            logger.AssertPartialOutputStringDoesNotExist(Language.Css.Name);

            void CheckProgressMessages(params Language[] languages)
            {
                var expected = languages.Select(GetDownloadProgressMessages).ToArray();
                notifications.AssertProgressMessages(expected);
            }

            static string GetDownloadProgressMessages(Language language)
                => string.Format(Strings.DownloadingQualityProfileProgressMessage, language.Name);
        }

        private void SetupAvailableLanguages(Mock<IBindingConfigProvider> configProvider, params Language[] languages)
        {
            foreach (var language in languages)
            {
                var profile = ConfigureQualityProfile(language, "Profile" + language.Name);
                configProvider.Setup(x => x.GetConfigurationAsync(profile, language, BindingConfiguration.Standalone, CancellationToken.None))
                    .ReturnsAsync(Mock.Of<IBindingConfig>());
            }
        }

        [TestMethod]
        public async Task DownloadQualityProfile_WhenBindingConfigIsNull_Fails()
        {
            var configPersister = new ConfigurableConfigurationProvider();
            this.serviceProvider.RegisterService(typeof(IConfigurationPersister), configPersister);

            // Arrange
            const string QualityProfileName = "SQQualityProfileName";
            const string ProjectName = "SQProjectName";

            var notifications = new ConfigurableProgressStepExecutionEvents();
            var progressAdapter = new FixedStepsProgressAdapter(notifications);

            var language = Language.VBNET;
            SonarQubeQualityProfile profile = this.ConfigureQualityProfile(language, QualityProfileName);

            var configProviderMock = new Mock<IBindingConfigProvider>();
            var bindingArgs = CreateBindCommandArgs("key", ProjectName, new ConnectionInformation(new Uri("http://connected")));
            var testSubject = this.CreateTestSubject(bindingArgs, configProviderMock.Object, languagesToBind: new[] { language });

            // Act
            var result = await testSubject.DownloadQualityProfileAsync(progressAdapter, CancellationToken.None);

            // Assert
            result.Should().BeFalse();
            testSubject.InternalState.QualityProfiles[language].Should().Be(profile);

            logger.AssertOutputStrings(1);
            var expectedOutput = string.Format(Strings.SubTextPaddingFormat,
                string.Format(Strings.FailedToCreateBindingConfigForLanguage, language.Name));
            logger.AssertOutputStrings(expectedOutput);
        }

        [TestMethod]
        public void GetBindingLanguages_ReturnsExpectedLanguages()
        {
            // Arrange
            var expectedLanguages = new[] { Language.CSharp, Language.VBNET };

            var testSubject = this.CreateTestSubject(languagesToBind: expectedLanguages);

            // Act
            var actualLanguages = testSubject.GetBindingLanguages();

            // Assert
            CollectionAssert.AreEquivalent(expectedLanguages, actualLanguages.ToArray(), "Unexpected languages for binding projects");
        }

        [TestMethod]
        public void GetBindingLanguages_PublicCtor_ReturnsAllKnownLanguages()
        {
            // Arrange
            var testSubject = this.CreateTestSubject();

            // Act
            var actualLanguages = testSubject.GetBindingLanguages();

            // Assert
            CollectionAssert.AreEquivalent(Language.KnownLanguages.ToArray(), actualLanguages.ToArray(), "Unexpected languages for binding projects");
        }

        [TestMethod]
        public void EmitBindingCompleteMessage()
        {
            // Arrange
            var testSubject = this.CreateTestSubject();

            // Test case 1: Default state is 'true'
            testSubject.InternalState.BindingOperationSucceeded.Should().BeTrue($"Initial state of {nameof(BindingProcessImpl.InternalState.BindingOperationSucceeded)} should be true");
        }

        #endregion Tests

        #region Helpers

        private BindingProcessImpl CreateTestSubject(BindCommandArgs bindingArgs = null,
            IBindingConfigProvider configProvider = null,
            ISonarQubeService sonarQubeService = null,
            IExclusionSettingsStorage exclusionSettingsStorage = null,
            IEnumerable<Language> languagesToBind = null)
        {
            bindingArgs = bindingArgs ?? CreateBindCommandArgs();
            configProvider = configProvider ?? new Mock<IBindingConfigProvider>().Object;
            exclusionSettingsStorage = exclusionSettingsStorage ?? Mock.Of<IExclusionSettingsStorage>();
            languagesToBind = languagesToBind ?? Language.KnownLanguages;

            this.host.SonarQubeService = sonarQubeService ?? this.sonarQubeServiceMock.Object;

            var slnBindOperation = new SolutionBindingOperation();

            return new BindingProcessImpl(this.host, bindingArgs, slnBindOperation, configProvider, exclusionSettingsStorage, false, languagesToBind);
        }

        private BindCommandArgs CreateBindCommandArgs(string projectKey = "key", string projectName = "name", ConnectionInformation connection = null)
        {
            connection = connection ?? new ConnectionInformation(new Uri("http://connected"));
            return new BindCommandArgs(projectKey, projectName, connection);
        }

        private SonarQubeQualityProfile ConfigureQualityProfile(Language language, string profileName)
        {
            var profile = new SonarQubeQualityProfile("", profileName, "", false, DateTime.Now);
            this.sonarQubeServiceMock
                .Setup(x => x.GetQualityProfileAsync(It.IsAny<string>(), It.IsAny<string>(), language.ServerLanguage, It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            return profile;
        }

        #endregion Helpers
    }
}
