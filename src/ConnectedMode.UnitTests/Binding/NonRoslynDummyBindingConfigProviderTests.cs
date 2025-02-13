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
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Binding;

[TestClass]
public class NonRoslynDummyBindingConfigProviderTests
{
    private NonRoslynDummyBindingConfigProvider testSubject;
    private ILanguageProvider languageProvider;

    private static IEnumerable<Language> SupportedLanguages { get; } = [Language.Cpp, Language.Js,]; // do not have to be the real lists
    private static IEnumerable<Language> RoslynLanguages { get; } = [Language.CSharp, Language.VBNET];

    public static IEnumerable<object[]> GetSupportedLanguages() => SupportedLanguages.Select(l => new object[] { l });

    public static IEnumerable<object[]> GetRoslynLanguages() => RoslynLanguages.Select(l => new object[] { l });

    public static IEnumerable<object[]> GetLanguagesWithSupport()
    {
        foreach (var nonRoslynLanguage in SupportedLanguages)
        {
            yield return [nonRoslynLanguage, true];
        }

        foreach (var roslynLanguage in RoslynLanguages)
        {
            yield return [roslynLanguage, false];
        }
    }

    [TestInitialize]
    public void TestInitialize()
    {
        languageProvider = Substitute.For<ILanguageProvider>();
        MockLanguageProvider();
        testSubject = new NonRoslynDummyBindingConfigProvider(languageProvider);
    }

    [DynamicData(nameof(GetLanguagesWithSupport), DynamicDataSourceType.Method)]
    [DataTestMethod]
    public void IsLanguageSupported_ReturnsTrueForNonRoslynLanguages(Language language, bool isSupported)
    {
        testSubject.IsLanguageSupported(language).Should().Be(isSupported);

        _ = languageProvider.Received().NonRoslynLanguages;
        _ = languageProvider.DidNotReceive().RoslynLanguages;
    }

    [DynamicData(nameof(GetRoslynLanguages), DynamicDataSourceType.Method)]
    [DataTestMethod]
    public async Task GetConfigurationAsync_ThrowsForUnsupported(Language language)
    {
        var act = () => testSubject.GetConfigurationAsync(default, language, default, default);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [DynamicData(nameof(GetSupportedLanguages), DynamicDataSourceType.Method)]
    [DataTestMethod]
    public async Task GetConfigurationAsync_ReturnsDummyConfigForSupported(Language language)
    {
        var config = await testSubject.GetConfigurationAsync(default, language, default, default);
        config.Should().NotBeNull().And.BeOfType<NonRoslynDummyBindingConfigProvider.DummyConfig>();

        var configSave = () => config.Save();
        configSave.Should().NotThrow();
    }

    private void MockLanguageProvider() => languageProvider.NonRoslynLanguages.Returns(SupportedLanguages);
}
