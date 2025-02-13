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

using SonarLint.VisualStudio.Core.VsInfo;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests;

[TestClass]
public class UserAgentProviderTests
{
    private IVsInfoProvider vsInfoProvider;

    [TestInitialize]
    public void TestInitialize()
    {
        vsInfoProvider = Substitute.For<IVsInfoProvider>();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<UserAgentProvider, IUserAgentProvider>(
            MefTestHelpers.CreateExport<IVsInfoProvider>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<UserAgentProvider>();

    [TestMethod]
    public void UserAgent_ReturnsExpectedFormat()
    {
        vsInfoProvider.Version.DisplayVersion.Returns("1.2.3.4");
        vsInfoProvider.Version.DisplayName.Returns("Vi Es Pro 2042");

        new UserAgentProvider(vsInfoProvider).UserAgent.Should().Be($"SonarQube for IDE (SonarLint) - Visual Studio {VersionHelper.SonarLintVersion} - Vi Es Pro 2042 1.2.3.4");
    }

    [TestMethod]
    public void UserAgent_VsVersionUnavailable_ReturnsExpectedFormatWithDefaults()
    {
        vsInfoProvider.Version.Returns((IVsVersion)null);

        new UserAgentProvider(vsInfoProvider).UserAgent.Should().Be($"SonarQube for IDE (SonarLint) - Visual Studio {VersionHelper.SonarLintVersion} - VisualStudio version unknown");
    }

    [TestMethod]
    public void UserAgent_ValueIsCached()
    {
        var testSubject = new UserAgentProvider(vsInfoProvider);

        _ = testSubject.UserAgent;
        _ = testSubject.UserAgent;
        _ = testSubject.UserAgent;

        _ = vsInfoProvider.Received(1).Version;
        _ = vsInfoProvider.Version.Received(1).DisplayName;
        _ = vsInfoProvider.Version.Received(1).DisplayVersion;
    }
}
