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
    public static VisualStudio.Core.Language ConvertToCoreLanguage(this Language language) =>
        language switch
        {
            Language.C => C,
            Language.CPP => Cpp,
            Language.CS => CSharp,
            Language.CSS => Css,
            Language.HTML => Html,
            Language.JS => Js,
            Language.SECRETS => Secrets,
            Language.TS => Ts,
            Language.VBNET => VBNET,
            Language.TSQL => TSql,
            _ => Unknown
        };

    public static Language ConvertToSlCoreLanguage(this VisualStudio.Core.Language language)
    {
        if (language.Id == C.Id)
        {
            return Language.C;
        }
        if (language.Id == Cpp.Id)
        {
            return Language.CPP;
        }
        if (language.Id == CSharp.Id)
        {
            return Language.CS;
        }
        if (language.Id == Css.Id)
        {
            return Language.CSS;
        }
        if (language.Id == Html.Id)
        {
            return Language.HTML;
        }
        if (language.Id == Js.Id)
        {
            return Language.JS;
        }
        if (language.Id == Secrets.Id)
        {
            return Language.SECRETS;
        }
        if (language.Id == Ts.Id)
        {
            return Language.TS;
        }
        if (language.Id == VBNET.Id)
        {
            return Language.VBNET;
        }
        if (language.Id == TSql.Id)
        {
            return Language.TSQL;
        }
        throw new ArgumentOutOfRangeException(nameof(language), language, null);
    }

    public static string GetPluginKey(this Language language) => language.ConvertToCoreLanguage().PluginInfo?.Key;
}
