﻿/*
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

using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.QualityProfiles;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Progress.Controller;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.QualityProfiles;

[TestClass]
public class RoslynQualityProfileDownloaderTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RoslynQualityProfileDownloader, IQualityProfileDownloader>(
            MefTestHelpers.CreateExport<IBindingConfigProvider>(),
            MefTestHelpers.CreateExport<IConfigurationPersister>(),
            MefTestHelpers.CreateExport<IOutOfDateQualityProfileFinder>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<ILanguageProvider>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<RoslynQualityProfileDownloader>();

    [TestMethod]
    public async Task UpdateAsync_NothingToUpdate_ReturnsFalse()
    {
        var boundSonarQubeProject = CreateBoundProject();
        SetupLanguagesToUpdate(out var outOfDateQualityProfileFinderMock,
            boundSonarQubeProject,
            Array.Empty<Language>());
        var logger = new TestLogger();

        var bindingConfigProvider = new Mock<IBindingConfigProvider>();

        var testSubject = CreateTestSubject(outOfDateQualityProfileFinderMock.Object,
            bindingConfigProvider.Object,
            logger: logger);

        var result = await testSubject.UpdateAsync(boundSonarQubeProject, null, CancellationToken.None);

        result.Should().BeFalse();
        bindingConfigProvider.Invocations.Should().BeEmpty();

        logger.AssertPartialOutputStringExists(string.Format(QualityProfilesStrings.SubTextPaddingFormat, QualityProfilesStrings.DownloadingQualityProfilesNotNeeded));
    }

    [TestMethod]
    public async Task UpdateAsync_MultipleQPs_ProgressEventsAreRaised()
    {
        // Arrange
        var logger = new TestLogger(logToConsole: true);
        var boundProject = CreateBoundProject();

        var languagesToBind = new[] { Language.CSharp, Language.VBNET };

        SetupLanguagesToUpdate(out var outOfDateQualityProfileFinderMock,
            boundProject,
            languagesToBind);

        var bindingConfigProviderMock = new Mock<IBindingConfigProvider>();
        bindingConfigProviderMock.Setup(x =>
                x.GetConfigurationAsync(It.IsAny<SonarQubeQualityProfile>(),
                    It.IsAny<Language>(),
                    It.IsAny<BindingConfiguration>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IBindingConfig>());

        var testSubject = CreateTestSubject(
            languageProvider: CreateLanguageProvider(languagesToBind).Object,
            outOfDateQualityProfileFinder: outOfDateQualityProfileFinderMock.Object,
            bindingConfigProvider: bindingConfigProviderMock.Object,
            logger: logger);

        var notifications = new ConfigurableProgressStepExecutionEvents();
        var progressAdapter = new Mock<IProgress<FixedStepsProgress>>();
        progressAdapter.Setup(x => x.Report(It.IsAny<FixedStepsProgress>()))
            .Callback<FixedStepsProgress>(x =>
                ((IProgressStepExecutionEvents)notifications).ProgressChanged(x.Message, x.CurrentStep));

        // Act
        var result = await testSubject.UpdateAsync(boundProject, progressAdapter.Object, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        // Progress notifications - percentage complete and messages
        notifications.AssertProgress(1.0, 2.0);
        CheckProgressMessages(languagesToBind);

        void CheckProgressMessages(params Language[] languages)
        {
            var expected = languages.Select(GetDownloadProgressMessages).ToArray();
            notifications.AssertProgressMessages(expected);
        }

        static string GetDownloadProgressMessages(Language language) => string.Format(QualityProfilesStrings.DownloadingQualityProfileProgressMessage, language.Name);
    }

    [TestMethod]
    public async Task UpdateAsync_OnNewBoundProject_InitializesOnlyRoslynLanguages()
    {
        // Arrange
        var boundProject = CreateBoundProject();
        var logger = new TestLogger();
        var progressAdapter = new Mock<IProgress<FixedStepsProgress>>();
        var mockLanguageProvider = CreateLanguageProvider();

        var outOfDateQualityProfileFinder = new Mock<IOutOfDateQualityProfileFinder>();
        outOfDateQualityProfileFinder
            .Setup(x => x.GetAsync(boundProject, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        boundProject.Profiles.Should().BeNull();

        var testSubject = CreateTestSubject(
            bindingConfigProvider: new Mock<IBindingConfigProvider>().Object,
            configurationPersister: new DummyConfigPersister(),
            outOfDateQualityProfileFinder: outOfDateQualityProfileFinder.Object,
            languageProvider: mockLanguageProvider.Object,
            logger: logger);

        // Act
        await testSubject.UpdateAsync(boundProject, progressAdapter.Object, CancellationToken.None);

        // Assert
        boundProject.Profiles.Count.Should().Be(2);
        boundProject.Profiles.ContainsKey(Language.VBNET).Should().BeTrue();
        boundProject.Profiles.ContainsKey(Language.CSharp).Should().BeTrue();
        mockLanguageProvider.Verify(x => x.RoslynLanguages, Times.Once);
        mockLanguageProvider.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task UpdateAsync_UpdatesOnlyRoslynLanguages()
    {
        // Arrange
        var boundProject = CreateBoundProject();
        var logger = new TestLogger(logToConsole: true);

        var mockLanguageProvider = CreateLanguageProvider();

        // Configure available languages on the server
        SetupLanguagesToUpdate(out var outOfDateQualityProfileFinderMock,
            boundProject,
            Language.CSharp, Language.VBNET, Language.Cpp);

        var configProvider = new Mock<IBindingConfigProvider>();
        var csharpConfig = SetupConfigProvider(configProvider, Language.CSharp);
        var vbnetConfig = SetupConfigProvider(configProvider, Language.VBNET);
        var cppConfig = SetupConfigProvider(configProvider, Language.Cpp);

        var configPersister = new DummyConfigPersister();

        var testSubject = CreateTestSubject(
            bindingConfigProvider: configProvider.Object,
            configurationPersister: configPersister,
            outOfDateQualityProfileFinder: outOfDateQualityProfileFinderMock.Object,
            languageProvider: mockLanguageProvider.Object,
            logger: logger);

        var notifications = new ConfigurableProgressStepExecutionEvents();
        var progressAdapter = new Mock<IProgress<FixedStepsProgress>>();
        progressAdapter.Setup(x => x.Report(It.IsAny<FixedStepsProgress>()))
            .Callback<FixedStepsProgress>(x =>
                ((IProgressStepExecutionEvents)notifications).ProgressChanged(x.Message, x.CurrentStep));

        // Act
        var result = await testSubject.UpdateAsync(boundProject, progressAdapter.Object, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        CheckRuleConfigSaved(csharpConfig);
        CheckRuleConfigSaved(vbnetConfig);
        CheckRuleConfigNotSaved(cppConfig);

        boundProject.Profiles.Count.Should().Be(2);
        boundProject.Profiles[Language.VBNET].ProfileKey.Should().NotBeNull();
        boundProject.Profiles[Language.CSharp].ProfileKey.Should().NotBeNull();
        mockLanguageProvider.Verify(x => x.RoslynLanguages, Times.Once);
        mockLanguageProvider.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task UpdateAsync_WhenQualityProfileIsNotAvailable_OtherLanguagesDownloadedSuccessfully()
    {
        var boundProject = CreateBoundProject();
        // Arrange
        var logger = new TestLogger(logToConsole: true);
        var languagesToBind = new[]
        {
            Language.Cpp, // unavailable
            Language.CSharp,
            Language.Secrets, // unavailable
            Language.VBNET
        };

        // Configure available languages on the server
        SetupLanguagesToUpdate(out var outOfDateQualityProfileFinderMock,
            boundProject,
            Language.CSharp,
            Language.VBNET);

        var configProvider = new Mock<IBindingConfigProvider>();
        var cppConfig = SetupConfigProvider(configProvider, Language.Cpp);
        var csharpConfig = SetupConfigProvider(configProvider, Language.CSharp);
        var secretsConfig = SetupConfigProvider(configProvider, Language.Secrets);
        var vbnetConfig = SetupConfigProvider(configProvider, Language.VBNET);

        var configPersister = new DummyConfigPersister();

        var testSubject = CreateTestSubject(
            bindingConfigProvider: configProvider.Object,
            configurationPersister: configPersister,
            languageProvider: CreateLanguageProvider(languagesToBind).Object,
            outOfDateQualityProfileFinder: outOfDateQualityProfileFinderMock.Object,
            logger: logger);

        var notifications = new ConfigurableProgressStepExecutionEvents();
        var progressAdapter = new Mock<IProgress<FixedStepsProgress>>();
        progressAdapter.Setup(x => x.Report(It.IsAny<FixedStepsProgress>()))
            .Callback<FixedStepsProgress>(x =>
                ((IProgressStepExecutionEvents)notifications).ProgressChanged(x.Message, x.CurrentStep));

        // Act
        var result = await testSubject.UpdateAsync(boundProject, progressAdapter.Object, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        CheckRuleConfigSaved(csharpConfig);
        CheckRuleConfigSaved(vbnetConfig);
        CheckRuleConfigNotSaved(cppConfig);
        CheckRuleConfigNotSaved(secretsConfig);

        boundProject.Profiles.Count.Should().Be(4);
        boundProject.Profiles[Language.VBNET].ProfileKey.Should().NotBeNull();
        boundProject.Profiles[Language.CSharp].ProfileKey.Should().NotBeNull();
        boundProject.Profiles[Language.Cpp].ProfileKey.Should().BeNull();
        boundProject.Profiles[Language.Secrets].ProfileKey.Should().BeNull();
    }

    [TestMethod]
    public async Task UpdateAsync_WhenBindingConfigIsNull_Throws()
    {
        // Arrange
        var boundProject = CreateBoundProject();
        var logger = new TestLogger(logToConsole: true);

        var language = Language.VBNET;
        SetupLanguagesToUpdate(out var outOfDateQualityProfileFinderMock,
            boundProject,
            (language, CreateQualityProfile()));

        var bindingConfigProvider = new Mock<IBindingConfigProvider>();
        var testSubject = CreateTestSubject(
            bindingConfigProvider: bindingConfigProvider.Object,
            languageProvider: CreateLanguageProvider([language]).Object,
            outOfDateQualityProfileFinder: outOfDateQualityProfileFinderMock.Object,
            logger: logger);

        // Act
        var act = () => testSubject.UpdateAsync(boundProject, null, CancellationToken.None);

        // Assert
        var expectedMessage = string.Format(QualityProfilesStrings.FailedToCreateBindingConfigForLanguage, language.Name);
        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Be(expectedMessage);
        bindingConfigProvider.Invocations.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task UpdateAsync_SavesConfiguration()
    {
        // Arrange
        var boundProject = CreateBoundProject();
        var configPersister = new DummyConfigPersister();

        const string myProfileKey = "my profile key";
        var language = Language.VBNET;
        var serverQpTimestamp = DateTime.UtcNow.AddHours(-2);
        var qp = CreateQualityProfile(myProfileKey, serverQpTimestamp);

        SetupLanguagesToUpdate(out var outOfDateQualityProfileFinderMock,
            boundProject,
            (language, qp));

        var bindingConfig = new Mock<IBindingConfig>().Object;
        var configProviderMock = new Mock<IBindingConfigProvider>();
        configProviderMock.Setup(x => x.GetConfigurationAsync(qp,
                language,
                It.IsAny<BindingConfiguration>(), CancellationToken.None))
            .ReturnsAsync(bindingConfig);

        var testSubject = CreateTestSubject(
            outOfDateQualityProfileFinderMock.Object,
            configProviderMock.Object,
            configurationPersister: configPersister,
            languageProvider: CreateLanguageProvider([language]).Object);

        // Act
        await testSubject.UpdateAsync(boundProject, null, CancellationToken.None);

        // Assert
        configPersister.SavedProject.Should().NotBeNull();

        var savedProject = configPersister.SavedProject;
        savedProject.ServerConnection.Id.Should().Be(boundProject.ServerConnection.Id);
        savedProject.Profiles.Should().HaveCount(1);
        savedProject.Profiles[Language.VBNET].ProfileKey.Should().Be(myProfileKey);
        savedProject.Profiles[Language.VBNET].ProfileTimestamp.Should().Be(serverQpTimestamp);
    }

    #region Helpers

    private static RoslynQualityProfileDownloader CreateTestSubject(
        IOutOfDateQualityProfileFinder outOfDateQualityProfileFinder = null,
        IBindingConfigProvider bindingConfigProvider = null,
        DummyConfigPersister configurationPersister = null,
        ILogger logger = null,
        ILanguageProvider languageProvider = null) =>
        new(
            bindingConfigProvider ?? Mock.Of<IBindingConfigProvider>(),
            configurationPersister ?? new DummyConfigPersister(),
            outOfDateQualityProfileFinder ?? Mock.Of<IOutOfDateQualityProfileFinder>(),
            logger ?? new TestLogger(logToConsole: true),
            languageProvider ?? CreateLanguageProvider().Object);

    private static SonarQubeQualityProfile CreateQualityProfile(string key = "key", DateTime timestamp = default) => new(key, default, default, default, timestamp);

    private static void SetupLanguagesToUpdate(
        out Mock<IOutOfDateQualityProfileFinder> outOfDateQualityProfileFinderMock,
        BoundServerProject boundProject,
        params Language[] languages) =>
        SetupLanguagesToUpdate(out outOfDateQualityProfileFinderMock,
            boundProject,
            languages.Select(x => (x, CreateQualityProfile())).ToArray());

    private static void SetupLanguagesToUpdate(
        out Mock<IOutOfDateQualityProfileFinder> outOfDateQualityProfileFinderMock,
        BoundServerProject boundProject,
        params (Language language, SonarQubeQualityProfile qualityProfile)[] qps)
    {
        outOfDateQualityProfileFinderMock = new Mock<IOutOfDateQualityProfileFinder>();
        outOfDateQualityProfileFinderMock
            .Setup(x =>
                x.GetAsync(boundProject, It.IsAny<CancellationToken>()))
            .ReturnsAsync(qps);
    }

    private static Mock<IBindingConfig> SetupConfigProvider(
        Mock<IBindingConfigProvider> bindingConfigProvider,
        Language language)
    {
        var bindingConfig = new Mock<IBindingConfig>();

        bindingConfigProvider.Setup(x => x.GetConfigurationAsync(
                It.IsAny<SonarQubeQualityProfile>(),
                language,
                It.IsAny<BindingConfiguration>(),
                CancellationToken.None))
            .ReturnsAsync(bindingConfig.Object);
        return bindingConfig;
    }

    private static BoundServerProject CreateBoundProject(
        string projectKey = "key",
        Uri uri = null) =>
        new BoundServerProject(
            "solution",
            projectKey,
            new ServerConnection.SonarQube(uri ?? new Uri("http://localhost/")));

    private static void CheckRuleConfigSaved(Mock<IBindingConfig> bindingConfig) => bindingConfig.Verify(x => x.Save(), Times.Once);

    private static void CheckRuleConfigNotSaved(Mock<IBindingConfig> bindingConfig) => bindingConfig.Verify(x => x.Save(), Times.Never);

    private class DummyConfigPersister : IConfigurationPersister
    {
        public BoundServerProject SavedProject { get; private set; }

        BindingConfiguration IConfigurationPersister.Persist(BoundServerProject project)
        {
            SavedProject = project;
            return new BindingConfiguration(project, SonarLintMode.Connected, "c:\\any");
        }
    }

    private static Mock<ILanguageProvider> CreateLanguageProvider(Language[] languagesToBind = null)
    {
        var mockLanguageProvider = new Mock<ILanguageProvider>();
        mockLanguageProvider.Setup(x => x.RoslynLanguages)
            .Returns(languagesToBind ?? LanguageProvider.Instance.RoslynLanguages.ToArray());
        return mockLanguageProvider;
    }

    #endregion Helpers
}
