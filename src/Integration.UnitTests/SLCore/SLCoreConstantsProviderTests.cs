/*
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

using System.Reflection;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.VsInfo;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.SLCore;
using SonarLint.VisualStudio.SLCore.Configuration;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.SLCore;

[TestClass]
public class SLCoreConstantsProviderTests
{
    private IVsInfoProvider infoProvider;
    private IUserAgentProvider userAgentProvider;
    private SLCoreConstantsProvider testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        userAgentProvider = Substitute.For<IUserAgentProvider>();
        infoProvider = Substitute.For<IVsInfoProvider>();

        testSubject = new SLCoreConstantsProvider(userAgentProvider, infoProvider);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<SLCoreConstantsProvider, ISLCoreConstantsProvider>(
            MefTestHelpers.CreateExport<IUserAgentProvider>(),
            MefTestHelpers.CreateExport<IVsInfoProvider>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<SLCoreConstantsProvider>();

    [TestMethod]
    public void ClientConstants_ShouldBeExpected()
    {
        const string userAgent = "Any UserAgent";
        userAgentProvider.UserAgent.Returns(userAgent);
        const string ideName = "Any Ide";
        infoProvider.Name.Returns(ideName);
        var expectedClientConstants = new ClientConstantInfoDto(ideName, userAgent);
        var actual = testSubject.ClientConstants;

        actual.Should().BeEquivalentTo(expectedClientConstants);
    }

    [TestMethod]
    public void FeatureFlags_ShouldBeExpected()
    {
        var expectedFeatureFlags = new FeatureFlagsDto(true, true, true, true, true, false, true, true, true);
        var actual = testSubject.FeatureFlags;

        actual.Should().BeEquivalentTo(expectedFeatureFlags);
    }

    [TestMethod]
    public void TelemetryConstants_ShouldBeExpected()
    {
        var version = Substitute.For<IVsVersion>();
        version.DisplayName.Returns("Visual Studio Professional 2022");
        version.InstallationVersion.Returns("17.10.55645.41");
        version.DisplayVersion.Returns("17.10.0 Preview 3.0");
        infoProvider.Version.Returns(version);
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
        infoProvider.Version.Returns((IVsVersion)null);
        VisualStudioHelpers.VisualStudioVersion = "1.2.3.4";

        var actual = testSubject.TelemetryConstants;
        actual.additionalAttributes["slvs_ide_info"].Should().BeNull();
    }
}
