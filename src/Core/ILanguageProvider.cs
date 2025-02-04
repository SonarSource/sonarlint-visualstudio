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

using System.ComponentModel.Composition;

namespace SonarLint.VisualStudio.Core;

public interface ILanguageProvider
{
    IReadOnlyList<Language> AllKnownLanguages { get; }

    IReadOnlyList<Language> SlCoreLanguages { get; }

    IReadOnlyList<Language> RoslynLanguages { get; }

    IReadOnlyList<Language> LanguagesInStandaloneMode { get; }

    IReadOnlyList<Language> ExtraLanguagesInConnectedMode { get; }

    /// <summary>
    /// Returns the language for the specified language key, or null if it does not match a known language
    /// </summary>
    Language GetLanguageFromLanguageKey(string languageKey);
}

[Export(typeof(ILanguageProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class LanguageProvider : ILanguageProvider
{
    [ImportingConstructor]
    public LanguageProvider()
    {
        SlCoreLanguages = AllKnownLanguages.Except(RoslynLanguages).ToList();
        LanguagesInStandaloneMode = AllKnownLanguages.Except(ExtraLanguagesInConnectedMode).ToList();
    }

    public IReadOnlyList<Language> SlCoreLanguages { get; }
    public IReadOnlyList<Language> RoslynLanguages { get; } = [Language.CSharp, Language.VBNET];
    public IReadOnlyList<Language> AllKnownLanguages { get; } =
        [Language.CSharp, Language.VBNET, Language.C, Language.Cpp, Language.Js, Language.Ts, Language.Css, Language.Secrets, Language.Html, Language.TSql];
    public IReadOnlyList<Language> LanguagesInStandaloneMode { get; }
    public IReadOnlyList<Language> ExtraLanguagesInConnectedMode { get; } = [Language.TSql];

    public Language GetLanguageFromLanguageKey(string languageKey) => AllKnownLanguages.FirstOrDefault(l => languageKey.Equals(l.ServerLanguage.Key, StringComparison.OrdinalIgnoreCase));
}
