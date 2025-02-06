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
using Language = SonarLint.VisualStudio.SLCore.Common.Models.Language;

namespace SonarLint.VisualStudio.SLCore.Configuration;

public interface ISLCoreLanguageProvider
{
    IReadOnlyList<Language> LanguagesInStandaloneMode { get; }
    IReadOnlyList<Language> ExtraLanguagesInConnectedMode { get; }
    IReadOnlyList<Language> AllAnalyzableLanguages { get; }
    IReadOnlyList<Language> LanguagesWithDisabledAnalysis { get; }
}

[Export(typeof(ISLCoreLanguageProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class SLCoreLanguageProvider : ISLCoreLanguageProvider
{
    [ImportingConstructor]
    public SLCoreLanguageProvider()
    {
        AllAnalyzableLanguages = LanguagesInStandaloneMode.Concat(ExtraLanguagesInConnectedMode).Except(LanguagesWithDisabledAnalysis).ToArray();
    }

    public IReadOnlyList<Language> LanguagesInStandaloneMode =>
    [
        Language.JS,
        Language.TS,
        Language.HTML,
        Language.CSS,
        Language.C,
        Language.CPP,
        Language.CS,
        Language.VBNET,
        Language.SECRETS,
    ];
    public IReadOnlyList<Language> ExtraLanguagesInConnectedMode =>
    [
        Language.TSQL
    ];

    public IReadOnlyList<Language> LanguagesWithDisabledAnalysis =>
    [
        Language.CS,
        Language.VBNET,
    ];

    public IReadOnlyList<Language> AllAnalyzableLanguages { get; }
}
