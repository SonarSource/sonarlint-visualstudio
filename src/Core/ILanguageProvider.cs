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

using System.ComponentModel.Composition;

namespace SonarLint.VisualStudio.Core;

public interface ILanguageProvider
{
    IReadOnlyList<Language> AllKnownLanguages { get; }

    IReadOnlyList<Language> NonRoslynLanguages { get; }

    IReadOnlyList<RoslynLanguage> RoslynLanguages { get; }

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
#pragma warning disable S4277
    public static readonly ILanguageProvider Instance = new LanguageProvider();
#pragma warning restore S4277

    [ImportingConstructor]
    public LanguageProvider()
    {
        AllKnownLanguages = NonRoslynLanguages.Union(RoslynLanguages).ToList();
        LanguagesInStandaloneMode = AllKnownLanguages.Except(ExtraLanguagesInConnectedMode).ToList();
    }

    public IReadOnlyList<Language> NonRoslynLanguages { get; } = [Language.C, Language.Cpp, Language.Js, Language.Ts, Language.Css, Language.Secrets, Language.Html, Language.TSql, Language.Text];
    public IReadOnlyList<RoslynLanguage> RoslynLanguages { get; } = [Language.CSharp, Language.VBNET];
    public IReadOnlyList<Language> AllKnownLanguages { get; }
    public IReadOnlyList<Language> LanguagesInStandaloneMode { get; }
    public IReadOnlyList<Language> ExtraLanguagesInConnectedMode { get; } = [Language.TSql, Language.Text];

    public Language GetLanguageFromLanguageKey(string languageKey) => AllKnownLanguages.FirstOrDefault(l => languageKey.Equals(l.ServerLanguageKey, StringComparison.OrdinalIgnoreCase));
}
