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
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.QualityProfiles
{
    [TestClass]
    public class QualityProfileDownloaderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<QualityProfileDownloader, IQualityProfileDownloader>(
                MefTestHelpers.CreateExport<IBindingConfigProvider>(),
                MefTestHelpers.CreateExport<IConfigurationPersister>(),
                MefTestHelpers.CreateExport<IOutOfDateQualityProfileFinder>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<QualityProfileDownloader>();
        }
        
        [TestMethod]
        public async Task UpdateAsync_MultipleQPs_ProgressEventsAreRaised()
        {
            // Arrange
            var logger = new TestLogger(logToConsole: true);
            var boundProject = CreateBoundProject();

            var languagesToBind = new[]
            {
                Language.Cpp,
                Language.CSharp,
                Language.Secrets,
                Language.VBNET
            };

            SetupAvailableLanguages(out var outOfDateQualityProfileFinderMock,
                boundProject,
                languagesToBind
                    .Select(x => (x, CreateQualityProfile()))
                    .ToArray());

            var bindingConfigProviderMock = new Mock<IBindingConfigProvider>();
            bindingConfigProviderMock.Setup(x =>
                    x.GetConfigurationAsync(It.IsAny<SonarQubeQualityProfile>(),
                        It.IsAny<Language>(),
                        It.IsAny<BindingConfiguration>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<IBindingConfig>());

            var testSubject = CreateTestSubject(
                languagesToBind: languagesToBind,
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
            SetupAvailableLanguages(out var outOfDateQualityProfileFinderMock,
                boundProject,
                new[] { Language.CSharp, Language.VBNET }
                    .Select(x => (x, CreateQualityProfile()))
                    .ToArray());

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
                outOfDateQualityProfileFinder: outOfDateQualityProfileFinderMock.Object,
                solutionBindingOperation: solutionBindingOperation.Object,
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

            solutionBindingOperation.Invocations.Count().Should().Be(1);
            var savedConfigs = solutionBindingOperation.Invocations[0].Arguments[0] as IEnumerable<IBindingConfig>;

            savedConfigs.Should().BeEquivalentTo(csharpConfig, vbnetConfig);

            boundProject.Profiles.Count().Should().Be(4);
            boundProject.Profiles[Language.VBNET].ProfileKey.Should().NotBeNull();
            boundProject.Profiles[Language.CSharp].ProfileKey.Should().NotBeNull();
            boundProject.Profiles[Language.Cpp].ProfileKey.Should().BeNull();
            boundProject.Profiles[Language.Secrets].ProfileKey.Should().BeNull();
        }

        [TestMethod]
        public async Task UpdateAsync_WhenBindingConfigIsNull_Fails()
        {
            // Arrange
            var boundProject = CreateBoundProject();
            var logger = new TestLogger(logToConsole: true);

            var language = Language.VBNET;
            SetupAvailableLanguages(out var outOfDateQualityProfileFinderMock,
                boundProject,
                (language, CreateQualityProfile()));

            var solutionBindingOperation = new Mock<ISolutionBindingOperation>();
            var testSubject = CreateTestSubject(
                languagesToBind: new[] { language },
                outOfDateQualityProfileFinder: outOfDateQualityProfileFinderMock.Object,
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
            // Arrange
            var boundProject = CreateBoundProject();
            var configPersister = new DummyConfigPersister();

            const string myProfileKey = "my profile key";
            var language = Language.VBNET;
            var serverQpTimestamp = DateTime.UtcNow.AddHours(-2);
            var qp = CreateQualityProfile(myProfileKey, serverQpTimestamp);

            SetupAvailableLanguages(out var outOfDateQualityProfileFinderMock,
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
                languagesToBind: new[] { language });

            // Act
            await testSubject.UpdateAsync(boundProject, null, CancellationToken.None);

            // Assert
            configPersister.SavedProject.Should().NotBeNull();

            var savedProject = configPersister.SavedProject;
            savedProject.ServerUri.Should().Be(boundProject.ServerUri);
            savedProject.Profiles.Should().HaveCount(1);
            savedProject.Profiles[Language.VBNET].ProfileKey.Should().Be(myProfileKey);
            savedProject.Profiles[Language.VBNET].ProfileTimestamp.Should().Be(serverQpTimestamp);
        }

        #region Helpers

        private static QualityProfileDownloader CreateTestSubject(
            IOutOfDateQualityProfileFinder outOfDateQualityProfileFinder = null,
            IBindingConfigProvider bindingConfigProvider = null,
            DummyConfigPersister configurationPersister = null,
            ISolutionBindingOperation solutionBindingOperation = null,
            ILogger logger = null,
            Language[] languagesToBind = null)
        {
            return new QualityProfileDownloader(
                bindingConfigProvider ?? Mock.Of<IBindingConfigProvider>(),
                configurationPersister ?? new DummyConfigPersister(),
                outOfDateQualityProfileFinder ?? Mock.Of<IOutOfDateQualityProfileFinder>(),
                logger ?? new TestLogger(logToConsole: true),
                solutionBindingOperation ?? Mock.Of<ISolutionBindingOperation>(),
                languagesToBind ?? Language.KnownLanguages);
        }

        private static SonarQubeQualityProfile CreateQualityProfile(string key = "key", DateTime timestamp = default)
        {
            return new SonarQubeQualityProfile(key, default, default, default, timestamp);
        }

        private static void SetupAvailableLanguages(
            out Mock<IOutOfDateQualityProfileFinder> outOfDateQualityProfileFinderMock,
            BoundSonarQubeProject boundProject,
            params (Language language, SonarQubeQualityProfile qualityProfile)[] qps)
        {
            outOfDateQualityProfileFinderMock = new Mock<IOutOfDateQualityProfileFinder>();
            outOfDateQualityProfileFinderMock
                .Setup(x =>
                    x.GetAsync(boundProject, It.IsAny<CancellationToken>()))
                .ReturnsAsync(qps);
        }

        private static IBindingConfig SetupConfigProvider(Mock<IBindingConfigProvider> bindingConfigProvider,
            Language language)
        {
            var bindingConfig = Mock.Of<IBindingConfig>();

            bindingConfigProvider.Setup(x => x.GetConfigurationAsync(
                    It.IsAny<SonarQubeQualityProfile>(),
                    language, 
                    It.IsAny<BindingConfiguration>(), 
                    CancellationToken.None))
                .ReturnsAsync(bindingConfig);
            return bindingConfig;
        }

        private static BoundSonarQubeProject CreateBoundProject(string projectKey = "key", string projectName = "name",
            Uri uri = null)
            => new BoundSonarQubeProject(
                uri ?? new Uri("http://any"),
                projectKey,
                projectName,
                null,
                null);

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
