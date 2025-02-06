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

using SonarLint.VisualStudio.SLCore.Common.Models;
using static SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.SLCore.Common.Helpers;

internal static class LanguageExtensions
{
    private static readonly Dictionary<VisualStudio.Core.Language, Language> CoreToSlCoreLanguageMap = new()
    {
        { CSharp, Language.CS },
        { VBNET, Language.VBNET },
        { C, Language.C },
        { Cpp, Language.CPP },
        { Css, Language.CSS },
        { Html, Language.HTML },
        { Js, Language.JS },
        { Secrets, Language.SECRETS },
        { Ts, Language.TS },
        { TSql, Language.TSQL },
    };

    public static VisualStudio.Core.Language ConvertToCoreLanguage(this Language language)
    {
        if (CoreToSlCoreLanguageMap.ContainsValue(language))
        {
            return CoreToSlCoreLanguageMap.First(kvp => kvp.Value == language).Key;
        }

        return Unknown;
    }

    public static Language ConvertToSlCoreLanguage(this VisualStudio.Core.Language language)
    {
        if (CoreToSlCoreLanguageMap.TryGetValue(language, out var coreLanguage))
        {
            return coreLanguage;
        }
        throw new ArgumentOutOfRangeException(nameof(language), language, null);
    }

    public static string GetPluginKey(this Language language) => language.ConvertToCoreLanguage().PluginInfo?.Key;
}
