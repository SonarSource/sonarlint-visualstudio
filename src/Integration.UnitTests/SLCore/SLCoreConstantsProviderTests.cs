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

using System.Reflection;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.VsInfo;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.SLCore;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Configuration;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.SLCore;

[TestClass]
public class SLCoreConstantsProviderTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<SLCoreConstantsProvider, ISLCoreConstantsProvider>(
            MefTestHelpers.CreateExport<IVsInfoProvider>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<SLCoreConstantsProvider>();
    }

    [TestMethod]
    public void ClientConstants_ShouldBeExpected()
    {
        const string ideName = "MyIde";
        var infoProvider = Substitute.For<IVsInfoProvider>();
        infoProvider.Name.Returns(ideName);
        var testSubject = CreateTestSubject(infoProvider);
        var expectedClientConstants = new ClientConstantsDto(ideName,
            $"SonarLint Visual Studio/{VersionHelper.SonarLintVersion}",
            Process.GetCurrentProcess().Id);
        var actual = testSubject.ClientConstants;

        actual.Should().BeEquivalentTo(expectedClientConstants);
    }

    [TestMethod]
    public void FeatureFlags_ShouldBeExpected()
    {
        var testSubject = CreateTestSubject();
        var expectedFeatureFlags = new FeatureFlagsDto(true, true, true, true, false, false, true, true);
        var actual = testSubject.FeatureFlags;

        actual.Should().BeEquivalentTo(expectedFeatureFlags);
    }

    [TestMethod]
    public void TelemetryConstants_ShouldBeExpected()
    {
        var infoProvider = Substitute.For<IVsInfoProvider>();
        var version = Substitute.For<IVsVersion>();
        version.DisplayName.Returns("Visual Studio Professional 2022");
        version.InstallationVersion.Returns("17.10.55645.41");
        version.DisplayVersion.Returns("17.10.0 Preview 3.0");
        infoProvider.Version.Returns(version);

        var testSubject = CreateTestSubject(infoProvider: infoProvider);
        VisualStudioHelpers.VisualStudioVersion = "1.2.3.4";
        var expectedString = $$"""
                               {
                                 "productKey": "visualstudio",
                                 "productName": "SonarLint Visual Studio",
                                 "productVersion": "{{typeof(VersionHelper).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version}}",
                                 "ideVersion": "1.2.3.4",
                                 "additionalAttributes": {
                                   "slvs_ide_info": {
                                     "name": "Visual Studio Professional 2022",
                                     "install_version": "17.10.55645.41",
                                     "display_version": "17.10.0 Preview 3.0"
                                   }
                                 }
                               }
                               """;

        var actual = testSubject.TelemetryConstants;

        var serializedString = JsonConvert.SerializeObject(actual, Formatting.Indented);
        serializedString.Should().Be(expectedString);
    }

    [TestMethod]
    public void TelemetryConstants_WhenVsVersionNull_ReturnNullWithoutException()
    {
        var versionProvider = Substitute.For<IVsInfoProvider>();
        versionProvider.Version.Returns((IVsVersion)null);

        var testSubject = CreateTestSubject(infoProvider: versionProvider);
        VisualStudioHelpers.VisualStudioVersion = "1.2.3.4";

        var actual = testSubject.TelemetryConstants;
        actual.additionalAttributes["slvs_ide_info"].Should().BeNull();
    }

    [TestMethod]
    public void StandaloneLanguages_ShouldBeExpected()
    {
        var testSubject = CreateTestSubject();
        var expected = new[]
        {
            Language.JS,
            Language.TS,
            Language.CSS,
            Language.C,
            Language.CPP,
            Language.CS,
            Language.VBNET,
            Language.SECRETS
        };

        var actual = testSubject.LanguagesInStandaloneMode;

        actual.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public void AnalyzableLanguages_ShouldBeExpected()
    {
        var testSubject = CreateTestSubject();
        var expected = new[]
        {
            Language.JS,
            Language.TS,
            Language.CSS,
            Language.SECRETS
        };

        var actual = testSubject.SLCoreAnalyzableLanguages;

        actual.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public void Verify_AllConfiguredLanguagesAreKnown()
    {
        var slCoreConstantsProvider = CreateTestSubject();

        var languages = slCoreConstantsProvider.LanguagesInStandaloneMode
            .Concat(slCoreConstantsProvider.SLCoreAnalyzableLanguages)
            .Select(x => x.ConvertToCoreLanguage());

        languages.Should().NotContain(Core.Language.Unknown);
    }

    [TestMethod]
    public void Verify_AllConfiguredLanguagesHaveKnownPluginKeys()
    {
        var slCoreConstantsProvider = CreateTestSubject();

        var languages = slCoreConstantsProvider.LanguagesInStandaloneMode
            .Concat(slCoreConstantsProvider.SLCoreAnalyzableLanguages)
            .Select(x => x.GetPluginKey());

        languages.Should().NotContainNulls();
    }

    private static SLCoreConstantsProvider CreateTestSubject(IVsInfoProvider infoProvider = null)
    {
        infoProvider ??= Substitute.For<IVsInfoProvider>();

        return new SLCoreConstantsProvider(infoProvider);
    }
}
