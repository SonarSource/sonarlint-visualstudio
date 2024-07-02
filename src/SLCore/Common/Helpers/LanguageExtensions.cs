/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

namespace SonarLint.VisualStudio.SLCore.Common.Helpers;

internal static class LanguageExtensions
{
    public static VisualStudio.Core.Language ConvertToCoreLanguage(this Language language)
    {
        return language switch
        {
            Language.C => VisualStudio.Core.Language.C,
            Language.CPP => VisualStudio.Core.Language.Cpp,
            Language.CS => VisualStudio.Core.Language.CSharp,
            Language.CSS => VisualStudio.Core.Language.Css,
            Language.JS => VisualStudio.Core.Language.Js,
            Language.SECRETS => VisualStudio.Core.Language.Secrets,
            Language.TS => VisualStudio.Core.Language.Ts,
            Language.VBNET => VisualStudio.Core.Language.VBNET,
            _ => VisualStudio.Core.Language.Unknown
        };
    }
}
