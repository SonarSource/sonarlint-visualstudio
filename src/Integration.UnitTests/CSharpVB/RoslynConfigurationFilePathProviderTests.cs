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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.Integration.CSharpVB;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.CSharpVB;

[TestClass]
public class RoslynConfigurationFilePathProviderTests
{
    private RoslynConfigurationFilePathProvider testSubject;

    [TestInitialize]
    public void TestInitialize() =>
        testSubject = new RoslynConfigurationFilePathProvider();

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RoslynConfigurationFilePathProvider, IRoslynConfigurationFilePathProvider>();

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<RoslynConfigurationFilePathProvider>();

    [DynamicData(nameof(RoslynLanguages))]
    [DataTestMethod]
    public void GetSolutionGlobalConfigFilePath(Language roslynLanguage)
    {
        roslynLanguage.SettingsFileNameAndExtension.Should().NotBeNullOrWhiteSpace();

        const string baseDirectory = @"C:\base\directory";

        testSubject.GetSolutionGlobalConfigFilePath(roslynLanguage, baseDirectory).Should().BeEquivalentTo($"C:\\base\\directory\\{roslynLanguage.SettingsFileNameAndExtension}");
    }

    [DynamicData(nameof(RoslynLanguages))]
    [DataTestMethod]
    public void GetSolutionAdditionalFilePath(Language roslynLanguage)
    {
        roslynLanguage.SettingsFileNameAndExtension.Should().NotBeNullOrWhiteSpace();

        const string baseDirectory = @"C:\base\directory";

        testSubject.GetSolutionAdditionalFilePath(roslynLanguage, baseDirectory).Should().BeEquivalentTo($"C:\\base\\directory\\{roslynLanguage.Id}\\SonarLint.xml");
    }

    public static object[][] RoslynLanguages => LanguageProvider.Instance.RoslynLanguages.Select(x => (object[])[x]).ToArray();
}
