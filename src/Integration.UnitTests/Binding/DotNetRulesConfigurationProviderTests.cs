/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client;
using SonarQube.Client.Messages;
using SonarQube.Client.Models;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class DotNetRulesConfigurationProviderTests
    {
        private Mock<ISonarQubeService> sonarQubeServiceMock;
        private TestLogger logger;

        [TestInitialize]
        public void TestInitialize()
        {
            this.logger = new TestLogger();
            this.sonarQubeServiceMock = new Mock<ISonarQubeService>();
        }

        [TestMethod]
        public async Task GetRulesConfig_Success()
        {
            // Arrange
            const string QualityProfileName = "SQQualityProfileName";
            const string ProjectName = "SQProjectName";

            // Record all of the calls to NuGetBindingOperation.ProcessExport
            var actualProfiles = new List<Tuple<Language, RoslynExportProfileResponse>>();
            Mock<INuGetBindingOperation> nuGetOpMock = new Mock<INuGetBindingOperation>();
            nuGetOpMock.Setup(x => x.ProcessExport(It.IsAny<Language>(), It.IsAny<RoslynExportProfileResponse>()))
                .Callback<Language, RoslynExportProfileResponse>((l, r) => actualProfiles.Add(new Tuple<Language, RoslynExportProfileResponse>(l, r)))
                .Returns(true);

            var testSubject = this.CreateTestSubject(ProjectName, "http://connected/", nuGetOpMock.Object);

            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSetWithRuleIds(new[] { "Key1", "Key2" });
            var expectedRuleSet = new RuleSet(ruleSet)
            {
                NonLocalizedDisplayName = string.Format(Strings.SonarQubeRuleSetNameFormat, ProjectName, QualityProfileName),
                NonLocalizedDescription = "\r\nhttp://connected/profiles/show?key="
            };
            var nugetPackages = new[] { new PackageName("myPackageId", new SemanticVersion("1.0.0")) };
            var additionalFiles = new[] { new AdditionalFileResponse { FileName = "abc.xml", Content = new byte[] { 1, 2, 3 } } };
            RoslynExportProfileResponse export = RoslynExportProfileHelper.CreateExport(ruleSet, nugetPackages, additionalFiles);

            var language = Language.VBNET;
            SonarQubeQualityProfile profile = this.ConfigureProfileExport(export, language, QualityProfileName);

            // Act
            var result = await testSubject.GetRulesConfigurationAsync(profile, "TODO", language, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            result.Should().NotBeNull();
            var dotNetResult = result as DotNetRulesConfiguration;
            dotNetResult.Should().NotBeNull();

            RuleSetAssert.AreEqual(expectedRuleSet, dotNetResult.RuleSet, "Unexpected rule set");
            VerifyNuGetPackgesDownloaded(nugetPackages, language, actualProfiles);

            this.logger.AssertOutputStrings(0); // not expecting anything in the case of success
        }

        [TestMethod]
        public async Task GetRulesConfig_WhenProfileExportIsNotAvailable_Fails()
        {
            // Arrange
            var testSubject = this.CreateTestSubject();

            var language = Language.CSharp;
            var profile = this.ConfigureProfileExport(null, language, "");

            // Act
            var result = await testSubject.GetRulesConfigurationAsync(profile, null, language, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            result.Should().BeNull();

            this.logger.AssertOutputStrings(1);
            var expectedOutput = string.Format(Strings.SubTextPaddingFormat,
                string.Format(Strings.QualityProfileDownloadFailedMessageFormat, string.Empty, string.Empty, language.Name));
            this.logger.AssertOutputStrings(expectedOutput);
        }

        [TestMethod]
        public async Task GetRulesConfig_WithNoRules_Fails()
        {
            // Arrange
            const string QualityProfileName = "SQQualityProfileName";
            const string ProjectName = "SQProjectName";
            var testSubject = this.CreateTestSubject("key", ProjectName);

            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSetWithRuleIds(Enumerable.Empty<string>());
            var nugetPackages = new[] { new PackageName("myPackageId", new SemanticVersion("1.0.0")) };
            var additionalFiles = new[] { new AdditionalFileResponse { FileName = "abc.xml", Content = new byte[] { 1, 2, 3 } } };
            RoslynExportProfileResponse export = RoslynExportProfileHelper.CreateExport(ruleSet, nugetPackages, additionalFiles);

            var language = Language.VBNET;
            var profile = this.ConfigureProfileExport(export, language, QualityProfileName);

            // Act
            var result = await testSubject.GetRulesConfigurationAsync(profile, null, language, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            result.Should().BeNull();

            this.logger.AssertOutputStrings(1);
            var expectedOutput = string.Format(Strings.SubTextPaddingFormat,
                string.Format(Strings.NoSonarAnalyzerActiveRulesForQualityProfile, QualityProfileName, language.Name));
            this.logger.AssertOutputStrings(expectedOutput);
        }

        [TestMethod]
        public async Task GetRulesConfig_WithNoActiveRules_Fails()
        {
            // Arrange
            const string QualityProfileName = "SQQualityProfileName";
            const string ProjectName = "SQProjectName";
            var testSubject = this.CreateTestSubject(ProjectName, "http://connected");

            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSetWithRuleIds(new[] { "Key1", "Key2" });
            foreach (var rule in ruleSet.Rules)
            {
                rule.Action = RuleAction.None;
            }
            var nugetPackages = new[] { new PackageName("myPackageId", new SemanticVersion("1.0.0")) };
            var additionalFiles = new[] { new AdditionalFileResponse { FileName = "abc.xml", Content = new byte[] { 1, 2, 3 } } };
            RoslynExportProfileResponse export = RoslynExportProfileHelper.CreateExport(ruleSet, nugetPackages, additionalFiles);

            var language = Language.VBNET;
            var profile = this.ConfigureProfileExport(export, language, QualityProfileName);

            // Act
            var result = await testSubject.GetRulesConfigurationAsync(profile, null, language, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            result.Should().BeNull();

            this.logger.AssertOutputStrings(1);
            var expectedOutput = string.Format(Strings.SubTextPaddingFormat,
                string.Format(Strings.NoSonarAnalyzerActiveRulesForQualityProfile, QualityProfileName, language.Name));
            this.logger.AssertOutputStrings(expectedOutput);
        }

        [TestMethod]
        public async Task GetRulesConfig_LegacyMode_WithNoNugetPackage_Fails()
        {
            // Arrange
            const string QualityProfileName = "SQQualityProfileName";
            const string ProjectName = "SQProjectName";
            var legacyNuGetBinding = new NuGetBindingOperation(new ConfigurableHost(), this.logger);
            var testSubject = this.CreateTestSubject("key", ProjectName, legacyNuGetBinding);

            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSetWithRuleIds(new[] { "Key1", "Key2" });
            var additionalFiles = new[] { new AdditionalFileResponse { FileName = "abc.xml", Content = new byte[] { 1, 2, 3 } } };
            RoslynExportProfileResponse export = RoslynExportProfileHelper.CreateExport(ruleSet, Enumerable.Empty<PackageName>(), additionalFiles);

            var language = Language.VBNET;
            var profile = this.ConfigureProfileExport(export, language, QualityProfileName);

            // Act
            var result = await testSubject.GetRulesConfigurationAsync(profile, null, language, CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            result.Should().BeNull();

            this.logger.AssertOutputStrings(1);
            var expectedOutput = string.Format(Strings.SubTextPaddingFormat,
                string.Format(Strings.NoNuGetPackageForQualityProfile, language.Name));
            this.logger.AssertOutputStrings(expectedOutput);
        }

        #region Helpers

        private DotNetRuleConfigurationProvider CreateTestSubject(string projectName = "anyProjectName", string serverUrl = "http://localhost",
            INuGetBindingOperation nuGetBindingOperation = null)
        {
            nuGetBindingOperation = nuGetBindingOperation ?? new NoOpNuGetBindingOperation(this.logger);

            return new DotNetRuleConfigurationProvider(this.sonarQubeServiceMock.Object, nuGetBindingOperation, serverUrl, projectName, this.logger);
        }

        private SonarQubeQualityProfile ConfigureProfileExport(RoslynExportProfileResponse export, Language language, string profileName)
        {
            var profile = new SonarQubeQualityProfile("", profileName, "", false, DateTime.Now);
            this.sonarQubeServiceMock
                .Setup(x => x.GetQualityProfileAsync(It.IsAny<string>(), It.IsAny<string>(), language.ToServerLanguage(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);
            this.sonarQubeServiceMock
                .Setup(x => x.GetRoslynExportProfileAsync(profileName, It.IsAny<string>(), It.IsAny<SonarQubeLanguage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(export);

            return profile;
        }

        private static void VerifyNuGetPackgesDownloaded(IEnumerable<PackageName> expectedPackages,
            Language language,
            IList<Tuple<Language, RoslynExportProfileResponse>> actualProfiles)
        {
            actualProfiles.All(p => p.Item1 == language).Should().BeTrue();

            var actualPackages = actualProfiles.SelectMany(p => p.Item2?.Deployment?.NuGetPackages.Select(
                ngp => new PackageName(ngp.Id, new SemanticVersion(ngp.Version))));

            actualPackages.Should().BeEquivalentTo(expectedPackages);
            actualPackages.Should().HaveSameCount(expectedPackages, "Different number of packages.");
            actualPackages.Select(x => x.ToString()).Should().Equal(expectedPackages.Select(x => x.ToString()));
        }

        #endregion
    }
}
