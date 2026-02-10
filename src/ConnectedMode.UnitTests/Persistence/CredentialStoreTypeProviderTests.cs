/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Persistence;

[TestClass]
public class CredentialStoreTypeProviderTests
{
    private ISonarLintSettings sonarLintSettings;
    private CredentialStoreTypeProvider testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        sonarLintSettings = Substitute.For<ISonarLintSettings>();
        testSubject = new CredentialStoreTypeProvider(sonarLintSettings);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<CredentialStoreTypeProvider, ICredentialStoreTypeProvider>(MefTestHelpers.CreateExport<ISonarLintSettings>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<CredentialStoreTypeProvider>();

    [TestMethod]
    [DataRow(CredentialStoreType.Default)]
    [DataRow(CredentialStoreType.DPAPI)]
    public void CredentialStoreType_ReturnsFromPreferences(CredentialStoreType type)
    {
        sonarLintSettings.CredentialStoreType.Returns(type);

        testSubject.CredentialStoreType.Should().Be(type);
    }
}
